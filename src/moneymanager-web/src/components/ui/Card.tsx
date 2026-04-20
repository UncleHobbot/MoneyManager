import type { FC, ReactNode } from 'react'

interface CardProps {
  title?: string
  subtitle?: string
  children: ReactNode
  className?: string
  bodyClassName?: string
}

export const Card: FC<CardProps> = ({ title, subtitle, children, className = '', bodyClassName = '' }) => (
  <div
    className={`rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm ${className}`}
  >
    {(title || subtitle) && (
      <div className="border-b border-gray-200 dark:border-gray-700 px-6 py-4">
        {title && <h3 className="text-base font-semibold text-gray-900 dark:text-gray-100">{title}</h3>}
        {subtitle && <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{subtitle}</p>}
      </div>
    )}
    <div className={`px-6 py-4 ${bodyClassName}`}>{children}</div>
  </div>
)
