import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Receipt,
  TrendingUp,
  Bot,
  Settings,
  ChevronDown,
  Wallet,
  DollarSign,
  PieChart,
  Landmark,
  Tags,
  BookOpen,
  Upload,
  Menu,
  X,
} from 'lucide-react'
import type { ReactNode } from 'react'

interface NavItemProps {
  to: string
  icon: ReactNode
  label: string
  onClick?: () => void
}

function NavItem({ to, icon, label, onClick }: NavItemProps) {
  return (
    <NavLink
      to={to}
      end
      onClick={onClick}
      className={({ isActive }) =>
        `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
          isActive
            ? 'bg-indigo-600 text-white'
            : 'text-gray-400 hover:text-white hover:bg-white/5'
        }`
      }
    >
      {icon}
      <span>{label}</span>
    </NavLink>
  )
}

interface CollapsibleGroupProps {
  icon: ReactNode
  label: string
  children: ReactNode
  defaultOpen?: boolean
}

function CollapsibleGroup({ icon, label, children, defaultOpen = false }: CollapsibleGroupProps) {
  const [open, setOpen] = useState(defaultOpen)

  return (
    <div>
      <button
        onClick={() => setOpen(prev => !prev)}
        className="flex items-center gap-3 px-3 py-2 w-full rounded-lg text-sm font-medium text-gray-400 hover:text-white hover:bg-white/5 transition-colors"
      >
        {icon}
        <span className="flex-1 text-left">{label}</span>
        <ChevronDown
          size={16}
          className={`transition-transform duration-200 ${open ? 'rotate-0' : '-rotate-90'}`}
        />
      </button>
      {open && <div className="ml-4 mt-1 flex flex-col gap-0.5">{children}</div>}
    </div>
  )
}

export function Sidebar() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const closeMobile = () => setMobileOpen(false)

  const navContent = (
    <>
      {/* Logo */}
      <div className="flex items-center gap-3 px-4 py-5 mb-2">
        <Wallet className="text-indigo-400" size={28} />
        <span className="text-lg font-bold text-white tracking-tight">MoneyManager</span>
      </div>

      {/* Navigation */}
      <nav className="flex flex-col gap-0.5 px-3 flex-1">
        <NavItem to="/" icon={<LayoutDashboard size={18} />} label="Home" onClick={closeMobile} />
        <NavItem to="/transactions" icon={<Receipt size={18} />} label="Transactions" onClick={closeMobile} />

        <CollapsibleGroup icon={<TrendingUp size={18} />} label="Trends" defaultOpen>
          <NavItem to="/charts/income" icon={<DollarSign size={16} />} label="Net Income" onClick={closeMobile} />
          <NavItem to="/charts/cumulative" icon={<TrendingUp size={16} />} label="Cumulative Spending" onClick={closeMobile} />
          <NavItem to="/charts/spending" icon={<PieChart size={16} />} label="By Category" onClick={closeMobile} />
        </CollapsibleGroup>

        <NavItem to="/import" icon={<Upload size={18} />} label="Import" onClick={closeMobile} />
        <NavItem to="/ai" icon={<Bot size={18} />} label="AI Analysis" onClick={closeMobile} />

        <CollapsibleGroup icon={<Settings size={18} />} label="Settings">
          <NavItem to="/settings" icon={<Settings size={16} />} label="App Settings" onClick={closeMobile} />
          <NavItem to="/settings/accounts" icon={<Landmark size={16} />} label="Accounts" onClick={closeMobile} />
          <NavItem to="/settings/categories" icon={<Tags size={16} />} label="Categories" onClick={closeMobile} />
          <NavItem to="/settings/rules" icon={<BookOpen size={16} />} label="Rules" onClick={closeMobile} />
        </CollapsibleGroup>
      </nav>
    </>
  )

  return (
    <>
      {/* Mobile toggle */}
      <button
        onClick={() => setMobileOpen(true)}
        className="fixed top-3 left-3 z-50 p-2 rounded-lg bg-gray-800 text-gray-300 lg:hidden"
        aria-label="Open navigation"
      >
        <Menu size={20} />
      </button>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={closeMobile}
        />
      )}

      {/* Mobile sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-50 w-64 flex flex-col bg-[#1a1a2e] border-r border-white/5 transition-transform duration-200 lg:hidden ${
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <button
          onClick={closeMobile}
          className="absolute top-4 right-3 text-gray-400 hover:text-white"
          aria-label="Close navigation"
        >
          <X size={20} />
        </button>
        {navContent}
      </aside>

      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:flex-col lg:w-64 lg:shrink-0 bg-[#1a1a2e] border-r border-white/5">
        {navContent}
      </aside>
    </>
  )
}
