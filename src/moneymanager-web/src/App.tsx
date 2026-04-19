import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@/components/layout/ThemeProvider'
import { AppLayout } from '@/components/layout/AppLayout'

const queryClient = new QueryClient()

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-semibold dark:text-white">{title}</h1>
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
              <Route path="/" element={<PlaceholderPage title="Dashboard" />} />
              <Route path="/transactions" element={<PlaceholderPage title="Transactions" />} />
              <Route path="/charts/income" element={<PlaceholderPage title="Net Income" />} />
              <Route path="/charts/cumulative" element={<PlaceholderPage title="Cumulative Spending" />} />
              <Route path="/charts/spending" element={<PlaceholderPage title="Spending by Category" />} />
              <Route path="/charts/month/:month" element={<PlaceholderPage title="Monthly Detail" />} />
              <Route path="/ai" element={<PlaceholderPage title="AI Analysis" />} />
              <Route path="/settings" element={<PlaceholderPage title="Settings" />} />
              <Route path="/settings/accounts" element={<PlaceholderPage title="Accounts" />} />
              <Route path="/settings/categories" element={<PlaceholderPage title="Categories" />} />
              <Route path="/settings/rules" element={<PlaceholderPage title="Rules" />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
