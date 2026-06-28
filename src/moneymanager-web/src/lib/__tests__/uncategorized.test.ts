import { describe, it, expect } from 'vitest'
import { UNCATEGORIZED_CATEGORY_NAME, isUncategorizedCategory } from '@/lib/uncategorized'

describe('isUncategorizedCategory', () => {
  it('matches the Uncategorized category case-insensitively', () => {
    expect(isUncategorizedCategory({ name: 'Uncategorized' })).toBe(true)
    expect(isUncategorizedCategory({ name: 'uncategorized' })).toBe(true)
    expect(isUncategorizedCategory({ name: 'UNCATEGORIZED' })).toBe(true)
  })

  it('does not match other categories', () => {
    expect(isUncategorizedCategory({ name: 'Food' })).toBe(false)
    expect(isUncategorizedCategory({ name: 'Income' })).toBe(false)
  })

  it('is false for null or undefined', () => {
    expect(isUncategorizedCategory(null)).toBe(false)
    expect(isUncategorizedCategory(undefined)).toBe(false)
  })
})

describe('UNCATEGORIZED_CATEGORY_NAME', () => {
  it('is the canonical display name', () => {
    expect(UNCATEGORIZED_CATEGORY_NAME).toBe('Uncategorized')
  })
})
