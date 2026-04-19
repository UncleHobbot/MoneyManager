import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { Sidebar } from '../Sidebar'

function renderSidebar(initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <Sidebar />
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
})
