/**
 * The canonical "Uncategorized" rule on the frontend. "Uncategorized" is a real
 * category named "Uncategorized" (not a null category) — see CONTEXT.md
 * ("Uncategorized (transaction state)"). This module is the single place that
 * names it and matches it, so the category-name check isn't re-spelled — and
 * case-drifted — per page.
 */

/** The display name of the dedicated "Uncategorized" category. */
export const UNCATEGORIZED_CATEGORY_NAME = 'Uncategorized'

/** True if the category is the dedicated "Uncategorized" bucket (case-insensitive). */
export function isUncategorizedCategory(category: { name: string } | null | undefined): boolean {
  return category?.name.toLowerCase() === UNCATEGORIZED_CATEGORY_NAME.toLowerCase()
}
