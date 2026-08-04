# DeadLock 개발 현황

## 현재 상태

- 프로젝트 분석 결과 `GameManager`가 주요 legacy coordination hotspot으로 확인되었다.
- DDD/MVP 마이그레이션, 로컬 문서, 비주얼 피드백, 수동 Unity 검증을 지원하는 로컬 프로젝트 스킬을 등록했다.
- FEEL, DOTween, Shapes, UniTask는 프로젝트에 들어와 있으며 프레젠테이션/이펙트 경계에서만 사용한다.
- Steam, Android, iOS 같은 플랫폼 구현은 `06.Infrastructure`로 분리하는 방향으로 정했다.
- 새 코드 루트로 `Assets/02.Scripts/01.Domain`부터 `06.Infrastructure`까지 계층 폴더를 생성했다.
- 도메인 설계에서 자원 단일 규칙은 `IResourceRule`, 보드 범위 관계 규칙은 `IBoardRule`로 분리하기로 정했다.
- Relay는 자원 타입이 아니라 `RelayRelation` 데이터와 `RelayLinkRule`, `RelayTransferRule` 같은 보드 Rule로 구현하기로 정했다.
- `Assets/Outdated/scripts`의 legacy 런타임 코드는 `DEADLOCK_LEGACY_OUTDATED` 심볼로 감싸 기본 컴파일에서 제외했다.
- 기존 `LevelCreator` 인스펙터와 `BatchRename`의 `LevelCreator` 직접 참조는 `DEADLOCK_LEGACY_LEVEL_EDITOR` 심볼로 비활성화했다.
- `Assets/02.Scripts/01.Domain` 아래에 `Board`, `ProcessNode`, `ProcessColorSlot`, `ResourceNode`, `Connection`, `LevelDefinition`, `LevelProgress` 등 순수 Domain 골격을 작성했다.
- `ColorId`, `ConnectionContext`, `RuleEffects`, `ResourceFocusInfo`, `ResourceFocusInfoBuilder`, `AssignConnectionResult` 같은 도메인 보조 타입을 작성했다.
- `IResourceRule`, `IBoardRule`, `NoResourceRule`, `RelayRelation`, `RelayLinkRule`, `RelayTransferRule`을 작성해 내부 자원 Rule과 보드 범위 Rule의 기본 경계를 세웠다.
- Relay Transfer의 의미는 방향성 `Sender -> Receiver` 색 전달로 정정했다. Receiver는 예약 단계에서 전달 색 후보로 연결할 수 있고, Sender를 실제 점유한 슬롯 색이 Receiver의 임시 색이 된다. Sender 반환 시 전달 색이 해제되며, Receiver가 이미 점유 중이면 기존 점유 색을 유지하고 반환 뒤 변경을 적용한다.
- `ProcessNode`는 `List<ProcessColorSlot>`을 직접 받는 소유권 이전 방식으로 정리했고, `ColorId`는 `Equals`, `GetHashCode`, `==`, `!=` 비교를 지원한다.
- `Assets/Outdated/scripts` 런타임 분석 결과, 레거시 핵심 규칙은 "프로세스가 리소스 슬롯을 점유하면 해당 프로세스의 모든 작업이 완료될 때까지 리소스를 반환하지 않는다"는 구조로 확인했다.
- 레거시 waiting은 재선택 상태가 아니라 리소스 `waitingList`에 머무는 상태다. 슬롯이 풀리면 대기 항목이 다음 라운드 맨 앞으로 재투입되고, waiting 중인 프로세스는 다른 리소스로 진행하지 않는다.
- 레거시에서는 시작 전 연결 가능 여부와 시뮬레이션 중 실제 연결 가능 여부가 다르다. 시작 전에는 리소스가 해당 색을 처리할 수 있는지 확인하고, 시뮬레이션 중에는 현재 리소스 상태, capacity, 프로세스 상태를 기준으로 판정한다.
- 레거시 리소스는 단일 타입 enum보다 `ResourceNode.Capacity`와 내부 Rule 조합에 가깝다. `Basic`, `Simultaneous`, `ColorSwitch`, `EmptyColor`, `Clock`은 `IResourceRule` 계열 조합으로 흡수하는 방향이 맞다.
- `DeadLock_GameDesign.md`와 `DeadLock_Architecture.md`에 레거시 라운드, 리소스 점유, waiting, 시작 전/시뮬레이션 중 연결 판정 규칙을 최신화했다.
- `Board.AssignConnection()`은 예약 전용 흐름으로 정리했고, `RunSimulation(int maxRoundCount)` 기반 최소 라운드 실행 흐름을 추가했다.
- `BoardPosition`, `RoundScheduleItem`, `WaitingRequest`, `RoundResult`, `SimulationReport`를 추가해 거리순 스케줄, waiting 재투입, 시뮬레이션 결과 보고의 최소 기반을 만들었다.
- `IResourceRule`과 `IBoardRule`은 예약 검증(`CanReserve`)과 실제 점유 검증(`CanOccupy`)을 분리했다.
- `ResourceNode`는 실제 점유 목록, waiting queue, Relay Transfer 임시 색과 점유 중 색 latch를 갖도록 보강했다.
- enum은 C# 컨벤션에 맞춰 개별 파일로 분리했다.
- `ColorSwitchRule`, `EmptyColorRule`, `ClockRule`, `SimultaneousRule`을 추가해 내부 자원 Rule의 첫 구현을 완료했다.
- `IResourceRule.ResetSimulationState()`를 추가해 `Board.RunSimulation()` 시작 시 리소스 상태와 Rule 내부 상태를 함께 초기화한다.
- `RuleEffects`와 `RoundResult`에 실패 보고를 추가했다. Clock OnToOff가 닫힐 때 점유 중인 미완료 프로세스가 있으면 관련 프로세스는 `Failed`, connection은 `Blocked`가 되고 시뮬레이션은 `Failed`로 종료된다.
- ColorSwitch는 정책 enum 없이 고정 동작으로 정했다. 반환 시 다음 색으로 전환하고, 리소스가 비어 있으며 해당 라운드에 점유 성공이 없으면 라운드 종료 때도 다음 색으로 전환한다.
- `Common`은 `Values`와 `Enums`, `Rules`는 `Contracts`, `Core`, `Resource`, `Board`, `Relations`, `Focus` 하위 폴더로 정리했다.
- ColorSwitch waiting 정책은 색 기반 우선순위 없이 strict FIFO로 확정했다. waiting head가 현재 색과 맞지 않아 실패해도 뒤 항목을 먼저 재투입하지 않는다.
- RelayTransfer Sender resource는 capacity 1만 허용하기로 확정했다. 이 제약은 인게임 Rule 방어가 아니라 레벨 에디터/ScriptableObject authoring 검증에서 보장한다.
- `LevelDefinition`을 순수 정의 데이터로 전환했다. 이제 `ProcessNode[]`, `ResourceNode[]`, `IBoardRule[]` 같은 런타임 객체를 직접 들지 않고 `ProcessDefinition`, `ResourceDefinition`, `BoardRuleDefinition` 계열 데이터를 보관한다.
- `LevelBoardFactory`를 추가해 `LevelDefinition`에서 매번 새 `Board`, `ProcessNode`, `ResourceNode`, Rule 인스턴스를 생성하도록 분리했다.
- `LevelDefinitionValidator`를 추가해 board size, id 중복, position 범위, slot 색, resource capacity, ColorSwitch/Clock 설정, Relay 참조, RelayTransfer Sender capacity 1 제약을 authoring 검증 단계에서 보고한다.
- `ColorId`는 실제 색상 코드가 아니라 규칙 판정용 ID로 유지한다. 실제 색상, 아이콘, 머티리얼, 색약 보정 팔레트는 이후 ScriptableObject/VisualSettings/View 계층에서 `ColorId`에 매핑한다.
- Unity 레벨 authoring 에셋 이름은 `LevelSO`로 정했다. `LevelSO` 하나가 스테이지 하나를 저장하며, process/resource/relay/test case 내부 데이터는 `LevelProcessData`, `LevelResourceData`, `LevelRelayData`, `LevelTestCaseData`처럼 `SO` 접미사 없는 직렬화 데이터로 둔다.
- `Assets/02.Scripts/02.Repository/Levels`에 `LevelSO`, `LevelProcessData`, `LevelProcessSlotData`, `LevelResourceData`, `LevelRelayData`, `LevelTestCaseData`, `LevelAssignedConnectionData`, `ELevelResourceRuleType`을 추가했다.
- Repository 레벨 authoring 타입은 Unity 직렬화용이므로 `UnityEngine`을 참조하지만, Domain의 `LevelDefinition`과 런타임 객체는 참조하지 않는다.
- 새 `LevelSO` 계열 타입은 프로젝트 C# 컨벤션에 맞춰 `[SerializeField]`를 필드 위 별도 줄에 두고, private backing field 바로 다음 줄에 getter를 배치했다.
- `LevelSOMapper`를 추가해 `LevelSO` authoring 데이터를 Domain `LevelDefinition`으로 변환하는 경계를 만들었다. Mapper는 검증을 호출하지 않고, 호출자가 필요 시 `LevelDefinitionValidator`를 직접 실행한다.
- 레거시의 복합 resource rule을 표현하기 위해 `CompositeResourceRule`을 추가했다. `ResourceNode`는 계속 하나의 `IResourceRule`만 소유하고, 여러 rule 조합은 composite이 내부 rule 목록을 순서대로 실행한다.
- `ResourceDefinition`은 단일 `ResourceRuleDefinition` 대신 `ResourceRuleDefinition[]`을 가진다. `LevelResourceData`도 단일 rule 필드 대신 `LevelResourceRuleData` 목록을 저장하고, `LevelSOMapper`와 `LevelBoardFactory`가 이를 다중 rule 구조로 변환한다.
- `LevelDefinitionValidator`는 resource rule 목록의 empty/null, ColorSwitch 색 목록, Clock round count, 중복 Clock 조합을 최종 authoring 검증 오류로 보고한다.
- 레벨 에디터는 uGUI가 아니라 UI Toolkit 기반 EditorWindow로 만든다. grid canvas, palette, 선택 항목 inspector, validation log, simulation log를 갖춘 전용 제작 도구를 목표로 한다.
- 레벨 에디터 제작 중 즉시 UX 검증은 `LevelSO` 데이터를 직접 보고 처리하고, 저장/테스트/게임 시작 전 최종 검증은 `LevelSOMapper.ToLevelDefinition()` 뒤 `LevelDefinitionValidator.Validate()`로 수행한다.
- 자동 난이도 테스트는 test case 기반 라운드 재생으로 시작한다. 저장된 예약 연결 목록을 `Board.AssignConnection()`에 적용하고 `Board.RunSimulation()` 결과를 round-by-round로 보여준 뒤, 클리어 라운드 수와 waiting/relay/clock 지표를 난이도 분석으로 확장한다.
- `Assets/Outdated/Levels`에는 기존 `LevelCreator` 기반 레벨 에셋이 61개 남아 있다. 새 레벨 에디터가 완성되더라도 이 데이터를 수동 재작성하지 않고, 레거시 `LevelCreator` 에셋을 새 `LevelSO`로 변환하는 호환 마이그레이션 도구를 작업 순서에 포함한다.
- 레거시 `Node.colors`는 실제 Unity 색상값이므로 새 Domain의 `ColorId`와 직접 같지 않다. 변환 단계에서는 기존 색상값을 새 `ColorId` 팔레트로 매핑하고, `maxCount`, `fixedNum`, `isSimul`, `isSwitchColor`, `isStartWithEmptyColor`, `isClockOnToOff`, `isClockOffToOn`, `clockNum`을 새 process/resource/rule/test data로 해석한다.
- `Tools/DeadLock/Levels/Migrate Legacy Levels` 메뉴로 실행하는 Editor 전용 마이그레이션 도구를 추가했다. 변환기는 레거시 타입을 직접 참조하지 않고 YAML을 읽어 `LevelSO`를 생성한다.
- 변환 결과는 기본적으로 `Assets/02.Scripts/02.Repository/Levels/Migrated`에 생성된다. 색상은 전체 대상 에셋의 첫 등장 순서대로 `ColorId` 1부터 자동 매핑하고, 매핑표와 validation 결과는 `LegacyLevelMigrationReport.txt`에 남긴다.
- 레거시 `fixedNum`과 활성화된 legacy block field는 이번 작업에서 새 Domain 규칙으로 구현하지 않고 변환 리포트 경고로 남긴다.
- Legacy Level 마이그레이션을 실제 실행해 `Assets/Outdated/Levels`의 61개 `LevelCreator` 에셋을 61개 `LevelSO` 에셋으로 변환했다. 생성된 `.asset.meta`도 61개이며, 원본 `Assets/Outdated/Levels` 에셋은 수정되지 않았다.
- 마이그레이션 결과 검수에서 process 255개, resource 224개가 원본과 변환 결과에서 일치했다. board size, node id, position, slot color id, selection order, resource capacity, 복합 rule 순서, Clock 설정, ColorSwitch 색 목록을 전체 에셋 기준으로 대조했고 오류는 없었다.
- 마이그레이션 색상 매핑은 총 37개 `ColorId`로 생성됐다. 실제 색상 hex는 `LevelSO`에 저장하지 않고 `LegacyLevelMigrationReport.txt`의 `ColorId -> legacy Color32` 매핑표로만 확인한다.
- `fixedNum` 미지원 경고는 17건이며 리포트에 남았다. 활성화된 legacy block field는 발견되지 않았다.
- `Tools/DeadLock/Levels/Level Editor` 메뉴로 여는 UI Toolkit 기반 `LevelEditorWindow` v1을 추가했다. `LevelSO` 선택/생성, 저장, grid canvas, Select/Add Process/Add Resource/Erase 툴, process slot 편집, resource rule 편집, 즉시 검증 로그, 최종 Domain 검증 버튼을 제공한다.
- Level Editor v1은 `LegacyLevelMigrationReport.txt`의 `ColorId -> legacy Color32` 매핑을 읽어 swatch를 표시한다. 실제 색상 hex는 여전히 `LevelSO`에 저장하지 않으며, 매핑이 없는 `ColorId`는 deterministic fallback 색상으로 표시한다.
- Level Editor v1의 검증은 제작 중 빠른 UX 검증과 최종 검증을 분리한다. 빠른 검증은 `LevelSO` 데이터를 직접 보고, 최종 검증 버튼은 `LevelSOMapper.ToLevelDefinition()` 뒤 `LevelDefinitionValidator.Validate()` 결과를 표시한다.
- Level Editor v1.5로 UX를 개선했다. 좌측에 항상 보이는 `ColorId` 팔레트를 두고, 선택 색으로 Process/Resource를 배치하거나 slot/resource/ColorSwitch 색에 적용할 수 있게 했다.
- Board는 단순 버튼 그리드 대신 canvas 스타일로 바꿨다. Process는 원형, Resource는 사각형으로 렌더링하고, 선택 cell, 겹침, invalid color/capacity/rule 같은 경고 상태를 보드에서 바로 볼 수 있게 했다.
- Board canvas에서는 Process/Resource의 내부 ID를 표시하지 않고, Inspector에서 위치 기반 ID를 읽기 전용으로만 보여준다. 노드 본체는 흰색 베이스로 통일하고 색 정보는 하단 color chip 목록으로 표현한다.
- Level Editor 중앙 canvas를 UI Toolkit `GraphView` 기반으로 전환했다. 노드는 pan/zoom 가능한 공간에서 직접 드래그할 수 있고, 이동 결과는 정수 row/column으로 snap되어 `LevelSO`에 저장된다. GraphView pixel 좌표는 저장하지 않는다.
- Level Editor 보드에서 `Ctrl+C`/`Ctrl+V`로 process/resource 노드를 복제할 수 있게 했다. 복제된 노드는 원본의 slot/rule 데이터를 유지하고 비어 있는 정수 좌표에 배치되며, id는 위치 기반으로 다시 계산한다.
- Level Editor GraphView canvas에 실제 인게임 보드 영역을 보여주는 외곽 테두리, row/column 격자선, 중앙 포인트를 추가했다. 노드는 색 chip을 포함한 전체 박스가 아니라 P/R 본체 중심이 각 셀 중심에 맞도록 배치된다.
- Level Editor Resource 노드에 Rule/Relay 요약 배지를 추가했다. `B`, `SW`, `EM`, `CK`, `xN`, `L`, `TX`, `RX` 배지로 Basic, ColorSwitch, EmptyColor, Clock, Simultaneous, RelayLink, RelayTransfer Sender/Receiver 여부를 보드에서 바로 식별할 수 있다.
- Level Editor에 Relay Link/Transfer 편집 UI를 추가했다. Relay 추가 툴로 Resource 두 개를 순서대로 선택해 Link 또는 Transfer를 생성하고, Transfer Sender는 capacity 1 Resource만 선택할 수 있다.
- Level Editor canvas에 Relay 선 시각화를 추가했다. Link는 보라색 무방향 선, Transfer는 Sender에서 Receiver로 향하는 붉은색 방향 선과 화살표로 표시하며, draft Relay와 선택 Relay는 강조 표시한다.
- 기존 Relay는 Inspector의 Relay 편집 패널에서 타입 변경, Transfer Sender 변경, 삭제를 할 수 있다. endpoint 변경은 v1에서 삭제 후 재생성으로 처리한다.
- Level Editor에 test case 편집과 자동 라운드 재생을 추가했다. test case는 이름, 최대 라운드 수, 예상 종료 상태, 예약 연결 목록을 저장하고, 실행 버튼은 `LevelSOMapper -> LevelDefinitionValidator -> LevelBoardFactory -> Board.AssignConnection() -> Board.RunSimulation()` 흐름으로 Domain 시뮬레이션을 실행한다.
- Test case 실행 로그는 하단 검증 패널에 표시한다. 예약 성공/실패, 최종 상태와 예상 상태 비교, 완료 process, 차단 connection, 라운드별 점유/대기/재투입/완료/반환/실패/차단 목록을 확인할 수 있다.
- Level Editor test case 예약 연결 UX를 보드 기반으로 바꿨다. test case 편집 시작 후 Process의 슬롯 color chip을 선택하고 Resource를 클릭해 예약 연결을 생성/교체하며, 숫자 ID 직접 입력은 노출하지 않는다.
- Test case 편집 중 보드에는 예약 연결선, 슬롯별 예약 순서 번호, Resource별 예약 배지를 표시한다. Resource를 선택하면 해당 Resource에 연결된 Process 슬롯과 예약선이 강조되어 인게임 플레이 순서를 더 쉽게 확인할 수 있다.
- Test case 실행 결과를 라운드별로 보드에서 확인할 수 있게 했다. 실행 후 하단 로그의 라운드 선택 컨트롤로 라운드를 바꾸면 선택 라운드까지 아직 시작되지 않은 connection은 숨기고, 점유는 초록, waiting은 노랑, 차단은 빨강으로 표시하며, 해당 라운드까지 완료된 Process의 연결선은 반투명하게 표시된다.
- `LevelSO`에 `LevelStarThresholdData`를 추가해 3/2/1별 허용 라운드를 저장하게 했다. 별 기준은 Domain 규칙이 아니라 레벨 authoring metadata이며, 테스트 실행 결과에서 실제 클리어 라운드와 비교해 별 평가를 표시한다.
- Editor 전용 `LevelSolutionFinder`를 추가했다. `LevelSO -> LevelDefinition -> Board` 흐름으로 가능한 예약 연결 조합을 탐색하고, 가장 적은 라운드 해와 별 기준 추천값을 보고한다.
- Level Editor의 테스트 케이스 섹션에 `자동 해 찾기 / 별 기준` 패널을 추가했다. 최적 해 찾기, 별 기준 저장, `Auto Optimal` 테스트 케이스 생성/갱신을 수행할 수 있다.
- 다음 검증/제작 자동화 방향을 정했다. 최적 해 찾기와 난이도 평가는 Unity Editor 내부 서비스로 만들고, 자동 레벨 생성은 Codex/AI 스킬이 후보를 만들고 Unity solver가 검증하는 반복 루프로 분리한다.
- AI 레벨 생성 입력은 챕터 전용 profile이 아니라 `LevelGenerationProfile`로 정리했다. 챕터는 필수 구조가 아니라 profile metadata로만 두고, 필수/허용/금지 Rule, 목표 난이도, 목표 라운드는 AI 생성 조건으로 표현한다.
- `Docs/DeadLock_LevelGenerationProfile.md`, `Docs/LevelGenerationProfile.schema.json`, `Docs/LevelGenerationProfile.example.json`을 추가했다. v1은 Unity `ScriptableObject`나 Editor UI가 아니라 문서와 JSON 스키마 중심이다.
- 프로젝트 로컬 Codex 스킬 `deadlock-level-generator`를 추가했다. 이 스킬은 `LevelGenerationProfile`을 읽고 Unity `.asset`이 아니라 `LevelGenerationCandidate` JSON 후보를 작성한다.
- `Docs/LevelGenerationCandidate.schema.json`을 추가해 후보 JSON 계약을 정의했다. 후보 JSON은 `LevelSO` authoring data와 맞춘 필드 이름을 사용하고, 실제 색상 hex 없이 `ColorId` 정수만 사용한다.
- `Tools/DeadLock/Levels/Import Generated Candidate` 메뉴를 추가했다. `LevelGenerationCandidate` JSON을 임시 `LevelSO`로 읽은 뒤 `LevelDefinitionValidator`, `LevelSolutionFinder`, `LevelDifficultyAnalyzer`를 통과한 경우에만 `Assets/02.Scripts/02.Repository/Levels/Generated`에 `LevelSO` 에셋으로 저장한다.
- 생성 후보 import 도구는 Unity `JsonUtility`용 DTO와 `SerializedObject` writer를 사용한다. `LevelSO` 및 내부 authoring data에는 public setter를 추가하지 않았고, 실패한 후보는 에셋을 만들지 않는다.
- Level Editor header에 `AI 후보 가져오기` 버튼을 추가했다. import 성공 시 생성된 `LevelSO`를 현재 편집 대상으로 자동 로드하고, import/검증 report를 하단 로그에 표시한다.
- `Docs/DeadLock_AILevelGenerationWorkflow.md`를 추가했다. Codex에게 레벨 생성을 어떻게 요청할지, 생성된 JSON을 Unity에 어떻게 가져올지, 실패 report를 어떻게 다시 전달할지 예시 중심으로 정리했다.
- AI 후보 관리 UX를 개선했다. `AI 후보 가져오기`와 독립 import 메뉴의 기본 탐색 폴더를 `GeneratedCandidates`로 맞추고, import report를 `GeneratedCandidates/ImportReports`에 `.txt`로 저장하며, Level Editor 하단 로그에서 report를 클립보드로 복사할 수 있게 했다.
- Stage28 실패 원인을 분석했고, 마이그레이션 데이터보다 라운드 실행 규칙 차이가 핵심이라고 판단했다. waiting 중인 프로세스의 이후 슬롯 스케줄이 사라지지 않고 다음 라운드 일반 후보로 이월되도록 `Board.RunSimulation()`을 보강했다.
- `RoundResult`에 deferred connection 목록을 추가해 라운드 로그에서 waiting queue 진입과 process-wait 이월을 구분할 수 있게 했다. 에디터 라운드 보드 시각화에서는 deferred를 출발 전 상태로 유지하고 로그에만 표시한다.
- `LevelSolutionFinder`는 탐색 노드 제한을 상향하고 완성 후보 평가 수 제한을 별도로 추가했다. Stage28처럼 후보 평가 수는 139,968개지만 중간 탐색 노드가 20만을 조금 넘는 레벨도 탐색 제한 때문에 오판하지 않도록 했다.
- Editor 전용 `LevelDifficultyAnalyzer`를 추가했다. `LevelSolutionFinder` 결과와 `LevelSO -> LevelDefinition` 정보를 사용해 최적 라운드, waiting/requeue/deferred, 성공 해 희소성, Rule/Relay 복잡도, 평균 연결 거리 기반의 `1.0 ~ 5.0` 난이도 점수와 등급을 계산한다.
- Level Editor의 자동 해 찾기 패널과 하단 로그에 난이도 요약을 표시하도록 연결했다. 분석 결과는 `LevelSO`에 저장하지 않고 에디터 계산 결과로만 유지한다.

