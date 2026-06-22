import type { FC } from 'react'
import { formatCAD } from '@/lib/format'

interface MoneyProps {
  /** The monetary value to display. */
  amount: number
  /** Show +/- sign prefix. Default: false. */
  signed?: boolean
  /**
   * Apply green (positive) or red (negative) text color via Tailwind.
   * Default: false. When false, inherits the surrounding text color.
   */
  color?: boolean
  /** Number of decimal places. Default: 2. Use 0 for chart-adjacent labels. */
  fractionDigits?: number
  /** Additional Tailwind classes to merge into the rendered span. */
  className?: string
}

/**
 * Displays a monetary value with consistent CAD formatting and optional
 * sign/color conventions. Replaces inline `{formatCurrency(amount)}` +
 * red/green span patterns scattered across transaction lists and dashboard
 * cards. For chart axis/tooltip formatters (which need a string, not JSX),
 * use {@link formatCAD} directly.
 *
 * @example
 * <Money amount={row.amountExt} signed color />     // "+$45.67" (green) or "-$45.67" (red)
 * <Money amount={total} fractionDigits={0} />        // "$1,234"
 */
export const Money: FC<MoneyProps> = ({
  amount,
  signed = false,
  color = false,
  fractionDigits = 2,
  className = '',
}) => {
  const text = formatCAD(amount, { signed, fractionDigits })

  const colorClass = color
    ? amount >= 0
      ? 'text-green-600 dark:text-green-400'
      : 'text-red-600 dark:text-red-400'
    : ''

  const combined = [colorClass, className].filter(Boolean).join(' ')

  return <span className={combined}>{text}</span>
}
