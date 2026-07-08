# DeadLock 아키텍처

## 목표

실용적 DDD와 Passive View MVP를 사용한다.

```text
Assets/02.Scripts/
  01.Domain/
  02.Repository/
  03.Manager/
  04.UI/
  05.Bootstrap/
  06.Infrastructure/
```

## 계층 의도

- Domain: 보드, 프로세스 노드, 리소스 노드, 연결, 리소스 Rule, 시뮬레이션.
- Repository: 저장/로드와 지속 플레이어 상태.
- Manager: Application Service 유스케이스와 DTO 캐시.
- UI: View, Presenter, 비주얼 피드백, DOTween, FEEL, Shapes, shader.
- Bootstrap: 씬 연결과 구체 객체 생성.
- Infrastructure: Steam, Android, iOS, Null 플랫폼 구현과 외부 저장소 구현.

## 도메인 모델

도메인은 핵심 규칙과 그 규칙을 지키기 위해 필요한 순수 상태를 뜻한다. Unity `ScriptableObject`, `MonoBehaviour`, View, DOTween, FEEL, Shapes, Steamworks는 도메인에 들어오지 않는다.

- `Board`: 현재 레벨의 런타임 보드다. 프로세스, 자원, 연결, 보드 범위 Rule을 소유한다.
- `ProcessNode`: 필요한 색 슬롯과 완료, 대기, 실패, 진행 상태를 가진 프로세스다.
- `ProcessColorSlot`: 프로세스의 개별 요구 색이다. 연결 여부, 완료 여부, 순서 상태를 가진다.
- `ResourceNode`: 색, 수용량, 사용 상태, 대기열, 자원 단일 Rule을 가진 자원이다.
- `Connection`: 프로세스 색 슬롯과 자원을 잇는 규칙상 연결이다. 렌더링 선은 UI 계층의 책임이다.
- `LevelDefinition`: 런타임 `Board`를 만들기 위한 순수 레벨 입력 모델이다. Unity 레벨 에셋 자체는 도메인이 아니며 Mapper나 Bootstrap에서 변환한다.
- `LevelProgress`: 스테이지 잠금 해제와 클리어 상태를 가진 진행도 도메인이다.

`SimulationReport`, `RoundResult`, `AssignConnectionResult` 같은 타입은 도메인 계층에 둘 수 있지만 엔티티라기보다 도메인 메서드의 결과값으로 분류한다.

도메인 폴더는 역할 기준으로 정리한다. `Common/Values`에는 `ColorId`, `BoardPosition` 같은 값 객체를 두고, `Common/Enums`에는 enum을 둔다. `Rules/Contracts`에는 Rule 인터페이스, `Rules/Core`에는 Rule 실행 보조 타입, `Rules/Resource`에는 자원 단일 Rule, `Rules/Board`에는 보드 범위 Rule, `Rules/Relations`에는 관계 데이터, `Rules/Focus`에는 포커스 조회 결과 타입을 둔다.

`LevelDefinition`은 `ProcessNode`, `ResourceNode`, `IResourceRule`, `IBoardRule` 같은 런타임 객체를 직접 보관하지 않는다. 대신 `ProcessDefinition`, `ResourceDefinition`, Rule definition 같은 순수 정의 데이터만 보관하고, `LevelDefinitionValidator`가 authoring 제약을 검증한 뒤 `LevelBoardFactory`가 매번 새 런타임 `Board`를 생성한다. 같은 레벨 정의로 재시도하거나 다시 시작해도 waiting queue, Rule 내부 상태, Relay 임시 색 같은 런타임 상태가 공유되지 않아야 한다.

`ColorId`는 실제 색상 코드가 아니라 도메인 규칙 판정용 ID다. Domain은 `ColorId` 값이 같은지만 판단하고, 실제 `UnityEngine.Color`, 아이콘, 머티리얼, 색약 보정 팔레트 같은 표현 데이터는 이후 ScriptableObject, VisualSettings, View 계층에서 `ColorId`에 매핑한다.