## 다음 작업 순서

1. Shapes View 프리팹 기반을 구현한다. 아직 구현 전이다.
    - `04.UI/LevelPlay/Views`에 passive `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView`와 UI 전용 visual state enum을 추가한다.
    - `VisualSettings_Default.asset`을 참조하는 Editor 생성 메뉴 `Tools/DeadLock/Visuals/Create LevelPlay View Prefabs`를 추가한다. 생성기는 기존 프리팹을 절대 덮어쓰지 않고, 없는 프리팹만 만든다.
    - 생성 대상은 `Assets/03.Prefabs/LevelPlay/Nodes/Prefab_Node_Process.prefab`, `Prefab_Node_Resource.prefab`, `Assets/03.Prefabs/LevelPlay/Connections/Prefab_Connection.prefab`, `Prefab_Relay.prefab`이다.
    - Process는 원형, Resource는 사각형, Connection/Relay는 Shapes 선으로 구성한다. 임시 uGUI 보드는 만들지 않는다.

2. `BoardPresenter`와 런타임 DTO 표시 경계를 연결한다.
    - Presenter는 `LevelPlayBootstrap.Manager` 이벤트와 DTO를 구독하고, View 입력을 `LevelPlayManager`의 계획 편집 유스케이스로 변환한다.
    - Rule/Relay 표시용 DTO가 부족하면 Manager가 Domain 객체를 노출하지 않는 선에서 DTO를 확장한다. pause, 배속, 현재 재생 라운드는 Presenter/View의 재생 상태로 유지한다.
    - 계획 단계의 슬롯 선택/연결 교체/삭제와 결과 단계의 라운드 순차 재생, Relay focus를 연결한다.

