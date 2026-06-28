import { useMemo, useState, type FC } from 'react'
import { Button, Dialog, DialogFooter, Input, Select } from '@/components/ui'
import { UNCATEGORIZED_CATEGORY_NAME } from '@/lib/uncategorized'
import type { Account, Category, CreateTransactionRequest } from '@/types'

interface AddTransactionDialogProps {
  open: boolean
  accounts: Account[]
  categories: Category[]
  isSaving: boolean
  errorMessage?: string | null
  onClose: () => void
  onCreate: (data: CreateTransactionRequest) => void
}

const TYPE_OPTIONS = [
  { label: 'Expense', value: 'debit' },
  { label: 'Income', value: 'credit' },
]

function todayIso(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset() * 60000
  return new Date(now.getTime() - offset).toISOString().slice(0, 10)
}

type AddTransactionFormProps = Omit<AddTransactionDialogProps, 'open'>

function AddTransactionForm({
  accounts,
  categories,
  isSaving,
  errorMessage,
  onClose,
  onCreate,
}: AddTransactionFormProps) {
  const [accountId, setAccountId] = useState<number | undefined>(accounts[0]?.id)
  const [date, setDate] = useState(todayIso())
  const [type, setType] = useState<'debit' | 'credit'>('debit')
  const [amount, setAmount] = useState('')
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [description, setDescription] = useState('')

  const accountOptions = useMemo(
    () => accounts.map((a) => ({ label: a.shownName, value: a.id })),
    [accounts],
  )
  const categoryOptions = useMemo(
    () => [
      { label: UNCATEGORIZED_CATEGORY_NAME, value: '' },
      ...categories.map((c) => ({ label: c.name, value: c.id })),
    ],
    [categories],
  )

  const amountValue = Number(amount)
  const isValid =
    accountId !== undefined &&
    description.trim() !== '' &&
    Number.isFinite(amountValue) &&
    amountValue > 0

  function handleSave() {
    if (!isValid || accountId === undefined) return
    onCreate({
      accountId,
      date,
      description: description.trim(),
      amount: amountValue,
      isDebit: type === 'debit',
      categoryId,
    })
  }

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2">
        <Select
          id="add-transaction-account"
          label="Account"
          options={accountOptions}
          value={accountId ?? ''}
          onChange={(v) => setAccountId(v ? Number(v) : undefined)}
          placeholder="Select account"
        />
        <Input
          id="add-transaction-date"
          label="Date"
          type="date"
          value={date}
          onChange={setDate}
        />
        <Select
          id="add-transaction-type"
          label="Type"
          options={TYPE_OPTIONS}
          value={type}
          onChange={(v) => setType(v as 'debit' | 'credit')}
        />
        <Input
          id="add-transaction-amount"
          label="Amount"
          type="number"
          min="0"
          step="0.01"
          placeholder="0.00"
          value={amount}
          onChange={setAmount}
        />
      </div>

      <div className="mt-4 flex flex-col gap-4">
        <Select
          id="add-transaction-category"
          label="Category"
          options={categoryOptions}
          value={categoryId ?? ''}
          onChange={(v) => setCategoryId(v ? Number(v) : undefined)}
        />
        <Input
          id="add-transaction-description"
          label="Description"
          value={description}
          onChange={setDescription}
          autoFocus
        />
      </div>

      {errorMessage && (
        <p className="mt-4 text-sm text-red-600 dark:text-red-400" role="alert">
          {errorMessage}
        </p>
      )}

      <DialogFooter>
        <Button variant="secondary" onClick={onClose} disabled={isSaving}>
          Cancel
        </Button>
        <Button onClick={handleSave} loading={isSaving} disabled={!isValid || isSaving}>
          Add Transaction
        </Button>
      </DialogFooter>
    </>
  )
}

export const AddTransactionDialog: FC<AddTransactionDialogProps> = ({ open, ...props }) => (
  <Dialog open={open} onClose={props.onClose} title="Add Transaction">
    {open && <AddTransactionForm {...props} />}
  </Dialog>
)
