import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { Sidebar } from '../Sidebar'
import { TRANSACTIONS_FILTERS_KEY } from '@/hooks/usePersistedFilters'

function renderSidebar(initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <Sidebar />
    </MemoryRouter>,
  )
}

function LocationProbe() {
  const loc = useLocation()
  return <div data-testid="loc">{loc.pathname + loc.search}</div>
}

function renderSidebarWithLocation(initialRoute = '/charts/income') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <Sidebar />
      <LocationProbe />
    </MemoryRouter>,
  )
}

describe('Sidebar', () => {
  it('renders the app brand name', () => {
    renderSidebar()
    expect(screen.getAllByText('MoneyManager').length).toBeGreaterThan(0)
  })

  it('renders top-level nav items', () => {
    renderSidebar()
    expect(screen.getAllByText('Home').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Transactions').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Import').length).toBeGreaterThan(0)
    expect(screen.getAllByText('AI Analysis').length).toBeGreaterThan(0)
  })

  it('renders collapsible group labels', () => {
    renderSidebar()
    expect(screen.getAllByText('Trends').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Settings').length).toBeGreaterThan(0)
  })

  it('shows Trends children by default (defaultOpen)', () => {
    renderSidebar()
    expect(screen.getAllByText('Net Income').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Cumulative Spending').length).toBeGreaterThan(0)
    expect(screen.getAllByText('By Category').length).toBeGreaterThan(0)
  })

  it('toggles Settings group open and closed', async () => {
    const user = userEvent.setup()
    renderSidebar()

    // Settings children not visible by default
    const desktopSidebar = screen.getAllByText('Settings')
    // Find the button (collapsible group toggle) in the desktop sidebar
    const settingsButton = desktopSidebar.find(el => el.closest('button'))!.closest('button')!
    
    // Initially collapsed — children should not be visible
    expect(screen.queryByText('App Settings')).not.toBeInTheDocument()

    // Open Settings group
    await user.click(settingsButton)
    expect(screen.getAllByText('App Settings').length).toBeGreaterThan(0)

    // Close it again
    await user.click(settingsButton)
    expect(screen.queryByText('App Settings')).not.toBeInTheDocument()
  })

  it('highlights active nav item', () => {
    renderSidebar('/transactions')
    const links = screen.getAllByText('Transactions')
    const activeLink = links.find(el => el.closest('a')?.className.includes('bg-indigo-600'))
    expect(activeLink).toBeTruthy()
  })

  it('navigates Transactions to the persisted filter view', async () => {
    const user = userEvent.setup()
    localStorage.setItem(TRANSACTIONS_FILTERS_KEY, 'period=y1&categoryId=5')
    renderSidebarWithLocation()

    await user.click(screen.getAllByText('Transactions')[0].closest('a')!)

    expect(screen.getByTestId('loc').textContent).toBe('/transactions?period=y1&categoryId=5')
  })

  it('navigates Transactions to the bare list when nothing is persisted', async () => {
    const user = userEvent.setup()
    renderSidebarWithLocation()

    await user.click(screen.getAllByText('Transactions')[0].closest('a')!)

    expect(screen.getByTestId('loc').textContent).toBe('/transactions')
  })
})
