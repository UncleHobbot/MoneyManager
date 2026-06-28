import { renderHook } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useRuleDraft, type UseRuleDraftParams } from '@/hooks/useRuleDraft'
import { useApplyRuleToTransaction, usePossibleRules, useUpdateRule } from '@/hooks/useRules'
import type { Category, Rule, TransactionDto } from '@/types'

vi.mock('@/hooks/useRules', () => ({
  usePossibleRules: vi.fn(),
  useApplyRuleToTransaction: vi.fn(),
  useUpdateRule: vi.fn(),
}))

const foodCategory: Category = { id: 9, parent: null, name: 'Food', icon: 'Food', isNew: false, pIcon: null }
const transportCategory: Category = { id: 12, parent: null, name: 'Transport', icon: null, isNew: false, pIcon: null }

function makeTransaction(overrides: Partial<TransactionDto> = {}): TransactionDto {
  return {
    id: 11,
    transaction: {} as TransactionDto['transaction'],
    account: {
      id: 7, name: 'Chequing', shownName: 'Main', description: null, type: 0, number: null,
      isHideFromGraph: false, alternativeName1: null, alternativeName2: null, alternativeName3: null,
      alternativeName4: null, alternativeName5: null, typeIconName: 'Cash',
    },
    date: '2026-03-15T00:00:00Z',
    description: 'Grocery store',
    originalDescription: 'GROCERY STORE #123',
    amount: 45.67,
    amountExt: -45.67,
    isDebit: true,
    category: foodCategory,
    isRuleApplied: false,
    ...overrides,
  }
}

function params(overrides: Partial<UseRuleDraftParams> = {}): UseRuleDraftParams {
  return {
    transaction: makeTransaction(),
    categories: [foodCategory, transportCategory],
    description: 'Grocery store',
    categoryId: 9,
    onDescriptionChange: vi.fn(),
    onCategoryChange: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  }
}

describe('useRuleDraft', () => {
  beforeEach(() => {
    vi.mocked(usePossibleRules).mockReturnValue({ data: [], isLoading: false, isError: false, refetch: vi.fn() } as never)
    vi.mocked(useApplyRuleToTransaction).mockReturnValue({ mutate: vi.fn(), isPending: false } as never)
    vi.mocked(useUpdateRule).mockReturnValue({ mutate: vi.fn(), isPending: false } as never)
  })

  it('starts on the create panel when a rule is already applied', () => {
    const { result } = renderHook(() => useRuleDraft(params({ transaction: makeTransaction({ isRuleApplied: true }) })))
    expect(result.current.activeRulePanel).toBe('create')
    expect(result.current.showApplyRulePanel).toBe(false)
  })

  it('shows the apply panel when matching rules exist', () => {
    const matching: Rule = {
      id: 5, originalDescription: 'GROCERY', newDescription: 'Groceries',
      compareType: 0, compareTypeString: 'Contains', category: foodCategory,
    }
    vi.mocked(usePossibleRules).mockReturnValue({ data: [matching], isLoading: false, isError: false, refetch: vi.fn() } as never)

    const { result } = renderHook(() => useRuleDraft(params()))
    expect(result.current.activeRulePanel).toBe('apply')
    expect(result.current.apply.rules).toHaveLength(1)
  })

  it('falls back to the create panel when no matching rules are found', () => {
    const { result } = renderHook(() => useRuleDraft(params()))
    expect(result.current.activeRulePanel).toBe('create')
  })

  it('gates create submission on a resolvable category', () => {
    const withoutCategory = renderHook(() =>
      useRuleDraft(params({ transaction: makeTransaction({ category: null }), categoryId: undefined })),
    )
    expect(withoutCategory.result.current.create.canSubmit).toBe(false)

    const withCategory = renderHook(() => useRuleDraft(params({ categoryId: 9 })))
    expect(withCategory.result.current.create.canSubmit).toBe(true)
  })
})
