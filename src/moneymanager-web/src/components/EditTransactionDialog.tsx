import { type FC } from 'react'
import { Button, CategorySelect, Dialog, DialogFooter, Input } from '@/components/ui'
import { RuleManagementPanel } from '@/components/RuleManagementPanel'
import { formatCAD } from '@/lib/format'
import type { Category, TransactionDto } from '@/types'

interface EditTransactionDialogProps {
  open: boolean
  transaction: TransactionDto | null
  description: string
  categoryId?: number
  categories: Category[]
  isSaving: boolean
  errorMessage?: string | null
  formatDate: (value: string) => string
  onDescriptionChange: (value: string) => void
  onCategoryChange: (value?: number) => void
  onClose: () => void
  onSave: () => void
}

interface EditTransactionDialogContentProps extends Omit<EditTransactionDialogProps, 'transaction' | 'open'> {
  transaction: TransactionDto
}

function EditTransactionDialogContent({
  transaction,
  description,
  categoryId,
  categories,
  isSaving,
  errorMessage,
  formatDate,
  onDescriptionChange,
  onCategoryChange,
  onClose,
  onSave,
}: EditTransactionDialogContentProps) {
  return (
    <>
      <div className="grid gap-4 sm:grid-cols-3">
        <Input id="edit-transaction-date" label="Date" value={formatDate(transaction.date)} readOnly />
        <Input id="edit-transaction-account" label="Account" value={transaction.account.shownName} readOnly />
        <Input id="edit-transaction-amount" label="Amount" value={formatCAD(transaction.amountExt, { signed: true })} readOnly />
      </div>
      <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
        Date, account and amount come from the imported bank record and can&apos;t be edited.
      </p>

      <div className="mt-4 flex flex-col gap-4">
        <CategorySelect
          id="edit-transaction-category"
          label="Category"
          categories={categories}
          value={categoryId}
          onChange={onCategoryChange}
        />
        <Input
          id="edit-transaction-description"
          label="Description"
          value={description}
          onChange={onDescriptionChange}
          autoFocus
        />
        <Input
          id="edit-transaction-original-description"
          label="Original Description"
          value={transaction.originalDescription}
          readOnly
        />

        <label className="flex items-center gap-3 text-sm text-gray-700 dark:text-gray-300">
          <input
            type="checkbox"
            checked={transaction.isRuleApplied}
            readOnly
            disabled
            className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-60 dark:border-gray-600 dark:bg-gray-700"
          />
          <span>Rule was applied</span>
        </label>
      </div>

      <RuleManagementPanel
        transaction={transaction}
        categories={categories}
        description={description}
        categoryId={categoryId}
        onDescriptionChange={onDescriptionChange}
        onCategoryChange={onCategoryChange}
        onClose={onClose}
      />

      {errorMessage && (
        <p className="mt-4 text-sm text-red-600 dark:text-red-400" role="alert">
          {errorMessage}
        </p>
      )}

      <DialogFooter>
        <Button variant="secondary" onClick={onClose} disabled={isSaving}>
          Cancel
        </Button>
        <Button onClick={onSave} loading={isSaving}>
          Save
        </Button>
      </DialogFooter>
    </>
  )
}

export const EditTransactionDialog: FC<EditTransactionDialogProps> = ({ open, transaction, ...props }) => (
  <Dialog open={open} onClose={props.onClose} title="Edit Transaction">
    {transaction && (
      <EditTransactionDialogContent
        key={transaction.id}
        transaction={transaction}
        {...props}
      />
    )}
  </Dialog>
)
