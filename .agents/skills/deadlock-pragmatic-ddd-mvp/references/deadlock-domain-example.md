# DeadLock 도메인 예시

## Resource Rule 형태

Resource Rule은 순수 C#으로 작성하고 Unity 객체를 참조하지 않는다.

```csharp
public interface IResourceRule
{
    bool CanConnect(ConnectionContext context);
    void OnConnected(ConnectionContext context, RuleEffects effects);
    bool CanFinish(ResourceState resource);
    void OnRoundEnded(ResourceState resource, int round, RuleEffects effects);
    IResourceRule Snapshot();
}
```

Rule은 다른 도메인 객체나 Manager가 적용할 상태 변화를 `RuleEffects`로 보고한다. Rule에서 시각 효과를 직접 재생하지 않는다.

## Manager 유스케이스 형태

```csharp
public sealed class LevelPlayManager
{
    public event Action<LevelDTO> OnLevelChanged;

    private Board _board;
    private LevelDTO _currentLevel;

    public LevelDTO CurrentLevel => _currentLevel;

    public AssignColorResult AssignColor(
        ProcessId processId,
        ColorSlotId colorSlotId,
        ResourceId resourceId)
    {
        AssignColorDomainResult result = _board.AssignColor(
            processId,
            colorSlotId,
            resourceId);

        if (!result.Success)
        {
            return AssignColorResult.Fail(result.ErrorCode);
        }

        RefreshDTO();
        OnLevelChanged?.Invoke(_currentLevel);
        return AssignColorResult.Ok();
    }
}
```

## Presenter 형태

```csharp
public sealed class BoardPresenter : IDisposable
{
    private readonly IBoardView _view;
    private readonly LevelPlayManager _manager;

    public void Initialize()
    {
        _view.OnColorDropped += HandleColorDropped;
        _manager.OnLevelChanged += HandleLevelChanged;
        _view.Refresh(_manager.CurrentLevel);
    }

    public void Dispose()
    {
        _view.OnColorDropped -= HandleColorDropped;
        _manager.OnLevelChanged -= HandleLevelChanged;
    }
}
```

## 플랫폼 포트 형태

Manager는 구체 플랫폼 구현 대신 포트에 의존한다.

```csharp
public interface IPlatformServices
{
    void Initialize();
    void Tick();
    void Shutdown();
    void UnlockAchievement(string achievementId);
    bool SupportsCloudSave { get; }
}
```

구체 구현은 `06.Infrastructure/Platform`에 둔다.

