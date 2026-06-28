import { useEffect, useRef } from 'react'
import type { SetURLSearchParams } from 'react-router-dom'

/** localStorage key for the persisted Transactions filter query string. */
export const TRANSACTIONS_FILTERS_KEY = 'transactions-filters'

/**
 * Persists a page's URL query string to localStorage and restores it when the
 * page is entered with no query params — e.g. a bare sidebar link. Deep links
 * that already carry params (a chart drill-in, a shared URL) are used as-is and
 * are not overwritten on entry, so persistence never fights an explicit URL.
 */
export function usePersistedFilters(
  key: string,
  searchParams: URLSearchParams,
  setSearchParams: SetURLSearchParams,
) {
  // Restore once on mount, and only when the URL is bare.
  const restored = useRef(false)
  useEffect(() => {
    if (restored.current) return
    restored.current = true
    if (searchParams.toString() === '') {
      const saved = localStorage.getItem(key)
      if (saved) setSearchParams(new URLSearchParams(saved), { replace: true })
    }
    // Mount-only: restore reads the initial URL exactly once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Persist on every change after the initial mount render (which we skip so a
  // bare entry doesn't clobber the saved value before restore runs).
  const skipMountSave = useRef(true)
  useEffect(() => {
    if (skipMountSave.current) {
      skipMountSave.current = false
      return
    }
    try {
      localStorage.setItem(key, searchParams.toString())
    } catch {
      // Ignore storage quota / availability errors — persistence is best-effort.
    }
  }, [key, searchParams])
}
