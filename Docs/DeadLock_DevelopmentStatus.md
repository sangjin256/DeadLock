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

## 다음 작업 순서

1. Level Editor에 Relay Link/Transfer 편집 UI를 추가한다.
    - v1에서는 기존 relay 데이터를 읽기 전용 요약과 검증 로그로만 보여준다.
    - 다음 단계에서는 board grid에서 두 resource를 선택해 Link/Transfer를 만들고, Transfer sender는 capacity 1 resource만 후보로 제한한다.

2. Level Editor에 test case 편집과 자동 라운드 재생을 추가한다.
    - test case는 예약 연결 목록, 예상 결과, 최대 라운드 수를 저장한다.
    - 에디터는 test case를 적용해 `Board.AssignConnection()`과 `Board.RunSimulation()`을 자동 실행하고 round-by-round 결과를 보여준다.
    - 난이도 지표는 클리어 라운드 수, waiting 횟수, 재투입 횟수, relay 사용, clock 여유 라운드부터 시작한다.

3. Unity Test Framework 기반 테스트 구조를 준비한다.
   - 순수 .NET console runner는 사용하지 않는다.
   - 씬 오브젝트와 런타임 연결 흐름이 준비되면 Unity EditMode 또는 PlayMode 테스트로 `ColorSwitch`, `EmptyColor`, `Clock`, `Simultaneous`, Relay Link, Relay Transfer 핵심 동작을 검증한다.

4. `LevelPlayManager`와 DTO를 작성한다.
    - 연결 할당/제거, 자원 포커스, 시뮬레이션 시작/라운드 진행, DTO 캐싱, 상태 변경 이벤트 발행을 담당한다.

5. MVP UI와 Bootstrap을 연결한다.
    - `BoardPresenter`, `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView`, 임시 수동 레벨 생성을 연결한다.

6. 저장/플랫폼/모바일 입력을 분리한다.
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
- Level Editor Window v1/v1.5 추가 뒤 Domain Unity 의존, Repository/Levels `[SerializeField]` 한 줄 배치, EditorWindow/UI Toolkit 타입 존재, `.meta` 누락, `git diff --check`를 확인했다.
- Unity Editor 컴파일/테스트는 아직 실행하지 않았다.

## 다음 스레드 시작 메모

다음 작업은 Level Editor에 Relay Link/Transfer 편집 UI를 추가하는 것이다. 이후 test case 편집, 자동 라운드 재생, 난이도 지표를 확장한다. RelayTransfer Sender capacity 1 제약은 에디터 즉시 UX 검증과 `LevelDefinitionValidator` 최종 검증으로 보장한다. 테스트가 필요해지면 순수 .NET console runner가 아니라 Unity Test Framework 기반으로 추가한다.

