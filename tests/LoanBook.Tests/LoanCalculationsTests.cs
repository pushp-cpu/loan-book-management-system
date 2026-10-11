using LoanBook.Api.Entities;
using LoanBook.Api.Services;
using Xunit;

namespace LoanBook.Tests;

public class LoanCalculationsTests
{
    [Fact]
    public void Emi_ZeroInterest_IsPrincipalDividedByMonths()
        => Assert.Equal(100m, LoanCalculations.CalculateEmi(1200m, 0m, 12));

    [Fact]
    public void Emi_WithInterest_IsGreaterThanPrincipalDividedByMonths()
        => Assert.True(LoanCalculations.CalculateEmi(1200m, 12m, 12) > 100m);

    [Fact]
    public void Emi_RejectsZeroPrincipal()
        => Assert.Throws<ArgumentException>(() => LoanCalculations.CalculateEmi(0m, 10m, 12));

    [Fact]
    public void Emi_RejectsNegativeRate()
        => Assert.Throws<ArgumentException>(() => LoanCalculations.CalculateEmi(1000m, -1m, 12));

    [Fact]
    public void Emi_RejectsZeroTenure()
        => Assert.Throws<ArgumentException>(() => LoanCalculations.CalculateEmi(1000m, 10m, 0));

    [Fact]
    public void Schedule_HasRequestedNumberOfInstallments()
    {
        var loan = SampleLoan(1200m, 0m, 12);
        Assert.Equal(12, LoanCalculations.BuildSchedule(loan).Count);
    }

    [Fact]
    public void Schedule_FinalBalanceIsZero()
    {
        var loan = SampleLoan(10000m, 12m, 12);
        var schedule = LoanCalculations.BuildSchedule(loan);
        Assert.Equal(0m, schedule[^1].RemainingBalance);
    }

    [Fact]
    public void Schedule_DueDatesAdvanceMonthly()
    {
        var loan = SampleLoan(1200m, 0m, 3);
        var schedule = LoanCalculations.BuildSchedule(loan);
        Assert.Equal(new DateOnly(2026, 2, 1), schedule[0].DueDate);
        Assert.Equal(new DateOnly(2026, 3, 1), schedule[1].DueDate);
        Assert.Equal(new DateOnly(2026, 4, 1), schedule[2].DueDate);
    }

    [Fact]
    public void Schedule_InterestIsZeroForZeroRate()
    {
        var schedule = LoanCalculations.BuildSchedule(SampleLoan(1200m, 0m, 12));
        Assert.All(schedule, item => Assert.Equal(0m, item.InterestPart));
    }

    [Fact]
    public void Schedule_PrincipalPartsSumToOriginalPrincipal()
    {
        var schedule = LoanCalculations.BuildSchedule(SampleLoan(10000m, 12m, 12));
        Assert.Equal(10000m, schedule.Sum(x => x.PrincipalPart));
    }

    private static Loan SampleLoan(decimal principal, decimal rate, int months)
    {
        var loan = new Loan
        {
            Principal = principal,
            AnnualInterestRate = rate,
            TenureMonths = months,
            StartDate = new DateOnly(2026, 1, 1)
        };
        loan.MonthlyEmi = LoanCalculations.CalculateEmi(principal, rate, months);
        return loan;
    }
}
