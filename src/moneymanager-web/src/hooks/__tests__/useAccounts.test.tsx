import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { useAccounts } from '../useAccounts'
import type { Account } from '@/types'
import type { ReactNode } from 'react'

const mockAccounts: Account[] = [
  {
    id: 1,
    name: 'Checking',
    shownName: 'Checking Account',
    description: 'Main checking',
    type: 0,
    number: '1234',
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
    name: 'Visa',
    shownName: 'Visa Card',
    description: null,
    type: 1,
    number: '5678',
    isHideFromGraph: false,
    alternativeName1: null,
    alternativeName2: null,
    alternativeName3: null,
    alternativeName4: null,
    alternativeName5: null,
    typeIconName: 'credit',
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

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
}

describe('useAccounts', () => {
  it('fetches accounts successfully', async () => {
    const { result } = renderHook(() => useAccounts(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toHaveLength(2)
    expect(result.current.data![0].shownName).toBe('Checking Account')
    expect(result.current.data![1].shownName).toBe('Visa Card')
  })

  it('starts in loading state', () => {
    const { result } = renderHook(() => useAccounts(), {
      wrapper: createWrapper(),
    })
    expect(result.current.isLoading).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('handles server error', async () => {
    server.use(
      http.get('/api/accounts', () => {
        return new HttpResponse(null, { status: 500 })
      }),
    )

    const { result } = renderHook(() => useAccounts(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(result.current.data).toBeUndefined()
  })

  it('returns empty array from empty response', async () => {
    server.use(
      http.get('/api/accounts', () => {
        return HttpResponse.json([])
      }),
    )

    const { result } = renderHook(() => useAccounts(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual([])
  })
})
