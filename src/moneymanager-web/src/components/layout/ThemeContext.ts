import { createContext } from 'react'

export type Theme = 'dark' | 'light'

export interface ThemeContextValue {
  theme: Theme
  toggleTheme: () => void
}

export const THEME_STORAGE_KEY = 'moneymanager-theme'

export const ThemeContext = createContext<ThemeContextValue | undefined>(undefined)
