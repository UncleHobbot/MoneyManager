import { describe, it, expect } from 'vitest'
import { chartTheme, chartAxis, CHART_PALETTE } from '@/lib/chartTheme'

describe('CHART_PALETTE', () => {
  it('is a non-empty list of valid hex colors', () => {
    expect(CHART_PALETTE.length).toBeGreaterThan(0)
    for (const c of CHART_PALETTE) {
      expect(c).toMatch(/^#[0-9A-Fa-f]{6}$/)
    }
  })

  it('has no duplicate colors', () => {
    expect(new Set(CHART_PALETTE).size).toBe(CHART_PALETTE.length)
  })
})

describe('chartTheme', () => {
  it('uses the shared palette as the color cycle', () => {
    expect(chartTheme(false).color).toEqual(CHART_PALETTE)
  })

  it('keeps the background transparent so the card shows through', () => {
    expect(chartTheme(true).backgroundColor).toBe('transparent')
    expect(chartTheme(false).backgroundColor).toBe('transparent')
  })

  it('themes text differently for dark vs light mode', () => {
    const dark = chartTheme(true).textStyle as { color: string }
    const light = chartTheme(false).textStyle as { color: string }
    expect(dark.color).not.toBe(light.color)
  })

  it('does not leak Cartesian axes into the base theme (pie/sankey safe)', () => {
    expect(chartTheme(true).xAxis).toBeUndefined()
    expect(chartTheme(true).yAxis).toBeUndefined()
  })
})

describe('chartAxis', () => {
  it('returns distinct label/line/split colors per mode', () => {
    const dark = chartAxis(true)
    const light = chartAxis(false)
    expect(dark.label).not.toBe(light.label)
    expect(dark.split).not.toBe(light.split)
  })
})
