import { describe, it, expect } from 'vitest'
import { moneyGrid, categoryAxis, cadValueAxis, cadAxisTooltip } from '@/lib/chartOptions'

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('moneyGrid', () => {
  it('provides sensible defaults with containLabel', () => {
    expect(moneyGrid()).toEqual({ left: 8, right: 16, top: 8, bottom: 8, containLabel: true })
  })

  it('merges overrides over the defaults', () => {
    expect(moneyGrid({ top: 40, right: 24 })).toEqual({
      left: 8,
      right: 24,
      top: 40,
      bottom: 8,
      containLabel: true,
    })
  })
})

describe('categoryAxis', () => {
  it('is a themed category axis with data and an axis line', () => {
    const axis = categoryAxis(false, ['Jan', 'Feb']) as any
    expect(axis.type).toBe('category')
    expect(axis.data).toEqual(['Jan', 'Feb'])
    expect(axis.axisLabel.color).toBeTruthy()
    expect(axis.axisLine.lineStyle.color).toBeTruthy()
    expect(axis.name).toBeUndefined()
  })

  it('adds a centered title when name is given', () => {
    const axis = categoryAxis(false, [1, 2], { name: 'Day of Month' }) as any
    expect(axis.name).toBe('Day of Month')
    expect(axis.nameLocation).toBe('middle')
    expect(axis.nameGap).toBe(28)
  })

  it('uses dark colors when isDark', () => {
    const light = categoryAxis(false, []) as any
    const dark = categoryAxis(true, []) as any
    expect(dark.axisLabel.color).not.toBe(light.axisLabel.color)
  })
})

describe('cadValueAxis', () => {
  it('is a value axis that formats labels as whole dollars', () => {
    const axis = cadValueAxis(false) as any
    expect(axis.type).toBe('value')
    const label = axis.axisLabel.formatter(1234.5)
    expect(label).toContain('$')
    expect(label).toContain('1,235')
    expect(label).not.toContain('.')
  })

  it('adds a title when name is given', () => {
    const axis = cadValueAxis(true, { name: 'Cumulative $ Spent' }) as any
    expect(axis.name).toBe('Cumulative $ Spent')
  })
})

describe('cadAxisTooltip', () => {
  it('formats values as whole dollars and null as a dash', () => {
    const tip = cadAxisTooltip() as any
    expect(tip.trigger).toBe('axis')
    expect(tip.valueFormatter(null)).toBe('—')
    expect(tip.valueFormatter(1234.5)).toContain('1,235')
    expect(tip.axisPointer).toBeUndefined()
  })

  it('adds a shadow axis pointer when requested', () => {
    const tip = cadAxisTooltip({ shadow: true }) as any
    expect(tip.axisPointer).toEqual({ type: 'shadow' })
  })
})
