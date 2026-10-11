namespace LoanBook.Api.Entities;

public class Borrower
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Loan> Loans { get; set; } = [];
}

public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BorrowerId { get; set; }
    public decimal Principal { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TenureMonths { get; set; }
    public decimal MonthlyEmi { get; set; }
    public DateOnly StartDate { get; set; }
    public decimal PenaltyAnnualRate { get; set; } = 0m;
    public string Status { get; set; } = "Active";
    public Borrower? Borrower { get; set; }
    public List<ScheduleItem> Schedule { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public class ScheduleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Emi { get; set; }
    public decimal PrincipalPart { get; set; }
    public decimal InterestPart { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "Pending";
    public Loan? Loan { get; set; }
    public List<PaymentAllocation> Allocations { get; set; } = [];
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public Loan? Loan { get; set; }
    public List<PaymentAllocation> Allocations { get; set; } = [];
}

public class PaymentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public Guid ScheduleItemId { get; set; }
    public decimal AmountAllocated { get; set; }
    public decimal PrincipalAllocated { get; set; }
    public decimal InterestAllocated { get; set; }
    public Payment? Payment { get; set; }
    public ScheduleItem? ScheduleItem { get; set; }
}