3. Unity Test Framework 검증을 확장한다.
   - `LevelPlayManager` EditMode 테스트를 실행하고, Scene/View가 추가되면 PlayMode로 연결 할당, 라운드 재생, pause, 배속, Relay 표시를 검증한다.
   - 순수 .NET console runner는 사용하지 않는다.

4. 저장/플랫폼/모바일 입력을 분리한다.
    - 진행도/설정 Repository, `IPlatformServices`, `06.Infrastructure` 구현을 진행한다.

## 검증

Unity 검증은 의도적으로 수동 전용이다. 검증 요청이 실행되지 않았다면 Unity 컴파일 성공을 가정하지 않는다.

최근 Domain 작업 뒤 정적 확인은 완료했다.

- `Assets/02.Scripts/01.Domain` 순수 C# 임시 classlib 컴파일은 성공했다.
- Unity 의존, 구 Rule API, 폐기된 enum 묶음/정책 enum 잔존 검색 결과는 없었다.
- 모든 Domain `.cs` 파일과 폴더에 Unity `.meta` 파일이 있는지 확인했다.
- `LevelSO` authoring 타입 추가 뒤 Repository/Levels의 `[SerializeField]` 한 줄 배치 잔존 여부와 `git diff --check`를 확인했다.
- `LevelSOMapper` 추가 뒤 Domain Unity 의존, Mapper 존재, Repository/Levels `[SerializeField]` 한 줄 배치, `.meta` 누락, `git diff --check`를 확인했다.
- Composite Resource Rule 추가 뒤 Domain Unity 의존, 다중 rule 변환 경계, 단일 rule 잔존, Repository/Levels `[SerializeField]` 한 줄 배치, `.meta` 누락, `git diff --check`를 확인했다.
- Legacy Level 마이그레이션 도구 추가 뒤 Domain Unity 의존, Editor 폴더 위치, `LevelCreator` 타입 직접 참조, Repository/Levels `[SerializeField]` 한 줄 배치, `.meta` 누락, `git diff --check`를 확인했다.
- Legacy Level 마이그레이션 실행 뒤 생성 결과를 파일 기준으로 검수했다. 61개 source asset과 61개 migrated `LevelSO`를 비교했고 process/resource 수, 좌표, 색 ID, capacity, rule 설정이 일치했다. 리포트의 failed asset과 validation error는 없었다.
- Level Editor Window v1/v1.5, Relay 편집 UI, test case 자동 라운드 재생, 보드 기반 test assignment 편집, 라운드별 보드 하이라이트, Stage28 라운드 이월 규칙, LevelDifficultyAnalyzer 추가 뒤 Domain Unity 의존, Repository/Levels `[SerializeField]` 한 줄 배치, EditorWindow/UI Toolkit/Relay/assignment line/analysis 타입 존재, `.meta` 누락, `git diff --check`를 확인했다.
- Unity Editor 컴파일/테스트는 아직 실행하지 않았다.

