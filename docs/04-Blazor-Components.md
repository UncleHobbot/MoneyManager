# Blazor Components Structure

## Overview

The Blazor UI layer is built with Microsoft Fluent UI components and organized into pages and reusable components. The application uses Blazor Hybrid (Windows Forms host) to provide a native desktop experience with modern web UI technologies.

## Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Main.razor (Layout)                      │
│                 [Navigation + Content Area]                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │  NavMenu    │  │   Pages/    │  │ Components/ │
    │  .razor     │  │   .razor    │  │   .razor    │
    │  (Sidebar)  │  │  (Views)    │  │  (Reusable) │
    └─────────────┘  └──────┬──────┘  └──────┬──────┘
                           │                 │
                           └────────┬────────┘
                                    │
                                    ▼
                            ┌───────────────┐
                            │   Services/    │
                            │  (Injection)   │
                            └───────────────┘
```

## Pages (`/Pages/`)

### Home.razor

**Route**: `/`

**Purpose**: Dashboard and landing page for the application.

**Features**:
- Transaction file import (RBC, CIBC, Mint.com)
- Account creation during import toggle
- Quick actions (Manage accounts, Apply rules, AI analysis)
- Navigation cards to main sections

**Key Components Used**:
- `<FluentGrid>` and `<FluentGridItem>` for layout
- `<FluentButton>` with icons for actions
- `<FluentSwitch>` for toggles
- Icon integration (custom bank logos)

**Code Structure**:
```razor
@page "/"
@inject TransactionService TransactionService
@inject DataService DataService

<FluentGrid Spacing="2">
    <FluentGridItem xs="4">
        <FluentCard Height="400px">
            <!-- Import section -->
        </FluentCard>
    </FluentGridItem>
</FluentGrid>

@code {
    private bool isCreateAccounts = true;
    private async Task ImportFileRBC() { }
}
```

**State Management**:
- Import file path tracking
- Account creation flag
- Import statistics display

---

### Transactions.razor

**Route**: `/transactions`

**Purpose**: View and manage all financial transactions.

**Features**:
- Full transaction list with filtering
- Filter by Account, Category, Date Range, Description
- Edit transaction details
- Delete transactions
- Apply categorization rules
- Export to CSV

**Key Components Used**:
- `<FluentDataGrid>` for transaction display
- `<FluentDialog>` for edit dialogs
- `<FluentButton>` for actions
- `<FluentAnchor>` for navigation

**Injected Services**:
```csharp
@inject DataService DataService
@inject TransactionService TransactionService
```

**Filtering Logic**:
- Multiple filter criteria with AND logic
- Clear all filters button
- Filter announcements for accessibility

**Data Grid Features**:
- Pagination
- Sorting
- Responsive layout
- Custom cell templates

---

### Accounts.razor

**Route**: `/accounts`

**Purpose**: Manage bank and financial accounts.

**Features**:
- List all accounts with type icons
- Add new account
- Edit account details
- Delete account
- Alternative name management

**Key Components Used**:
- `<FluentDataGrid>` for account list
- `<FluentButton>` for CRUD operations
- `<FluentIcon>` for account type display
- `<FluentCheckbox>` for hide from graph toggle

**Account Types** (Icons):
- Cash (MoneyHand icon)
- Credit Card (Payment icon)
- Investment (ArrowTrendingLines icon)
- Other (CurrencyDollarEuro icon)

**Injected Services**:
```csharp
@inject DataService DataService
@inject SettingsService SettingsService
```

---

### Categories.razor

**Route**: `/categories`

**Purpose**: Manage hierarchical category structure.

**Features**:
- View all categories with icons
- Add parent/child categories
- Edit category details
- Delete category
- Icon selection (22 types)
- Parent-child relationship management

**Key Components Used**:
- Tree-like display (hierarchical)
- `<FluentDialog>` for add/edit
- Icon selector dropdown
- Parent category dropdown

**Category Hierarchy**:
```
Food
├── Groceries
├── Restaurants
└── Other Food
```

**Injected Services**:
```csharp
@inject DataService DataService
```

---

### CategoriesS.razor

**Route**: `/categoriess`

**Purpose**: Alternative category management view (possibly simplified).

**Features**:
- Similar to Categories.razor
- Custom CSS scope (`custom-scope-identifier`)
- Different layout/presentation

**Special Features**:
- Scoped CSS via `CategoriesS.razor.css`
- DependentUpon in csproj for file grouping

---

### Rules.razor

**Route**: `/rules`

**Purpose**: Manage auto-categorization rules.

**Features**:
- List all rules with match types
- Add new rule
- Edit rule
- Delete rule
- Test rule application

**Rule Match Types**:
- Contains
- StartsWith
- EndsWith
- Equals

**Key Components Used**:
- `<FluentDataGrid>` for rule list
- `<FluentSelect>` for match type
- `<FluentTextField>` for pattern input
- Category selector dropdown

**Rule Structure**:
```csharp
{
    OriginalDescription: "NETFLIX",
    NewDescription: "Netflix Subscription",
    CompareType: 3,  // Equals
    Category: "Entertainment"
}
```

**Injected Services**:
```csharp
@inject DataService DataService
```

---

### AI.razor

**Route**: `/ai`

**Purpose**: AI-powered financial analysis and insights.

**Features**:
- Select analysis period (m1, y1, 12, w, a)
- Choose analysis type:
  - SpendingGeneral: Top categories and habits
  - SpendingBudget: 50/30/20 budget
  - SpendingTrends: Month-over-month comparison
- Display bilingual results (English/Russian)
- Token usage tracking

**Key Components Used**:
- `<FluentSelect>` for period/type selection
- `<FluentButton>` for analysis trigger
- `<FluentProgressRing>` for loading state
- Markdown rendering (via Markdig)

**Injected Services**:
```csharp
@inject AIService AIService
```

**Display Format**:
```markdown
**Summary:**
[Bilingual summary]

