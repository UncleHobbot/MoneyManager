import { useMemo, useState } from 'react'
import { useBudgetVsActual } from '@/hooks/useCharts'
import { useBudgets, useSetBudget, useDeleteBudget } from '@/hooks/useBudgets'
import { useCategories } from '@/hooks/useCategories'
import { useTheme } from '@/components/layout/useTheme'
import { Spinner, Card, ChartCard, EChart, CategoryIcon, Input, Button } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { chartAxis } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'
import type { BudgetDto, Category } from '@/types'

// Income / Transfer / Uncategorized aren't spending budgets.
const NON_BUDGETABLE = new Set(['Income', 'Transfer', 'Uncategorized'])

function BudgetRow({
  category,
  budget,
  onSave,
  onClear,
  saving,
}: {
  category: Category
  budget: BudgetDto | undefined
  onSave: (categoryId: number, amount: number) => void
  onClear: (categoryId: number) => void
  saving: boolean
}) {
  const [value, setValue] = useState(budget ? String(budget.amount) : '')
  const amount = Number(value)
  const dirty = value !== (budget ? String(budget.amount) : '')

  return (
    <div className="flex items-center gap-3 py-2">
      <CategoryIcon icon={category.icon ?? undefined} size={18} className="text-gray-500 dark:text-gray-400" />
      <span className="flex-1 truncate text-sm text-gray-900 dark:text-gray-100">{category.name}</span>
      <Input
        type="number"
        value={value}
        onChange={setValue}
        placeholder="No budget"
        className="w-32"
      />
      <Button
        size="sm"
        onClick={() => onSave(category.id, amount)}
        disabled={saving || !dirty || !Number.isFinite(amount) || amount <= 0}
      >
        Save
      </Button>
      {budget && (
        <Button size="sm" variant="ghost" onClick={() => onClear(category.id)} disabled={saving}>
          Clear
        </Button>
      )}
    </div>
  )
}

export default function BudgetsPage() {
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: vsActual, isLoading: vaLoading } = useBudgetVsActual()
  const { data: budgets, isLoading: bLoading } = useBudgets()
  const { data: categories, isLoading: cLoading } = useCategories()
  const setBudget = useSetBudget()
  const deleteBudget = useDeleteBudget()

  // Largest actual at the top of the horizontal bars.
  const rows = useMemo(() => [...(vsActual ?? [])].reverse(), [vsActual])

  const option = useMemo<EChartsOption>(() => {
    const axis = chartAxis(isDark)
    return {
      grid: { left: 8, right: 24, top: 28, bottom: 8, containLabel: true },
      legend: { top: 0 },
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'shadow' },
        valueFormatter: (v) => formatCAD(Number(v), { fractionDigits: 0 }),
      },
      xAxis: {
        type: 'value',
        axisLabel: { color: axis.label, formatter: (v: number) => formatCAD(v, { fractionDigits: 0 }) },
        splitLine: { lineStyle: { color: axis.split } },
      },
      yAxis: {
        type: 'category',
        data: rows.map(r => r.name),
        axisLabel: { color: axis.label },
      },
      series: [
        {
          name: 'Actual',
          type: 'bar',
          barGap: 0,
          data: rows.map(r => ({
            value: r.actual,
            itemStyle: { color: r.actual > r.budget ? '#EF4444' : '#22C55E', borderRadius: [0, 3, 3, 0] },
          })),
        },
        {
          name: 'Budget',
          type: 'bar',
          data: rows.map(r => r.budget),
          itemStyle: { color: isDark ? '#4B5563' : '#D1D5DB', borderRadius: [0, 3, 3, 0] },
        },
      ],
    }
  }, [rows, isDark])

  const topLevel = useMemo(
    () =>
      (categories ?? [])
        .filter(c => c.parent == null && !NON_BUDGETABLE.has(c.name))
        .sort((a, b) => a.name.localeCompare(b.name)),
    [categories],
  )

  const budgetByCategory = useMemo(
    () => new Map((budgets ?? []).map(b => [b.categoryId, b])),
    [budgets],
  )

  if (vaLoading || bLoading || cLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  const handleSave = (categoryId: number, amount: number) => setBudget.mutate({ categoryId, amount })
  const handleClear = (categoryId: number) => deleteBudget.mutate(categoryId)

  const chartHeight = Math.max(220, rows.length * 46 + 48)

  return (
    <div className="space-y-6 p-6 lg:p-8">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Budgets</h1>

      <ChartCard
        title="Budget vs Actual (this month)"
        isEmpty={rows.length === 0}
        emptyMessage="No budgets set yet — add one below."
        height={chartHeight}
      >
        <EChart option={option} height={chartHeight} />
      </ChartCard>

      <Card title="Set monthly budgets">
        <p className="mb-2 text-sm text-gray-500 dark:text-gray-400">
          Budgets apply to top-level categories.
        </p>
        <div className="divide-y divide-gray-100 dark:divide-gray-700">
          {topLevel.map(c => (
            <BudgetRow
              // Include the amount so the row remounts (re-seeding its input) after
              // a save/clear, instead of keeping the previously typed value.
              key={`${c.id}:${budgetByCategory.get(c.id)?.amount ?? 'none'}`}
              category={c}
              budget={budgetByCategory.get(c.id)}
              onSave={handleSave}
              onClear={handleClear}
              saving={setBudget.isPending || deleteBudget.isPending}
            />
          ))}
        </div>
      </Card>
    </div>
  )
}
