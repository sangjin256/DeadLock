using System.Collections.Generic;

public sealed class LevelPlayProcessDTO
{
    public readonly int Id;
    public readonly int Row;
    public readonly int Column;
    public readonly ELevelPlayProcessState State;
    public readonly LevelPlaySlotDTO[] SlotArray;

    public LevelPlayProcessDTO(int id,
                               int row,
                               int column,
                               ELevelPlayProcessState state,
                               IReadOnlyList<LevelPlaySlotDTO> slotList)
    {
        Id = id;
        Row = row;
        Column = column;
        State = state;
        SlotArray = new LevelPlaySlotDTO[slotList.Count];

        for (int i = 0; i < slotList.Count; i++)
        {
            SlotArray[i] = slotList[i];
        }
    }
}
