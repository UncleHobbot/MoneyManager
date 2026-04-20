import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditTransactionDialog } from '@/components/EditTransactionDialog'
import { useApplyRuleToTransaction, usePossibleRules, useUpdateRule } from '@/hooks/useRules'
import type { TransactionDto } from '@/types'

vi.mock('@/hooks/useRules', () => ({
  usePossibleRules: vi.fn(),
  useApplyRuleToTransaction: vi.fn(),
  useUpdateRule: vi.fn(),
}))

const transaction: TransactionDto = {
  id: 11,
  transaction: {} as TransactionDto['transaction'],
  account: {
    id: 7,
    name: 'Chequing',
    shownName: 'Main Chequing',
    description: null,
    type: 0,
    number: null,
    isHideFromGraph: false,
    alternativeName1: null,
    alternativeName2: null,
    alternativeName3: null,
    alternativeName4: null,
    alternativeName5: null,
    typeIconName: 'Cash',
  },
  date: '2026-03-15T00:00:00Z',
  description: 'Grocery store',
  originalDescription: 'GROCERY STORE #123',
  amount: 45.67,
  amountExt: -45.67,
  isDebit: true,
  category: {
    id: 9,
    parent: null,
    name: 'Food',
    icon: 'Food',
    isNew: false,
    pIcon: null,
  },
  isRuleApplied: true,
}