**Result:**
[Detailed analysis]

**Action Plan:**
[Actionable steps]

**Tips & Habits:**
[Practical advice]
```

---

### Settings.razor

**Route**: `/settings`

**Purpose**: Application configuration and preferences.

**Features**:
- Dark mode toggle
- Backup path configuration
- Database backup/restore
- Clear all transactions
- Reset database

**Key Components Used**:
- `<FluentSwitch>` for dark mode
- `<FluentButton>` for actions
- `<FluentAnchor>` for file paths
- Confirmation dialogs

**Injected Services**:
```csharp
@inject SettingsService SettingsService
@inject DBService DBService
```

**Settings Stored**:
```json
{
  "IsDarkMode": true,
  "BackupPath": "C:\\Backups"
}
```

---

## Reusable Components (`/Components/`)

### TransactionsList.razor

**Purpose**: Reusable transaction list with filtering and editing.

**Parameters**:
```csharp
[Parameter] public TransactionListModeEnum Mode { get; set; } = TransactionListModeEnum.Full;
[Parameter] public EventCallback<Transaction> OnTransactionChanged { get; set; }
```

**Modes**:
- `Full`: Complete filtering and editing
- `Short`: Simplified view, minimal filtering

**Features**:
- Account filter dropdown
- Category filter dropdown
- Date range picker
- Description search
- Clear all filters
- Edit transaction dialog
- Delete transaction

**Accessibility**:
- Screen reader announcements
- ARIA labels
- Keyboard navigation

**CSS Scoping**:
- Custom styles for filter options
- Responsive layout
- Min-height constraints

---

### Spending.razor

**Purpose**: Display spending breakdown by category.

**Features**:
- ApexCharts integration
- Category spending visualization
- Period selector
- Income vs expense comparison

**Key Components**:
- `<ApexChart>` (Blazor-ApexCharts)
- Custom chart options
- Responsive sizing

**Injected Services**:
```csharp
@inject DataService DataService
```

---

### NetIncome.razor

**Purpose**: Display net income over time (income vs expenses).

**Features**:
- Monthly trend chart
- Income line chart
- Expense bar chart
- Period selector (12, y1, y2, etc.)

**Chart Type**:
- Mixed chart (line + bar)
- Dual axes (income on one, expenses on other)
- Hover tooltips

**Injected Services**:
```csharp
@inject DataService DataService
```

---

### CumulativeSpending.razor

**Purpose**: Compare current month spending vs last month, day by day.

**Features**:
- 31-day comparison chart
- Two line series (current vs last month)
- Daily spending tracking
- Month-to-date totals

**Chart Type**:
- Line chart with two series
- X-axis: Day 1-31
- Y-axis: Cumulative amount

**Data Logic**:
```csharp
var dayValue = new CumulativeSpendingChart
{
    DayNumber = day,
    LastMonthExpenses = ...,
    ThisMonthExpenses = ...
};
```

**Injected Services**:
```csharp
@inject DataService DataService
```

---

### EditTransactionDialog.razor

**Purpose**: Modal dialog for editing transaction details.

**Parameters**:
```csharp
[Parameter] public Transaction Transaction { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Fields**:
- Date picker
- Description text field
- Amount number field
- Debit/Credit toggle
- Account selector
- Category selector

**Key Components**:
- `<FluentDialog>` container
- `<FluentDatePicker>`
- `<FluentTextField>`
- `<FluentNumberField>`
- `<FluentSwitch>`
- `<FluentSelect>` (CategorySelector)

---

### EditAccountDialog.razor

**Purpose**: Modal dialog for editing account details.

**Parameters**:
```csharp
[Parameter] public Account Account { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Fields**:
- Name
- Shown Name (display name)
- Description
- Type (dropdown)
- Number
- Alternative Names (1-5)
- Hide from Graph (checkbox)

**Key Components**:
- `<FluentDialog>` container
- `<FluentTextField>`
- `<FluentSelect>` (type)
- `<FluentCheckbox>`

---

### EditRuleDialog.razor

**Purpose**: Modal dialog for editing auto-categorization rules.

**Parameters**:
```csharp
[Parameter] public Rule Rule { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Fields**:
- Original Description (pattern)
- New Description (replacement)
- Compare Type (dropdown)
- Category (dropdown)

**Key Components**:
- `<FluentDialog>` container
- `<FluentTextField>`
- `<FluentSelect>` (compare type, category)

---

### EditCategoryDialog.razor

**Purpose**: Modal dialog for editing category details.

**Parameters**:
```csharp
[Parameter] public Category Category { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Fields**:
- Name
- Parent (dropdown)
- Icon (dropdown with 22 options)

**Key Components**:
- `<FluentDialog>` container
- `<FluentTextField>`
- `<FluentSelect>` (parent, icon)

---

### NewCategoryDialog.razor

**Purpose**: Modal dialog for creating new categories (simplified).

**Similar to**: EditCategoryDialog.razor

**Differences**:
- New category creation mode
- Defaults for new entries
- Validation for required fields

---

### CategorySelector.razor

**Purpose**: Dropdown component for category selection with hierarchy.

**Features**:
- Flattened category tree for dropdown
- Parent category display
- Search/filter capability
- Icon display

**Parameters**:
```csharp
[Parameter] public Category? SelectedCategory { get; set; }
[Parameter] public EventCallback<Category> SelectedCategoryChanged { get; set; }
```

**Data Structure**:
```csharp
public class CategoryDropItem(Category c, string parentId)
{
    public int Id { get; }
    public string Name { get; }
    public string ParentCategory { get; set; }
}
```

---

### ImportFileDialog.razor

**Purpose**: Modal dialog for selecting and previewing import files.

**Features**:
- File picker dialog
- File type selection
- Preview of data
- Import options
- Progress indication

**Key Components**:
- `<FluentDialog>` container
- `<FluentAnchor>` for file selection
- `<FluentSelect>` for file type
- Preview data grid

---

## Shared Components and Patterns

### Fluent UI Design System

The application uses `Microsoft.FluentUI.AspNetCore.Components` throughout:

**Common Components**:
- `<FluentButton>` - Buttons with various appearances
- `<FluentDialog>` - Modal dialogs
- `<FluentDataGrid>` - Data tables with sorting/pagination
- `<FluentTextField>` - Text input fields
- `<FluentNumberField>` - Numeric input
- `<FluentDatePicker>` - Date selection
- `<FluentSelect>` - Dropdown selection
- `<FluentSwitch>` - Toggle switches
- `<FluentCheckbox>` - Checkboxes
- `<FluentCard>` - Card containers
- `<FluentGrid>` and `<FluentGridItem>` - Layout system
- `<FluentIcon>` - Icons
- `<FluentProgressRing>` - Loading indicators
- `<FluentAnchor>` - Hyperlinks

**Appearance Variants**:
- `Appearance.Accent` - Primary action buttons
- `Appearance.Stealth` - Secondary actions
- `Appearance.Outline` - Bordered buttons
- `Appearance.Filled` - Solid fill

**Density**:
```razor
<FluentDesignSystemProvider Density="-1">
    <!-- All components -1 density (more compact) -->
</FluentDesignSystemProvider>
```

---

### Dialog Pattern

All edit dialogs follow the same pattern:

```razor
<FluentDialog @bind-Open="@isOpen" Title="@title">
    <DialogContent>
        <FluentTextField @bind-Value="@item.Name" Label="Name" />
        <!-- Other fields -->
    </DialogContent>
    <DialogFooter>
        <FluentButton OnClick="@OnCancel">Cancel</FluentButton>
        <FluentButton Appearance="Appearance.Accent" OnClick="@OnSave">Save</FluentButton>
    </DialogFooter>
</FluentDialog>

@code {
    private bool isOpen = false;
    [Parameter] public Item Item { get; set; }
    // ...
}
```

---

### Data Grid Pattern

Standard data grid implementation:

```razor
<FluentDataGrid Items="@items" TGridItem="@ItemType">
    <PropertyColumn Property="@(x => x.Name)" Sortable="true">
        Name
    </PropertyColumn>
    <TemplateColumn Title="Actions">
        <FluentButton IconStart="@(new Icons.Regular.Size16.Edit())"
                      OnClick="@(() => EditItem(context.Item))">
            Edit
        </FluentButton>
    </TemplateColumn>
</FluentDataGrid>

@code {
    private IQueryable<ItemType> items;
    // ...
}
```

---

### Form Validation Pattern

Server-side validation in dialogs:

```csharp
private async Task HandleSave()
{
    if (string.IsNullOrWhiteSpace(item.Name))
    {
        // Show error message
        return;
    }

    await DataService.UpdateItem(item);
    await OnSave.InvokeAsync();
    isOpen = false;
}
```

---

### Service Injection Pattern

All components inject required services:

```razor
@page "/route"
@inject DataService DataService
@inject SettingsService SettingsService

@code {
    protected override async Task OnInitializedAsync()
    {
        items = await DataService.GetItems();
    }
}
```

---

## Navigation Structure

### Main.razor (Layout)

```razor
<FluentLayout>
    <FluentSidebar>
        <NavMenu />
    </FluentSidebar>
    <FluentBodyContent>
        @Body
    </FluentBodyContent>
</FluentLayout>
```

### NavMenu.razor

Navigation links:
- Home (/)
- Transactions (/transactions)
- Accounts (/accounts)
- Categories (/categories)
- Rules (/rules)
- AI Analysis (/ai)
- Settings (/settings)

**Current Page Highlighting**:
```razor
<FluentAnchor Href="/" class="@NavLinkClass("/", "/")">
    Home
</FluentAnchor>

@code {
    private string NavLinkClass(string href, string currentUrl)
    {
        return href == currentUrl ? "nav-item-selected" : "nav-item";
    }
}
```

---

## Styling and Theming

### Dark Mode

Applied globally via `<FluentDesignSystemProvider>`:

```razor
<FluentDesignSystemProvider Mode="@currentMode">
    @* Components *@
</FluentDesignSystemProvider>

@code {
    private DesignMode currentMode = DesignMode.Dark;
}
```

**User Preference**:
```csharp
var settings = await SettingsService.LoadSettings();
currentMode = settings.IsDarkMode ? DesignMode.Dark : DesignMode.Light;
```

### CSS Scoping

Scoped CSS for specific components:

```razor
@* Components/CategoriesS.razor *@
<style>
    .custom-scope-identifier .my-class {
        /* Scoped styles */
    }
</style>
```

**csproj Configuration**:
```xml
<None Update="Pages\CategoriesS.razor.css" CssScope="custom-scope-identifier">
    <DependentUpon>CategoriesS.razor</DependentUpon>
</None>
```

---

## Accessibility

### Screen Reader Support

- `aria-label` attributes on interactive elements
- Filter announcements for screen readers:
```razor
@if (!string.IsNullOrWhiteSpace(filterAnnouncement))
{
    <div class="visually-hidden">@filterAnnouncement</div>
}
```

### Keyboard Navigation

- All interactive elements accessible via keyboard
- Tab order follows logical flow
- Enter/Space for button activation

### High Contrast Support

- Fluent UI components support high contrast mode
- Custom icons maintain contrast

---

## Performance Optimizations

### Virtualization

Large datasets use virtual scrolling (planned):
```razor
<FluentVirtualize Items="@largeList">
    <ItemContent Context="item">
        <!-- Item template -->
    </ItemContent>
</FluentVirtualize>
```

### Pagination

Data grids implement pagination:
```razor
<FluentDataGrid Items="@items" Pagination="@pagination">
</FluentDataGrid>
```

### Lazy Loading

Charts and large lists loaded on demand:
```razor
@* Load data when component becomes visible *@
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await LoadData();
    }
}
```

---

## Error Handling

### Try-Catch in Event Handlers

```razor
private async Task HandleAction()
{
    try
    {
        await DataService.SomeOperation();
    }
    catch (Exception ex)
    {
        // Show error dialog or toast
        errorMessage = ex.Message;
    }
}
```

### Loading States

```razor
@if (isLoading)
{
    <FluentProgressRing />
}
else
{
    @* Content *@
}
```

---

## Component Lifecycle

### OnInitializedAsync
- Service calls to load initial data
- Set default values
- Initialize state

### OnParametersSetAsync
- Handle parameter changes
- React to parent component updates

### OnAfterRenderAsync
- JavaScript interop calls (if needed)
- DOM manipulations (if needed)
- Load deferred data

---

## Future Component Enhancements

1. **Toast Notifications**: Success/error messages
2. **Confirmation Dialogs**: For destructive actions
3. **File Upload Component**: Drag-and-drop CSV import
4. **Chart Component Library**: Reusable chart wrappers
5. **Date Range Picker Component**: Custom range selection
6. **Rich Text Editor**: For notes/memos
7. **Export Dialog**: Format selection (PDF, Excel, CSV)
8. **Search Component**: Global search across entities
9. **Bulk Edit Component**: Multi-select and edit
10. **Dashboard Widgets**: Customizable home screen
