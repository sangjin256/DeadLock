using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BatchRename : ScriptableWizard
{
    public string BaseName = "MyObject_";
    public int StartNumber = 0;
    public int Increment = 1;

    [MenuItem("Edit/Batch Rename...")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("Batch Rename", typeof(BatchRename), "Rename");
    }

    private void OnEnable()
    {
        UpdateSelectionHelper();
    }

    private void OnSelectionChange()
    {
        UpdateSelectionHelper();
    }

    void UpdateSelectionHelper()
    {
        helpString = "";
        if (Selection.objects != null) helpString = "Number of objects selected: " + Selection.objects.Length;
    }

    private void OnWizardCreate()
    {
        if (Selection.objects == null) return;

        int PostFix = StartNumber;

        foreach(Object O in Selection.objects)
        {
            if(O is LevelCreator)
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(O), BaseName + PostFix);
            }
            else O.name = BaseName + PostFix;
            PostFix += Increment;
        }
    }
}
