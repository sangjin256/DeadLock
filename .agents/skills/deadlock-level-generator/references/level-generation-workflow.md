# Level Generation Workflow

## Inputs

Read the user's prompt and any supplied `LevelGenerationProfile` JSON. If no profile is pasted, ask for a profile or use an existing profile file only when the user names it.

Required profile signals:

- Board row/column range.
- Color count and allowed `ColorId` range.
- Process count and slot count per process.
- Resource count and capacity range.
- Required, allowed, and forbidden ResourceRules.
- Required, allowed, and forbidden BoardRules and RelayTypes.
- Target difficulty score and target optimal round count.
- Solver limits for later Unity validation.

## Candidate Creation

Create a candidate that can become a `LevelSO`:

1. Choose board size within profile bounds.
2. Choose `ColorId` values inside the allowed range.
3. Place process/resource nodes at integer row/column positions inside the board.
4. Create process slots with `id`, `requiredColorId`, and `selectionOrder`.
5. Create resources with `initialColorId`, `capacity`, and one or more rule entries.
6. Add required Relay Link/Transfer only when allowed by the profile.
7. For Relay Transfer, set `senderResourceId` to one endpoint resource with capacity 1.
8. Add optional test seed assignments only when they help explain the intended route.

## Design Heuristics

- Required rules must be necessary to the intended solution, not decorative.
- Avoid using forbidden rules even if they would make generation easier.
- Keep Basic tutorial levels visually obvious and low pressure.
- For ColorSwitch, make order matter through current color changes.
- For Clock, ensure the intended path has plausible deadline pressure.
- For Simultaneous, ensure enough process slots can fill the capacity.
- For Relay Link, make choosing one resource meaningfully block another.
- For Relay Transfer, make Sender color delivery necessary for Receiver access.

## Self-Check Before Output

Before returning JSON, check:

- No duplicate process/resource/relay IDs.
- No process and resource share the same row/column.
- All positions are inside board bounds.
- No `ColorId` is 0.
- Every resource has at least one rule.
- Required rules appear at least once.
- Forbidden rules do not appear.
- Relay endpoints exist and are resources.
- Relay Transfer sender is one endpoint and has capacity 1.
- The design has a plausible route within `targetOptimalRoundCount`.

## Feedback Iteration

When Unity validation feedback is provided:

- If `LevelDefinitionValidator` fails, fix data integrity first.
- If no solution is found, change assignments, resource colors, capacities, or layout while preserving profile constraints.
- If difficulty is too low, add pressure through distance, waiting, rule interaction, or fewer alternative routes.
- If difficulty is too high, reduce rule interactions, shorten distances, or add clearer Basic alternatives.
- Preserve the core concept unless the user asks for a new candidate.

