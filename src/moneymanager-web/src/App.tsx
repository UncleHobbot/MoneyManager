import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@/components/layout/ThemeProvider'
import { AppLayout } from '@/components/layout/AppLayout'

const ImportPage = lazy(() => import('@/pages/ImportPage'))
import CategoriesPage from '@/pages/CategoriesPage'
import RulesPage from '@/pages/RulesPage'
import { AccountsPage } from '@/pages/AccountsPage'
import SettingsPage from '@/pages/SettingsPage'
import NetIncomePage from '@/pages/NetIncomePage'
import CumulativeSpendingPage from '@/pages/CumulativeSpendingPage'
import SpendingByCategoryPage from '@/pages/SpendingByCategoryPage'
import MonthDetailPage from '@/pages/MonthDetailPage'
import DashboardPage from '@/pages/DashboardPage'
import TransactionsPage from '@/pages/TransactionsPage'
import AIAnalysisPage from '@/pages/AIAnalysisPage'

const queryClient = new QueryClient()

function LoadingFallback() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-semibold dark:text-white">Loading...</h1>
    </div>
  )
}

function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route element={<AppLayout />}>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/transactions" element={<TransactionsPage />} />
              <Route path="/charts/income" element={<NetIncomePage />} />
              <Route path="/charts/cumulative" element={<CumulativeSpendingPage />} />
              <Route path="/charts/spending" element={<SpendingByCategoryPage />} />
              <Route path="/charts/month/:month" element={<MonthDetailPage />} />
              <Route path="/ai" element={<AIAnalysisPage />} />
              <Route path="/settings" element={<SettingsPage />} />
              <Route path="/settings/accounts" element={<AccountsPage />} />
              <Route path="/settings/categories" element={<CategoriesPage />} />
              <Route path="/settings/rules" element={<RulesPage />} />
              <Route path="/import" element={<Suspense fallback={<LoadingFallback />}><ImportPage /></Suspense>} />
            </Route>
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
