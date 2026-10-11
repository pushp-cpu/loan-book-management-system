using LoanBook.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanBook.Api.Data;

public class LoanBookDbContext(DbContextOptions<LoanBookDbContext> options) : DbContext(options)
{
    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<ScheduleItem> ScheduleItems => Set<ScheduleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Borrower>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Borrower>().Property(x => x.FullName).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Borrower>().Property(x => x.Email).HasMaxLength(254).IsRequired();

        modelBuilder.Entity<Loan>().Property(x => x.Principal).HasPrecision(18, 2);
        modelBuilder.Entity<Loan>().Property(x => x.AnnualInterestRate).HasPrecision(7, 4);
        modelBuilder.Entity<Loan>().Property(x => x.PenaltyAnnualRate).HasPrecision(7, 4);
        modelBuilder.Entity<Loan>().Property(x => x.MonthlyEmi).HasPrecision(18, 2);
        modelBuilder.Entity<Loan>().HasOne(x => x.Borrower).WithMany(x => x.Loans)
            .HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduleItem>().HasIndex(x => new { x.LoanId, x.InstallmentNumber }).IsUnique();
        modelBuilder.Entity<ScheduleItem>().Property(x => x.Emi).HasPrecision(18, 2);
        modelBuilder.Entity<ScheduleItem>().Property(x => x.PrincipalPart).HasPrecision(18, 2);
        modelBuilder.Entity<ScheduleItem>().Property(x => x.InterestPart).HasPrecision(18, 2);
        modelBuilder.Entity<ScheduleItem>().Property(x => x.RemainingBalance).HasPrecision(18, 2);
        modelBuilder.Entity<ScheduleItem>().Property(x => x.AmountPaid).HasPrecision(18, 2);
        modelBuilder.Entity<ScheduleItem>().HasOne(x => x.Loan).WithMany(x => x.Schedule)
            .HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().HasIndex(x => new { x.LoanId, x.Reference }).IsUnique();
        modelBuilder.Entity<Payment>().HasOne(x => x.Loan).WithMany(x => x.Payments)
            .HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentAllocation>().Property(x => x.AmountAllocated).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentAllocation>().Property(x => x.PrincipalAllocated).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentAllocation>().Property(x => x.InterestAllocated).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentAllocation>().HasOne(x => x.Payment).WithMany(x => x.Allocations)
            .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PaymentAllocation>().HasOne(x => x.ScheduleItem).WithMany(x => x.Allocations)
            .HasForeignKey(x => x.ScheduleItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
