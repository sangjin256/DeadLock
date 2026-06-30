# DeadLock 아키텍처 참고 문서

## 계층 모델

DeadLock은 실용적 DDD와 Passive View MVP를 사용한다.

```text
01.Domain          순수 게임 상태와 규칙
02.Repository      지속 저장 경계
03.Manager         Application Service와 유스케이스
04.UI              View와 Presenter
05.Bootstrap       구체 객체 생성과 연결
06.Infrastructure  외부 플랫폼/저장소 구현
```

## Domain

Domain이 소유하는 것:

- 보드 상태.
- 프로세스 노드와 리소스 노드 상태.
- 연결 계획과 시뮬레이션 상태.
- 리소스 Rule.
- 예측 가능한 실패 결과 코드.

Domain이 소유하지 않는 것:

- Unity 씬 오브젝트.
- 렌더링이나 피드백.
- 오디오.
- DOTween, FEEL, Shapes, shader, material, GameObject 참조.
- Steam, Android, iOS 같은 플랫폼 호출.

잘못된 프로그래머 입력에는 예외를 사용하고, 예상 가능한 게임 규칙 실패에는 `Try` 메서드나 결과 enum을 사용한다.

## Repository

Repository는 지속 저장 상태를 담당한다.

- 플레이어 진행도.
- 설정.
- 저장 데이터 버전.
- Steam cloud, 모바일 로컬 저장, 일반 로컬 파일 같은 저장 구현.

정적 레벨 에셋 조회는 기본적으로 Repository 책임이 아니다. Bootstrap이나 전용 Mapper가 ScriptableObject를 읽고 Domain 객체를 만들 수 있다.

## Manager

Manager는 Application Service 계층이다.

- 기능 유스케이스를 조율한다.
- Domain 메서드를 호출한다.
- 지속성이 필요한 경우에만 Repository를 사용한다.
- 현재 DTO를 캐싱한다.
- 성공적인 상태 변경 뒤에 이벤트를 발행한다.
- 플랫폼 기능이 필요하면 `IPlatformServices` 같은 포트에 의존한다.

Manager는 Unity 생명주기가 꼭 필요하지 않다면 일반 C# 클래스로 작성한다.

## Infrastructure

Infrastructure는 외부 의존 구현을 둔다.

- `SteamPlatformServices`
- `AndroidPlatformServices`
- `IOSPlatformServices`
- `NullPlatformServices`
- `SteamCloudSaveRepository`
- `AndroidSaveRepository`
- `IOSSaveRepository`

Infrastructure 구현은 Steamworks, Android/iOS API, 파일 시스템, 클라우드 저장소 같은 외부 세부사항을 알아도 된다. Domain과 Manager는 구체 구현을 직접 참조하지 않는다.

## UI와 MVP

View:

- MonoBehaviour일 수 있다.
- 직렬화된 Unity 참조를 소유한다.
- Unity 콜백을 C# 이벤트로 변환한다.
- DTO 값을 표시하고 요청받은 UI 피드백을 재생한다.

Presenter:

- 가능하면 일반 C# 클래스로 작성한다.
- `Initialize()`에서 구독하고 `Dispose()`에서 해제한다.
- Manager 유스케이스를 호출한다.
- 게임 규칙을 직접 판단하지 않는다.

## Bootstrap

Bootstrap:

- 보통 씬 또는 기능 영역당 하나의 MonoBehaviour다.
- 직렬화된 View와 정적 레벨 에셋을 읽는다.
- Domain, Repository, Manager, Presenter, 플랫폼 서비스를 만든다.
- 구체 구현 연결을 소유한다.

서비스 로케이터와 전역 싱글톤은 정말 프로세스 전체에서 하나여야 하는 서비스에만 제한적으로 사용한다.

## 비주얼과 피드백 경계

DOTween, FEEL, Shapes, shader, material, audio는 UI/Presenter/View/Effect 코드에서만 사용한다. Domain과 Manager는 프레젠테이션 계층이 시각화할 수 있는 결과를 발행해야 한다.