Unity authoring 에셋은 `LevelSO`라는 이름을 사용한다. `LevelSO` 하나가 스테이지 하나를 나타내며, 그 안에 process, process slot, resource, relay, test case 데이터를 모두 저장한다. `LevelProcessData`, `LevelProcessSlotData`, `LevelResourceData`, `LevelRelayData`, `LevelTestCaseData` 같은 내부 데이터 타입은 ScriptableObject가 아니라 Unity 직렬화용 `[Serializable]` 데이터이므로 `SO` 접미사를 붙이지 않는다.

`LevelSOMapper`는 Unity authoring 데이터인 `LevelSO`를 Domain 입력 모델인 `LevelDefinition`으로 변환만 한다. Mapper는 `LevelDefinitionValidator`를 직접 호출하지 않으며, EditorWindow, 마이그레이션 도구, Bootstrap 같은 호출자가 필요 시 `LevelSOMapper.ToLevelDefinition(levelSO)` 뒤 `LevelDefinitionValidator.Validate(definition)`를 실행한다.

레벨 에디터는 uGUI가 아니라 UI Toolkit 기반 `EditorWindow`로 만든다. uGUI는 런타임 GameObject 기반 UI에 가깝고, 에디터 확장 UI는 UI Toolkit을 우선한다. 에디터는 `Tools/DeadLock/Levels/Level Editor` 메뉴로 열고, 좌측 `ColorId` 팔레트, 중앙 canvas, 우측 선택 항목 inspector, 하단 validation log 구조를 사용한다. Process는 실제 게임 시각 언어에 맞춰 원형 노드로, Resource는 사각 노드로 표시한다. Relay 편집, test case 편집, 자동 라운드 재생은 후속 단계로 둔다.

중앙 canvas는 `UnityEditor.Experimental.GraphView` 기반으로 구성한다. 사용자는 행동 트리 편집기처럼 pan/zoom 가능한 공간에서 노드를 직접 끌어 배치할 수 있지만, 저장 데이터에는 임의 pixel 좌표를 남기지 않는다. GraphView 좌표는 에디터 전용 표현이고, `LevelSO`에는 항상 정수 `Row`, `Column`, 위치 기반 `Id`만 저장한다. 노드 이동은 정수 grid point로 snap되며, 현재 단계에서는 `LevelSO.RowCount x ColumnCount` 범위 안으로 clamp한다. canvas에는 실제 인게임 보드로 쓰일 `RowCount x ColumnCount` 영역을 별도 테두리와 내부 격자선, 중앙 포인트로 표시한다. `Ctrl+C`/`Ctrl+V` 복제도 원본 data를 복사한 뒤 비어 있는 정수 좌표에 새 노드를 배치하는 방식으로 처리한다.

레벨 에디터 제작 중 즉시 UX 검증은 `LevelSO` 데이터를 직접 보고 처리한다. 예를 들어 RelayTransfer Sender 선택 UI는 capacity 1 resource만 후보로 보여주고, 필드 단위 경고는 현재 편집 중인 data를 기준으로 표시한다. 저장, 테스트 실행, 게임 시작 전 같은 최종/전체 검증은 `LevelDefinition`으로 변환한 뒤 `LevelDefinitionValidator`를 사용한다.

Level Editor의 색상 swatch는 `LevelSO`에 실제 색상 hex를 저장하지 않고, 마이그레이션 리포트인 `Migrated/LegacyLevelMigrationReport.txt`의 `ColorId -> legacy Color32` 매핑을 읽어 표시한다. 매핑이 없거나 새 `ColorId`인 경우에는 결정적 fallback 색상을 사용하며, Domain과 저장 데이터의 source of truth는 계속 `ColorId` 정수 ID다. 배치와 색 편집은 현재 선택된 팔레트 색을 기준으로 동작한다.

