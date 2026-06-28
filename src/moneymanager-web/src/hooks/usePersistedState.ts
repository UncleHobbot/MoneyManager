import { useEffect, useState, type Dispatch, type SetStateAction } from 'react'

/**
 * useState whose value is mirrored to localStorage under `key`, so it survives
 * reloads and navigation. Falls back to `initial` when nothing is stored or
 * storage is unavailable. Same shape as useState — a value and a setter.
 */
export function usePersistedState<T>(key: string, initial: T): [T, Dispatch<SetStateAction<T>>] {
  const [value, setValue] = useState<T>(() => {
    try {
      const stored = localStorage.getItem(key)
      return stored === null ? initial : (JSON.parse(stored) as T)
    } catch {
      return initial
    }
  })

  useEffect(() => {
    try {
      localStorage.setItem(key, JSON.stringify(value))
    } catch {
      // Ignore storage quota / availability errors — persistence is best-effort.
    }
  }, [key, value])

  return [value, setValue]
}
