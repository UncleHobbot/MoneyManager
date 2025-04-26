
# MoneyManager Project Structure

## Overview
MoneyManager is a financial management application built with .NET 9.0, utilizing Blazor for UI components within a Windows Forms application. The application appears to be designed for tracking financial transactions, managing accounts, categories, and financial rules.

## Technology Stack
- **.NET 9.0** (Windows target)
- **Blazor** for UI components
- **Windows Forms** as the application container
- **Entity Framework Core** with SQLite for data persistence
- **FluentUI** for modern UI components
- **Serilog** for logging
- **CsvHelper** for CSV file processing
- **ApexCharts** for data visualization

## Project Structure

### Main Components
- **MainForm** - The primary Windows Forms container that hosts the Blazor UI
- **Program.cs** - Application entry point

### Services
The application follows a service-based architecture with specialized services:

- **DataService** - Core data management service with specialized implementations:
  - `DataService.Account.cs` - Account management
  - `DataService.Category.cs` - Category management
  - `DataService.Transaction.cs` - Transaction management
  - `DataService.Rule.cs` - Rules management
  - `DataService.Chart.cs` - Chart data management

- **TransactionService** - Handles transaction operations with bank-specific implementations:
  - `TransactionService.Mint.cs` - Support for Mint transactions
  - `TransactionService.RBC.cs` - Support for RBC bank transactions
  - `TransactionService.CIBC.cs` - Support for CIBC bank transactions

### Models
The application contains various data models, including:
- Database context models
- Settings models
- Financial data models like CumulativeSpending

### Pages/UI Components
- Blazor pages for different sections of the application
- Custom CSS styling for specific components (e.g., Categories page)

### Database
- SQLite database with Entity Framework Core
- Migration support via EF Core Design and Tools

## Dependencies
The project uses several key packages:
- Microsoft.AspNetCore.Components.WebView.WindowsForms
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.FluentUI.AspNetCore.Components
- Blazor-ApexCharts
- CsvHelper
- Serilog
- WindowsAPICodePack-Shell

## Project Features
- Account management
- Transaction tracking and importing from various banks
- Categorization of financial transactions
- Financial rules management
- Data visualization and reporting
- CSV import/export capabilities