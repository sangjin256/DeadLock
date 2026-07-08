using System;
using System.Collections.Generic;

public sealed class CompositeResourceRule : IResourceRule
{
    private readonly IResourceRule[] _ruleArray;

    public CompositeResourceRule(IReadOnlyList<IResourceRule> ruleList)
    {
        if (ruleList is null)
        {
            throw new ArgumentNullException(nameof(ruleList));
        }

        _ruleArray = new IResourceRule[ruleList.Count];

        for (int i = 0; i < ruleList.Count; i++)
        {
            _ruleArray[i] = ruleList[i] ?? NoResourceRule.Instance;
        }
    }

    public bool CanReserve(ConnectionContext context)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            if (!_ruleArray[i].CanReserve(context))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanOccupy(ConnectionContext context)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            if (!_ruleArray[i].CanOccupy(context))
            {
                return false;
            }
        }

        return true;
    }

    public void OnOccupied(ConnectionContext context, RuleEffects effects)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            _ruleArray[i].OnOccupied(context, effects);
        }
    }

    public bool CanFinish(ResourceNode resource)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            if (!_ruleArray[i].CanFinish(resource))
            {
                return false;
            }
        }

        return true;
    }

    public void OnReleased(ConnectionContext context, RuleEffects effects)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            _ruleArray[i].OnReleased(context, effects);
        }
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            _ruleArray[i].OnRoundEnded(resource, round, effects);
        }
    }

    public void ResetSimulationState(ResourceNode resource)
    {
        for (int i = 0; i < _ruleArray.Length; i++)
        {
            _ruleArray[i].ResetSimulationState(resource);
        }
    }

    public IResourceRule Snapshot()
    {
        IResourceRule[] snapshotArray = new IResourceRule[_ruleArray.Length];

        for (int i = 0; i < _ruleArray.Length; i++)
        {
            snapshotArray[i] = _ruleArray[i].Snapshot();
        }

        return new CompositeResourceRule(snapshotArray);
    }
}
