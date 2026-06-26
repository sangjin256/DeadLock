using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IResourceComponent
{
    public void ActiveGameObject(int maxCount, Transform parent);

    public bool CheckConnect(Process P, ProcessColor pc, bool isOnGame);
    public void ConnectComponent(Process P, ProcessColor pc);
    public bool IsFinishable();
    public void FinishComponent(int round, Process P, Resource parent);
    public void DoAfterRound(int round, Resource resource);

    public IResourceComponent DeepCopy();
}
