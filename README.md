# Loan Book Management System

A beginner-friendly backend API for tracking borrowers, loans, amortization schedules, payments, overdue installments, and portfolio totals.

## Tech stack
- C# and ASP.NET Core 8 Minimal API
- Entity Framework Core 8
- PostgreSQL
- Swagger / OpenAPI
- xUnit
- Docker Compose (optional)

## Prerequisites
- .NET 8 SDK
- PostgreSQL 15+ (or Docker Desktop)

## Run with Docker PostgreSQL
From the project root:

```powershell
docker compose up -d postgres
dotnet restore LoanBook.sln
dotnet tool install --global dotnet-ef --version 8.0.31
dotnet ef migrations add InitialCreate --project src/LoanBook.Api --startup-project src/LoanBook.Api
dotnet ef database update --project src/LoanBook.Api --startup-project src/LoanBook.Api
dotnet run --project src/LoanBook.Api
```

Open the URL shown in the terminal followed by `/swagger`.

The default local development connection string is in `src/LoanBook.Api/appsettings.json`. It is for local development only; change credentials for your own environment and never commit real passwords.

## API endpoints
- `GET /api/borrowers` — list borrowers
- `GET /api/borrowers/{id}` — get borrower
- `POST /api/borrowers` — create borrower
- `GET /api/loans` — list loans
- `GET /api/loans/{id}` — get a loan and schedule
- `POST /api/loans` — create loan and schedule
- `GET /api/loans/overdue` — list overdue installments
- `GET /api/payments` — list payments
- `POST /api/payments` — record a payment
- `GET /api/portfolio/summary` — portfolio totals

## Example requests

Create borrower:

```json
{
  "fullName": "Asha Patel",
  "email": "asha@example.com",
  "phone": "9000000000"
}
```

Create loan (use the borrower's returned ID):

```json
{
  "borrowerId": "REPLACE-WITH-BORROWER-GUID",
  "principal": 100000,
  "annualInterestRate": 12,
  "tenureMonths": 12,
  "startDate": "2026-01-01",
  "penaltyAnnualRate": 2
}
```

Record payment (use the loan ID):

```json
{
  "loanId": "REPLACE-WITH-LOAN-GUID",
  "amount": 10000,
  "reference": "BANK-TXN-0001"
}
```

## EMI formula
For monthly interest rate `r` and tenure `n` months:

`EMI = P × r × (1+r)^n / ((1+r)^n - 1)`

where `P` is principal and `r = annual interest rate / 12 / 100`. If the rate is zero, EMI is principal divided by tenure. The final scheduled installment is adjusted to clear rounding residue.

## Payment allocation
Payments are allocated to the earliest due installment with an outstanding balance. Amounts beyond the total schedule balance are returned as unallocated; they are not silently assigned to an installment.

## Tests
```powershell
dotnet test LoanBook.sln
```

The included unit tests cover EMI input validation and amortization schedule behavior.

## Important limitations
This is a learning/demo project, not a production lending platform. It does not integrate with a bank or payment gateway, calculate legally compliant penalty charges, provide authentication/authorization, or handle every concurrent-payment race. The penalty annual rate is stored and exposed for overdue reporting but is not yet charged to borrowers. Review these limitations before using real financial data.

## Interview preparation
1. Why use EF Core?
2. What does a DbContext do?
3. Why use decimal for money instead of double?
4. How is EMI calculated?
5. What is an amortization schedule?
6. How are partial payments allocated?
7. Why are database unique constraints important?
8. What is a database migration?
9. What is the difference between 400, 404, and 409 HTTP responses?
10. What would you add before production use?