기존 `Assets/Outdated/Levels`의 `LevelCreator` 에셋은 삭제하거나 수동 재작성하지 않고, 새 `LevelSO`로 변환하는 호환 마이그레이션 경로를 둔다. 변환 도구는 레거시 타입을 직접 참조하지 않고 YAML을 읽어 `LevelSO`를 생성한다. 레거시 `Node.colors`의 실제 Unity 색상값은 전체 변환 대상의 첫 등장 순서대로 `ColorId` 1부터 자동 매핑하고, 매핑표는 변환 리포트에 남긴다. 레거시 `fixedNum`은 아직 새 Domain 규칙으로 구현하지 않고, 변환 리포트에 미지원 경고로 남긴다.

## Rule 설계

자원 하나에 붙는 규칙과 보드 범위에서 작동하는 규칙을 분리한다.

- `IResourceRule`: 자원 하나 내부에서 작동하는 규칙이다. `ColorSwitchRule`, `EmptyColorRule`, `ClockRule`, `SimultaneousRule` 같은 타입이 여기에 속한다.
- `IBoardRule`: 자원 하나를 넘어 자원 관계나 보드 범위에서 작동하는 규칙이다. Relay 같은 공간/관계 퍼즐이 여기에 속한다.

Resource Rule은 Strategy + Composite 구조로 사용한다. `ResourceNode`와 `Board`는 여전히 `resource.Rule.CanReserve(context)`, `resource.Rule.CanOccupy(context)`처럼 하나의 `IResourceRule`만 호출한다. 단일 규칙이면 해당 Rule 인스턴스를 그대로 쓰고, 레거시처럼 `Simultaneous + ColorSwitch + Clock` 조합이 필요한 경우에는 `CompositeResourceRule`이 여러 `IResourceRule`을 순서대로 감싸 하나의 Rule처럼 노출한다. `CanReserve`, `CanOccupy`, `CanFinish`는 내부 Rule이 모두 통과해야 성공하고, `OnOccupied`, `OnReleased`, `OnRoundEnded`, `ResetSimulationState`는 내부 Rule 순서대로 실행한다.

`ResourceDefinition`과 `LevelResourceData`도 이 구조에 맞춰 다중 resource rule 목록을 가진다. `LevelResourceRuleData`는 Unity authoring용 직렬화 entry이며, Mapper가 이를 `ResourceRuleDefinition[]`으로 변환하고 `LevelBoardFactory`가 실제 `IResourceRule` 인스턴스 목록으로 만든 뒤 필요하면 `CompositeResourceRule`로 감싼다. rule 목록이 없으면 제작 오류로 보고하며, 단순 Basic 리소스도 명시적인 Basic entry를 가진다.

`Capacity`는 내부 Rule보다 `ResourceNode`의 기본 속성으로 둔다. 이렇게 해야 수용량이 있는 Clock, Relay 대상 자원 같은 조합을 자연스럽게 만들 수 있다.

Relay는 자원 타입이 아니라 보드 범위 관계다.

- `RelayRelation`: 두 자원과 Relay 종류, 방향 정보를 나타내는 순수 데이터다.
- `RelayLinkRule`: `RelayRelation`을 사용해 상호 잠금 같은 Link 규칙을 적용한다.
- `RelayTransferRule`: `RelayRelation`의 Sender/Receiver 방향을 사용해 Sender에 들어온 실제 점유 색을 Receiver의 임시 색으로 전달한다.

`Relation`은 구조, `Rule`은 행동으로 구분한다. 따라서 `RelayRelation`이 직접 Rule을 상속하기보다, Relay Rule이 Relation 데이터를 사용한다.

## 라운드/점유/대기 모델

레거시 동작 보존을 위해 시작 전 예약과 시뮬레이션 중 실제 점유를 분리한다.

