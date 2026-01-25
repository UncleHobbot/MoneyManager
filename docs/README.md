# MoneyManager Documentation

This directory contains comprehensive technical documentation for the MoneyManager application.

## Documentation Structure

### [01-Architecture.md](./01-Architecture.md)
**System Architecture and Design Overview**

Topics covered:
- Technology stack and framework choices
- System architecture diagrams
- Design patterns (Service, Repository, DTO, Partial Classes, Component-Based)
- Data flow for transaction import and AI analysis
- Architectural decisions and rationale
- Security and performance considerations
- Deployment model and configuration management
- Monitoring and debugging approaches

**Who should read**: Developers, architects, technical leads

---

### [02-Database-Schema.md](./02-Database-Schema.md)
**Complete Database Documentation**

Topics covered:
- Database overview and technology (SQLite + EF Core 10)
- Entity relationship diagram (ERD)
- Detailed table schemas:
  - Accounts
  - Categories
  - Transactions
  - Rules
  - Balances
- Entity relationships and foreign keys
- EF Core interceptors (query and save)
- Migration strategy
- Indexes and optimization
- Data integrity and transactions
- Backup and restore procedures

**Who should read**: Database administrators, developers, data engineers

---

### [03-Services.md](./03-Services.md)
**Business Logic and Services Documentation**

Topics covered:
- Service architecture and dependency injection
- Complete service documentation:
  - DataService (7 partial classes)
  - TransactionService (core + bank-specific imports)
  - AIService (OpenAI integration)
  - DBService (backup/restore)
  - SettingsService (user preferences)
  - FolderPicker (utility)
- Method signatures and usage examples
- Import formats and processing (Mint, RBC, CIBC)
- AI analysis types and prompts
- Common patterns (async/await, context factory, static caching)
- Error handling and testing considerations

**Who should read**: Backend developers, service maintainers, QA engineers

---

### [04-Blazor-Components.md](./04-Blazor-Components.md)
**UI Components and Pages Documentation**

Topics covered:
- Component architecture and organization
- Complete page documentation:
  - Home (dashboard)
  - Transactions
  - Accounts
  - Categories
  - Rules
  - AI (analysis)
  - Settings
- Reusable components:
  - TransactionsList
  - Spending (charts)
  - NetIncome
  - CumulativeSpending
  - Edit dialogs (Transaction, Account, Rule, Category)
  - CategorySelector
  - ImportFileDialog
- Fluent UI Design System usage
- Dialog, data grid, and form patterns
- Navigation structure
- Styling and theming (dark mode)
- Accessibility features
- Performance optimizations
- Component lifecycle

**Who should read**: Frontend developers, UI/UX designers, full-stack developers

---

### [05-Future-Improvements.md](./05-Future-Improvements.md)
**Roadmap and Enhancement Proposals**

Topics covered:
- High-priority improvements (user impact):
  - Transaction bulk operations
  - Recurring transaction management
  - Enhanced search functionality
  - Transaction tags and notes
  - Export and reporting
- Medium-priority features:
  - Budget management
  - Transaction reconciliation
  - Advanced charting
  - Split transactions
  - Investment tracking
- Low-priority enhancements:
  - Multi-currency support
  - Receipt/document attachment
  - Goal and savings tracking
  - Bill reminders
  - Import wizard
- Architectural improvements:
  - Cross-platform support
  - Cloud synchronization
  - Testing framework
  - API layer
  - CI/CD pipeline
- Performance and security enhancements
- Prioritization framework and implementation timeline

**Who should read**: Product managers, technical leads, developers, stakeholders

---

## Quick Reference

### For New Developers
1. Start with [01-Architecture.md](./01-Architecture.md) to understand the system
2. Review [02-Database-Schema.md](./02-Database-Schema.md) for data structures
3. Check [03-Services.md](./03-Services.md) for business logic patterns
4. Explore [04-Blazor-Components.md](./04-Blazor-Components.md) for UI implementation
5. Reference [05-Future-Improvements.md](./05-Future-Improvements.md) for roadmap

### For Feature Development
1. Identify relevant services in [03-Services.md](./03-Services.md)
2. Check database schema in [02-Database-Schema.md](./02-Database-Schema.md)
3. Review UI patterns in [04-Blazor-Components.md](./04-Blazor-Components.md)
4. Consider impact on future improvements in [05-Future-Improvements.md](./05-Future-Improvements.md)

### For Bug Fixing
1. Understand architecture in [01-Architecture.md](./01-Architecture.md)
2. Locate service methods in [03-Services.md](./03-Services.md)
3. Check component lifecycle in [04-Blazor-Components.md](./04-Blazor-Components.md)
4. Review data model in [02-Database-Schema.md](./02-Database-Schema.md)