## 다음 스레드 시작 메모

다음 작업은 사용자가 지정하는 제작/구현 방향으로 이어간다. AI 후보 생성/검증 루프는 후보 JSON 저장, Unity import, report 저장/복사까지 기본 작업이 완료된 상태다. RelayTransfer Sender capacity 1 제약은 에디터 즉시 UX 검증과 `LevelDefinitionValidator` 최종 검증으로 보장한다. 테스트가 필요해지면 순수 .NET console runner가 아니라 Unity Test Framework 기반으로 추가한다.

## 2026-07-09 SelectionOrder 라운드 순서 보정

- `Board.RunSimulation()` 스케줄 생성 기준을 슬롯 배열 index에서 `ProcessColorSlot.SelectionOrder`로 변경했다.
- Level Editor test case 실행은 예약 연결 목록의 저장 순서를 플레이어가 연결한 순서로 해석하고, 실행 직전에 임시 `LevelDefinition`의 각 프로세스 슬롯 `SelectionOrder`로 반영한다.
- 순서 후보 탐색은 모든 순열 완전 탐색이 아니므로, 이론상 최소 라운드에 도달한 경우만 최적 증명으로 보고 그 외 성공 해는 현재 최선으로 표시한다.
- 순서 후보 평가 증가에 맞춰 자동 해 찾기 기본 완성 후보 평가 제한을 2,000,000으로 올렸다.
## 2026-07-09 Waiting Capacity And Solver Order Expansion

