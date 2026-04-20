import { useMemo, useState, type FC } from 'react'
import { useApplyRuleToTransaction, usePossibleRules, useUpdateRule } from '@/hooks/useRules'
import { Button, Dialog, DialogFooter, Input, Select, Spinner, CategoryIcon } from '@/components/ui'
import type { Category, Rule, TransactionDto } from '@/types'

const RULE_COMPARE_OPTIONS = [
  { label: 'Contains', value: 0 },
  { label: 'Starts With', value: 1 },
  { label: 'Ends With', value: 2 },
  { label: 'Equals', value: 3 },
] as const

interface EditTransactionDialogProps {
  open: boolean
  transaction: TransactionDto | null
  description: string
  categoryId?: number
  categories: Category[]
  isSaving: boolean
  formatDate: (value: string) => string
  formatAmount: (value: number) => string
  onDescriptionChange: (value: string) => void
  onCategoryChange: (value?: number) => void
  onClose: () => void
  onSave: () => void
}

interface EditTransactionDialogContentProps extends Omit<EditTransactionDialogProps, 'transaction' | 'open'> {
  transaction: TransactionDto
}

function findMatchingRule(rules: Rule[], ruleToMatch: Rule) {
  return [...rules]
    .sort((left, right) => right.id - left.id)
    .find(rule =>
      rule.originalDescription === ruleToMatch.originalDescription
      && rule.newDescription === ruleToMatch.newDescription
      && rule.compareType === ruleToMatch.compareType
      && rule.category.id === ruleToMatch.category.id,
    )
}

