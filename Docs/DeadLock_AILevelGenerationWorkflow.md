# DeadLock AI Level Generation Workflow

이 문서는 AI로 DeadLock 레벨 후보를 만들고 Unity Level Editor에서 검증하는 실제 작업 절차를 정리한다.

## 한 줄 요약

1. Codex에게 조건을 주고 `LevelGenerationCandidate` JSON을 생성시킨다.
2. JSON을 `.json` 파일로 저장한다.
3. Unity Level Editor에서 `AI 후보 가져오기`로 import한다.
4. 성공하면 생성된 `LevelSO`를 편집하고, 실패하면 report를 Codex에게 다시 주고 JSON을 수정시킨다.

## 좋은 생성 요청 예시

기본형은 보드 크기, 색, process/resource 수, 필수/허용/금지 Rule, 목표 라운드, 목표 난이도를 함께 주는 것이다.

```text
DeadLock 레벨 후보 JSON 하나 만들어줘.

조건:
- 보드 크기: 5x6
- ColorId: 1, 2, 3 사용
- Process: 3개, 각 2~3 슬롯
- Resource: 5개
- 필수 Rule: ColorSwitch 1개 이상
- 허용 Rule: Basic, ColorSwitch
- 금지 Rule: Clock, Simultaneous, Relay
- 목표 최적 라운드: 4~6
- 목표 난이도: 2.0~3.0
- 튜토리얼 후반 느낌으로, ColorSwitch 순서가 핵심이 되게 만들어줘.
```

복합 Rule을 테스트하고 싶으면 핵심 상호작용을 명시한다.

```text
DeadLock 레벨 후보 JSON 하나 만들어줘.

조건:
- 보드 크기: 6x7
- ColorId: 1~4
- Process: 4개, 각 3 슬롯
- Resource: 6개
- 필수 Rule: Clock OnToOff, Simultaneous
- 허용 Rule: Basic, Clock, Simultaneous
- 금지 Rule: Relay Link, Relay Transfer, ColorSwitch, EmptyColor
- Clock + Simultaneous 조합이 핵심이어야 함
- 목표 최적 라운드: 5~7
- 목표 난이도: 3.5~4.2
```

Relay를 쓰는 요청은 Sender 제약을 같이 적는다.

```text
DeadLock 레벨 후보 JSON 하나 만들어줘.

조건:
- 보드 크기: 5x7
- ColorId: 1, 2, 3
- Process: 3개, 각 2 슬롯
- Resource: 5개
- 필수 BoardRule: Relay Transfer 1개
- 허용 ResourceRule: Basic, EmptyColor
- Transfer Sender resource는 capacity 1이어야 함
- Sender 색 전달이 Receiver 접근에 반드시 필요해야 함
- 목표 최적 라운드: 4~6
- 목표 난이도: 3.0~3.8
```

## Unity로 가져오는 방법

1. Codex가 출력한 JSON만 복사해 `.json` 파일로 저장한다.
   - 예: `Generated_ColorSwitch_Intro_01.json`
   - Unity `.asset` YAML을 직접 만들지 않는다.
2. Unity에서 `Tools > DeadLock > Levels > Level Editor`를 연다.
3. 상단 `AI 후보 가져오기` 버튼을 누른다.
4. 저장한 JSON 파일을 선택한다.
5. 성공하면 `Assets/02.Scripts/02.Repository/Levels/Generated` 아래에 `LevelSO`가 생성되고, Level Editor가 자동으로 해당 레벨을 연다.
6. 실패하면 Level Editor 하단 `AI 후보 가져오기` 로그와 Unity Console에 실패 report가 표시된다.

## Unity 검증에서 하는 일

Import 도구는 후보 JSON을 바로 정답으로 취급하지 않는다.

- JSON을 임시 `LevelSO`로 읽는다.
- `LevelSOMapper`로 Domain `LevelDefinition`으로 변환한다.
- `LevelDefinitionValidator`로 데이터 오류를 찾는다.
- `LevelSolutionFinder`로 성공 가능한 해를 찾는다.
- `LevelDifficultyAnalyzer`로 난이도를 계산한다.
- 성공 해가 있을 때만 `Generated` 폴더에 `LevelSO`를 저장한다.

최적 해가 증명되지 않았더라도 성공 해가 있으면 저장할 수 있다. 이 경우 report에 경고가 남는다.

## 실패했을 때 다시 요청하는 방법

실패 report를 그대로 붙이고, 같은 컨셉 유지 여부를 말한다.

```text
이 후보 import가 실패했어.
아래 Unity report를 기준으로 같은 컨셉을 유지하면서 JSON을 수정해줘.

[여기에 Level Editor 하단 AI 후보 가져오기 로그 또는 Unity Console report 붙여넣기]
```

컨셉을 바꿔도 된다면 이렇게 말한다.

```text
이 후보는 구조적으로 안 풀리는 것 같아.
아래 Unity report를 참고해서 조건은 유지하되 새 후보로 다시 설계해줘.

[report]
```

## 실패 유형별 수정 방향

- `Validation` 실패: id 중복, 위치 범위, ColorId 0, 빈 rule list, Relay 참조 오류 같은 데이터 무결성을 먼저 고친다.
- `NoSolution`: process slot 색, resource 색, capacity, rule 조합, relay 방향, 위치를 조정해 성공 가능한 route를 만든다.
- 난이도 너무 낮음: 대기, 거리, rule 상호작용, 선택지 제한을 늘린다.
- 난이도 너무 높음: 거리를 줄이고, 복합 rule 수를 줄이고, Basic 대체 route를 조금 더 명확히 둔다.
- RelayTransfer 실패: sender가 relay endpoint 중 하나인지, sender capacity가 1인지, receiver 접근에 전달색이 실제로 필요한지 확인한다.
- Clock 실패: deadline 안에 해당 process가 모든 slot을 완료할 수 있는지 확인한다.
- Simultaneous 실패: capacity를 채울 만큼 같은 resource에 도착할 수 있는 process slot이 있는지 확인한다.

## 추천 작업 루프

```text
Profile/조건 작성
-> Codex가 후보 JSON 생성
-> Unity Level Editor에서 AI 후보 가져오기
-> 성공하면 LevelSO 편집/저장
-> 실패하면 report를 Codex에게 전달
-> Codex가 JSON 수정
-> 다시 import
```

처음에는 후보를 한 번에 여러 개 만들기보다 하나씩 만들고 검증하는 편이 좋다. 실패 원인을 좁히기 쉽고, solver/analyzer report를 다음 후보에 더 정확히 반영할 수 있다.