- `Board.RunSimulation()`의 waiting 재투입을 capacity-aware strict FIFO로 보강했다.
- 기존에는 resource별 waiting head 1개만 다음 라운드 priority 후보로 올렸지만, 이제 resource의 남은 capacity 수만큼 queue 앞쪽 항목을 priority 후보로 올린다.
- 같은 resource에서 나온 waiting priority 후보는 queue index를 보관하고, 실행 정렬에서도 같은 resource 안에서는 FIFO 순서를 먼저 지킨다.
- 뒤 waiting 항목은 앞 항목이 실제 점유에 성공해 queue head에서 제거되기 전에는 실행되지 않는다.
- `LevelSolutionFinder`의 process slot order 후보를 확장했다.
- 기존 기본/탐색/urgency/distance 순서에 더해 OffToOn early-wait 순서와 deterministic random process order 후보를 추가했다.
- Stage30은 resource 선택보다 process별 slot 실행 순서가 핵심인 케이스로 확인되었고, 이번 solver 보강 대상에 해당한다.
- Unity Editor 컴파일/테스트는 명시 요청이 없어 실행하지 않았다.

## 2026-07-10 Migrated Level Naming

- Migrated `LevelSO` asset naming was changed from legacy global names like `Stage1` and `Tutorial1` to chapter/stage based names.
- Tutorial assets now use `Level_Tutorial_S01` through `Level_Tutorial_S10`.
- Main stage assets now use `Level_C01_S01` through `Level_C05_S10`, where `Cxx` is the chapter number and `Sxx` is the stage number inside that chapter.
- `Stage28_new` was renamed to `Level_C03_S08_Alt01` and kept as an alternate Chapter 3 Stage 8 level.
- `.asset.meta` files were moved together with the assets, so Unity GUID references are preserved.

## 2026-07-10 Migrated Level Solver Audit

- The 61 migrated `LevelSO` assets were evaluated once with the same search limits as the Level Editor's optimal-solution action: `max(30, one-star round count)` rounds, 2,000,000 search nodes, and 2,000,000 evaluated candidates.
- Results: 9 optimal solutions proven, 39 successful solutions with optimality unproven, 7 levels with no success found by the current Solver, 6 search-limit results, and no validation or audit errors.
- The detailed report is stored in `Docs/DeadLock_MigratedLevelSolveAudit.md` and `Docs/DeadLock_MigratedLevelSolveAudit.json`.
- `MigratedLevelSolveAuditRunner` now checkpoints after each completed level. The first 40 classifications were recovered from the prior batch log after an unexpected shutdown, so their detailed node/candidate counters are intentionally recorded as unavailable rather than rerun.
- The next investigation target is the current-Solver no-solution/search-limit group, especially Chapter 5, without treating those results as mathematical impossibility proofs.

## 2026-07-12 Migrated Level Replacement Start

- Archived the 13 migrated levels classified as `NoSolutionFoundByCurrentSolver` or `SearchLimitReached` by renaming both their `.asset` and `.asset.meta` files with a `[Obsolete]` prefix. The assets remain available as references and retain their Unity GUIDs.
- Added `Docs/DeadLock_LevelReplacementPlan.md` to define the C03/C04/C05 replacement scope, chapter rule progression, target difficulty bands below `4.0`, and acceptance criteria.
- C03 S08-S09, C04 S09-S10, and C05 S02-S10 will be replaced in that order. The later Cxx S11-S15 expert extension is intentionally out of scope and reserved for difficulty `4.0+`.

## 2026-07-12 Replacement Validation Progress

- Added Editor-only replacement candidate import and promotion runners. Candidates are imported into `Generated`, validated with the existing Solver and difficulty analyzer, then promoted into `Migrated` only when the configured difficulty band is satisfied.
- Promotion writes the recommended star thresholds and an `Auto Optimal` test case from the Solver's best candidate before moving the asset to its canonical migrated path.
- Promoted replacements: C03 S08 (3.1), C03 S09 (3.3), C04 S09 (3.5), C04 S10 (3.7), C05 S02 (1.4), C05 S03 (1.5), C05 S04 (2.4), C05 S05 (2.9), C05 S06 (2.9), C05 S07 (3.1), C05 S08 (3.5), C05 S09 (3.8), and C05 S10 (3.9).
- C04 S09 revision 01 was preserved as a rejected reference because its 4.4 score exceeded the standard chapter cap. Revision 02 replaced it at 3.5.
- C05 S07 was tuned down from 3.5 to 3.1, while C05 S10 was tuned up from 2.8 through 3.5 to 3.9. All rejected source JSON and generated assets remain preserved as references.
- The 13 archived migrated levels now have canonical replacements. The final chapter-curve review must still separate the currently tied C05 S05/S06 difficulty scores before the migrated-level audit is refreshed.

