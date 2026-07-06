using System;
using System.Collections.Generic;

public sealed class ColorSwitchRule : IResourceRule
{
    private readonly ColorId[] _colorArray;

    private int _currentColorIndex;
    private int _lastReleasedProcessId;
    private bool _wasReleasedThisRound;

    public ColorSwitchRule(IReadOnlyList<ColorId> colorList)
    {
        if (colorList is null)
        {
            throw new ArgumentNullException(nameof(colorList));
        }

        _colorArray = new ColorId[colorList.Count];

        for (int i = 0; i < colorList.Count; i++)
        {
            _colorArray[i] = colorList[i];
        }

        _lastReleasedProcessId = -1;
    }

    public bool CanReserve(ConnectionContext context)
    {
        return ContainsColor(context.Slot.RequiredColor);
    }

    public bool CanOccupy(ConnectionContext context)
    {
        return context.Resource.Color == context.Slot.RequiredColor;
    }

    public void OnOccupied(ConnectionContext context, RuleEffects effects)
    {
    }

    public bool CanFinish(ResourceNode resource)
    {
        return true;
    }

    public void OnReleased(ConnectionContext context, RuleEffects effects)
    {
        if (_lastReleasedProcessId == context.Process.Id)
        {
            return;
        }

        AdvanceColor(context.Resource);
        _lastReleasedProcessId = context.Process.Id;
        _wasReleasedThisRound = true;
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
        if (resource.OccupiedConnectionIdList.Count == 0 && !resource.WasOccupiedThisRound && !_wasReleasedThisRound)
        {
            AdvanceColor(resource);
        }

        _lastReleasedProcessId = -1;
        _wasReleasedThisRound = false;
    }

    public void ResetSimulationState(ResourceNode resource)
    {
        _currentColorIndex = 0;
        _lastReleasedProcessId = -1;
        _wasReleasedThisRound = false;

        if (_colorArray.Length > 0)
        {
            resource.ChangeColor(_colorArray[_currentColorIndex]);
        }
    }

    public IResourceRule Snapshot()
    {
        return new ColorSwitchRule(_colorArray);
    }

    private bool ContainsColor(ColorId color)
    {
        for (int i = 0; i < _colorArray.Length; i++)
        {
            if (_colorArray[i] == color)
            {
                return true;
            }
        }

        return false;
    }

    private void AdvanceColor(ResourceNode resource)
    {
        if (_colorArray.Length == 0)
        {
            return;
        }

        _currentColorIndex++;

        if (_currentColorIndex >= _colorArray.Length)
        {
            _currentColorIndex = 0;
        }

        resource.ChangeColor(_colorArray[_currentColorIndex]);
    }
}