- `Board.AssignConnection()`은 실제 점유가 아니라 계획 연결을 만든다.
- 시작 전 연결 검증은 "이 리소스가 이 색을 처리할 수 있는가"를 확인한다.
- 시작 전 예약 단계에서 `ResourceNode.Capacity`를 소모하지 않는다.
- `ProcessColorSlot`은 할당된 리소스, 선택 순서, 완료 상태를 표현해야 한다.
- `ResourceNode`는 capacity, available capacity, waiting queue, 현재 색/상태를 가진다.
- 스케줄은 각 프로세스의 n번째 슬롯을 n번째 라운드 후보로 만든다.
- 같은 라운드 안의 스케줄 항목은 프로세스와 리소스 사이 거리순, 동거리면 선택 순서순으로 처리한다.
- 시뮬레이션 중 실제 연결 검증은 프로세스 상태, 현재 리소스 상태, 남은 capacity, Rule 조건으로 판단한다.
- 연결에 성공하면 리소스를 점유하고 해당 슬롯을 완료 처리한다.
- 리소스 점유는 개별 슬롯 작업이 아니라 프로세스 완료 시점까지 유지된다.
- 연결할 수 없으면 프로세스는 해당 리소스의 waiting queue에 들어가고 다른 리소스로 진행하지 않는다.
- 라운드 종료 시 완료 가능한 프로세스를 판정하고, 완료된 프로세스가 점유한 리소스를 반환한다.
- 리소스가 반환되면 waiting queue의 맨 앞 항목만 다음 라운드 맨 앞으로 재투입한다. 맨 앞 항목이 현재 리소스 상태와 맞지 않아 실패해도 뒤 항목을 먼저 꺼내지 않는 strict FIFO를 유지한다.
- 모든 미완료 프로세스가 waiting이고 clock waiting처럼 풀릴 가능성이 없으면 deadlock 실패로 본다.

## Rule 책임

`IResourceRule`은 시작 전 검증과 시뮬레이션 중 검증을 분리할 수 있어야 한다. 레거시에서는 ColorSwitch처럼 시작 전에는 색 목록으로 예약 가능 여부를 판단하지만, 시뮬레이션 중에는 현재 색으로 실제 연결 여부를 판단하는 규칙이 있다.

- `ColorSwitchRule`: 색 목록, 현재 색, 반환 시 색 순환, idle 라운드 종료 시 색 순환을 담당한다. 별도 정책 enum 없이 이 동작으로 고정하고, waiting queue는 색 일치 항목 우선 검색 없이 strict FIFO를 따른다.
- `EmptyColorRule`: 첫 실제 연결 색으로 현재 색을 고정하고, 시뮬레이션 리셋 시 고정 상태와 색을 초기화한다.
- `ClockRule`: 라운드 종료마다 카운트를 감소시키고 열림/닫힘을 전환한다. OnToOff가 닫히는 순간 점유 중인 미완료 프로세스가 있으면 실패 효과를 발생시킨다.
- `SimultaneousRule`: 연결 가능 여부보다 프로세스 완료 가능 여부를 보류한다. 필요한 동시 점유 수는 `ResourceNode.Capacity`와 같다.
- 모든 `IResourceRule`은 `ResetSimulationState()` 훅을 가진다. `Board.RunSimulation()` 시작 시 리소스 런타임 상태와 Rule 내부 상태를 함께 초기화한다.
- `RuleEffects`는 lock/unlock, Relay 임시 색 set/clear뿐 아니라 점유 중 리소스 실패 효과도 표현한다. 실패 효과가 적용되면 관련 프로세스는 `Failed`, 관련 connection은 `Blocked`가 되고 시뮬레이션은 `Failed`로 종료된다.
- Relay Link Rule은 리소스 내부 Rule이 아니라 기존 리소스 Rule 위에 얹히는 `IBoardRule`로 유지한다. 예약 단계는 막지 않고, 실행 중 한쪽 리소스가 점유되면 반환될 때까지 반대쪽 점유를 막는다.
- Relay Transfer Rule도 `IBoardRule`로 유지한다. Receiver는 시작 전 예약 단계에서 전달 색 후보로 예약을 허용하고, Sender가 실제 점유될 때 슬롯 색을 Receiver의 임시 색으로 전달한다. Sender 반환 시 전달 색을 해제하며, Receiver가 이미 점유 중이면 색 변경/해제는 Receiver 반환 뒤에 적용한다. Sender capacity는 1만 지원하며, 이 제약은 런타임 Rule 방어보다 레벨 에디터/ScriptableObject authoring 검증에서 보장한다.

