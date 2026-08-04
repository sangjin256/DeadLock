using System.Collections.Generic;
using Shapes;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class LevelPlayPrefabContractValidator
{
    private const string MenuPath = "Tools/DeadLock/Visuals/Validate LevelPlay Prefab Contracts";
    private const string DefaultSettingsPath = "Assets/05.Visual Resources/Settings/VisualSettings_Default.asset";
    private const string ProcessPrefabPath = "Assets/03.Prefabs/LevelPlay/Nodes/Prefab_Node_Process.prefab";
    private const string ResourcePrefabPath = "Assets/03.Prefabs/LevelPlay/Nodes/Prefab_Node_Resource.prefab";
    private const string ConnectionPrefabPath = "Assets/03.Prefabs/LevelPlay/Connections/Prefab_Connection.prefab";
    private const string RelayPrefabPath = "Assets/03.Prefabs/LevelPlay/Connections/Prefab_Relay.prefab";
    private const int MaximumProcessSlotCount = 6;
    private const int MaximumResourceCapacity = 4;
    private const int MaximumColorSwitchColorCount = 5;

    [MenuItem(MenuPath)]
    public static void ValidatePrefabContracts()
    {
        List<string> errorList = new List<string>();

        ValidateProcessPrefab(errorList);
        ValidateResourcePrefab(errorList);
        ValidateConnectionPrefab(errorList);
        ValidateRelayPrefab(errorList);

        if (errorList.Count == 0)
        {
            Debug.Log("[LevelPlayPrefabContractValidator] All LevelPlay prefab contracts are valid.");
            return;
        }

        for (int i = 0; i < errorList.Count; i++)
        {
            Debug.LogError("[LevelPlayPrefabContractValidator] " + errorList[i]);
        }

        Debug.LogError("[LevelPlayPrefabContractValidator] Contract validation failed with " + errorList.Count + " issue(s).");
    }

    private static void ValidateProcessPrefab(List<string> errorList)
    {
        GameObject prefab = LoadPrefab(ProcessPrefabPath, errorList);

        if (prefab == null)
        {
            return;
        }

        ProcessView view = RequireComponent<ProcessView>(prefab.transform, string.Empty, errorList);
        RequireVisualSettings(view, ProcessPrefabPath, errorList);
        RequireComponent<Disc>(prefab.transform, "Background/Shadow", errorList);
        RequireComponent<Disc>(prefab.transform, "Background/Stroke", errorList);
        RequireComponent<Disc>(prefab.transform, "Background/Fill", errorList);
        RequireComponent<Disc>(prefab.transform, "Port/Disc", errorList);
        RequireTransform(prefab.transform, "RequiredColorTray/Variants", errorList);

        for (int slotCount = 1; slotCount <= MaximumProcessSlotCount; slotCount++)
        {
            string variantPath = "RequiredColorTray/Variants/SlotCount_" + slotCount;
            RequireTransform(prefab.transform, variantPath, errorList);

            for (int chipIndex = 1; chipIndex <= slotCount; chipIndex++)
            {
                string chipPath = variantPath + "/ColorChips/Chip_" + chipIndex;
                RequireComponent<ProcessSlotInput>(prefab.transform, chipPath, errorList);
                RequireComponent<CircleCollider2D>(prefab.transform, chipPath, errorList);
                RequireComponent<Disc>(prefab.transform, chipPath + "/Rim", errorList);
                RequireComponent<Disc>(prefab.transform, chipPath + "/Fill", errorList);
            }
        }

        RequireTransform(prefab.transform, "StateOverlay/Selected", errorList);
        RequireTransform(prefab.transform, "StateOverlay/Waiting", errorList);
        RequireTransform(prefab.transform, "StateOverlay/Failed", errorList);
        RequireTransform(prefab.transform, "StateOverlay/Completed", errorList);
        RequireComponent<Disc>(prefab.transform, "StateOverlay/Selected", errorList);
        RequireComponent<Disc>(prefab.transform, "StateOverlay/Waiting", errorList);
        RequireComponent<Disc>(prefab.transform, "StateOverlay/Failed", errorList);
        RequireComponent<Disc>(prefab.transform, "StateOverlay/Completed", errorList);
    }

    private static void ValidateResourcePrefab(List<string> errorList)
    {
        GameObject prefab = LoadPrefab(ResourcePrefabPath, errorList);

        if (prefab == null)
        {
            return;
        }

        ResourceView view = RequireComponent<ResourceView>(prefab.transform, string.Empty, errorList);
        RequireVisualSettings(view, ResourcePrefabPath, errorList);
        RequireComponent<BoxCollider2D>(prefab.transform, string.Empty, errorList);
        RequireComponent<Rectangle>(prefab.transform, "Background/Shadow", errorList);
        RequireComponent<Rectangle>(prefab.transform, "Background/Stroke", errorList);
        RequireComponent<Rectangle>(prefab.transform, "Background/Fill", errorList);

        for (int capacity = 1; capacity <= MaximumResourceCapacity; capacity++)
        {
            string capacityPath = "OccupancySlots/Capacity_" + capacity;
            RequireTransform(prefab.transform, capacityPath, errorList);

            for (int slotIndex = 1; slotIndex <= capacity; slotIndex++)
            {
                string slotPath = capacityPath + "/Slot_" + slotIndex;
                RequireComponent<Disc>(prefab.transform, slotPath + "/Rim", errorList);
                RequireComponent<Disc>(prefab.transform, slotPath + "/Fill", errorList);

                string simultaneousSlotPath = "OccupancySlots/SimultaneousCapacity_" + capacity + "/Slot_" + slotIndex;
                RequireComponent<Disc>(prefab.transform, simultaneousSlotPath + "/Rim", errorList);
                RequireComponent<Disc>(prefab.transform, simultaneousSlotPath + "/Fill", errorList);
            }
        }

        for (int colorCount = 1; colorCount <= MaximumColorSwitchColorCount; colorCount++)
        {
            string variantPath = "RuleVisuals/ColorSwitch/Variant_" + colorCount;
            RequireTransform(prefab.transform, variantPath, errorList);

            for (int chipIndex = 1; chipIndex <= colorCount; chipIndex++)
            {
                string chipPath = variantPath + "/Chip_" + chipIndex;
                RequireComponent<Disc>(prefab.transform, chipPath + "/Rim", errorList);
                RequireComponent<Disc>(prefab.transform, chipPath + "/Fill", errorList);
                RequireComponent<Disc>(prefab.transform, chipPath + "/NextMarker", errorList);
            }

            RequireTransform(prefab.transform, variantPath + "/Pointer", errorList);

            for (int linkIndex = 1; linkIndex < colorCount; linkIndex++)
            {
                RequireComponent<Line>(prefab.transform, variantPath + "/Links/Link_" + linkIndex, errorList);
            }
        }

        RequireTextMeshPro(prefab.transform, "RuleVisuals/Clock/Normal", errorList);
        RequireTextMeshPro(prefab.transform, "RuleVisuals/Clock/Compact", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Normal/Badge/Shadow", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Normal/Badge/Stroke", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Normal/Badge/Fill", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Compact/Badge/Shadow", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Compact/Badge/Stroke", errorList);
        RequireComponent<Disc>(prefab.transform, "RuleVisuals/Clock/Compact/Badge/Fill", errorList);
        RequireTransform(prefab.transform, "RuleVisuals/EmptyColor", errorList);

        for (int capacity = 1; capacity <= MaximumResourceCapacity; capacity++)
        {
            string simultaneousPath = "RuleVisuals/Simultaneous/Capacity_" + capacity;
            RequireComponent<Disc>(prefab.transform, simultaneousPath + "/ActivationLight", errorList);

            for (int linkIndex = 1; linkIndex <= capacity; linkIndex++)
            {
                RequireComponent<Line>(prefab.transform, simultaneousPath + "/Links/Link_" + linkIndex, errorList);
            }
        }

        RequireComponent<Disc>(prefab.transform, "StateOverlay/Lock", errorList);

        if (prefab.transform.Find("RelayAnchors") != null)
        {
            errorList.Add(ResourcePrefabPath + ": RelayAnchors must not exist. Relay endpoints are direction-derived at runtime.");
        }
    }

    private static void ValidateConnectionPrefab(List<string> errorList)
    {
        GameObject prefab = LoadPrefab(ConnectionPrefabPath, errorList);

        if (prefab == null)
        {
            return;
        }

        ConnectionView view = RequireComponent<ConnectionView>(prefab.transform, string.Empty, errorList);
        RequireVisualSettings(view, ConnectionPrefabPath, errorList);
        RequireComponent<Line>(prefab.transform, "Background/Shadow", errorList);
        RequireComponent<Line>(prefab.transform, "Track/Line", errorList);
    }

    private static void ValidateRelayPrefab(List<string> errorList)
    {
        GameObject prefab = LoadPrefab(RelayPrefabPath, errorList);

        if (prefab == null)
        {
            return;
        }

        RelayView view = RequireComponent<RelayView>(prefab.transform, string.Empty, errorList);
        RequireVisualSettings(view, RelayPrefabPath, errorList);
        RequireComponent<Line>(prefab.transform, "Links/StartStub", errorList);
        RequireComponent<Line>(prefab.transform, "Links/EndStub", errorList);
        RequireComponent<Disc>(prefab.transform, "Endpoints/LinkStart", errorList);
        RequireComponent<Disc>(prefab.transform, "Endpoints/LinkEnd", errorList);
        RequireComponent<Disc>(prefab.transform, "Endpoints/TransferSender", errorList);
        RequireComponent<Disc>(prefab.transform, "Endpoints/TransferReceiver", errorList);
        RequireComponent<Disc>(prefab.transform, "Endpoints/TransferDirection", errorList);
    }

    private static GameObject LoadPrefab(string prefabPath, List<string> errorList)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            errorList.Add("Prefab is missing: " + prefabPath);
        }

        return prefab;
    }

    private static Transform RequireTransform(Transform root, string path, List<string> errorList)
    {
        Transform transform = string.IsNullOrEmpty(path) ? root : root.Find(path);

        if (transform == null)
        {
            errorList.Add(root.name + ": required transform is missing: " + path);
        }

        return transform;
    }

    private static T RequireComponent<T>(Transform root, string path, List<string> errorList)
        where T : Component
    {
        Transform transform = RequireTransform(root, path, errorList);

        if (transform == null)
        {
            return null;
        }

        T component = transform.GetComponent<T>();

        if (component == null)
        {
            errorList.Add(root.name + ": required " + typeof(T).Name + " is missing at " + path);
        }

        return component;
    }

    private static void RequireTextMeshPro(Transform root, string path, List<string> errorList)
    {
        Transform transform = RequireTransform(root, path, errorList);

        if (transform == null)
        {
            return;
        }

        if (transform.GetComponentInChildren<TextMeshPro>(true) == null)
        {
            errorList.Add(root.name + ": required TextMeshPro is missing below " + path);
        }
    }

    private static void RequireVisualSettings(Component view, string prefabPath, List<string> errorList)
    {
        if (view == null)
        {
            return;
        }

        SerializedObject serializedView = new SerializedObject(view);
        SerializedProperty visualSettingsProperty = serializedView.FindProperty("_visualSettings");

        if (visualSettingsProperty == null || visualSettingsProperty.objectReferenceValue == null)
        {
            errorList.Add(prefabPath + ": " + view.GetType().Name + " must reference VisualSettings_Default.asset.");
            return;
        }

        string settingsPath = AssetDatabase.GetAssetPath(visualSettingsProperty.objectReferenceValue);

        if (settingsPath != DefaultSettingsPath)
        {
            errorList.Add(prefabPath + ": " + view.GetType().Name + " must reference " + DefaultSettingsPath + ".");
        }
    }
}
