# DeadLock Level Candidate JSON

Output candidate JSON using the same naming style as `LevelSO` authoring data. The canonical schema is `Docs/LevelGenerationCandidate.schema.json`.

## Top-Level Shape

```json
{
  "id": 10001,
  "name": "Generated_ColorSwitch_Intro_01",
  "rowCount": 5,
  "columnCount": 6,
  "processDataList": [],
  "resourceDataList": [],
  "relayDataList": [],
  "starThresholdData": {
    "threeStarRoundCount": 4,
    "twoStarRoundCount": 6,
    "oneStarRoundCount": 8
  },
  "testCaseDataList": [],
  "generationNotes": []
}
```

## Process Data

Each process uses integer position and slot data.

```json
{
  "id": 0,
  "row": 0,
  "column": 1,
  "slotDataList": [
    {
      "id": 0,
      "requiredColorId": 1,
      "selectionOrder": 0
    }
  ]
}
```

## Resource Data

Every resource must have at least one rule. `Basic` maps to the current no-special-rule resource behavior.

```json
{
  "id": 8,
  "row": 2,
  "column": 3,
  "initialColorId": 1,
  "capacity": 1,
  "ruleDataList": [
    {
      "ruleType": "ColorSwitch",
      "colorIdList": [1, 2, 3],
      "clockMode": null,
      "clockRoundCount": null
    }
  ]
}
```

Resource rule names:

- `Basic`
- `ColorSwitch`
- `EmptyColor`
- `Clock`
- `Simultaneous`

Clock modes:

- `OnToOff`
- `OffToOn`

## Relay Data

Relay type names:

- `Link`
- `Transfer`

For `Link`, set `senderResourceId` to 0. For `Transfer`, `senderResourceId` must be either `firstResourceId` or `secondResourceId`, and that sender resource must have capacity 1.

```json
{
  "id": 0,
  "relayType": "Transfer",
  "firstResourceId": 8,
  "secondResourceId": 14,
  "senderResourceId": 8
}
```

## Test Seed

`testCaseDataList` is optional in generated candidates. Use it only as a suggested route seed, not as proof of correctness.

```json
{
  "name": "AI Seed",
  "maxRoundCount": 8,
  "expectedEndState": "Succeeded",
  "assignedConnectionDataList": [
    {
      "processId": 0,
      "slotId": 0,
      "resourceId": 8
    }
  ]
}
```

