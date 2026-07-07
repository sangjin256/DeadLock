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

## 다음 작업 순서

1. `LevelDefinition`을 순수 정의 데이터로 정리한다.
    - 현재는 `ProcessNode[]`, `ResourceNode[]`, `IBoardRule[]`를 직접 받아 `Board`를 만든다.
    - 이후 레거시 `LevelCreator.Node` 또는 새 ScriptableObject 입력과 매핑될 수 있는 정의 타입으로 분리한다.
    - RelayTransfer Sender capacity 1 같은 authoring 검증 규칙을 이 변환/검증 단계에서 적용한다.

2. Unity Test Framework 기반 테스트 구조를 준비한다.
   - 순수 .NET console runner는 사용하지 않는다.
   - 씬 오브젝트와 런타임 연결 흐름이 준비되면 Unity EditMode 또는 PlayMode 테스트로 `ColorSwitch`, `EmptyColor`, `Clock`, `Simultaneous`, Relay Link, Relay Transfer 핵심 동작을 검증한다.

3. `LevelPlayManager`와 DTO를 작성한다.
    - 연결 할당/제거, 자원 포커스, 시뮬레이션 시작/라운드 진행, DTO 캐싱, 상태 변경 이벤트 발행을 담당한다.

4. MVP UI와 Bootstrap을 연결한다.
    - `BoardPresenter`, `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView`, 임시 수동 레벨 생성을 연결한다.

5. 저장/플랫폼/모바일 입력을 분리한다.
    - 진행도/설정 Repository, `IPlatformServices`, `06.Infrastructure` 구현을 진행한다.
    - 새 레벨 에디터는 마지막에 설계한다.

## 검증

Unity 검증은 의도적으로 수동 전용이다. 검증 요청이 실행되지 않았다면 Unity 컴파일 성공을 가정하지 않는다.

최근 Domain 작업 뒤 정적 확인은 완료했다.

- `Assets/02.Scripts/01.Domain` 순수 C# 임시 classlib 컴파일은 성공했다.
- Unity 의존, 구 Rule API, 폐기된 enum 묶음/정책 enum 잔존 검색 결과는 없었다.
- 모든 Domain `.cs` 파일과 폴더에 Unity `.meta` 파일이 있는지 확인했다.
- Unity Editor 컴파일/테스트는 아직 실행하지 않았다.

## 다음 스레드 시작 메모

다음 작업은 `LevelDefinition`을 순수 정의 데이터와 Board 생성용 mapper/validator 흐름으로 분리하는 것이다. RelayTransfer Sender capacity 1 제약은 이 authoring 검증 단계에서 보장한다. 테스트가 필요해지면 순수 .NET console runner가 아니라 Unity Test Framework 기반으로 추가한다.

