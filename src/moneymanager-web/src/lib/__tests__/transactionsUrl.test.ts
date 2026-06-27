import { describe, it, expect } from 'vitest'
import { transactionsUrl, monthRange } from '@/lib/transactionsUrl'

function params(url: string): URLSearchParams {
  return new URLSearchParams(url.split('?')[1] ?? '')
}

describe('transactionsUrl', () => {
  it('emits each facet', () => {
    const p = params(
      transactionsUrl({
        period: '12',
        categoryId: 5,
        search: 'x',
        from: '2025-01-01',
        to: '2025-02-01',
        uncategorized: true,
      }),
    )
    expect(p.get('period')).toBe('12')
    expect(p.get('categoryId')).toBe('5')
    expect(p.get('search')).toBe('x')
    expect(p.get('from')).toBe('2025-01-01')
    expect(p.get('to')).toBe('2025-02-01')
    expect(p.get('uncategorized')).toBe('1')
  })

  it('returns a bare path when no facets are set', () => {
    expect(transactionsUrl({})).toBe('/transactions')
  })

  it('drops undefined and empty facets', () => {
    const p = params(transactionsUrl({ period: '12', search: '' }))
    expect(p.has('search')).toBe(false)
    expect(p.get('period')).toBe('12')
  })

  it('omits uncategorized when false, emits 1 when true', () => {
    expect(params(transactionsUrl({ period: '12', uncategorized: false })).has('uncategorized')).toBe(false)
    expect(params(transactionsUrl({ uncategorized: true })).get('uncategorized')).toBe('1')
  })

  it('stringifies numeric categoryId', () => {
    expect(params(transactionsUrl({ categoryId: 42 })).get('categoryId')).toBe('42')
  })

  it('url-encodes search', () => {
    const url = transactionsUrl({ search: 'A & B' })
    expect(url).toContain('search=A+%26+B')
    expect(params(url).get('search')).toBe('A & B')
  })

  it('builds a from/to-only window without period', () => {
    const p = params(transactionsUrl({ from: '2025-03-01', to: '2025-04-01' }))
    expect(p.has('period')).toBe(false)
    expect(p.get('from')).toBe('2025-03-01')
    expect(p.get('to')).toBe('2025-04-01')
  })

  it('builds period + categoryId', () => {
    expect(transactionsUrl({ period: 'y1', categoryId: 3 })).toBe('/transactions?period=y1&categoryId=3')
  })
})

describe('monthRange', () => {
  it('spans the first of the month to the first of the next', () => {
    expect(monthRange('2025-03-15T00:00:00')).toEqual({ from: '2025-03-01', to: '2025-04-01' })
  })

  it('rolls over December to next January', () => {
    expect(monthRange('2025-12-01T00:00:00')).toEqual({ from: '2025-12-01', to: '2026-01-01' })
  })

  it('zero-pads single-digit months', () => {
    expect(monthRange('2025-07-04T00:00:00')).toEqual({ from: '2025-07-01', to: '2025-08-01' })
  })
})
