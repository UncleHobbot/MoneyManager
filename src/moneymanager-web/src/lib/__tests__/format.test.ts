import { formatCAD } from '@/lib/format'

describe('formatCAD', () => {
  describe('default behavior (no options)', () => {
    it('formats a positive value as CAD currency', () => {
      expect(formatCAD(45.67)).toBe('$45.67')
    })

    it('formats a negative value as absolute (no sign by default)', () => {
      expect(formatCAD(-45.67)).toBe('$45.67')
    })

    it('formats zero', () => {
      expect(formatCAD(0)).toBe('$0.00')
    })

    it('formats large values with thousands separators', () => {
      expect(formatCAD(1234567.89)).toBe('$1,234,567.89')
    })
  })

  describe('signed option', () => {
    it('prefixes + for positive values', () => {
      expect(formatCAD(45.67, { signed: true })).toBe('+$45.67')
    })

    it('prefixes - for negative values', () => {
      expect(formatCAD(-45.67, { signed: true })).toBe('-$45.67')
    })

    it('treats zero as non-negative (+)', () => {
      expect(formatCAD(0, { signed: true })).toBe('+$0.00')
    })
  })

  describe('fractionDigits option', () => {
    it('defaults to 2 decimal places', () => {
      expect(formatCAD(45.6)).toBe('$45.60')
    })

    it('rounds to 0 decimal places when specified', () => {
      expect(formatCAD(45.67, { fractionDigits: 0 })).toBe('$46')
    })

    it('combines with signed', () => {
      expect(formatCAD(-45.67, { signed: true, fractionDigits: 0 })).toBe('-$46')
    })
  })

  describe('edge cases', () => {
    it('handles very small values', () => {
      expect(formatCAD(0.01)).toBe('$0.01')
    })

    it('handles very large values', () => {
      expect(formatCAD(1000000)).toBe('$1,000,000.00')
    })

    it('rounds correctly at boundaries', () => {
      expect(formatCAD(45.5, { fractionDigits: 0 })).toBe('$46')
      expect(formatCAD(45.4, { fractionDigits: 0 })).toBe('$45')
    })
  })
})
