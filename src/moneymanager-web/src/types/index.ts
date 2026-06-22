export interface Account {
  id: number
  name: string
  shownName: string
  description: string | null
  type: number
  number: string | null
  isHideFromGraph: boolean
  alternativeName1: string | null
  alternativeName2: string | null
  alternativeName3: string | null
  alternativeName4: string | null
  alternativeName5: string | null
  typeIconName: string
}

export interface Category {
  id: number
  parent: Category | null
  name: string
  icon: string | null
  isNew: boolean
  pIcon: string | null
}

export interface CategoryTree {
  id: number
  name: string
  icon: string | null
  children: CategoryTree[]
  parent: CategoryTree | null
}

export interface Transaction {
  id: number
  account: Account
  date: string
  description: string
  originalDescription: string
  amount: number
  amountExt: number
  isDebit: boolean
  category: Category | null
  isRuleApplied: boolean
}

export interface TransactionDto {
  id: number
  transaction: Transaction
  account: Account
  date: string
  description: string
  originalDescription: string
  amount: number
  amountExt: number
  isDebit: boolean
  category: Category | null
  isRuleApplied: boolean
}

export interface Rule {
  id: number
  originalDescription: string
  newDescription: string
  compareType: number
  compareTypeString: string
  category: Category
}

export interface BalanceChart {
  month: string
  monthLabel: string
  monthKey: string
  firstDate: string
  income: number
  expenses: number
}

export interface CumulativeSpendingChart {
  dayNumber: number
  lastMonthExpenses: number
  thisMonthExpenses: number
}

export interface CategoryChart {
  name: string
  icon: string | null
  amount: number
  percentage: number
}

export interface SpendingTrendMonth {
  label: string
  from: string
  to: string
}

export interface SpendingTrendSeries {
  categoryId: number | null
  name: string
  icon: string | null
  data: number[]
}

export interface SpendingTrendChart {
  months: SpendingTrendMonth[]
  series: SpendingTrendSeries[]
}

export interface MerchantSpend {
  name: string
  amount: number
  count: number
}

export interface SankeyNode {
  name: string
  categoryId: number | null
  kind: 'income' | 'hub' | 'expense' | 'uncategorized' | 'other' | 'savings' | 'deficit'
}

export interface SankeyLink {
  source: string
  target: string
  value: number
}

export interface CashFlowChart {
  nodes: SankeyNode[]
  links: SankeyLink[]
}

export interface BudgetDto {
  id: number
  categoryId: number
  categoryName: string
  icon: string | null
  amount: number
}

export interface BudgetVsActual {
  categoryId: number
  name: string
  icon: string | null
  budget: number
  actual: number
}

export interface ImportResult {
  importedCount: number
  skippedCount: number
  totalCount: number
  bankType: string
  errors: string[]
}

export interface AiProvider {
  id: number
  name: string
  providerType: string
  encryptedApiKey: string
  apiUrl: string
  model: string
  isDefault: boolean
  createdAt: string
}

export interface AnalysisResult {
  isSuccess: boolean
  content: string
  errorMessage: string | null
  tokensUsed: number
  model: string
}

export interface SettingsModel {
  isDarkMode: boolean
  backupPath: string | null
}

export interface BackupInfo {
  fileName: string
  createdAt: string
  sizeBytes: number
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface TransactionStats {
  income: number
  expenses: number
  net: number
  count: number
}

export interface ChartPeriod {
  code: string
  label: string
}

export interface AnalysisType {
  type: string
  name: string
  group: string
  description: string
}

export interface UpdateTransactionRequest {
  description?: string
  categoryId?: number
}

export interface CreateTransactionRequest {
  accountId: number
  date: string
  description: string
  amount: number
  isDebit: boolean
  categoryId?: number
}

export interface AiProviderRequest {
  name: string
  providerType: string
  apiKey: string
  apiUrl: string
  model: string
  isDefault: boolean
}

export interface AnalysisRequest {
  period: string
  analysisType: string
  providerId?: number
}
