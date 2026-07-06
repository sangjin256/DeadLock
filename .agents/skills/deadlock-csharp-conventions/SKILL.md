---
name: deadlock-csharp-conventions
description: "DeadLock 프로젝트의 Unity C# 코딩 컨벤션입니다. Domain 모델, DTO, Repository, Manager, Presenter, View, Bootstrap, Infrastructure, UniTask 비동기 코드, DOTween/FEEL/Shapes 프레젠테이션 코드, 아키텍처 경계 검토를 작성/수정/리팩터링/리뷰할 때 사용합니다."
---

# DeadLock C# 컨벤션

이 스킬은 `Development Guide_Deadlock_2.pdf` v1.0.0의 코딩 컨벤션을 DeadLock의 실용 DDD와 Passive View MVP 구조에 맞게 적용한다.

## 적용 범위

- 신규 코드와 마이그레이션 대상 코드는 `Assets/02.Scripts` 구조와 이 컨벤션을 우선 적용한다.
- `Assets/99.External Assets` 아래 외부 패키지와 샘플 코드는 검사 대상에서 제외한다.
- `Assets/Outdated` 코드는 레거시로 보고, 기능 단위로 옮기거나 수정할 때만 컨벤션을 적용한다.
- 기존 코드를 컨벤션만 맞추기 위해 대규모로 정리하지 않는다. 요청된 작업 범위 안에서 점진적으로 맞춘다.
- 사용자가 명시적으로 요청하지 않은 동작 변경은 피한다.
- Unity 검증은 자동 실행하지 않는다. 대신 검증 여부를 보고한다.

## 작업 순서

1. 각 타입을 Domain, DTO, Repository, Manager, View, Presenter, Bootstrap, Infrastructure, 플랫폼 서비스, 비주얼/이펙트 코드 중 하나로 분류한다.
2. 새 타입을 작성하거나 스타일에 민감한 코드를 리뷰할 때 `references/csharp-examples.md`를 읽는다.
3. DDD/MVP 경계가 관련되면 `deadlock-pragmatic-ddd-mvp` 스킬을 함께 따른다.
4. 비주얼 피드백, DOTween, FEEL, Shapes, shader, UniTask 연출 흐름이 관련되면 `deadlock-visual-feedback` 스킬을 함께 따른다.
5. 사용자가 명시적으로 요청하지 않은 동작 변경은 피한다.
6. Unity 검증은 자동 실행하지 않는다. 대신 검증 여부를 보고한다.

## 이름과 파일

- 타입, 메서드, 프로퍼티, public 필드, 이벤트는 `PascalCase`를 사용한다.
- private, protected, internal, protected internal 필드는 `_camelCase`를 사용한다.
- 매개변수와 지역 변수는 `camelCase`를 사용한다.
- 약어도 하나의 단어처럼 취급한다. 예: `MyRPC` 대신 `MyRpc`, `LoadURL` 대신 `LoadUrl`.
- 인터페이스는 `I`, enum은 `E` 접두사를 사용한다.
- `DTO`, `ToDTO()`는 프로젝트 관례상 대문자 표기를 유지한다.
- 파일명은 최상위 타입명과 맞춘다.
- interface, enum, class, struct는 기본적으로 파일을 분리한다. 한 파일에 최상위 타입을 여러 개 두지 않는다.
- 컬렉션 필드명에는 컨테이너 접미사를 붙인다: `List`, `Dict`, `Set`, `Queue`, `Stack`, `Array`.
- `Dictionary` 접미사는 `Dict`로 축약한다.
- `SortedList`, `SortedSet`, `SortedDictionary`는 필드명 접미사에서 `Sorted`를 생략한다.
- UI 계층 타입은 `UI_` 접두사를 강제하지 않는다. DeadLock은 역할 기반 이름인 `NodeView`, `ConnectionView`, `LevelPresenter`, `FeedbackPresenter`를 우선한다.
- 프로젝트 namespace 정책이 안정되기 전까지 namespace를 강제하지 않는다.

## 포맷과 배치

