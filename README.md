# Currency Converter

A .NET 10 WPF desktop application that converts amounts using rates stored in a SQL Server LocalDB database. Rates are maintained manually in the Currency Master tab; they are not live exchange rates.

## Features

- Convert between currencies stored in the database.
- Add, edit, and delete currency rates in the Currency Master tab.
- Use EF Core for database access.

## Requirements

- Windows
- .NET 10 SDK
- SQL Server Express LocalDB
- Visual Studio 2026 with the .NET desktop development workload (optional)

## Database configuration

The application uses `|DataDirectory|\Database\CurrencyConverter.mdf` from the `CurrencyConverter` connection string in `appsettings.json`. At startup, `DataDirectory` is set to the executable directory, and the project copies the MDF and log file into its `Database` subfolder for builds and publishes. No machine-specific path update is needed. LocalDB must be installed on the machine running the application.

LocalDB writes to the database files, so run the application from a writable folder. An installation under a protected folder such as `Program Files` may require storing the database in a user-writable location instead.

The existing database must contain the `Currency_Master` table with these columns:

| Column | SQL Server type | Purpose |
| --- | --- | --- |
| `Id` | `int IDENTITY` primary key | Record identifier |
| `Amount` | `float` | Currency rate; must be positive for conversion |
| `CurrencyName` | `nvarchar(50)` | Currency name |

The application maps this existing table; it does not create or migrate the database schema. The Currency Master tab requires a non-empty currency name and a positive amount. The converter dropdowns include records with a name and a positive rate.

## Build and run

From the project folder, restore dependencies and build:

```powershell
dotnet restore .\CurrencyConverter_DB.csproj
dotnet build .\CurrencyConverter_DB.csproj
```

Run the application with:

```powershell
dotnet run --project .\CurrencyConverter_DB.csproj
```

Alternatively, open `CurrencyConverter_DB.slnx` in Visual Studio, select the WPF project as the startup project, and press **F5**.

## Verify

1. Start the application and confirm the Currency Master grid loads.
2. Add a currency with a positive rate and confirm it appears in the grid and converter dropdowns.
3. Convert an amount and verify the result uses `amount × from rate ÷ to rate`.
4. Edit or delete the currency and confirm the grid and dropdowns refresh.
