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

## 다음 유력 작업

- `Board`, `ProcessNode`, `ProcessColorSlot`, `ResourceNode`, `Connection`, `LevelDefinition`, `LevelProgress` 순수 Domain 타입을 작성한다.
- `IResourceRule`과 `IBoardRule`을 작성하고, Relay의 초기 규칙인 Link 상호 잠금과 Transfer 상태 전달을 구현한다.
- 게임 동작 변경 전 focused Domain 테스트를 추가한다.
- 이후 `LevelPlayManager`, DTO, MVP View/Presenter, Bootstrap, 저장/플랫폼 분리를 순서대로 진행한다.

## 검증

Unity 검증은 의도적으로 수동 전용이다. 검증 요청이 실행되지 않았다면 Unity 컴파일 성공을 가정하지 않는다.

