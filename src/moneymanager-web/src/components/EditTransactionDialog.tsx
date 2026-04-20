import type { FC } from 'react'
import { Button, Dialog, DialogFooter, Input, Select } from '@/components/ui'
import type { TransactionDto } from '@/types'

interface EditTransactionDialogProps {
  open: boolean
  transaction: TransactionDto | null
  description: string
  categoryId?: number
  categoryOptions: Array<{ label: string; value: string | number }>
  isSaving: boolean
  formatDate: (value: string) => string
  formatAmount: (value: number) => string
  onDescriptionChange: (value: string) => void
  onCategoryChange: (value?: number) => void
  onClose: () => void
  onSave: () => void
}

export const EditTransactionDialog: FC<EditTransactionDialogProps> = ({
  open,
  transaction,
  description,
  categoryId,
  categoryOptions,
  isSaving,
  formatDate,
  formatAmount,
  onDescriptionChange,
  onCategoryChange,
  onClose,
  onSave,
}) => (
  <Dialog open={open} onClose={onClose} title="Edit Transaction">
    {transaction && (
      <>
        <div className="grid gap-4 sm:grid-cols-3">
          <Input label="Date" value={formatDate(transaction.date)} readOnly />
          <Input label="Account" value={transaction.account.shownName} readOnly />
          <Input label="Amount" value={formatAmount(transaction.amountExt)} readOnly />
        </div>

        <div className="mt-4 flex flex-col gap-4">
          <Select
            label="Category"
            options={categoryOptions}
            value={categoryId ?? ''}
            onChange={(value) => onCategoryChange(value ? Number(value) : undefined)}
            placeholder="Select category"
          />
          <Input
            label="Description"
            value={description}
            onChange={onDescriptionChange}
            autoFocus
          />
          <Input
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

        <DialogFooter>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={onSave} loading={isSaving}>
            Save
          </Button>
        </DialogFooter>
      </>
    )}
  </Dialog>
)
