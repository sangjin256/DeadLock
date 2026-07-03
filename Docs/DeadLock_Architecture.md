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

## Rule 설계

자원 하나에 붙는 규칙과 보드 범위에서 작동하는 규칙을 분리한다.

- `IResourceRule`: 자원 하나 내부에서 작동하는 규칙이다. `ColorSwitchRule`, `EmptyColorRule`, `ClockRule`, `SimultaneousRule` 같은 타입이 여기에 속한다.
- `IBoardRule`: 자원 하나를 넘어 자원 관계나 보드 범위에서 작동하는 규칙이다. Relay 같은 공간/관계 퍼즐이 여기에 속한다.

`Capacity`는 내부 Rule보다 `ResourceNode`의 기본 속성으로 둔다. 이렇게 해야 수용량이 있는 Clock, Relay 대상 자원 같은 조합을 자연스럽게 만들 수 있다.

Relay는 자원 타입이 아니라 보드 범위 관계다.

- `RelayRelation`: 두 자원과 Relay 종류, 방향 정보를 나타내는 순수 데이터다.
- `RelayLinkRule`: `RelayRelation`을 사용해 상호 잠금 같은 Link 규칙을 적용한다.
- `RelayTransferRule`: `RelayRelation`을 사용해 상태 전달 같은 Transfer 규칙을 적용한다.

`Relation`은 구조, `Rule`은 행동으로 구분한다. 따라서 `RelayRelation`이 직접 Rule을 상속하기보다, Relay Rule이 Relation 데이터를 사용한다.

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
7. 임시 수동 `LevelDefinition` 또는 Mapper를 작성한다.
8. `LevelPlayManager`와 DTO를 작성한다.
9. MVP View/Presenter와 Bootstrap을 연결한다.
10. 저장, 플랫폼, 모바일 입력을 분리한다.
11. 새 레벨 에디터는 마지막에 설계한다.

## 검증 정책

Unity 검증은 수동 전용이다. 명시 요청 없이는 리컴파일이나 테스트를 실행하지 않는다.

