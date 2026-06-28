import { Button, CategorySelect, Input, Select, Spinner, CategoryIcon } from '@/components/ui'
import { useRuleDraft, RULE_COMPARE_OPTIONS, type UseRuleDraftParams } from '@/hooks/useRuleDraft'

/**
 * The "Rule management" section of the Edit Transaction dialog. Renders the
 * apply/create tabs and, by active panel, either the matching-rules list or the
 * create-rule form (rendered once). All logic lives in {@link useRuleDraft}.
 */
export function RuleManagementPanel(props: UseRuleDraftParams) {
  const { categories, description } = props
  const { activeRulePanel, showApplyRulePanel, selectPanel, apply, create } = useRuleDraft(props)

  return (
    <div className="mt-6 rounded-lg border border-gray-200 p-4 dark:border-gray-700">
      <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">Rule management</h3>

      <div className="mt-4 flex gap-2 border-b border-gray-200 pb-3 dark:border-gray-700">
        {showApplyRulePanel && (
          <Button
            variant={activeRulePanel === 'apply' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => selectPanel('apply')}
          >
            Apply rule
          </Button>
        )}
        <Button
          variant={activeRulePanel === 'create' ? 'primary' : 'secondary'}
          size="sm"
          onClick={() => selectPanel('create')}
        >
          Create rule
        </Button>
      </div>

      {activeRulePanel === 'create' ? (
        <div className="mt-4 flex flex-col gap-4">
          <Select
            id="edit-rule-compare-type"
            label="Comparison type"
            options={RULE_COMPARE_OPTIONS.map(option => ({ label: option.label, value: option.value }))}
            value={create.compareType}
            onChange={(value) => create.setCompareType(Number(value))}
          />
          <Input
            id="edit-rule-description"
            label="Rule match text"
            value={create.matchText}
            onChange={create.setMatchText}
          />
          <Input
            id="edit-rule-new-description"
            label="Rule replacement description"
            value={create.newDescription}
            onChange={create.setNewDescription}
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Leave blank to use the current edited description: {description}
          </p>
          <CategorySelect
            id="edit-rule-category"
            label="Rule category"
            categories={categories}
            value={create.categoryId}
            onChange={create.setCategoryId}
          />
          <div className="flex justify-start">
            <Button
              onClick={create.onSubmit}
              loading={create.isSaving}
              disabled={!create.canSubmit || create.isSaving}
            >
              Save New Rule
            </Button>
          </div>
        </div>
      ) : apply.isLoading ? (
        <div className="mt-4 flex items-center gap-3 text-sm text-gray-500 dark:text-gray-400">
          <Spinner size="sm" />
          <span>Loading matching rules...</span>
        </div>
      ) : apply.isError ? (
        <p className="mt-4 text-sm text-red-600 dark:text-red-400">
          Couldn&apos;t load matching rules.
        </p>
      ) : apply.rules.length > 0 ? (
        <div className="mt-4 space-y-3">
          {apply.rules.map(rule => (
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
                onClick={() => apply.onApply(rule.id)}
                loading={apply.isApplying}
                disabled={apply.isApplying}
              >
                Apply
              </Button>
            </div>
          ))}
        </div>
      ) : (
        <p className="mt-4 text-sm text-gray-500 dark:text-gray-400">
          No matching rules found for this transaction.
        </p>
      )}
    </div>
  )
}
