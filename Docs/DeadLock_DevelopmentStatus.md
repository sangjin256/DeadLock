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
- Relay Transfer의 의미는 "source 자원을 사용하면 target 자원이 unlock된다"로 정리했다. 따라서 `ActivateResource`, `IsExternallyActivated` 계열 상태는 제거하고 `UnlockResource` 효과만 사용한다.
- `ProcessNode`는 `List<ProcessColorSlot>`을 직접 받는 소유권 이전 방식으로 정리했고, `ColorId`는 `Equals`, `GetHashCode`, `==`, `!=` 비교를 지원한다.

## 다음 유력 작업

- `IResourceRule` 기반 내부 자원 규칙을 구현한다: `ColorSwitchRule`, `EmptyColorRule`, `ClockRule`, `SimultaneousRule`. Capacity는 `ResourceNode.Capacity` 기본 속성으로 유지한다.
- 도메인 동작을 보강한다: 연결 제거, 라운드 종료 처리, 리소스 대기열/순서 처리, 프로세스 완료 판정, 실패/대기/완료 결과 타입.
- 게임 동작 변경 전 focused Domain 테스트를 추가한다: 기본 색 연결, 수용량 초과 실패, Relay Link 상호 잠금, Relay Transfer unlock, 각 내부 Rule 핵심 동작.
- `LevelDefinition`을 나중에 에디터/ScriptableObject 입력에 맞는 순수 정의 데이터로 정리한다. 현재는 `ProcessNode[]`, `ResourceNode[]`, `IBoardRule[]`를 직접 받아 `Board`를 만든다.
- `LevelPlayManager`와 DTO를 작성한다: 연결 할당/제거, 자원 포커스, 시뮬레이션 시작, DTO 캐싱, 상태 변경 이벤트 발행.
- MVP UI와 Bootstrap을 연결한다: `BoardPresenter`, `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView`, 임시 수동 레벨 생성.
- 저장/플랫폼 분리를 진행한다: 진행도/설정 Repository, `IPlatformServices`, `06.Infrastructure` 구현.
- 새 레벨 에디터는 마지막에 설계한다.

## 검증

Unity 검증은 의도적으로 수동 전용이다. 검증 요청이 실행되지 않았다면 Unity 컴파일 성공을 가정하지 않는다.

