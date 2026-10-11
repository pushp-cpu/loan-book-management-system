using LoanBook.Api.Contracts;
using LoanBook.Api.Data;
using LoanBook.Api.Entities;
using LoanBook.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LoanBook")
    ?? throw new InvalidOperationException("Connection string 'LoanBook' is missing.");

builder.Services.AddDbContext<LoanBookDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { name = "Loan Book Management API", docs = "/swagger" }));

app.MapGet("/api/borrowers", async (LoanBookDbContext db) =>
    Results.Ok(await db.Borrowers.AsNoTracking().OrderBy(b => b.FullName).ToListAsync()));

app.MapGet("/api/borrowers/{id:guid}", async (Guid id, LoanBookDbContext db) =>
{
    var borrower = await db.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    return borrower is null ? Results.NotFound(new { error = "Borrower not found." }) : Results.Ok(borrower);
});

app.MapPost("/api/borrowers", async (CreateBorrowerRequest request, LoanBookDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new { error = "Full name and email are required." });

    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Borrowers.AnyAsync(b => b.Email == email))
        return Results.Conflict(new { error = "A borrower with this email already exists." });

    var borrower = new Borrower
    {
        FullName = request.FullName.Trim(),
        Email = email,
        Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
    };
    db.Borrowers.Add(borrower);
    await db.SaveChangesAsync();
    return Results.Created($"/api/borrowers/{borrower.Id}", borrower);
});

app.MapGet("/api/loans", async (LoanBookDbContext db) =>
    Results.Ok(await db.Loans.AsNoTracking().Include(l => l.Borrower)
        .OrderByDescending(l => l.StartDate).ToListAsync()));

app.MapPost("/api/loans", async (CreateLoanRequest request, LoanBookDbContext db) =>
{
    if (request.BorrowerId == Guid.Empty || request.Principal <= 0 ||
        request.AnnualInterestRate is < 0 or > 100 ||
        request.TenureMonths is < 1 or > 600 ||
        request.PenaltyAnnualRate is < 0 or > 100)
        return Results.BadRequest(new { error = "Loan terms are outside the allowed range." });

    if (!await db.Borrowers.AnyAsync(b => b.Id == request.BorrowerId))
        return Results.NotFound(new { error = "Borrower not found." });

    var loan = new Loan
    {
        BorrowerId = request.BorrowerId,
        Principal = request.Principal,
        AnnualInterestRate = request.AnnualInterestRate,
        TenureMonths = request.TenureMonths,
        StartDate = request.StartDate,
        PenaltyAnnualRate = request.PenaltyAnnualRate,
        MonthlyEmi = LoanCalculations.CalculateEmi(
            request.Principal, request.AnnualInterestRate, request.TenureMonths)
    };

    loan.Schedule = LoanCalculations.BuildSchedule(loan);
    db.Loans.Add(loan);
    await db.SaveChangesAsync();

    return Results.Created($"/api/loans/{loan.Id}", new
    {
        loan.Id, loan.BorrowerId, loan.Principal, loan.AnnualInterestRate,
        loan.TenureMonths, loan.MonthlyEmi, loan.StartDate, loan.PenaltyAnnualRate,
        Schedule = loan.Schedule
    });
});

app.MapGet("/api/loans/{id:guid}", async (Guid id, LoanBookDbContext db) =>
{
    var loan = await db.Loans.AsNoTracking().Include(l => l.Borrower)
        .Include(l => l.Schedule.OrderBy(s => s.InstallmentNumber))
        .FirstOrDefaultAsync(l => l.Id == id);
    return loan is null ? Results.NotFound(new { error = "Loan not found." }) : Results.Ok(loan);
});

app.MapGet("/api/loans/overdue", async (LoanBookDbContext db) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var overdue = await db.ScheduleItems.AsNoTracking()
        .Where(s => s.DueDate < today && s.AmountPaid < s.Emi)
        .Include(s => s.Loan).ThenInclude(l => l!.Borrower)
        .OrderBy(s => s.DueDate)
        .Select(s => new
        {
            s.LoanId,
            Borrower = s.Loan!.Borrower!.FullName,
            s.InstallmentNumber,
            s.DueDate,
            s.Emi,
            s.AmountPaid,
            Outstanding = s.Emi - s.AmountPaid,
            DaysOverdue = today.DayNumber - s.DueDate.DayNumber,
            s.Loan!.PenaltyAnnualRate
        }).ToListAsync();
    return Results.Ok(overdue);
});

