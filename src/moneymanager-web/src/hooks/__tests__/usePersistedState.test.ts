import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, beforeEach } from 'vitest'
import { usePersistedState } from '@/hooks/usePersistedState'

describe('usePersistedState', () => {
  beforeEach(() => localStorage.clear())

  it('returns the initial value when nothing is stored', () => {
    const { result } = renderHook(() => usePersistedState('chart-period:test', '12'))
    expect(result.current[0]).toBe('12')
  })

  it('persists updates to localStorage', () => {
    const { result } = renderHook(() => usePersistedState('chart-period:test', '12'))
    act(() => result.current[1]('y1'))
    expect(result.current[0]).toBe('y1')
    expect(JSON.parse(localStorage.getItem('chart-period:test')!)).toBe('y1')
  })

  it('reads a previously stored value on mount', () => {
    localStorage.setItem('chart-period:test', JSON.stringify('m1'))
    const { result } = renderHook(() => usePersistedState('chart-period:test', '12'))
    expect(result.current[0]).toBe('m1')
  })

  it('falls back to the initial value when stored JSON is corrupt', () => {
    localStorage.setItem('chart-period:test', '{not json')
    const { result } = renderHook(() => usePersistedState('chart-period:test', '12'))
    expect(result.current[0]).toBe('12')
  })
})
