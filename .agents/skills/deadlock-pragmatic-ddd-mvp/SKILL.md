---
name: deadlock-pragmatic-ddd-mvp
description: "DeadLock Unity 프로젝트의 실용적 DDD와 Passive View MVP 아키텍처 지침입니다. 게임 규칙, 저장/로드 경계, Manager/Application Service, Presenter/View UI 흐름, Bootstrap 조립, DTO, Repository, 06.Infrastructure 플랫폼 분리, 도메인 Rule을 설계/구현/리팩터링/리뷰할 때 사용합니다."
---

# DeadLock 실용 DDD와 MVP

## 작업 순서

1. 변경 대상의 책임 계층을 먼저 분류한다: Domain, Repository, Manager, UI, Bootstrap, Infrastructure.
2. 아키텍처 경계를 바꾸는 작업이면 `references/architecture.md`를 읽는다.
3. 보드, 프로세스, 리소스, 연결, Rule 작업이면 `references/deadlock-domain-example.md`를 읽는다.
4. 사용자가 명시적으로 요청하지 않은 게임 동작 변경은 피하고, 기존 동작 보존을 우선한다.
5. Unity 검증은 자동으로 실행하지 않는다. 사용자가 검증을 명시적으로 요청한 경우에만 `deadlock-unity-validation-manual`을 사용한다.

## 목표 구조

```text
Assets/Scripts/Game/
  01.Domain/
  02.Repository/
  03.Manager/
  04.UI/
  05.Bootstrap/
  06.Infrastructure/
```

새 코드는 이 구조를 우선 사용한다. 기존 `Assets/Outdated/scripts` 코드는 한 번에 크게 옮기지 말고, 기능 단위로 점진적으로 흡수한다.

## 계층 책임

- Domain은 변경 가능한 게임 상태, 불변 조건, 게임 규칙에 따른 상태 전이를 소유한다. Unity API, Steamworks, FEEL, DOTween, Shapes, SceneManager, Resources, Addressables, UI, Manager를 참조하지 않는다.
- Repository는 지속 저장 상태를 저장하고 복원한다. 단순한 정적 레벨 데이터 조회나 임시 런타임 객체 생성을 위해 Repository를 만들지 않는다.
- Manager는 Application Service 계층이다. 유스케이스를 조율하고, Domain을 호출하고, 지속성이 필요할 때만 Repository를 사용하며, DTO를 캐싱하고 상태 변경 이벤트를 발행한다.
- UI는 Passive View MVP를 사용한다. View는 MonoBehaviour 표면이고, Presenter는 View 입력과 Manager 상태 이벤트를 구독해 Manager 유스케이스 호출과 View 갱신을 담당한다.
- Bootstrap은 Composition Root다. Domain 객체, Repository, Manager, Presenter, 플랫폼 서비스를 생성하고 연결한다.
- Infrastructure는 외부 시스템 구현 계층이다. Steam, Android, iOS, 로컬 파일, 클라우드 저장, 플랫폼 업적, 콜백 처리 등 외부 의존 구현을 둔다.

## 의존 방향

```text
View -> Presenter -> Manager -> Domain
                         |
                         +-> Repository -> Domain
                         |
                         +-> Platform Port

Infrastructure -> Repository/Platform Port 구현
Bootstrap -> 구체 객체 생성과 연결
```

Domain은 Manager, Presenter, View, Bootstrap, Repository 구현, Infrastructure 구현을 몰라야 한다.

## DeadLock 도메인 이름

퍼즐 언어와 맞는 이름을 우선 사용한다.

- `Board`: 현재 레벨 상태와 노드 모음.
- `ProcessNode`: 색상 슬롯과 완료 상태를 가진 시작 노드.
- `ResourceNode`: 수용량, 색상, 대기열, Rule을 가진 대상 노드.
- `Connection`: 프로세스 색상 슬롯과 리소스를 잇는 계획 또는 실행 연결.
- `ResourceRule`: 색상 일치, 수용량, 동시 연결, 색상 전환, 빈 색상, 시계 같은 순수 C# 규칙.
- `LevelProgress`: 스테이지 잠금 해제와 클리어 상태.

## Manager는 Application Service

Manager는 유스케이스 경계이며 전역 Unity 싱글톤으로 만들지 않는다.

예시:

- `LevelPlayManager.AssignColor(...)`
- `LevelPlayManager.RemoveColor(...)`
- `LevelPlayManager.StartSimulation()`
- `ProgressManager.MarkLevelCleared(...)`

Manager는 단순 결과 enum, bool, DTO를 반환할 수 있다. 피드백 재생, 선 애니메이션, 씬 오브젝트 접근은 하지 않는다.

## Presenter는 Presentation Adapter

Presenter는 Application 계층이 아니다. Presenter는 다음 변환만 담당한다.

- View 이벤트를 Manager 호출로 변환한다.
- Manager DTO/이벤트를 View 표시 호출로 변환한다.
- Manager 실패 결과를 UI 피드백 요청으로 변환한다.

Presenter는 Domain 규칙을 다시 구현하지 않는다.

## Infrastructure와 플랫폼 분리

Steam, Android, iOS, Null 구현은 `06.Infrastructure`에 둔다.

권장 구조:

```text
03.Manager/
  Platform/
    IPlatformServices.cs

06.Infrastructure/
  Platform/
    NullPlatformServices.cs
    SteamPlatformServices.cs
    AndroidPlatformServices.cs
    IOSPlatformServices.cs
  Save/
    SteamCloudSaveRepository.cs
    AndroidSaveRepository.cs
    IOSSaveRepository.cs
```

인터페이스는 Manager가 의존하는 포트이므로 Manager 쪽에 둘 수 있다. 구체 구현 선택은 Bootstrap에서 한다.

## DTO 규칙

- Manager/UI 경계에는 불변 DTO 클래스를 사용한다.
- DTO는 Manager에서 캐싱하고 상태 변경 후에만 갱신한다.
- 매 프레임 또는 자주 호출되는 프로퍼티에서 DTO를 새로 할당하지 않는다.
- Manager/UI 경계를 넘는 Domain 상태에만 `ToDTO()`를 둔다.
- 저장 데이터가 UI DTO와 달라지면 별도 `SaveData`를 도입한다.

