import type { FC, SelectHTMLAttributes } from 'react'

interface SelectOption {
  label: string
  value: string | number
}

interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'onChange'> {
  label?: string
  options: SelectOption[]
  value?: string | number
  onChange?: (value: string) => void
  placeholder?: string
  error?: string
}

export const Select: FC<SelectProps> = ({
  label,
  options,
  value,
  onChange,
  placeholder,
  error,
  id,
  className = '',
  ...rest
}) => {
  const selectId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={selectId} className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {label}
        </label>
      )}
      <select
        id={selectId}
        value={value}
        onChange={onChange ? (e) => onChange(e.target.value) : undefined}
        className={`rounded-lg border px-3 py-2 text-sm transition-colors duration-150 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed ${
          error
            ? 'border-red-500 dark:border-red-400'
            : 'border-gray-200 dark:border-gray-700'
        } ${className}`}
        {...rest}
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
      {error && <p className="text-xs text-red-500 dark:text-red-400">{error}</p>}
    </div>
  )
}
