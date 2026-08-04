# Migrated Level Replacement Plan

## Goal

Replace migrated levels that either found no successful solution with the current Solver or reached the editor-equivalent search limit. Keep the original assets as `[Obsolete]` references and create new canonical `Level_Cxx_Syy` assets in their places.

The standard Chapter 1-5 path is limited to a `LevelDifficultyAnalyzer` score below `4.0`. Stages 11-15 are intentionally reserved for a later expert extension and may target `4.0` or higher.

## Archived Assets

- C03: S08, S09
- C04: S09, S10
- C05: S02 through S10

`Level_C03_S08_Alt01` is not archived because the current Solver found a successful solution. It remains an alternate design reference.

## Chapter Rule Progression

| Chapter | Primary new concept | Allowed prior concepts | Notes |
| --- | --- | --- | --- |
| C01 | Basic / Capacity | None | Foundation for color matching and contention. |
| C02 | Simultaneous | Basic | Introduce capacity-fill completion. |
| C03 | Clock | Basic, Simultaneous | Teach timing before adding dense combinations. |
| C04 | ColorSwitch | Basic, Simultaneous, Clock | Teach color-cycle timing and strict FIFO. |
| C05 | EmptyColor | Basic, Simultaneous, Clock, ColorSwitch | Teach first-use color commitment. |

Relay Link and Relay Transfer are not required for these migrated Chapter 1-5 replacements. They should be introduced in later dedicated chapters instead of being mixed into the existing legacy progression.

## Difficulty Bands

Use these bands as authoring targets, then tune against the retained neighboring stages after validation. A stage must remain below `4.0`.

| Replacement | Target difficulty | Design intent |
| --- | ---: | --- |
| C03 S08 | 3.0 - 3.4 | Clock timing with one readable prior-rule interaction. |
| C03 S09 | 3.3 - 3.8 | Chapter Clock finale without an opaque timing trap. |
| C04 S09 | 3.1 - 3.5 | ColorSwitch queue planning with limited prior-rule pressure. |
| C04 S10 | 3.4 - 3.9 | ColorSwitch chapter finale with a clear, teachable schedule. |
| C05 S02 | 1.4 - 1.7 | EmptyColor first-use commitment. |
| C05 S03 | 1.5 - 1.9 | Two matching slots share the newly fixed color. |
| C05 S04 | 2.3 - 2.5 | Shared fixed color followed by basic capacity pressure. |
| C05 S05 | 2.7 - 3.0 | Shared fixed color with a stronger, readable capacity decision. |
| C05 S06 | 2.8 - 3.0 | More follow-up capacity pressure after a shared fixed color. |
| C05 S07 | 3.1 - 3.3 | Multiple commitment paths with readable consequences. |
| C05 S08 | 3.4 - 3.6 | EmptyColor and timing interaction. |
| C05 S09 | 3.6 - 3.8 | Multi-process planning without hidden requirements. |
| C05 S10 | 3.8 - 3.9 | EmptyColor chapter finale using an intentional rule combination. |

## Acceptance Criteria

Every replacement candidate must satisfy all of the following before it receives the canonical asset name.

1. The current chapter's primary rule is required by the solution, not merely present as decoration.
2. `LevelDefinitionValidator` succeeds.
3. `LevelSolutionFinder` finds at least one successful solution with the normal Level Editor limits.
4. `LevelDifficultyAnalyzer` falls within the target band and below `4.0`.
5. An `Auto Optimal` test case is generated and its simulation succeeds.
6. The result is reviewed against the immediately preceding and following stage to preserve the chapter curve.

## Validated Replacements

| Level | Difficulty | Best clear rounds | Status |
| --- | ---: | ---: | --- |
| C03 S08 | 3.1 | 4 | Promoted |
| C03 S09 | 3.3 | 5 | Promoted |
| C04 S09 | 3.5 | 8 | Promoted after one overly difficult draft was rejected. |
| C04 S10 | 3.7 | 12 | Promoted |
| C05 S02 | 1.4 | 2 | Promoted |
| C05 S03 | 1.5 | 1 | Promoted |
| C05 S04 | 2.4 | 3 | Promoted |
| C05 S05 | 2.9 | 4 | Promoted |
| C05 S06 | 2.9 | 4 | Promoted; final curve review should separate it from S05. |
| C05 S07 | 3.1 | 4 | Promoted after two difficulty revisions. |
| C05 S08 | 3.5 | 4 | Promoted; EmptyColor split commitment. |
| C05 S09 | 3.8 | 5 | Promoted; EmptyColor plus OffToOn timing. |
| C05 S10 | 3.9 | 5 | Promoted after three difficulty revisions; chapter finale. |

Candidate JSON files that pass promotion move to `GeneratedCandidates/Replacement/Validated`. Rejected drafts remain in the corresponding `Rejected` folders with their generated asset and source JSON preserved for reference.

## Later Expert Extension

`Level_Cxx_S11` through `Level_Cxx_S15` are intentionally not created in this pass. They will form a separate expert extension with difficulty `4.0+`, deliberate multi-rule combinations, and a separate progression review.