## 2026-07-12 LevelPlayManager Foundation

- Added pure C# `LevelPlayManager` and immutable UI DTOs under `03.Manager/LevelPlay`. The Manager receives `LevelDefinition` and `LevelPlaySettings`, and does not reference `LevelSO` or Unity APIs.
- Planning supports connection assignment, planned-connection removal, transactional resource replacement with rollback, process-local `SelectionOrder` normalization, and restart from the last valid plan.
- Simulation is calculated once through `Board.RunSimulation()` and exposed as a complete round DTO list with end state and 0-3 star evaluation. Pause, speed, and current playback round remain future presentation state.
- Added `Board.RemoveConnection()` for planned connections only, plus resource-focus DTO conversion and Manager state/simulation/focus events.
- Added Unity Test Framework EditMode coverage for loading validation, plan editing, replacement rollback, incomplete plans, simulation/stars, restart restoration, and Relay focus. The Unity test suite has not been executed in this task.

## 2026-07-13 LevelPlay Bootstrap Foundation

- Added code-only `LevelPlayBootstrap` under `05.Bootstrap/LevelPlay`. It maps its Inspector `LevelSO` into `LevelDefinition` and `LevelPlaySettings`, creates `LevelPlayManager`, and calls `LoadLevel()` in `Awake()`.
- The component exposes only `LevelSO`, `Manager`, `IsInitialized`, and `InitializationError`. Invalid references, star thresholds, or level definitions stop initialization and produce a contextual Unity error log.
- No runtime Scene, Prefab, View, Presenter, singleton, or Scene lookup was added. Shapes-based runtime visuals remain the next implementation step.

## 2026-07-13 비주얼 준비 단계

- 현재 구현 경계는 `LevelPlayBootstrap`까지 완료됐다. 다음 작업은 임시 런타임 보드를 만드는 것이 아니라, 씬 오브젝트를 분석하고 프리팹 경계를 정하는 것으로 시작한다.
- `Docs/DeadLock_VisualDirection.md`를 기준으로 현재 씬의 오브젝트를 `Board`, `Process`, `Resource`, `Connection`, `Relay`, `HUD/Overlay`, `Feedback` 책임으로 분류한 뒤 프리팹을 만든다.
- 후보마다 프리팹화 여부, 단일/복수 생성 여부, View가 소유할 값, 후속 `BoardPresenter`가 DTO로 주입할 값을 기록한다.
- 작업 순서는 비주얼 기획서 검토 -> 현재 시즌 오브젝트 목록화 -> 프리팹 후보 표 -> Shapes 비주얼 기반 -> Presenter/View 연결 -> 라운드 재생, pause, 배속 제어 순으로 진행한다.

## 2026-07-13 Shapes 상태 검증 보드

- `04.UI/VisualPrototype/Editor/ShapesVisualPrototypeBuilder`를 추가했다. `VisualPrototype_Shapes` 씬에서 `Tools/DeadLock/Visuals/Rebuild Shapes Visual Prototype` 메뉴를 실행하면, 기존 정적 prototype hierarchy를 상태 검증용 Shapes hierarchy로 다시 만든다.
- Process는 Default, Selected, Waiting, Completed 상태를 한 View shell에서 비교할 수 있게 두었고, Resource는 capacity 1-4의 모든 점유 수 조합을 한 화면에서 확인할 수 있게 구성했다.
- Resource 점유 슬롯은 빈 상태에서 muted, 실제 점유 상태에서 connection 색으로 점등한다. waiting은 resource slot을 점등하지 않으며 Process port 피드백으로 표현한다.
- Rule 시각 요소는 composable 구조로 정리했다. Clock은 상단 badge, ColorSwitch는 상단 ColorSwitchTrack, EmptyColor는 점유 슬롯 뒤의 X, Simultaneous는 capacity slot 주위의 center hub를 사용한다. 복합 Rule은 shell을 늘리지 않고 같은 layer를 조합한다.
- 이 작업은 Editor 전용 상태 기준을 만든 것이며, runtime Prefab, Presenter, View, Scene 직렬화 데이터는 아직 변경하지 않았다. Unity 메뉴 실행과 화면 검토 뒤 이 기준을 실제 Prefab 후보 표로 전환한다.
- ColorSwitch는 하단 후보 색 점 방식이 아니라, Resource 상단의 단방향 `ColorSwitchTrack`으로 수정했다. 현재 색, 다음 색, 현재 전이 구간을 각각 강조하고 마지막 색에서 첫 색으로 이어지는 순환선은 표시하지 않는다.
- ColorSwitch + Clock + Capacity 2 상태 샘플을 추가했다. 이 조합에서는 ColorSwitchTrack이 상단을 사용하고 Clock은 좌상단 compact badge로 이동해 겹침을 확인한다.

## 2026-07-13 VisualSettings Foundation

- Presentation 전용 `VisualSettingsSO`와 `VisualColorEntry`를 `04.UI/LevelPlay/Settings`에 추가했다. Runtime ColorId 조회, 중립/상태/Relay 색, Process/Resource/Connection/ColorSwitch 공통 Shapes 수치를 한 에셋에서 관리한다.
- `ColorId <= 0`은 muted, 등록되지 않은 양수 ID는 missing magenta로 표시한다. Domain이나 `LevelSO`에 실제 Unity 색을 넣지 않는다.
- Editor 전용 `VisualSettingsDefaultAssetGenerator`를 추가했다. 메뉴 실행 시 마이그레이션 리포트의 기존 37색을 `Assets/05.Visual Resources/Settings/VisualSettings_Default.asset`으로 seed하며, 이미 만든 에셋은 덮어쓰지 않는다.
- `LevelPlayBootstrap`, Manager, DTO, Prefab, Presenter는 이번 작업에서 변경하지 않았다. 다음 작업은 `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView` 프리팹과 passive View shell 구현이다.

## 2026-07-13 LevelPlay Prefab/View Foundation

### 현재 완료 범위

- 실제 런타임 조립은 `LevelPlayBootstrap -> LevelSOMapper -> LevelDefinitionValidator -> LevelBoardFactory -> LevelPlayManager`까지 완료됐다.
- 상태 검증 기준은 `VisualPrototype_Shapes` 씬과 `ShapesVisualPrototypeBuilder`에 있다. 이 씬은 프리팹 원본이 아니라 Capacity 1~4, Rule 조합, 연결/Relay 상태의 배치와 겹침을 확인하는 참조 보드다.
- 런타임 색상과 Shapes 기본 수치는 `Assets/05.Visual Resources/Settings/VisualSettings_Default.asset`이 source of truth다. 이 에셋에는 마이그레이션 팔레트 `ColorId 1~37`가 seed되어 있다.
- `04.UI/LevelPlay/Views`에 네 passive View와 UI 전용 visual state/type, `ResourceRuleVisualData`를 추가했다. `Assets/03.Prefabs/LevelPlay` 프리팹은 생성 메뉴가 최초 생성하도록 두었으며, 이 메뉴와 Prefab Mode 검증은 아직 실행하지 않았다. `BoardPresenter`와 런타임 Scene 직렬화 데이터는 아직 만들지 않았다.

