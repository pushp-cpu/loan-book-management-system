using System.ComponentModel.DataAnnotations;

namespace LoanBook.Api.Contracts;

public record CreateBorrowerRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [StringLength(30)] string? Phone);

public record CreateLoanRequest(
    [Required] Guid BorrowerId,
    [Range(typeof(decimal), "0.01", "1000000000")] decimal Principal,
    [Range(typeof(decimal), "0", "100")] decimal AnnualInterestRate,
    [Range(1, 600)] int TenureMonths,
    DateOnly StartDate,
    [Range(typeof(decimal), "0", "100")] decimal PenaltyAnnualRate = 0m);

public record CreatePaymentRequest(
    [Required] Guid LoanId,
    [Range(typeof(decimal), "0.01", "1000000000")] decimal Amount,
    [StringLength(100)] string? Reference);

public record PaymentAllocationResult(
    int InstallmentNumber, decimal AmountAllocated, decimal InstallmentOutstanding, string Status);

public record PaymentResult(
    Guid Id, Guid LoanId, decimal Amount, DateTime PaymentDateUtc, string? Reference,
    decimal AmountAllocated, decimal UnallocatedAmount, List<PaymentAllocationResult> Allocations);
