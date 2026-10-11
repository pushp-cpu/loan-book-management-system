using LoanBook.Api.Entities;

namespace LoanBook.Api.Services;

public static class LoanCalculations
{
    public static decimal CalculateEmi(decimal principal, decimal annualRate, int months)
    {
        if (principal <= 0 || annualRate < 0 || annualRate > 100 || months <= 0)
            throw new ArgumentException("Principal and tenure must be positive; annual rate must be between 0 and 100.");

        decimal monthlyRate = annualRate / 100m / 12m;
        if (monthlyRate == 0)
            return Math.Round(principal / months, 2, MidpointRounding.AwayFromZero);

        double r = (double)monthlyRate;
        double factor = Math.Pow(1 + r, months);
        return Math.Round(principal * (decimal)(r * factor / (factor - 1)), 2,
            MidpointRounding.AwayFromZero);
    }

    public static List<ScheduleItem> BuildSchedule(Loan loan)
    {
        var result = new List<ScheduleItem>();
        decimal balance = loan.Principal;
        decimal monthlyRate = loan.AnnualInterestRate / 100m / 12m;

        for (int month = 1; month <= loan.TenureMonths; month++)
        {
            decimal interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            decimal principalPart = Math.Min(balance, loan.MonthlyEmi - interest);
            if (month == loan.TenureMonths) principalPart = balance;
            decimal emi = principalPart + interest;
            balance = Math.Max(0m, balance - principalPart);

            result.Add(new ScheduleItem
            {
                LoanId = loan.Id,
                InstallmentNumber = month,
                DueDate = loan.StartDate.AddMonths(month),
                Emi = emi,
                PrincipalPart = principalPart,
                InterestPart = interest,
                RemainingBalance = balance
            });
        }

        return result;
    }
}