### 구현 파일과 책임

- `Assets/02.Scripts/04.UI/LevelPlay/Views/ProcessView.cs`
  - Shapes 원형 Process 본체, 중앙 port, 필요 ColorId chip tray, 선택/waiting/failed/completed overlay를 표시한다.
  - `int ColorId`, UI 전용 상태 enum, 위치 같은 표시값만 받고 Domain/Manager/DTO를 직접 참조하지 않는다.
- `Assets/02.Scripts/04.UI/LevelPlay/Views/ResourceView.cs`
  - Shapes 사각 Resource 본체, 기본 색, Capacity 1~4 점유 슬롯, lock, ColorSwitch, Clock, EmptyColor, Simultaneous, Relay anchor를 표시한다.
  - 실제 점유 슬롯만 connection 색으로 점등하고, waiting은 Resource slot을 점등하지 않는다. waiting 피드백은 Process port가 담당한다.
- `Assets/02.Scripts/04.UI/LevelPlay/Views/ConnectionView.cs`
  - 두 끝점, ColorId, Planned/Occupied/Waiting/Blocked/Completed 상태를 Shapes 선으로 표시한다.
- `Assets/02.Scripts/04.UI/LevelPlay/Views/RelayView.cs`
  - Link/Transfer 형태, 양 끝점, 강조 상태를 표시한다. Transfer는 sender/receiver와 방향 장식을 분리해 표현한다.
- `Assets/02.Scripts/04.UI/LevelPlay/Editor/LevelPlayViewPrefabGenerator.cs`
  - `Tools/DeadLock/Visuals/Create LevelPlay View Prefabs` 메뉴를 제공한다.
  - `VisualSettings_Default.asset`이 없으면 오류만 기록하고 생성하지 않는다. 기존 프리팹은 수동 디자인 변경을 보존하기 위해 건너뛴다.
- `Assets/02.Scripts/04.UI/LevelPlay/Settings/VisualSettingsSO.cs`
  - node shadow offset, state ring, Clock normal/compact badge, Simultaneous hub/slot/link, Relay stub/endpoint, ColorSwitch rail/chip/link/pointer에 필요한 Shapes 수치만 추가한다.
  - 기존 37개 palette entry는 변경하지 않는다.

### 프리팹 고정 구조

- `Prefab_Node_Process.prefab`
  - `Background`
  - `Port`
  - `RequiredColorTray/Background`
  - `RequiredColorTray/ColorChips`
  - `StateOverlay`
- `Prefab_Node_Resource.prefab`
  - `Background`
  - `Port`
  - `OccupancySlots`
  - `RuleVisuals`
  - `StateOverlay`
  - `RelayAnchors/Left`
  - `RelayAnchors/Right`
- `Prefab_Connection.prefab`
  - `Background/Shadow`
  - `Track/Line`
- `Prefab_Relay.prefab`
  - `Links/StartStub`
  - `Links/EndStub`
  - `Endpoints`

`ColorChips`, Capacity slot, ColorSwitch track, Clock badge, EmptyColor X, Simultaneous hub/links 같은 가변 요소는 프리팹에 고정 개수로 두지 않고 해당 container 아래에서 View가 생성/정리한다. `Shadow`, `Stroke`, `Fill` 같은 렌더링 부품은 루트가 아니라 역할별 container 아래에 둔다.

### 표시 규칙 확정

- Capacity 1~4: 빈 슬롯은 muted, 실제 점유 슬롯만 해당 connection 색으로 점등한다. Capacity 1은 중앙, 2는 가로, 3은 방사형, 4는 2x2로 둔다.
- ColorSwitch: Resource 상단 `ColorSwitchTrack`에 Definition 순서대로 color chip을 배치한다. 현재 색은 큰 흰 rim, 다음 색은 ring과 아래 pointer, 현재 -> 다음 인접 구간은 밝은 선/chevron으로 표시한다. 마지막 색에서 첫 색으로 돌아가는 선이나 화살표는 그리지 않는다.
- Clock: 기본은 Resource 상단 중앙 badge, ColorSwitch와 함께 있을 때는 좌상단 compact badge로 이동한다.
- EmptyColor: 고정 전에는 occupancy slot 뒤에 muted X를 두고, 최초 실제 점유로 색이 고정되면 제거한다.
- Simultaneous: 점유 슬롯과 중앙 hub를 연결한다. capacity 미달 hub는 red, 충족 hub는 green이다.
- Relay: Resource 본체의 종류가 아니라 별도 `RelayView`로 표현한다. Link는 무방향, Transfer는 sender -> receiver 방향과 endpoint 차이를 유지한다.

### 후속 구현 순서

1. `Assets/01.Scenes/LevelPlayRuntime.unity`에서 실제 보드 표시를 확인한다. `BoardPresenter`는 Bootstrap의 Manager DTO를 구독해 Process, Resource, Connection View를 생성·갱신한다.
2. 계획 입력을 실제 씬에서 확인한다. Process slot을 클릭한 뒤 Resource를 클릭하면 예약/교체하고, 예약된 slot을 선택한 뒤 `Delete` 또는 `Backspace`로 제거한다.
3. 결과 재생: `LevelSimulationDTO` 전체 라운드 결과를 pause/배속 가능한 표시 상태로 순차 재생한다. 전체 시뮬레이션 계산은 시작 시 한 번만 수행한다.
4. Relay focus와 Resource Rule의 세부 표시 DTO를 추가하고, waiting/complete/blocked 피드백, DOTween/FEEL polish, 모바일 입력과 PlayMode 테스트를 추가한다.

### 다음 스레드 시작 지시

- 첫 작업은 `LevelPlayRuntime` 씬에서 실제 보드와 계획 입력을 수동 확인한 뒤 결과 재생을 Presenter로 연결하는 것이다. Domain, Repository, Manager, Bootstrap의 규칙 코드와 `LevelSO` 저장 구조는 변경하지 않는다.
- View는 passive하게 유지한다. View가 `LevelPlayManager`, Domain 객체, Domain enum을 직접 참조하거나 게임 규칙을 판단하면 안 된다.
- Unity 컴파일/테스트는 사용자가 별도로 요청하기 전에는 실행하지 않는다. 구현 후에는 정적 검색, `.meta` 누락 확인, `git diff --check`만 수행한다.

## 2026-07-13 LevelPlay Runtime Board Foundation

