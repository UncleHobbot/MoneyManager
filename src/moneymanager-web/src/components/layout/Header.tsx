import { useLocation } from 'react-router-dom'
import { Moon, Sun } from 'lucide-react'
import { useTheme } from './useTheme'

const routeTitles: Record<string, string> = {
  '/': 'Dashboard',
  '/transactions': 'Transactions',
  '/charts/income': 'Net Income',
  '/charts/cumulative': 'Cumulative Spending',
  '/charts/spending': 'Spending by Category',
  '/ai': 'AI Analysis',
  '/settings': 'Settings',
  '/settings/accounts': 'Accounts',
  '/settings/categories': 'Categories',
  '/settings/rules': 'Rules',
}

export function Header() {
  const { pathname } = useLocation()
  const { theme, toggleTheme } = useTheme()
  const title = routeTitles[pathname] ?? 'MoneyManager'

  return (
    <header className="flex items-center justify-between h-14 px-6 border-b border-gray-200 dark:border-white/5 bg-white dark:bg-gray-900 shrink-0">
      {/* Left spacer on mobile for hamburger button */}
      <div className="w-10 lg:hidden" />

      <h1 className="text-lg font-semibold text-gray-900 dark:text-white">{title}</h1>

      <button
        onClick={toggleTheme}
        className="p-2 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors"
        aria-label="Toggle theme"
      >
        {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
      </button>
    </header>
  )
}
