# DeadLock C# 예시

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

