import { renderHook } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { usePersistedFilters } from '@/hooks/usePersistedFilters'

describe('usePersistedFilters', () => {
  beforeEach(() => localStorage.clear())

  it('restores saved params when the URL is bare', () => {
    localStorage.setItem('f', 'period=y1&categoryId=5')
    const setSearchParams = vi.fn()

    renderHook(() => usePersistedFilters('f', new URLSearchParams(''), setSearchParams))

    expect(setSearchParams).toHaveBeenCalledTimes(1)
    const [restored, opts] = setSearchParams.mock.calls[0]
    expect((restored as URLSearchParams).get('period')).toBe('y1')
    expect((restored as URLSearchParams).get('categoryId')).toBe('5')
    expect(opts).toEqual({ replace: true })
  })

  it('does not restore when the URL already carries params (deep link)', () => {
    localStorage.setItem('f', 'period=y1')
    const setSearchParams = vi.fn()

    renderHook(() => usePersistedFilters('f', new URLSearchParams('period=m1'), setSearchParams))

    expect(setSearchParams).not.toHaveBeenCalled()
  })

  it('does nothing on a bare URL when nothing is saved', () => {
    const setSearchParams = vi.fn()

    renderHook(() => usePersistedFilters('f', new URLSearchParams(''), setSearchParams))

    expect(setSearchParams).not.toHaveBeenCalled()
  })

  it('persists params when they change (skipping the mount render)', () => {
    const setSearchParams = vi.fn()
    const { rerender } = renderHook(
      ({ params }) => usePersistedFilters('f', params, setSearchParams),
      { initialProps: { params: new URLSearchParams('period=m1') } },
    )

    // Mount save is skipped so a bare entry can't clobber the saved value.
    rerender({ params: new URLSearchParams('period=y2&search=x') })

    const saved = new URLSearchParams(localStorage.getItem('f') ?? '')
    expect(saved.get('period')).toBe('y2')
    expect(saved.get('search')).toBe('x')
  })
})
