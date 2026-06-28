import { useState } from 'react'
import { useApplyRuleToTransaction, usePossibleRules, useUpdateRule } from '@/hooks/useRules'
import type { Category, Rule, TransactionDto } from '@/types'

/** Rule comparison types, in the order the backend's RuleCompareType enum defines them. */
export const RULE_COMPARE_OPTIONS = [
  { label: 'Contains', value: 0 },
  { label: 'Starts With', value: 1 },
  { label: 'Ends With', value: 2 },
  { label: 'Equals', value: 3 },
] as const

/** Finds a just-saved rule in a rules list by matching its defining fields (newest id first). */
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

export type RulePanel = 'apply' | 'create'

export interface UseRuleDraftParams {
  transaction: TransactionDto
  categories: Category[]
  description: string
  categoryId?: number
  onDescriptionChange: (value: string) => void
  onCategoryChange: (value?: number) => void
  onClose: () => void
}

export interface RuleDraft {
  activeRulePanel: RulePanel
  showApplyRulePanel: boolean
  selectPanel: (panel: RulePanel) => void
  apply: {
    isLoading: boolean
    isError: boolean
    rules: Rule[]
    isApplying: boolean
    onApply: (ruleId: number) => void
  }
  create: {
    compareType: number
    setCompareType: (value: number) => void
    matchText: string
    setMatchText: (value: string) => void
    newDescription: string
    setNewDescription: (value: string) => void
    categoryId?: number
    setCategoryId: (value?: number) => void
    canSubmit: boolean
    isSaving: boolean
    onSubmit: () => void
  }
}

/**
 * The rule-draft engine behind the Edit Transaction dialog: owns the apply/create
 * panel state, the possible-rules query, and the create → find → apply → sync
 * orchestration. The dialog renders; this hook decides. See CONTEXT.md
 * ("Categorization") for the matching it ultimately drives.
 */
export function useRuleDraft({
  transaction,
  categories,
  description,
  categoryId,
  onDescriptionChange,
  onCategoryChange,
  onClose,
}: UseRuleDraftParams): RuleDraft {
  const [preferredRulePanel, setPreferredRulePanel] = useState<RulePanel>(transaction.isRuleApplied ? 'create' : 'apply')
  const [compareType, setCompareType] = useState(0)
  const [matchText, setMatchText] = useState(transaction.originalDescription)
  const [newDescription, setNewDescription] = useState('')
  const [categoryDraftId, setCategoryDraftId] = useState<number | undefined>()

  const possibleRules = usePossibleRules(transaction.id, !transaction.isRuleApplied)
  const applyRule = useApplyRuleToTransaction()
  const updateRule = useUpdateRule()

  const matchingRules = possibleRules.data ?? []
  const hasDraftChanges = description !== transaction.description || categoryId !== transaction.category?.id
  const hasMatchingRules = matchingRules.length > 0
  const effectiveRuleDescription = newDescription.trim() || description.trim()
  const effectiveRuleCategoryId = categoryDraftId ?? categoryId ?? transaction.category?.id
  const activeRulePanel: RulePanel = transaction.isRuleApplied
    ? 'create'
    : preferredRulePanel === 'create' || (!possibleRules.isLoading && !possibleRules.isError && !hasMatchingRules)
      ? 'create'
      : 'apply'
  const canSubmit = matchText.trim() !== '' && effectiveRuleCategoryId !== undefined
  const isSaving = updateRule.isPending || applyRule.isPending

  function syncFromApplied(updatedTransaction: TransactionDto) {
    onDescriptionChange(updatedTransaction.description)
    onCategoryChange(updatedTransaction.category?.id)
  }

  function onSubmit() {
    const selectedCategory = categories.find(category => category.id === effectiveRuleCategoryId)
    if (!selectedCategory) {
      return
    }

    const compareTypeLabel = RULE_COMPARE_OPTIONS.find(option => option.value === compareType)?.label ?? ''
    const newRule: Rule = {
      id: 0,
      originalDescription: matchText.trim(),
      newDescription: effectiveRuleDescription,
      compareType,
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
          {
            onSuccess: (updatedTransaction: TransactionDto) => {
              syncFromApplied(updatedTransaction)
              possibleRules.refetch()
              setPreferredRulePanel('apply')
            },
          },
        )
      },
    })
  }

  function onApply(ruleId: number) {
    if (
      hasDraftChanges &&
      typeof window !== 'undefined' &&
      !window.confirm('Applying a rule will replace your unsaved edits. Continue?')
    ) {
      return
    }

    applyRule.mutate(
      { transactionId: transaction.id, ruleId },
      {
        onSuccess: (updatedTransaction: TransactionDto) => {
          syncFromApplied(updatedTransaction)
          onClose()
        },
      },
    )
  }

  return {
    activeRulePanel,
    showApplyRulePanel: !transaction.isRuleApplied,
    selectPanel: setPreferredRulePanel,
    apply: {
      isLoading: possibleRules.isLoading,
      isError: possibleRules.isError,
      rules: matchingRules,
      isApplying: applyRule.isPending,
      onApply,
    },
    create: {
      compareType,
      setCompareType,
      matchText,
      setMatchText,
      newDescription,
      setNewDescription,
      categoryId: effectiveRuleCategoryId,
      setCategoryId: setCategoryDraftId,
      canSubmit,
      isSaving,
      onSubmit,
    },
  }
}
