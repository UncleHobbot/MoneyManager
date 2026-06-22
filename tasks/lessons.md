# Lessons

Patterns learned from corrections during sessions on this repo. Each entry: the
trigger, the rule, and why it matters.

## Format

- **Session date** — when the correction was made.
- **Trigger** — what the user said/did that prompted the correction.
- **Rule** — the actionable rule to follow from now on.
- **Why** — the load-bearing reason, so future-me doesn't re-litigate it.

---

## 2026-06-21 — Grilling questions in Russian, artifacts in English

- **Trigger** — During an `/improve-codebase-architecture` run, after I delivered the
  HTML report (in English) and started the grilling loop (in English), the user said:
  "ask grilling questions in russian, create artifacts in English".
- **Rule** — For this user: **all conversational/dialogue output (grilling questions,
  clarifying questions, plan-mode discussions, plain-text explanations) goes in Russian.**
  **All artifacts (HTML reports, CONTEXT.md, ADRs, code, code comments, commit messages,
  PR descriptions, docs) stay in English.**
- **Why** — The user is more comfortable reasoning through design decisions in Russian,
  but the repo is English-language and must stay so. The split is conversation-vs-artifact,
  not topic-dependent. Don't mix languages inside a single artifact (no Russian prose in
  an English ADR, no English explanation when the user asked a Russian question).

### How to apply
- Grilling, plan-mode, /teach, /domain-modeling, /grill-with-docs conversations: Russian.
- HTML reports, ADRs, CONTEXT.md, code, comments, README updates, commit/PR text: English.
- If unsure which category an output falls into, ask.
