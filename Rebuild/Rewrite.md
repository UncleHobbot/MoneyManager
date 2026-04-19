I want to re-write existing MoneyManager as web-application.

## Architecture
- Backend: ASP.NET 10. Reuse as much logic from Services as possible
- Frontend: React. Existing Blazor UI must gone
- Database: SQLite @Data/MoneyManager.db
- Depoyable as Docker image. Database must be preseved when application image is updated. Docker container will be deployed to Synology NAS.
- For internal use only. No authrization/authentification.

## Requirements

### Main screen
 Side menu, ability to import source files from banks, latest transactions, unparsed transactions, dashboard. Use @Rebuild/Main_Screen.png for ideas
 
### System
 - File import: one button to upload csv file. Analyze it's structure and call dedicated parser (ImportCIBCCSV, ImportRBCCSV). Banks list should be extendable later if needed. Then rename file to a pattern "YYYY-MM-DD {Bank}".csv and add to an CSV archive. 
 - Database backup: manual by request, automatic before and after file import. Be able to see existing backups and restore it. Feature to clean up old backups.
 - CSV Archive: view list, view files in table format, re-import specific files
 - AI Keys management. Ability to store multiple keys for different providers (OpenAI, Z.ai, etc)
 
### Objects manager
 - Account manager: CRUD operations, don't allow to delete account if there are transactions related to the Account
 - Categories management: CRUD operations, parent-child relationship, create descriptive icons for parent Categories
 - Rules managemtn: CRUD operations, statistics how many time the rule is used

### List of transactions
 - Columns: Date, Account, Amount, Category, Description, Is Rule Applied
 - Sort and filter by any column. Be able to filter by simple clicking on Account, Category, Description in table
 - Statistics for filtered data: $ Income, $ Expenses, $ Net, Quantity

### Charts and analytics
 - All charts must be filtered by date period (see existing code in @Pages/Charts, possible options are in DataService.GetDates). Filtering occures on backend side always.
 - Simplified versions are shown on starting dashboard.
 - Cummulative spending: compare cummulative spending by days to previous month (@Rebuild/Cumulative_spending.png)
 - Net income: Income vs Expenses by months (@Rebuild/Net_Income.png). Monthes are clickable and must lead to "Spenging by category" chart
 - Spenging by category: show income and expenses by category (@Rebuild/Spending_by_category.png)
 
### AI analytics
 - see AIService for current implementation. Offer improvements. Don't store API keys in source code
 - Ability to download list of transaction for filtered period for analysis in external tools (Claude)
 
Offer improvements and new features based on other Personal Finance Managers