- Allman 중괄호 스타일을 사용한다.
- `if`, `for`, `foreach`, `while`, `using`, `lock` 블록은 본문이 한 줄이어도 항상 중괄호를 연다.
- 메서드 선언 뒤 여는 중괄호는 새 줄에 둔다.
- `[SerializeField]`는 필드 위 별도 줄에 둔다.
- `[SerializeField] private float _value;`처럼 한 줄에 쓰지 않는다.
- 필드 선언 순서는 public, protected, private 순서를 기본으로 한다.
- 프로퍼티는 관련 backing field 바로 아래에 둔다.
- Unity 생명주기 메서드는 일반 메서드보다 위쪽에 둔다.
- Unity 생명주기 메서드는 초기화부, 게임 로직부, 해체부 순서로 둔다.
  - 초기화부: `Awake()`, `OnEnable()`, `Start()`
  - 게임 로직부: `Update()`, `FixedUpdate()`, collision/trigger 이벤트
  - 해체부: `OnApplicationQuit()`, `OnDisable()`, `OnDestroy()`
- Unity 생명주기 메서드에는 접근 제한자를 반드시 명시한다.
- base 생명주기 구현이 필요한 상속 타입은 override 메서드에서 `base.Awake()`처럼 명시적으로 호출한다.

## 필드와 프로퍼티

- 필드는 기본적으로 `private` 또는 `private readonly`로 둔다.
- Inspector 노출이 필요한 필드는 `[SerializeField] private`로 선언한다.
- 외부 접근이 필요한 경우에만 프로퍼티를 선언한다. 프로퍼티 선언은 필수가 아니다.
- 일반 상태 노출은 자동 프로퍼티보다 backing field와 읽기 전용 getter를 우선한다.
- 지양:

```csharp
public int number { get; private set; }
```

- 권장:

```csharp
private int _number;
public int Number => _number;
```

- 외부에서 상태를 바꿔야 한다면 public setter보다 의도가 드러나는 메서드를 우선한다. 예: `SetScore(int score)`, `MarkCleared(int levelId)`.
- 자동 프로퍼티는 다음처럼 backing field가 불필요한 좁은 경우에만 제한적으로 사용한다.
  - 인터페이스 구현을 위한 단순 pass-through
  - 테스트 전용 fixture나 임시 local helper
  - 값이 생성자에서만 정해지고 타입의 관례상 자동 프로퍼티가 더 명확한 경우
- DTO, 값 객체, 생성 후 불변 데이터는 `public readonly` 필드를 사용할 수 있다.
- Serializable class와 ScriptableObject는 Unity 직렬화 요구가 있을 때 public 필드를 예외적으로 사용할 수 있다.
- public 필드를 새로 만들 때는 DTO/값 객체/Unity 직렬화 요구인지 먼저 확인한다.

## 메서드 이름

- 초기화 메서드는 `Init` + 대상 이름을 사용한다. 예: `InitPlayerData()`, `InitBoard()`.
- 이미 초기화된 대상의 내부 데이터 변경이나 다른 대상 대입은 `Set` + 대상 이름을 사용한다. 예: `SetQuest()`, `SetNodeState()`.
- UI나 View에 데이터 반영은 `Refresh` + 대상 이름을 사용한다. 예: `RefreshNode()`, `RefreshConnectionList()`.
- 적절한 이름이 떠오르지 않으면 임시로 애매한 이름을 만들기보다 PR 설명이나 TODO에 이름 고민을 남긴다.
- 예상 가능한 실패는 예외보다 result, `Try` 메서드, enum으로 표현한다.

## null 체크

- Domain, DTO, Repository, Manager 같은 POCO 코드에서는 `is null`, `is not null`, `ReferenceEquals()`를 사용한다.
- 생성자와 public 메서드의 필수 인자는 필요하면 `ArgumentNullException`으로 빠르게 실패시킨다.
- Unity 객체는 destroyed fake-null 의미가 있으므로 View, Presenter, Bootstrap, Effect 코드에서 `== null`을 사용할 수 있다.
- Domain 계층에는 Unity 객체 null 의미가 들어오면 안 된다.

## 계층 제한

