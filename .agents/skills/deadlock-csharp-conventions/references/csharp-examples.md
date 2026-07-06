# DeadLock C# 예시

## Backing Field와 프로퍼티

```csharp
public sealed class LevelProgress
{
    private int _unlockedLevelId;
    public int UnlockedLevelId => _unlockedLevelId;

    public void UnlockLevel(int levelId)
    {
        if (levelId <= _unlockedLevelId)
        {
            return;
        }

        _unlockedLevelId = levelId;
    }
}
```

자동 프로퍼티보다 private backing field와 읽기 전용 getter를 우선한다. 상태 변경은 setter보다 의도가 드러나는 메서드로 표현한다.

## SerializeField와 Tooltip

```csharp
public sealed class NodeView : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Node body renderer used for color and highlight feedback.")]
    private SpriteRenderer _bodyRenderer;
    public SpriteRenderer BodyRenderer => _bodyRenderer;

    [SerializeField]
    private float _highlightDuration = 0.2f;
    public float HighlightDuration => _highlightDuration;
}
```

Inspector 노출 필드는 `[SerializeField] private`를 사용하고, 설명이 필요하면 주석보다 `[Tooltip]`을 사용한다.

## DTO

```csharp
public sealed class LevelProgressDTO
{
    public readonly int UnlockedStage;
    public readonly int[] ClearedStages;

    public LevelProgressDTO(int unlockedStage, int[] clearedStages)
    {
        UnlockedStage = unlockedStage;
        ClearedStages = clearedStages;
    }
}
```

DTO는 생성자에서 초기화되는 `public readonly` 필드를 가진 불변 `sealed class`로 작성한다.

## Manager 이벤트

```csharp
public sealed class ProgressManager
{
    public event Action<LevelProgressDTO> OnProgressChanged;

    private LevelProgress _progress;
    private LevelProgressDTO _currentProgress;
    public LevelProgressDTO CurrentProgress => _currentProgress;

    public void MarkCleared(int stageIndex)
    {
        if (!_progress.TryMarkCleared(stageIndex))
        {
            return;
        }

        _currentProgress = _progress.ToDTO();
        OnProgressChanged?.Invoke(_currentProgress);
    }
}
```

이벤트는 `public event Action...` 형태를 기본으로 하고, 상태 변경 후 캐싱된 DTO를 발행한다.

## View 생명주기와 구독

```csharp
public sealed class LevelView : MonoBehaviour
{
    public event Action OnRetryClicked;

    [SerializeField]
    private Button _retryButton;

    private void OnEnable()
    {
        _retryButton.onClick.AddListener(HandleRetryClicked);
    }

    private void OnDisable()
    {
        _retryButton.onClick.RemoveListener(HandleRetryClicked);
    }

    private void HandleRetryClicked()
    {
        OnRetryClicked?.Invoke();
    }
}
```

Unity 생명주기 메서드는 접근 제한자를 명시하고 일반 메서드보다 위쪽에 둔다. 구독과 해제는 `OnEnable()`/`OnDisable()`에서 짝을 맞춘다.

## Presenter 구독과 해제

```csharp
public sealed class LevelPresenter : IDisposable
{
    private readonly LevelView _view;
    private readonly LevelPlayManager _manager;

    public LevelPresenter(LevelView view, LevelPlayManager manager)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public void Initialize()
    {
        _view.OnRetryClicked += HandleRetryClicked;
        _manager.OnLevelChanged += HandleLevelChanged;
    }

    public void Dispose()
    {
        _view.OnRetryClicked -= HandleRetryClicked;
        _manager.OnLevelChanged -= HandleLevelChanged;
    }

    private void HandleRetryClicked()
    {
        _manager.RestartLevel();
    }

    private void HandleLevelChanged(LevelDTO level)
    {
        _view.RefreshLevel(level);
    }
}
```

Presenter는 `Initialize()`에서 구독하고 `Dispose()`에서 해제한다. 게임 규칙은 Presenter가 다시 판단하지 않는다.

## UniTask View 흐름

```csharp
public async UniTask PlayConnectionAsync(
    ConnectionViewModel model,
    CancellationToken cancellationToken)
{
    await _connectionView.PlayAsync(model, cancellationToken);
}
```

비동기 프레젠테이션 코드는 씬 또는 View 생명주기에 맞춰 취소 가능하게 만든다.

## UniTask와 MonoBehaviour 취소

```csharp
public sealed class ConnectionView : MonoBehaviour
{
    public async UniTask PlayAsync(ConnectionViewModel model, CancellationToken cancellationToken)
    {
        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        ApplyModel(model);
    }

    public UniTask PlayWithDestroyCancellationAsync(ConnectionViewModel model)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        return PlayAsync(model, cancellationToken);
    }

    private void ApplyModel(ConnectionViewModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        // Apply visual state here.
    }
}
```

장기 실행 작업과 View 생명주기에 묶인 작업은 `CancellationToken`을 받는다. MonoBehaviour 파괴 시 취소가 필요하면 `GetCancellationTokenOnDestroy()`를 사용한다.

## 백그라운드 작업

```csharp
public async UniTask<LevelSaveData> ParseSaveDataAsync(
    string json,
    CancellationToken cancellationToken)
{
    LevelSaveData saveData = await UniTask.RunOnThreadPool(
        () => LevelSaveDataParser.Parse(json),
        cancellationToken: cancellationToken);

    await UniTask.SwitchToMainThread(cancellationToken);
    return saveData;
}
```

백그라운드 스레드에서는 Unity API에 접근하지 않는다. Unity 객체 접근이 필요하면 메인 스레드로 복귀한다.

## 컬렉션 필드와 읽기 전용 노출

```csharp
public sealed class ResourceNode
{
    private readonly List<int> _connectionIdList;
    public IReadOnlyList<int> ConnectionIdList => _connectionIdList;

    public ResourceNode()
    {
        _connectionIdList = new List<int>();
    }

    public void AddConnection(int connectionId)
    {
        if (_connectionIdList.Contains(connectionId))
        {
            return;
        }

        _connectionIdList.Add(connectionId);
    }
}
```

컬렉션 필드에는 `List`, `Dict`, `Set` 같은 접미사를 붙이고, 외부에는 mutable 컬렉션을 그대로 노출하지 않는다.

## Bootstrap 플랫폼 선택

```csharp
private IPlatformServices CreatePlatformServices()
{
#if DEADLOCK_STEAM
    return new SteamPlatformServices();
#elif UNITY_ANDROID
    return new AndroidPlatformServices();
#elif UNITY_IOS
    return new IOSPlatformServices();
#else
    return new NullPlatformServices();
#endif
}
```

구체 구현 선택은 Bootstrap에서 하고, Manager는 `IPlatformServices`만 알게 한다.
