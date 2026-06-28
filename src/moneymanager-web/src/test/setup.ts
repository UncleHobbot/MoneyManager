import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'

// Keep persisted state (localStorage) from leaking between tests.
afterEach(() => {
  localStorage.clear()
})

// jsdom doesn't implement matchMedia; ThemeProvider reads it for the initial
// (no stored preference) theme. Default `matches: false` keeps the default dark.
if (!window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }),
  })
}
