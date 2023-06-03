using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum NodeTypes
{
    Empty, Process, Resource
}

[CreateAssetMenu()]
[System.Serializable]
public class LevelCreator : ScriptableObject
{
    [SerializeField]
    public List<Node> level = new List<Node>();
    public int row, col;

    public LevelCreator()
    {
        level = new List<Node>();
    }

    public void SetEmptyLevels()
    {
        if (level.Count != row*col)
        {
            level.Clear();
            for (int r = row - 1; r >= 0; r--)
            {
                for (int c = 0; c < col; c++)
                {
                    level.Add(new Node());
                }
            }
        }
    }
}

[System.Serializable]
public class Node
{
    [SerializeField]
    public NodeTypes nodeTypes;
    public List<Color> colors;
    public int maxCount;
    public int fixedNum;
    public bool isSimul;
    public bool isSwitchColor;
    public bool isStartWithEmptyColor;
    public bool isClockOnToOff;
    public bool isClockOffToOn;
    public int clockNum;

    public Node()
    {
        nodeTypes = NodeTypes.Empty;
        fixedNum = 0;
    }
}