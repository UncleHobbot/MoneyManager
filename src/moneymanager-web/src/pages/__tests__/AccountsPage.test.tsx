import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { AccountsPage } from '@/pages/AccountsPage'
import type { Account } from '@/types'

vi.mock('react-apexcharts', () => ({ default: () => null }))

const mockAccounts: Account[] = [
  {
    id: 1,
    name: 'Checking',
    shownName: 'My Checking',
    description: 'Primary checking',
    type: 0,
    number: '1111',
    isHideFromGraph: false,
    alternativeName1: null,
    alternativeName2: null,
    alternativeName3: null,
    alternativeName4: null,
    alternativeName5: null,
    typeIconName: 'cash',
  },
  {
    id: 2,
    name: 'Savings',
    shownName: 'Savings Account',
    description: null,
    type: 0,
    number: '2222',
    isHideFromGraph: true,
    alternativeName1: null,
    alternativeName2: null,
    alternativeName3: null,
    alternativeName4: null,
    alternativeName5: null,
    typeIconName: 'cash',
  },
]

const server = setupServer(
  http.get('/api/accounts', () => {
    return HttpResponse.json(mockAccounts)
  }),
)

beforeAll(() => server.listen())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AccountsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('AccountsPage', () => {
  it('renders the page heading', async () => {
    renderPage()
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /accounts/i })).toBeInTheDocument()
    })
  })

  it('renders account data in the table', async () => {
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('My Checking')).toBeInTheDocument()
    })
    expect(screen.getByText('Savings Account')).toBeInTheDocument()
  })

  it('shows the Add Account button', async () => {
    renderPage()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add account/i })).toBeInTheDocument()
    })
  })

  it('opens the add dialog when Add Account is clicked', async () => {
    const user = userEvent.setup()
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add account/i })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /add account/i }))
    // Dialog title is an h2 — the button also has "Add Account" text
    expect(screen.getByRole('heading', { level: 2, name: /add account/i })).toBeInTheDocument()
    expect(screen.getByLabelText('Shown Name')).toBeInTheDocument()
  })

  it('displays Hidden badge for hidden accounts', async () => {
    renderPage()
    await waitFor(() => {
      // The Badge component renders a <span> with rounded-full class
      const badges = screen.getAllByText('Hidden')
      const badge = badges.find(el => el.className.includes('rounded-full'))
      expect(badge).toBeTruthy()
    })
  })
})
