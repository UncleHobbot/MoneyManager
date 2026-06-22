/**
 * Currency formatting for the MoneyManager frontend.
 *
 * One canonical CAD formatter replaces the 5+ per-page formatCurrency /
 * formatAmount / inline toLocaleString duplications that existed pre-migration.
 * See CONTEXT.md ("Money formatting") and Candidate 5 grilling.
 */

export interface FormatCadOptions {
  /**
   * Show a +/- sign prefix based on the value's sign. Default: false
   * (absolute value only, no prefix).
   *
   * When true: positive values get "+", negative values get "-".
   * Zero is treated as non-negative (gets "+").
   */
  signed?: boolean

  /**
   * Number of decimal places. Default: 2 (for transaction lists).
   * Use 0 for chart axis labels and donut totals where cents add noise.
   */
  fractionDigits?: number
}

// Intl.NumberFormat construction is relatively expensive; cache per
// fraction-digit count. The cache is module-scoped and lazily populated.
const cadFormatters = new Map<number, Intl.NumberFormat>()

function getCadFormatter(fractionDigits: number): Intl.NumberFormat {
  let formatter = cadFormatters.get(fractionDigits)
  if (!formatter) {
    formatter = new Intl.NumberFormat('en-CA', {
      style: 'currency',
      currency: 'CAD',
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    })
    cadFormatters.set(fractionDigits, formatter)
  }
  return formatter
}

/**
 * Formats a number as a Canadian dollar string.
 *
 * @example
 * formatCAD(45.67)                    // "$45.67"
 * formatCAD(-45.67)                   // "$45.67" (absolute by default)
 * formatCAD(45.67, { signed: true })  // "+$45.67"
 * formatCAD(-45.67, { signed: true }) // "-$45.67"
 * formatCAD(45.67, { fractionDigits: 0 }) // "$46"
 */
export function formatCAD(value: number, opts?: FormatCadOptions): string {
  const { signed = false, fractionDigits = 2 } = opts ?? {}
  const formatter = getCadFormatter(fractionDigits)
  const formatted = formatter.format(Math.abs(value))

  if (!signed) return formatted
  return `${value < 0 ? '-' : '+'}${formatted}`
}