## 포커스와 비주얼 조회

View는 특정 자원이 Relay인지, 어떤 Rule인지 직접 판단하지 않는다. 자원 hover/tap 같은 입력은 Presenter와 Manager를 거쳐 Board에 포커스 정보를 요청한다.

```text
ResourceView hover/tap
-> BoardPresenter
-> LevelPlayManager.FocusResource(resourceId)
-> Board.GetResourceFocusInfo(resourceId)
-> DTO 갱신
-> ResourceView / RelayView / FeedbackPresenter 표시
```

`GetRelatedResources`처럼 Relay에 특화된 이름은 피한다. 대신 `ResourceFocusInfo`처럼 일반화된 결과를 사용한다.

- `FocusedResourceId`
- `HighlightedResourceIds`
- `HighlightedConnectionIds`
- `ActiveBoardRuleIds`
- `FocusKind`: `None`, `Single`, `Pair`, `Group`, `Board`

Board는 레벨 생성 시 `ResourceId`에서 관련 `IBoardRule`을 찾을 수 있는 인덱스를 만들 수 있다. 이렇게 하면 Relay처럼 두 자원을 묶는 퍼즐, 자원 하나만 강조하는 외부 퍼즐, 자원 그룹이나 보드 전체를 강조하는 퍼즐을 같은 흐름으로 처리할 수 있다.

## 중요한 경계

Manager가 Application 계층이다. Presenter는 프레젠테이션 어댑터다.

플랫폼 인터페이스는 Manager가 의존하는 포트로 둘 수 있고, 구체 구현은 Infrastructure에 둔다.

```text
03.Manager/Platform/IPlatformServices.cs
06.Infrastructure/Platform/SteamPlatformServices.cs
06.Infrastructure/Platform/AndroidPlatformServices.cs
06.Infrastructure/Platform/IOSPlatformServices.cs
06.Infrastructure/Platform/NullPlatformServices.cs
```

## 마이그레이션 메모

기존 코드는 현재 `Assets/Outdated/scripts` 아래에 있다. 새 아키텍처 코드는 점진적으로 도입하고, 동작 보존을 우선한다.

초기 마이그레이션 순서:

1. `Assets/02.Scripts` 아래 새 계층 폴더를 유지한다.
2. `Outdated` 런타임 코드가 새 코드와 충돌하지 않게 레퍼런스를 차단한다.
3. 기존 `LevelCreator`와 Editor 레퍼런스는 삭제하지 않고 주석 처리한다.
4. 순수 Domain 타입을 먼저 작성한다.
5. `IResourceRule` 기반 내부 자원 규칙을 작성한다.
6. `IBoardRule` 기반 Relay 규칙을 작성한다.
7. `LevelSO`와 Domain `LevelDefinition` Mapper를 작성한다.
8. 레거시 `LevelCreator` 에셋을 `LevelSO`로 변환하는 호환 마이그레이션 도구를 작성한다.
9. UI Toolkit 기반 레벨 에디터의 기본 창, grid canvas, validation log를 작성한다.
10. `LevelPlayManager`와 DTO를 작성한다.
11. MVP View/Presenter와 Bootstrap을 연결한다.
12. 저장, 플랫폼, 모바일 입력을 분리한다.

## 검증 정책

Unity 검증은 수동 전용이다. 명시 요청 없이는 리컴파일이나 테스트를 실행하지 않는다.