- Domain, Repository, Manager는 기본적으로 일반 C# 클래스다.
- Domain은 UnityEngine, MonoBehaviour, DOTween, FEEL, Shapes, SceneManager, Steamworks, Resources, Addressables, UI 타입에 의존하지 않는다.
- Repository는 저장 구현 세부사항에 의존할 수 있지만 UI에는 의존하지 않는다.
- Manager는 Domain과 Repository를 호출하고, DTO를 캐싱하고, 이벤트를 발행할 수 있다. 애니메이션, 피드백 재생, 씬 오브젝트 접근은 하지 않는다.
- Infrastructure는 Steamworks, Android/iOS API, 파일 시스템, 클라우드 저장 같은 외부 구현 세부사항을 가질 수 있다.
- View, Presenter, Bootstrap, Effect/View helper는 Unity API를 사용할 수 있다.
- Manager는 전역 Unity 싱글톤으로 만들지 않는다. Bootstrap이 생성과 연결을 담당한다.
- 싱글톤은 플랫폼 전역 서비스처럼 정말 프로세스 전체에서 하나여야 하는 경우에만 예외적으로 허용한다.
- 싱글톤의 `OnDestroy()`나 `OnApplicationQuit()`에서 다른 싱글톤 인스턴스에 접근하지 않는다.

## DTO 규칙

- DTO는 생성자에서 초기화되는 `public readonly` 필드를 가진 불변 `sealed class`로 작성한다.
- DTO 생성자는 Domain 객체를 직접 받지 않는다. `ToDTO()`에서 원시 값이나 다른 DTO 값을 전달한다.
- `Update()`나 자주 호출되는 프로퍼티에서 DTO를 할당하지 않는다.
- 상태 변경 후 DTO를 캐싱한다.
- 저장 데이터가 UI DTO와 달라지면 별도 `SaveData`를 도입한다.

## 이벤트와 대리자

- 외부 구독용 콜백은 `public event Action OnSomething` 형태를 기본으로 한다.
- 이벤트는 `event` 키워드로 선언한다.
- 이벤트 이름은 `On` 접두사를 사용한다.
- 이벤트와 delegate 필드는 일반 필드와 프로퍼티보다 위쪽에 둔다.
- `public Action OnSomething;`처럼 외부에서 직접 대입 가능한 delegate 필드는 지양한다.
- 구독과 해제를 짝으로 작성한다.
- View는 보통 `OnEnable()`에서 구독하고 `OnDisable()`에서 해제한다.
- Presenter는 보통 `Initialize()`에서 구독하고 `Dispose()`에서 해제한다.
- GameObject 활성화/비활성화에 맞춰 구독 상태가 바뀌어야 하면 `OnEnable()`과 `OnDisable()`을 사용한다.

## UniTask 비동기 규칙

- 신규 Unity-facing 비동기 흐름에는 UniTask를 사용한다.
- 신규 코드에서 코루틴을 사용하지 않는다. 기존 코루틴은 기능 단위 마이그레이션 때 UniTask로 옮긴다.
- 비동기 메서드에는 `Async` suffix를 붙인다.
- 비동기 메서드는 `UniTask` 또는 `UniTask<T>`를 반환한다.
- `async void`는 사용하지 않는다.
- `UniTaskVoid`는 호출자가 완료를 기다리지 않는 Unity 최상위 이벤트 핸들러에만 제한적으로 사용한다.
- 호출자, 씬, View보다 오래 지속될 수 있는 작업에는 `CancellationToken`을 전달한다.
- MonoBehaviour 생명주기에 묶이는 작업은 `GetCancellationTokenOnDestroy()`를 사용해 파괴 시 취소되게 한다.
- 모든 비동기 로직은 특별한 이유가 없으면 메인 스레드 실행을 기본으로 한다.
- 특정 PlayerLoop 타이밍이 필요하면 `UniTask.Yield(PlayerLoopTiming.X)`를 명시한다.
- CPU 부하가 크고 Unity API에 접근하지 않는 순수 C# 작업만 `UniTask.SwitchToThreadPool()` 또는 `UniTask.RunOnThreadPool()`을 사용한다.
- 백그라운드 스레드에서 Unity API에 접근하지 않는다.
- fire-and-forget은 되도록 피하고, 필요하면 실패 처리 의도를 분명히 드러낸다.

## DOTween, FEEL, Shapes

- DOTween, FEEL, Shapes는 View, Presenter, Effect, feedback, scene composition 코드에서만 사용한다.
- Domain, Repository, Manager에서는 사용하지 않는다.
- 재사용 가능한 비주얼 동작은 `ConnectionView`, `NodeView`, `FeedbackPresenter`, 전용 effect component 같은 프레젠테이션 클래스에 둔다.
- View가 비활성화되거나 Dispose될 때 tween과 비동기 시퀀스를 종료하거나 취소한다.

