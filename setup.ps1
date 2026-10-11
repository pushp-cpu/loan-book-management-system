$ErrorActionPreference = "Stop"

Write-Host "== Loan Book Management System setup ==" -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
}
$version = dotnet --version
Write-Host "Detected .NET SDK: $version"

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host "Starting PostgreSQL with Docker Compose..."
    docker compose up -d postgres
    if ($LASTEXITCODE -ne 0) { throw "Docker Compose failed. Install/start Docker Desktop or configure PostgreSQL manually." }
} else {
    Write-Host "Docker was not found. Continuing assuming PostgreSQL is already running on localhost:5432."
}

Write-Host "Restoring NuGet packages..."
dotnet restore LoanBook.sln
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Host "Installing local EF CLI tool if needed..."
$toolList = dotnet tool list --global
if ($toolList -notmatch "dotnet-ef") {
    dotnet tool install --global dotnet-ef --version 8.0.31
    if ($LASTEXITCODE -ne 0) { throw "Could not install dotnet-ef." }
} else {
    dotnet tool update --global dotnet-ef --version 8.0.31
    if ($LASTEXITCODE -ne 0) { throw "Could not update dotnet-ef." }
}
$ef = Join-Path $env:USERPROFILE ".dotnet\tools\dotnet-ef.exe"
if (-not (Test-Path $ef)) { $ef = "dotnet-ef" }

Write-Host "Creating migration..."
& $ef migrations add InitialCreate --project src/LoanBook.Api --startup-project src/LoanBook.Api
if ($LASTEXITCODE -ne 0) { throw "Migration generation failed. Check whether migrations already exist." }

Write-Host "Applying migration..."
& $ef database update --project src/LoanBook.Api --startup-project src/LoanBook.Api
if ($LASTEXITCODE -ne 0) { throw "Database update failed. Check PostgreSQL connection settings in appsettings.json." }

Write-Host "Running tests..."
dotnet test LoanBook.sln
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

Write-Host "Setup complete. Starting API; open /swagger at the printed localhost URL." -ForegroundColor Green
dotnet run --project src/LoanBook.Api