### For Planning and Roadmap
1. Review all improvements in [05-Future-Improvements.md](./05-Future-Improvements.md)
2. Consider architectural constraints in [01-Architecture.md](./01-Architecture.md)
3. Evaluate complexity based on [03-Services.md](./03-Services.md) and [04-Blazor-Components.md](./04-Blazor-Components.md)

## Key Concepts

### Technology Stack
- **.NET 10.0-windows** - Platform framework
- **Blazor Hybrid** - UI framework (WebAssembly in WebView)
- **Windows Forms** - Application shell
- **Entity Framework Core 10** - ORM and data access
- **SQLite** - Database engine
- **Fluent UI Blazor** - UI component library
- **ApexCharts** - Charting library
- **OpenAI API** - AI-powered insights
- **Serilog** - Structured logging

### Design Patterns
- **Service Pattern** - Business logic encapsulation
- **Repository Pattern** - Data access abstraction
- **DTO Pattern** - Data transfer objects
- **Partial Classes** - Service organization
- **Component-Based Architecture** - Reusable UI

### Core Services
- **DataService** - CRUD operations (7 partial classes)
- **TransactionService** - Bank imports (Mint, RBC, CIBC)
- **AIService** - OpenAI integration and analysis
- **DBService** - Database backup/restore
- **SettingsService** - User preferences

### Data Models
- **Account** - Financial accounts with alternative names
- **Category** - Hierarchical categories (22 icons)
- **Transaction** - Financial transactions with rules
- **Rule** - Auto-categorization rules (4 match types)
- **Balance** - Historical balance snapshots

### UI Components
- **Pages** - Main application views
- **Dialogs** - Edit forms for entities
- **Data Grids** - List displays with filtering
- **Charts** - Data visualization components
- **Selectors** - Dropdown components for entities

## Conventions

### Code Style
- PascalCase for classes, methods, properties, constants
- _camelCase for private fields
- I-prefixed interfaces
- Async methods end with `Async` suffix
- 4-space indentation
- Opening braces on new line

### File Organization
- `/Data/` - EF Core models and DbContext
- `/Services/` - Business logic (partial classes)
- `/Pages/` - Blazor pages (views)
- `/Components/` - Reusable Blazor components
- `/Model/` - DTOs and enums
- `/Helpers/` - Utility classes
- `/docs/` - Documentation

### Naming Conventions
- Services: `EntityService` (e.g., `TransactionService`)
- Pages: `Entity.razor` (e.g., `Transactions.razor`)
- Dialogs: `EditEntityDialog.razor` (e.g., `EditAccountDialog.razor`)
- Partial classes: `Service.Feature.cs` (e.g., `DataService.Account.cs`)

## Common Tasks

### Add New Feature
1. Review architecture [01-Architecture.md](./01-Architecture.md)
2. Check existing patterns [03-Services.md](./03-Services.md), [04-Blazor-Components.md](./04-Blazor-Components.md)
3. Update data model if needed [02-Database-Schema.md](./02-Database-Schema.md)
4. Create EF Core migration
5. Implement service methods
6. Create/update UI components
7. Test and document

### Fix Bug
1. Reproduce issue
2. Locate relevant service/component
3. Add diagnostic logging if needed
4. Implement fix
5. Add tests if applicable
6. Update documentation

### Performance Tuning
1. Identify bottleneck (profiling, EF Core logging)
2. Review query patterns [03-Services.md](./03-Services.md)
3. Add indexes [02-Database-Schema.md](./02-Database-Schema.md)
4. Implement caching
5. Optimize component rendering [04-Blazor-Components.md](./04-Blazor-Components.md)

## Contributing

When contributing to MoneyManager:

1. **Read the Documentation**: Understand architecture and patterns before making changes
2. **Follow Conventions**: Adhere to established coding style and file organization
3. **Add Tests**: Create unit and integration tests for new features
4. **Update Docs**: Document changes in relevant documentation files
5. **Check Future Improvements**: Ensure changes don't conflict with planned features

## Support and Feedback

For questions or feedback on documentation:

1. Check relevant documentation sections first
2. Review code examples and patterns
3. Consult the future improvements roadmap
4. Consider adding clarifications if documentation is unclear

## Documentation Updates

This documentation should be updated when:

- New features are added
- Architecture changes occur
- Database schema is modified
- New services or components are created
- Patterns or conventions evolve
- Bug fixes introduce new concepts

---

**Last Updated**: January 2025

**Maintained by**: MoneyManager Development Team

**Version**: 1.0.0