app.MapPost("/api/payments", async (CreatePaymentRequest request, LoanBookDbContext db) =>
{
    if (request.LoanId == Guid.Empty || request.Amount <= 0)
        return Results.BadRequest(new { error = "A valid loan ID and positive amount are required." });

    var reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
    if (reference is not null)
    {
        var existing = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.LoanId == request.LoanId && p.Reference == reference);
        if (existing is not null)
        {
            if (existing.Amount != request.Amount)
                return Results.Conflict(new { error = "This reference was already used with a different amount." });

            var existingAllocations = await db.PaymentAllocations.AsNoTracking()
                .Where(a => a.PaymentId == existing.Id)
                .Select(a => new PaymentAllocationResult(
                    a.ScheduleItem!.InstallmentNumber,
                    a.AmountAllocated,
                    Math.Max(0m, a.ScheduleItem.Emi - a.ScheduleItem.AmountPaid),
                    a.ScheduleItem.Status))
                .ToListAsync();

            return Results.Ok(new PaymentResult(existing.Id, existing.LoanId, existing.Amount,
                existing.PaymentDateUtc, existing.Reference, existingAllocations.Sum(a => a.AmountAllocated),
                existing.Amount - existingAllocations.Sum(a => a.AmountAllocated), existingAllocations));
        }
    }

    await using var tx = await db.Database.BeginTransactionAsync();
    var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId);
    if (loan is null)
        return Results.NotFound(new { error = "Loan not found." });

    var installments = await db.ScheduleItems.Where(s => s.LoanId == loan.Id)
        .OrderBy(s => s.DueDate).ThenBy(s => s.InstallmentNumber).ToListAsync();

    var payment = new Payment { LoanId = loan.Id, Amount = request.Amount, Reference = reference };
    decimal remaining = request.Amount;
    var responseAllocations = new List<PaymentAllocationResult>();

    foreach (var item in installments)
    {
        var outstanding = Math.Max(0m, item.Emi - item.AmountPaid);
        if (outstanding == 0m || remaining == 0m) continue;

        var allocated = Math.Min(outstanding, remaining);
        var previouslyAllocatedInterest = await db.PaymentAllocations
            .Where(a => a.ScheduleItemId == item.Id)
            .SumAsync(a => (decimal?)a.InterestAllocated) ?? 0m;
        var interestOutstanding = Math.Max(0m, item.InterestPart - previouslyAllocatedInterest);
        var interestAllocated = Math.Min(allocated, interestOutstanding);

        item.AmountPaid += allocated;
        item.Status = item.AmountPaid >= item.Emi ? "Paid" : "PartiallyPaid";
        remaining -= allocated;

        db.PaymentAllocations.Add(new PaymentAllocation
        {
            PaymentId = payment.Id,
            ScheduleItemId = item.Id,
            AmountAllocated = allocated,
            InterestAllocated = interestAllocated,
            PrincipalAllocated = allocated - interestAllocated
        });

        responseAllocations.Add(new PaymentAllocationResult(item.InstallmentNumber, allocated,
            Math.Max(0m, item.Emi - item.AmountPaid), item.Status));
    }

    db.Payments.Add(payment);
    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return Results.Created($"/api/payments/{payment.Id}", new PaymentResult(
        payment.Id, payment.LoanId, payment.Amount, payment.PaymentDateUtc, payment.Reference,
        request.Amount - remaining, remaining, responseAllocations));
});

app.MapGet("/api/payments", async (LoanBookDbContext db) =>
    Results.Ok(await db.Payments.AsNoTracking().OrderByDescending(p => p.PaymentDateUtc).ToListAsync()));

app.MapGet("/api/portfolio/summary", async (LoanBookDbContext db) =>
{
    var loans = await db.Loans.AsNoTracking().ToListAsync();
    var payments = await db.Payments.AsNoTracking().SumAsync(p => (decimal?)p.Amount) ?? 0m;
    var outstanding = await db.ScheduleItems.AsNoTracking()
        .SumAsync(s => (decimal?)(s.Emi - s.AmountPaid)) ?? 0m;

    return Results.Ok(new
    {
        BorrowerCount = await db.Borrowers.CountAsync(),
        LoanCount = loans.Count,
        ActiveLoanCount = loans.Count(l => l.Status == "Active"),
        TotalPrincipalIssued = loans.Sum(l => l.Principal),
        TotalPaymentsReceived = payments,
        TotalScheduledOutstanding = Math.Max(0m, outstanding)
    });
});

app.Run();

public partial class Program { }
