import type { FC, InputHTMLAttributes } from 'react'

interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  label?: string
  error?: string
  onChange?: (value: string) => void
}

export const Input: FC<InputProps> = ({
  label,
  error,
  id,
  onChange,
  className = '',
  ...rest
}) => {
  const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {label}
        </label>
      )}
      <input
        id={inputId}
        onChange={onChange ? (e) => onChange(e.target.value) : undefined}
        className={`rounded-lg border px-3 py-2 text-sm transition-colors duration-150 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed ${
          error
            ? 'border-red-500 dark:border-red-400'
            : 'border-gray-200 dark:border-gray-700'
        } ${className}`}
        {...rest}
      />
      {error && <p className="text-xs text-red-500 dark:text-red-400">{error}</p>}
    </div>
  )
}