- `04.UI/LevelPlay/Presentation/BoardPresenter`를 추가했다. Presenter는 `LevelPlayBootstrap`이 조립한 `LevelPlayManager`의 `OnLevelChanged` DTO만 구독하며, `BoardRoot` 아래에 Process, Resource, Connection View를 id별로 생성·재사용한다.
- row/column은 보드 중심 기준의 local position으로 변환하고, Process slot 색과 connection 관계를 View의 primitive 표시값으로 변환한다. 기존 View는 Domain, Manager, DTO를 직접 참조하지 않는다.
- `Assets/01.Scenes/LevelPlayRuntime.unity`를 추가했다. 이 씬은 `Level_C03_S08`과 네 View prefab, orthographic camera, `LevelPlayBootstrap`, `BoardPresenter`, `BoardRoot`를 연결한다.
- 현재 Manager DTO에는 Resource Rule과 Relay 관계의 표시 데이터가 없으므로, 이 첫 runtime board는 Process, Resource, Connection 기본 상태만 표시한다. Rule/Relay 세부 표시와 플레이어 입력은 다음 단계다.
- Unity에서 씬을 열거나 Play Mode를 실행하지 않았다.

## 2026-07-13 LevelPlay Planning Input Foundation

- `ProcessView`는 필요한 색 chip마다 2D 입력 collider를 생성하고, 클릭된 process/slot id를 event로 발행한다. `ResourceView`는 Resource id 클릭 event를 발행하며, 기존 생성 프리팹에도 runtime `BoxCollider2D`를 보강해 입력이 동작하게 한다.
- `BoardPresenter`는 선택한 process slot을 selected 상태로 다시 표시하고, Resource 클릭을 `AssignConnection` 또는 `ReplaceConnection` Manager 유스케이스로 변환한다. 선택된 예약 slot의 삭제는 desktop 임시 입력인 `Delete`/`Backspace`로 `RemoveConnection`을 호출한다.
- 이 입력은 Presenter가 Manager를 호출하고 View가 입력 event만 발행하는 MVP 경계를 유지한다. 터치 제스처, 버튼 UI, feedback은 모바일/연출 단계에서 별도로 구현한다.
- Unity에서 입력을 실행 검증하지 않았다.

## 2026-07-13 LevelPlay Round Playback Foundation

- `BoardPresenter`는 계획 입력이 완성된 경우에만 `LevelPlayManager.StartSimulation()`을 호출하고, 계산이 완료되어 전달된 `LevelSimulationDTO`를 저장된 Planning DTO를 기준으로 라운드별 표시 상태로 재생한다. 시뮬레이션 규칙 계산은 시작 시 한 번만 수행하며, 재생 중에는 Manager나 Domain 상태를 변경하지 않는다.
- `LevelRoundDTO`의 occupied, waiting, requeued, released, blocked, completed, failed 목록을 프레젠테이션 상태 집합으로 반영한다. 그 결과 Process/Resource/Connection View는 기존의 표시 전용 API만으로 라운드별 상태를 다시 그린다.
- 재생 중 `Space`는 일시정지/재개, `1`/`2`/`3`은 0.5x/1x/2x 재생 속도, `R`은 계획 상태로 재시작한다. Planning 상태에서는 `Space`로 시뮬레이션을 시작한다.
- 재생 대기는 `UniTask`와 파괴 취소 토큰을 사용한다. Scene을 나가거나 재시작하면 진행 중인 재생은 취소된다.
- Unity 컴파일, Play Mode, 수동 입력 검증은 이번 변경에서는 실행하지 않았다.

## 2026-07-13 LevelPlay Runtime Validation Attempt

- Unity 6.4에서 `Assets/01.Scenes/LevelPlayRuntime.unity`가 열리는 것과 Editor.log의 최초 컴파일 결과를 확인했다.
- 컴파일은 `ProcessView.RefreshRequiredColorTray`가 `slotIdList`를 매개변수로 받지 않아 발생한 `CS0103` 세 건을 보고했다. `Refresh`가 slot id 목록을 전달하도록 수정했다.
- Unity MCP 브리지는 연결 후 응답이 시간 초과되는 상태라 수정본의 재컴파일 완료와 Play Mode 상호작용은 이 세션에서 확인하지 못했다. 다음 검증에서는 Unity Console에서 새 컴파일 결과를 먼저 확인한 뒤 슬롯 선택/예약/재생 흐름을 실행한다.

## 2026-07-13 LevelPlay Clock Typography Prefabization

- `Prefab_Node_Resource`의 `RuleVisuals` 아래에 `Dynamic` container와 고정 `Clock/Badge`, `Clock/TurnCountNormal`, `Clock/TurnCountCompact`을 추가했다. 두 TMP 라벨은 LiberationSans SDF font asset, 정렬, no-wrap, normal 5 / compact 3.5 크기와 위치를 프리팹에 보관한다.
- `ResourceView`는 Rule의 가변 Shapes만 `Dynamic`과 `Clock/Badge`에서 생성·정리한다. Clock 숫자는 고정 TMP 중 하나를 활성화하여 text와 열림 상태 color만 갱신하며, 런타임에 폰트·크기·정렬·줄바꿈을 결정하지 않는다.
- 새 Resource prefab을 생성하는 editor generator도 같은 고정 Clock 텍스트 hierarchy를 만든다. 기존 프리팹을 생성기가 덮어쓰지 않는 원칙은 유지한다.

## 2026-07-13 LevelPlay Connection Boundary And Relay Focus

- Process에서 Resource로 향하는 Connection은 Process 중앙 Port에서 시작하고, Resource의 `BoxCollider2D` shell 경계에서 끝난다. 계획 보드와 라운드 재생은 같은 경계 계산을 사용하며, Relay도 동일한 API를 사용한다.
- `BoardPresenter`는 `LevelPlayManager.OnResourceFocusChanged`의 불변 `ResourceFocusDTO`를 구독한다. 슬롯을 선택하지 않은 상태에서 Resource를 누르면 해당 Resource와 연관 Resource를 강조하고, 관련 Relay의 전체 표시를 강조한다.
- Resource focus는 슬롯 선택, 시뮬레이션 시작, 재시작 시 Presenter 표시 상태에서 해제된다. Domain, DTO, Manager, 프리팹의 정적 구조는 변경하지 않았다.

## 2026-07-13 LevelPlay Pre-HUD Interaction And Feedback

- Process slot은 짧은 탭으로 선택하고, 0.45초 길게 누르면 계획 단계에서 해당 슬롯의 예약 연결을 제거한다. 기존 `Delete`/`Backspace` 제거 경로는 desktop fallback으로 유지한다.
- `VisualSettingsSO`에 Waiting Port pulse와 잘못된 입력 shake의 presentation 값을 추가했다. Waiting Process의 중앙 Port는 반복 pulse하며, 잘못된 Resource 연결·예약 삭제·미완성 상태의 시작 시도는 선택 Process만 짧게 shake한다.
- 모든 피드백은 기존 Process prefab의 Transform과 Shapes만 갱신하고, 런타임 GameObject/Shape/Collider를 생성하거나 제거하지 않는다. 게임 HUD와 결과 화면은 아직 구현하지 않았다.
