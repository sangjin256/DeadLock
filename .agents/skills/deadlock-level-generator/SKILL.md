---
name: deadlock-level-generator
description: Generate DeadLock puzzle level candidate JSON from a LevelGenerationProfile. Use when asked to design, generate, revise, or iterate AI-created DeadLock levels, puzzle candidates, stage drafts, or LevelSO-compatible JSON using project rules, solver feedback, target difficulty, target optimal rounds, required/allowed/forbidden ResourceRules, BoardRules, Relay Link/Transfer, ColorId constraints, or LevelGenerationProfile files.
---

# DeadLock Level Generator

Use this skill to create or revise DeadLock level candidate JSON. Do not create Unity `.asset` YAML. Produce JSON that can later be imported and verified by Unity tooling.

## Core Workflow

1. Read the requested `LevelGenerationProfile`.
2. Load `references/level-generation-workflow.md` for the generation and revision loop.
3. Load `references/deadlock-level-json.md` for the candidate JSON contract.
4. Generate one candidate at a time unless the user asks for multiple alternatives.
5. Keep every color as a `ColorId` integer. Do not invent hex colors, sprites, materials, or visual palette data.
6. Treat the candidate as unverified until Unity `LevelDefinitionValidator`, `LevelSolutionFinder`, and `LevelDifficultyAnalyzer` results are available.

## Output Rules

- Output a single fenced `json` block for the candidate unless the user asks for explanation first.
- Include a short Korean summary before the JSON only when useful.
- Keep IDs deterministic and position-based where practical.
- Include `generationNotes` explaining intended core trick, required rule usage, expected difficulty, and validation assumptions.
- If the user provides Unity validation feedback, revise the same candidate instead of starting from scratch unless the failure makes the design invalid.

## References

- Use `references/level-generation-workflow.md` for profile reading, self-checks, and feedback iteration.
- Use `references/deadlock-level-json.md` for field names, rule names, and the minimal JSON shape.

