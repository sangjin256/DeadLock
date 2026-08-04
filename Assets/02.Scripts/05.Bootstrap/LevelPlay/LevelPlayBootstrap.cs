using UnityEngine;

public sealed class LevelPlayBootstrap : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Runtime level asset used to create the LevelPlayManager session.")]
    private LevelSO _levelSO;
    public LevelSO LevelSO => _levelSO;

    private LevelPlayManager _manager;
    public LevelPlayManager Manager => _manager;

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private string _initializationError = string.Empty;
    public string InitializationError => _initializationError;

    private void Awake()
    {
        InitializeLevelPlay();
    }

    private void InitializeLevelPlay()
    {
        _manager = null;
        _isInitialized = false;
        _initializationError = string.Empty;

        if (_levelSO == null)
        {
            FailInitialization("LevelSO reference is missing.");
            return;
        }

        LevelSOMapper mapper = new LevelSOMapper();
        LevelDefinition definition = mapper.ToLevelDefinition(_levelSO);
        LevelStarThresholdData starThresholdData = _levelSO.StarThresholdData;
        LevelPlaySettings settings = CreateSettings(starThresholdData);
        LevelPlayManager manager = new LevelPlayManager(new LevelBoardFactory(), new LevelDefinitionValidator());
        LevelPlayCommandResult result = manager.LoadLevel(definition, settings);

        if (!result.Success)
        {
            FailInitialization(CreateInitializationError(result));
            return;
        }

        _manager = manager;
        _isInitialized = true;
    }

    private static LevelPlaySettings CreateSettings(LevelStarThresholdData starThresholdData)
    {
        if (starThresholdData == null)
        {
            return null;
        }

        return new LevelPlaySettings(starThresholdData.ThreeStarRoundCount,
                                     starThresholdData.TwoStarRoundCount,
                                     starThresholdData.OneStarRoundCount);
    }

    private static string CreateInitializationError(LevelPlayCommandResult result)
    {
        if (result.ValidationMessageArray.Length > 0)
        {
            return string.Join("\n", result.ValidationMessageArray);
        }

        switch (result.Error)
        {
            case ELevelPlayCommandError.InvalidSettings:
                return "Level star thresholds must be positive and ordered as three-star <= two-star <= one-star.";

            case ELevelPlayCommandError.InvalidDefinition:
                return "LevelDefinition validation failed.";

            default:
                return $"LevelPlayManager initialization failed: {result.Error}.";
        }
    }

    private void FailInitialization(string error)
    {
        _initializationError = error;
        Debug.LogError($"[LevelPlayBootstrap] {_initializationError}", this);
    }
}
