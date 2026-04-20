import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditTransactionDialog } from '@/components/EditTransactionDialog'
import type { TransactionDto } from '@/types'

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
  it('shows legacy read-only fields and editable category and description fields', async () => {
    const user = userEvent.setup()
    const onDescriptionChange = vi.fn()
    const onCategoryChange = vi.fn()
    const onClose = vi.fn()
    const onSave = vi.fn()

    render(
      <EditTransactionDialog
        open
        transaction={transaction}
        description={transaction.description}
        categoryId={transaction.category?.id}
        categoryOptions={[
          { label: 'Food', value: 9 },
          { label: 'Transport', value: 12 },
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
    expect(screen.getByLabelText('Rule was applied')).toBeChecked()
    expect(screen.getByLabelText('Rule was applied')).toBeDisabled()

    await user.type(screen.getByLabelText('Description'), ' updated')
    expect(onDescriptionChange).toHaveBeenCalled()

    await user.selectOptions(screen.getByLabelText('Category'), '12')
    expect(onCategoryChange).toHaveBeenCalledWith(12)

    await user.click(screen.getByRole('button', { name: 'Save' }))
    expect(onSave).toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onClose).toHaveBeenCalled()
  })
})
