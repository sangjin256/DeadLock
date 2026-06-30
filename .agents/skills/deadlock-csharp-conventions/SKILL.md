---
name: deadlock-csharp-conventions
description: "DeadLock 프로젝트의 Unity C# 코딩 컨벤션입니다. Domain 모델, DTO, Repository, Manager, Presenter, View, Bootstrap, Infrastructure, UniTask 비동기 코드, DOTween/FEEL/Shapes 프레젠테이션 코드, 아키텍처 경계 검토를 작성/수정/리팩터링/리뷰할 때 사용합니다."
---

# DeadLock C# 컨벤션

## 작업 순서

1. 각 타입을 Domain, DTO, Repository, Manager, View, Presenter, Bootstrap, Infrastructure, 플랫폼 서비스, 비주얼/이펙트 코드 중 하나로 분류한다.
2. 새 타입을 작성하거나 스타일에 민감한 코드를 리뷰할 때 `references/csharp-examples.md`를 읽는다.
3. 사용자가 명시적으로 요청하지 않은 동작 변경은 피한다.
4. Unity 검증은 자동 실행하지 않는다. 대신 검증 여부를 보고한다.

## 이름과 배치

- 타입, 메서드, 프로퍼티, public 필드, 이벤트는 `PascalCase`를 사용한다.
- private/protected 필드는 `_camelCase`를 사용한다.
- 매개변수와 지역 변수는 `camelCase`를 사용한다.
- 인터페이스는 `I`, enum은 `E` 접두사를 사용한다.
- `DTO`, `ToDTO()`는 대문자 표기를 유지한다.
- 파일명은 최상위 타입명과 맞춘다.
- Allman 중괄호 스타일을 선호한다.
- 필요한 경우 backing field는 관련 프로퍼티 바로 아래에 둔다.
- `[SerializeField]`는 필드 위 별도 줄에 둔다.
- 프로젝트 namespace 정책이 안정되기 전까지 namespace를 강제하지 않는다.

## 계층 제한

- Domain, Repository, Manager는 기본적으로 일반 C# 클래스다.
- Domain은 UnityEngine, MonoBehaviour, DOTween, FEEL, Shapes, SceneManager, Steamworks, Resources, Addressables, UI 타입에 의존하지 않는다.
- Repository는 저장 구현 세부사항에 의존할 수 있지만 UI에는 의존하지 않는다.
- Manager는 Domain과 Repository를 호출하고, DTO를 캐싱하고, 이벤트를 발행할 수 있다. 애니메이션, 피드백 재생, 씬 오브젝트 접근은 하지 않는다.
- Infrastructure는 Steamworks, Android/iOS API, 파일 시스템, 클라우드 저장 같은 외부 구현 세부사항을 가질 수 있다.
- View, Presenter, Bootstrap, Effect/View helper는 Unity API를 사용할 수 있다.

## DTO 규칙

- DTO는 생성자에서 초기화되는 `public readonly` 필드를 가진 불변 `sealed class`로 작성한다.
- DTO 생성자는 Domain 객체를 직접 받지 않는다. `ToDTO()`에서 원시 값이나 다른 DTO 값을 전달한다.
- `Update()`나 자주 호출되는 프로퍼티에서 DTO를 할당하지 않는다.
- 상태 변경 후 DTO를 캐싱한다.

## UniTask 비동기 규칙

- 새 Unity-facing 비동기 흐름에는 UniTask를 사용한다.
- 비동기 메서드에는 `Async` suffix를 붙인다.
- 호출자, 씬, View보다 오래 지속될 수 있는 작업에는 `CancellationToken`을 전달한다.
- 백그라운드 스레드에서 Unity API에 접근하지 않는다.
- fire-and-forget은 되도록 피하고, 필요하면 실패 처리 의도를 분명히 드러낸다.

## DOTween, FEEL, Shapes

- DOTween, FEEL, Shapes는 View, Presenter, Effect, feedback, scene composition 코드에서만 사용한다.
- Domain, Repository, Manager에서는 사용하지 않는다.
- 재사용 가능한 비주얼 동작은 `ConnectionView`, `NodeView`, `FeedbackPresenter`, 전용 effect component 같은 프레젠테이션 클래스에 둔다.
- View가 비활성화되거나 Dispose될 때 tween과 비동기 시퀀스를 종료하거나 취소한다.

## 이벤트

- 이벤트는 `event` 키워드로 선언한다.
- 구독과 해제를 짝으로 작성한다.
- View는 보통 `OnEnable()`에서 구독하고 `OnDisable()`에서 해제한다.
- Presenter는 보통 `Initialize()`에서 구독하고 `Dispose()`에서 해제한다.

## 자체 리뷰

- 게임 규칙이 Presenter나 View가 아니라 Domain에 있는지 확인한다.
- 예상 가능한 게임 실패가 예외가 아니라 result, `Try` 메서드, enum으로 표현되는지 확인한다.
- DTO가 매 프레임 생성되지 않는지 확인한다.
- Domain과 Manager에 Unity 전용 의존성이 들어가지 않았는지 확인한다.
- Steam/Android/iOS 구체 구현이 Infrastructure 밖으로 새지 않았는지 확인한다.