## 주석과 Tooltip

- 잘못된 코드를 대체하거나 설명하기 위해 주석을 추가하지 않는다.
- 적절한 이름이 부여된 클래스, 변수, 메서드는 주석을 대신한다.
- 대부분의 상황에서는 `//` 주석을 사용하고, 복잡한 의도나 제약을 짧게 설명할 때만 쓴다.
- `[SerializeField]`가 붙은 Inspector 노출 필드에는 주석보다 `[Tooltip]`을 우선 사용한다.
- 죽은 코드나 이전 구현을 주석으로 남기지 않는다.

## 매직 값과 문자열

- 의미를 알 수 없는 숫자나 문자열을 직접 사용하지 않는다.
- 상수는 `const`, 런타임 설정이나 Unity 조정값은 `readonly`, 설정 객체, ScriptableObject, serialized field 중 적절한 형태로 분리한다.
- 빈 문자열은 `""` 대신 `string.Empty`를 사용한다.
- Animator parameter, PlayerPrefs key, Addressables key, scene name 같은 문자열 key는 상수 또는 전용 정의로 모은다.

## var 사용

- 우변에서 타입이 명확한 경우 `var`를 사용할 수 있다. 예: `var nodeList = new List<ProcessNode>();`.
- 메서드 반환값, LINQ 결과, 숫자 리터럴, Unity API 반환처럼 타입이 흐려질 수 있는 경우 명시 타입을 사용한다.
- `int`, `float`, `bool`, `string`, enum 값은 명시 타입을 우선한다.

## 컬렉션 노출

- 외부에서 Add/Remove가 되면 안 되는 컬렉션은 mutable 컬렉션을 그대로 노출하지 않는다.
- 읽기 전용 조회는 `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`, `ReadOnlyCollection<T>`, `ReadOnlyDictionary<TKey, TValue>` 중 적절한 타입을 사용한다.
- `readonly List<T>`는 필드 재할당만 막을 뿐 Add/Remove를 막지 못한다는 점을 기억한다.
- Domain 내부 mutable 컬렉션은 private로 숨기고, 필요한 경우 읽기 전용 인터페이스나 snapshot을 제공한다.

## Unity Editor와 에셋 이름

- Scene과 Script 파일은 PascalCase를 사용한다.
- Prefab, Sprite, AudioClip, Animation 등 에셋은 역할 접두사를 포함한 Snake_Case 변형을 사용한다. 예: `Prefab_UI_ResultInfo`, `Sprite_Node_Process`, `AudioClip_Item_Drop`.
- 외부에서 import한 에셋 폴더는 `Assets/99.External Assets` 아래에 둔다.
- 프로젝트 폴더는 기능보다 콘텐츠 중심 이름을 우선한다.
- Hierarchy와 Prefab의 GameObject 이름은 역할을 알 수 있게 작성한다. `GameObject`, `New Object`, `Temp` 같은 의미 없는 이름을 피한다.

## 자체 리뷰

- 게임 규칙이 Presenter나 View가 아니라 Domain에 있는지 확인한다.
- 예상 가능한 게임 실패가 예외가 아니라 result, `Try` 메서드, enum으로 표현되는지 확인한다.
- DTO가 매 프레임 생성되지 않는지 확인한다.
- Domain과 Manager에 Unity 전용 의존성이 들어가지 않았는지 확인한다.
- Steam/Android/iOS 구체 구현이 Infrastructure 밖으로 새지 않았는지 확인한다.
- 자동 프로퍼티가 backing field + getter 규칙을 어기고 있지 않은지 확인한다.
- public setter가 의도 있는 메서드로 대체될 수 있는지 확인한다.
- Unity 생명주기 메서드의 접근 제한자와 배치 순서가 맞는지 확인한다.
- 이벤트 구독/해제가 짝을 이루는지 확인한다.
- 신규 코드에 코루틴이 들어가지 않았는지 확인한다.
- `Assets/99.External Assets`와 `Assets/Outdated`를 불필요하게 리팩터링하지 않았는지 확인한다.
