# DeadLock LevelGenerationProfile

`LevelGenerationProfile`은 AI 기반 레벨 생성을 위한 입력 조건표다. 실제 레벨 데이터나 Unity `ScriptableObject`가 아니라, Codex/AI가 어떤 퍼즐 후보를 만들지 이해하기 위한 제작 지시서 역할을 한다.

## 목적

- AI에게 보드 크기, 색 수, process/resource 범위, 필수/허용/금지 Rule, 목표 난이도, 목표 최적 라운드를 전달한다.
- 생성된 후보는 곧바로 정답으로 보지 않고 Unity Editor의 `LevelSolutionFinder`와 `LevelDifficultyAnalyzer`로 검증한다.
- 검증에 실패하거나 목표 범위를 벗어나면 AI가 profile 조건을 유지한 채 후보를 수정하는 반복 루프로 사용한다.
- 챕터는 필수 구조가 아니다. 필요하면 `chapterId`와 `chapterName` metadata로만 표현한다.

## 생성 루프

1. 제작자가 `LevelGenerationProfile`을 선택하거나 작성한다.
2. Codex/AI가 `deadlock-level-generator` 스킬로 profile을 읽고 후보 레벨 데이터를 만든다.
3. 후보는 `Docs/LevelGenerationCandidate.schema.json`을 따르는 `LevelGenerationCandidate` JSON이어야 한다.
4. Unity Editor에서 `LevelSOMapper`, `LevelDefinitionValidator`, `LevelSolutionFinder`, `LevelDifficultyAnalyzer`로 검증한다.
5. 실패 사유나 난이도 편차를 AI에게 돌려주고 후보를 다시 생성한다.

## Profile 구성

- `profileId`, `displayName`, `description`: 사람이 구분하기 위한 이름과 설명.
- `metadata`: 선택적 챕터/태그 정보. 챕터 전용 검증 UI를 만들기 위한 데이터가 아니라 생성 의도를 설명하는 보조 정보다.
- `board`: row/column 고정값 또는 허용 범위.
- `colors`: 색 종류 수와 사용할 수 있는 `ColorId` 범위.
- `processes`: process 개수와 process별 slot 개수 범위.
- `resources`: resource 개수와 capacity 범위.
- `rules`: 필수/허용/금지 resource rule, board rule, relay type.
- `difficulty`: 목표 난이도 점수와 목표 최적 라운드 범위.
- `solverLimits`: 생성 후보 검증에 사용할 최대 라운드와 탐색 제한.
- `notes`: AI에게 전달할 자유 기획 메모.

## Rule 표현

Resource rule 이름은 Unity authoring enum인 `ELevelResourceRuleType`과 맞춘다.

- `Basic`
- `ColorSwitch`
- `EmptyColor`
- `Clock`
- `Simultaneous`

Relay type은 Domain `ERelayType`과 맞춘다.

- `Link`
- `Transfer`

`required`는 생성 후보에 반드시 포함되어야 하는 요소다. 예를 들어 ColorSwitch 소개 레벨은 `requiredResourceRules`에 `ColorSwitch`를 넣는다. `allowed`는 사용할 수 있는 전체 집합이고, `forbidden`은 이번 profile에서 쓰면 안 되는 요소다.

## 범위 밖

- `LevelGenerationProfile`은 `LevelSO` 저장 구조를 대체하지 않는다.
- v1에서는 Unity Editor UI, ScriptableObject, 후보 자동 import 버튼을 만들지 않는다.
- 챕터 전용 검증 패널이나 batch 분석 UI는 현재 우선순위에서 제외한다.
- 실제 색상 hex, 아이콘, 머티리얼 정보는 profile에 넣지 않는다. 색은 계속 `ColorId` 기준으로만 제한한다.

## 연결된 스킬

`D:/DeadLock/.agents/skills/deadlock-level-generator`는 이 profile을 입력으로 받아 `LevelGenerationCandidate` JSON을 작성하는 프로젝트 로컬 Codex 스킬이다. 이 스킬은 Unity `.asset`을 직접 만들지 않고, 다음 단계의 import/검증 도구가 읽을 수 있는 후보 JSON만 만든다.

실제 생성 요청 예시, Unity import 절차, 실패 report를 다시 AI에게 전달하는 방법은 `Docs/DeadLock_AILevelGenerationWorkflow.md`에 기록한다.