describe('EditTransactionDialog', () => {
  it('shows legacy read-only fields and allows applying a matching rule', async () => {
    const user = userEvent.setup()
    const onDescriptionChange = vi.fn()
    const onCategoryChange = vi.fn()
    const onClose = vi.fn()
    const onSave = vi.fn()
    const applyMutate = vi.fn((_vars, options) => options?.onSuccess?.(transaction))

    vi.mocked(usePossibleRules).mockReturnValue({
      data: [{
        id: 5,
        originalDescription: 'GROCERY',
        newDescription: 'Groceries',
        compareType: 1,
        compareTypeString: 'Starts With',
        category: transaction.category!,
      }],
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as never)
    vi.mocked(useApplyRuleToTransaction).mockReturnValue({
      mutate: applyMutate,
      isPending: false,
    } as never)
    vi.mocked(useUpdateRule).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)

    render(
      <EditTransactionDialog
        open
        transaction={{ ...transaction, isRuleApplied: false }}
        description={transaction.description}
        categoryId={transaction.category?.id}
        categories={[
          transaction.category!,
          { id: 12, parent: null, name: 'Transport', icon: null, isNew: false, pIcon: null },
        ]}
        isSaving={false}
        formatDate={() => 'Mar 15, 2026'}
        formatAmount={() => '-$45.67'}
        onDescriptionChange={onDescriptionChange}
        onCategoryChange={onCategoryChange}
        onClose={onClose}
        onSave={onSave}
      />,
    )

    expect(screen.getByLabelText('Date')).toHaveValue('Mar 15, 2026')
    expect(screen.getByLabelText('Date')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Account')).toHaveValue('Main Chequing')
    expect(screen.getByLabelText('Account')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Amount')).toHaveValue('-$45.67')
    expect(screen.getByLabelText('Amount')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Original Description')).toHaveValue('GROCERY STORE #123')
    expect(screen.getByLabelText('Original Description')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Rule was applied')).not.toBeChecked()
    expect(screen.getByLabelText('Rule was applied')).toBeDisabled()
    expect(screen.getByRole('heading', { name: 'Rule management' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Apply rule' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Create rule' })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Description'), ' updated')
    expect(onDescriptionChange).toHaveBeenCalled()

    await user.selectOptions(screen.getByLabelText('Category'), '12')
    expect(onCategoryChange).toHaveBeenCalledWith(12)

    await user.click(screen.getByRole('button', { name: 'Create rule' }))
    expect(screen.getByRole('button', { name: 'Save New Rule' })).toBeInTheDocument()
    expect(screen.getByLabelText('Comparison type')).toHaveValue('0')
    expect(screen.getByLabelText('Rule match text')).toHaveValue('GROCERY STORE #123')

    await user.click(screen.getByRole('button', { name: 'Apply rule' }))

    await user.click(screen.getByRole('button', { name: 'Apply rule for Food: Groceries' }))
    expect(applyMutate).toHaveBeenCalledWith(
      { transactionId: transaction.id, ruleId: 5 },
      expect.any(Object),
    )
    expect(onClose).toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Save' }))
    expect(onSave).toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onClose).toHaveBeenCalled()
  })

  it('offers creating a new rule when no matching rules exist', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const applyMutate = vi.fn((_vars, options) => options?.onSuccess?.(transaction))
    const createMutate = vi.fn((_rule, options) => options?.onSuccess?.([{
      id: 27,
      originalDescription: 'GROCERY STORE #123',
      newDescription: 'Grocery store',
      compareType: 0,
      compareTypeString: 'Contains',
      category: { id: 12, parent: null, name: 'Transport', icon: null, isNew: false, pIcon: null },
    }]))
    const refetch = vi.fn()

    vi.mocked(usePossibleRules).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      refetch,
    } as never)
    vi.mocked(useApplyRuleToTransaction).mockReturnValue({
      mutate: applyMutate,
      isPending: false,
    } as never)
    vi.mocked(useUpdateRule).mockReturnValue({
      mutate: createMutate,
      isPending: false,
    } as never)

    render(
      <EditTransactionDialog
        open
        transaction={{ ...transaction, isRuleApplied: false, category: null }}
        description={transaction.description}
        categoryId={undefined}
        categories={[
          { id: 9, parent: null, name: 'Food', icon: 'Food', isNew: false, pIcon: null },
          { id: 12, parent: null, name: 'Transport', icon: null, isNew: false, pIcon: null },
        ]}
        isSaving={false}
        formatDate={() => 'Mar 15, 2026'}
        formatAmount={() => '-$45.67'}
        onDescriptionChange={vi.fn()}
        onCategoryChange={vi.fn()}
        onClose={onClose}
        onSave={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Rule management' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Create rule' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Apply' })).not.toBeInTheDocument()
    expect(screen.getByLabelText('Comparison type')).toHaveValue('0')
    expect(screen.getByLabelText('Rule match text')).toHaveValue('GROCERY STORE #123')

    await user.type(screen.getByLabelText('Rule replacement description'), 'Grocery store')
    await user.selectOptions(screen.getByLabelText('Rule category'), '12')
    await user.click(screen.getByRole('button', { name: 'Save New Rule' }))

    expect(createMutate).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 0,
          originalDescription: 'GROCERY STORE #123',
          newDescription: 'Grocery store',
          compareType: 0,
          category: expect.objectContaining({ id: 12, name: 'Transport' }),
        }),
        expect.any(Object),
    )
    expect(applyMutate).toHaveBeenCalledWith(
      { transactionId: transaction.id, ruleId: 27 },
      expect.any(Object),
    )
    expect(onClose).toHaveBeenCalled()
    expect(refetch).not.toHaveBeenCalled()
  })

  it('keeps the create-rule flow available when matching rules fail to load', async () => {
    const user = userEvent.setup()

    vi.mocked(usePossibleRules).mockReturnValue({
      data: [],
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as never)
    vi.mocked(useApplyRuleToTransaction).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)
    vi.mocked(useUpdateRule).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)

    render(
      <EditTransactionDialog
        open
        transaction={{ ...transaction, isRuleApplied: false }}
        description={transaction.description}
        categoryId={transaction.category?.id}
        categories={[transaction.category!]}
        isSaving={false}
        formatDate={() => 'Mar 15, 2026'}
        formatAmount={() => '-$45.67'}
        onDescriptionChange={vi.fn()}
        onCategoryChange={vi.fn()}
        onClose={vi.fn()}
        onSave={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Apply rule' }))
    expect(screen.getByText("Couldn't load matching rules.")).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Create rule' }))
    expect(screen.getByLabelText('Rule match text')).toBeInTheDocument()
    expect(screen.getByLabelText('Rule category')).toBeInTheDocument()
  })

  it('warns before applying a rule over unsaved edits', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const applyMutate = vi.fn((_vars, options) => options?.onSuccess?.(transaction))

    vi.mocked(usePossibleRules).mockReturnValue({
      data: [{
        id: 5,
        originalDescription: 'GROCERY',
        newDescription: 'Groceries',
        compareType: 1,
        compareTypeString: 'Starts With',
        category: transaction.category!,
      }],
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as never)
    vi.mocked(useApplyRuleToTransaction).mockReturnValue({
      mutate: applyMutate,
      isPending: false,
    } as never)
    vi.mocked(useUpdateRule).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)

    render(
      <EditTransactionDialog
        open
        transaction={{ ...transaction, isRuleApplied: false }}
        description="Edited grocery store"
        categoryId={12}
        categories={[
          transaction.category!,
          { id: 12, parent: null, name: 'Transport', icon: null, isNew: false, pIcon: null },
        ]}
        isSaving={false}
        formatDate={() => 'Mar 15, 2026'}
        formatAmount={() => '-$45.67'}
        onDescriptionChange={vi.fn()}
        onCategoryChange={vi.fn()}
        onClose={vi.fn()}
        onSave={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Apply rule for Food: Groceries' }))

    expect(confirmSpy).toHaveBeenCalledWith('Applying a rule will replace your unsaved edits. Continue?')
    expect(applyMutate).toHaveBeenCalledWith(
      { transactionId: transaction.id, ruleId: 5 },
      expect.any(Object),
    )

    confirmSpy.mockRestore()
  })
})
