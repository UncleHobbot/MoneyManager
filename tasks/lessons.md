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

---

## 2026-06-23 — Validate bank-format detection against REAL exports, not idealized ones

- **Trigger** — User reported CIBC import failing on `in/cibc black 2026-06-14.csv`.
  The file format was correct, but `ImportEndpoints.DetectBankType` rejected it.
  The heuristic `firstLine.Any(char.IsLetter)` claimed "CIBC first row is purely
  numeric/data" — false. CIBC rows always contain text descriptions
  (e.g. `"COSTCO WHOLESALE W521 BROSSARD, QC"`). The endpoint returned 400
  "Unable to determine the bank type" for every real CIBC upload that used the
  default "Auto" dropdown.
- **Rule** — When writing any format-detection heuristic (bank CSV, file magic
  bytes, header sniffing), the test corpus MUST include a real first line from
  an actual export, not a hand-rolled minimal one. Hand-rolled lines often
  miss properties that are universal in real data (text inside quotes, BOM,
  CRLF, masked account numbers, currency suffixes). Add a `BankDetectionTests`
  case per bank using a real first line verbatim.
- **Why** — The default frontend mode is "Auto", which routes everything
  through detection. A detection regression is effectively an outage for that
  bank's users — and they can't fix it without manually picking the bank from
  the dropdown, which they may not realize is the workaround. Prefer the
  robust pattern: identify each known format by its UNIQUE header signature,
  then default to the no-header format as the fallback (validation rejects
  true garbage). Never use negative shape heuristics like "no letters" — they
  break the moment real data has more variety than you imagined.

### How to apply
- Adding/changing any `DetectBankType` / sniffing logic: include a test that
  uses a real first line from `in/*.csv` or an archived `csv-archive/*.csv`.
- Negative heuristics ("doesn't contain X", "no letters") are a smell — prefer
  positive identification of known formats plus a sane default.
- For `DetectBankType` specifically: Mint/RBC by header keywords, then fall
  through to `CibcImporter` (the only no-header format). `CibcImporter.Validate`
  catches garbage.