function EditTransactionDialogContent({
  transaction,
  description,
  categoryId,
  categories,
  isSaving,
  formatDate,
  formatAmount,
  onDescriptionChange,
  onCategoryChange,
  onClose,
  onSave,
}: EditTransactionDialogContentProps) {
  const [preferredRulePanel, setPreferredRulePanel] = useState<'apply' | 'create'>(transaction.isRuleApplied ? 'create' : 'apply')
  const [newRuleCompareType, setNewRuleCompareType] = useState(0)
  const [newRuleOriginalDescription, setNewRuleOriginalDescription] = useState(transaction.originalDescription)
  const [newRuleDescription, setNewRuleDescription] = useState('')
  const [newRuleCategoryId, setNewRuleCategoryId] = useState<number | undefined>(transaction.category?.id)

  const categoryOptions = useMemo(
    () => categories.map(category => ({ label: category.name, value: category.id })),
    [categories],
  )
  const possibleRules = usePossibleRules(transaction.id, !transaction.isRuleApplied)
  const applyRule = useApplyRuleToTransaction()
  const updateRule = useUpdateRule()

  const matchingRules = possibleRules.data ?? []
  const hasDraftChanges = description !== transaction.description || categoryId !== transaction.category?.id
  const showApplyRulePanel = !transaction.isRuleApplied
  const hasMatchingRules = matchingRules.length > 0
  const activeRulePanel = transaction.isRuleApplied
    ? 'create'
    : preferredRulePanel === 'create' || (!possibleRules.isLoading && !possibleRules.isError && !hasMatchingRules)
      ? 'create'
      : 'apply'
  const canCreateRule = newRuleOriginalDescription.trim() !== '' && newRuleCategoryId !== undefined
  const isSavingRule = updateRule.isPending || applyRule.isPending

  function handleCreateRule() {
    const selectedCategory = categories.find(category => category.id === newRuleCategoryId)
    if (!selectedCategory) {
      return
    }

    const compareTypeLabel = RULE_COMPARE_OPTIONS.find(option => option.value === newRuleCompareType)?.label ?? ''
    const newRule: Rule = {
      id: 0,
      originalDescription: newRuleOriginalDescription.trim(),
      newDescription: newRuleDescription,
      compareType: newRuleCompareType,
      compareTypeString: compareTypeLabel,
      category: selectedCategory,
    }

    updateRule.mutate(newRule, {
      onSuccess: async (savedRules: Rule[]) => {
        const createdRule = findMatchingRule(savedRules, newRule)
          ?? findMatchingRule((await possibleRules.refetch()).data ?? [], newRule)

        if (!createdRule) {
          setPreferredRulePanel('apply')
          return
        }

        applyRule.mutate(
          { transactionId: transaction.id, ruleId: createdRule.id },
          { onSuccess: () => onClose() },
        )
      },
    })
  }

  function handleApplyRule(ruleId: number) {
    if (
      hasDraftChanges &&
      typeof window !== 'undefined' &&
      !window.confirm('Applying a rule will replace your unsaved edits. Continue?')
    ) {
      return
    }

    applyRule.mutate(
      { transactionId: transaction.id, ruleId },
      { onSuccess: () => onClose() },
    )
  }

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-3">
        <Input id="edit-transaction-date" label="Date" value={formatDate(transaction.date)} readOnly />
        <Input id="edit-transaction-account" label="Account" value={transaction.account.shownName} readOnly />
        <Input id="edit-transaction-amount" label="Amount" value={formatAmount(transaction.amountExt)} readOnly />
      </div>

      <div className="mt-4 flex flex-col gap-4">
        <Select
          id="edit-transaction-category"
          label="Category"
          options={categoryOptions}
          value={categoryId ?? ''}
          onChange={(value) => onCategoryChange(value ? Number(value) : undefined)}
          placeholder="Select category"
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

      <div className="mt-6 rounded-lg border border-gray-200 p-4 dark:border-gray-700">
        <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">Rule management</h3>

        <div className="mt-4 flex gap-2 border-b border-gray-200 pb-3 dark:border-gray-700">
          {showApplyRulePanel && (
            <Button
              variant={activeRulePanel === 'apply' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setPreferredRulePanel('apply')}
            >
              Apply rule
            </Button>
          )}
          <Button
            variant={activeRulePanel === 'create' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setPreferredRulePanel('create')}
          >
            Create rule
          </Button>
        </div>

        {possibleRules.isLoading ? (
          activeRulePanel === 'apply' ? (
            <div className="mt-4 flex items-center gap-3 text-sm text-gray-500 dark:text-gray-400">
              <Spinner size="sm" />
              <span>Loading matching rules...</span>
            </div>
          ) : (
            <div className="mt-4 flex flex-col gap-4">
              <Select
                id="edit-rule-compare-type"
                label="Comparison type"
                options={RULE_COMPARE_OPTIONS.map(option => ({ label: option.label, value: option.value }))}
                value={newRuleCompareType}
                onChange={(value) => setNewRuleCompareType(Number(value))}
              />
              <Input
                id="edit-rule-description"
                label="Rule match text"
                value={newRuleOriginalDescription}
                onChange={setNewRuleOriginalDescription}
              />
              <Input
                id="edit-rule-new-description"
                label="Rule replacement description"
                value={newRuleDescription}
                onChange={setNewRuleDescription}
              />
              <Select
                id="edit-rule-category"
                label="Rule category"
                options={categoryOptions}
                value={newRuleCategoryId ?? ''}
                onChange={(value) => setNewRuleCategoryId(value ? Number(value) : undefined)}
                placeholder="Select category"
              />
              <div className="flex justify-start">
                <Button
                  onClick={handleCreateRule}
                  loading={isSavingRule}
                  disabled={!canCreateRule || isSavingRule}
                >
                  Save New Rule
                </Button>
              </div>
            </div>
          )
        ) : possibleRules.isError && activeRulePanel === 'apply' ? (
          <p className="mt-4 text-sm text-red-600 dark:text-red-400">
            Couldn&apos;t load matching rules.
          </p>
        ) : activeRulePanel === 'apply' && hasMatchingRules ? (
          <div className="mt-4 space-y-3">
            {matchingRules.map(rule => (
              <div
                key={rule.id}
                className="flex items-center justify-between gap-4 rounded-lg border border-gray-200 px-3 py-3 dark:border-gray-700"
              >
                <div className="min-w-0">
                  <div className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {rule.newDescription || (
                      <span className="italic text-gray-500 dark:text-gray-400">
                        No new description
                      </span>
                    )}
                  </div>
                  <div className="mt-1 inline-flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-400">
                    <CategoryIcon icon={rule.category.icon ?? rule.category.pIcon ?? undefined} size={14} />
                    <span>{rule.category.name}</span>
                  </div>
                </div>
                <Button
                  size="sm"
                  aria-label={`Apply rule for ${rule.category.name}${rule.newDescription ? `: ${rule.newDescription}` : ''}`}
                  onClick={() => handleApplyRule(rule.id)}
                  loading={applyRule.isPending}
                  disabled={applyRule.isPending}
                >
                  Apply
                </Button>
              </div>
            ))}
          </div>
        ) : activeRulePanel === 'apply' ? (
          <p className="mt-4 text-sm text-gray-500 dark:text-gray-400">
            No matching rules found for this transaction.
          </p>
        ) : (
          <div className="mt-4 flex flex-col gap-4">
            <Select
              id="edit-rule-compare-type"
              label="Comparison type"
              options={RULE_COMPARE_OPTIONS.map(option => ({ label: option.label, value: option.value }))}
              value={newRuleCompareType}
              onChange={(value) => setNewRuleCompareType(Number(value))}
            />
            <Input
              id="edit-rule-description"
              label="Rule match text"
              value={newRuleOriginalDescription}
              onChange={setNewRuleOriginalDescription}
            />
            <Input
              id="edit-rule-new-description"
              label="Rule replacement description"
              value={newRuleDescription}
              onChange={setNewRuleDescription}
            />
            <Select
              id="edit-rule-category"
              label="Rule category"
              options={categoryOptions}
              value={newRuleCategoryId ?? ''}
              onChange={(value) => setNewRuleCategoryId(value ? Number(value) : undefined)}
              placeholder="Select category"
            />
            <div className="flex justify-start">
              <Button
                onClick={handleCreateRule}
                loading={isSavingRule}
                disabled={!canCreateRule || isSavingRule}
              >
                Save New Rule
              </Button>
            </div>
          </div>
        )}
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
