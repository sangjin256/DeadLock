internal sealed class LevelNodeColorChipData
{
    private readonly int _colorId;
    public int ColorId => _colorId;

    private readonly int _slotId;
    public int SlotId => _slotId;

    private readonly int _assignmentOrder;
    public int AssignmentOrder => _assignmentOrder;

    private readonly bool _isAssigned;
    public bool IsAssigned => _isAssigned;

    private readonly bool _isSelected;
    public bool IsSelected => _isSelected;

    private readonly bool _isFocused;
    public bool IsFocused => _isFocused;

    public LevelNodeColorChipData(int colorId,
                                  int slotId,
                                  int assignmentOrder,
                                  bool isAssigned,
                                  bool isSelected,
                                  bool isFocused)
    {
        _colorId = colorId;
        _slotId = slotId;
        _assignmentOrder = assignmentOrder;
        _isAssigned = isAssigned;
        _isSelected = isSelected;
        _isFocused = isFocused;
    }
}
