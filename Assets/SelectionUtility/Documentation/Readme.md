# Selection Utility

## Description

Selection Utility is a lightweight Unity tool which facilitates selecting GameObjects in the Scene View
by displaying a context menu with all objects currently under the mouse cursor as a popup list.

## Setup

This tool has no dependencies other than the Unity editor itself and becomes available as soon as it is installed.
The minimum supported Unity version is 2021.3.

## Usage

### Basic

Context-click over GameObjects in the SceneView to show a popup list of all objects that can be selected.
Left-click an item in the list to select it.

### Filter

The filter bar at the top allows you to narrow down the visibility of GameObjects to 3D, 2D and UI in the popup list.

### Search

Press CMD/CTRL + F to focus the search field.
For a simple search, type the name of a GameObject in the search field.
Letter case and whitespace around words is ignored.
For an advanced search, type 't:' followed by a simple type name to find only GameObjects with a specific component,
e.g. 't:SpriteRenderer' will find all GameObjects that have a UnityEngine.SpriteRenderer component attached to them.
A simple and advanced query can be combined by separating them with a space, e.g. 'Foreground_Tree t:SpriteRenderer'.

### Hierarchy Display

When enabled and if a GameObject has parents, the _Hierarchy_ pane will show next to the selection popup.
It displays the currently hovered GameObject (at the bottom) and its parents (above) in the hierarchy.
The root of the object tree is at the top of the list.

## Key Actions

While hovering over an item, press the `Delete` key to record an undo and delete the hovered GameObject.
If the GameObject is a child of a prefab instance root, a dialog will prompt to either delete the root object
or open the prefab stage for manual editing.

While hovering over a prefab item, press the `P` key to open the prefab stage for editing.

### Advanced Selection

Hold Shift or Ctrl while clicking an item in the list to toggle it in the current selection
(it will be added to or removed from the already existing selection).
Without the modifier key, the selection will be replaced with the clicked item.

## Settings

Open the Unity Preferences window to find the Selection Utility settings. All settings are stored per user on the local
machine.

Enabled: Turns the tool on and off globally.

Click Dead Zone: The selection popup opens when the right mouse button was clicked down and released over approximately
the same position. The threshold defines the distance in pixels how far the mouse pointer may move before the open
action is cancelled. Increase this value if you are experiencing issues with opening the popup when using e.g. a
touchpad.

| Setting                           | Description                                                                                                                                                                                                                                                                                                                                          |
|-----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enabled                           | A global switch to toggle the utility.                                                                                                                                                                                                                                                                                                               |
| Click Dead Zone                   | The selection popup opens when the right mouse button was clicked down and released over approximately the same position. The threshold defines the distance in pixels how far the mouse pointer may move before the open action is cancelled. Increase this value if you are experiencing issues with opening the popup when using e.g. a touchpad. |
| Enable Hover Selection Outline    | When enabled, a colored outline will be drawn around 3D and 2D (SpriteRenderer) objects. Only the builtin render pipeline is supported.                                                                                                                                                                                                              |
| Hover Selection Outline Thickness | Increase this value to make the hover selection outline more prominent.                                                                                                                                                                                                                                                                              |
| Hover Selection Outline Color     | RGB values control the outline color. The alpha value controls the transparency of the opaque overlay on top of objects.                                                                                                                                                                                                                             |
| Show Hierarchy                    | If a hovered GameObject has parents, show them in a separate Hierarchy pane next within the popup.                                                                                                                                                                                                                                                   |
| Sort Mode                         | Transform Hierarchy: Sort popup items as they appear in the Unity Hierarchy window. Draw Order: Sort the closest item to the top of the list.                                                                                                                                                                                                        |
| Hidden Icons                      | The simple type names (e.g. 'Transform') of component icons to hide to avoid repetitive clutter.                                                                                                                                                                                                                                                     |

Use Defaults: Reverts all settings back to their default values.

Context-click on a single setting label: Opens the property context menu with the 'Reset' option to revert this property
back to its default value.

## Extensions

### Filters

It is possible to add custom options to the popup filter toolbar.
Assign `SelectionPopupExtensions.FilterModifier` during InitializeOnLoad (edit time)
or OnEnable (runtime) to change the default filter list.
You can add, remove, replace or reorder filters in this manner.
See the provided sample scripts for more information: CustomExtension.cs and CustomRuntimeFilter.cs

### KeyActions

In `InitializeOnLoad`, modify `SelectionPopupExtensions.KeyActions` to customize the key actions.
You can remove existing actions or add new custom ones.

For example:

```csharp
SelectionPopupExtensions.KeyActions.Add(new KeyAction
{
	ShouldExecute = context => context.Event.keyCode == KeyCode.L,
	Action = context => Debug.Log("Test"),
});
```

`ShouldExecute` is called when a key down event happens. Use the context object to return true,
when the action should execute.
`Action` is called right after `ShouldExecute` returns true. Use the context object to retrieve the hovered GameObject
or manipulate the popup window itself.
See `Sample/CustomKeyAction.cs` for a full example.

## Troubleshooting

1) If the scene view gets stuck in orbit mode after using the context click, please update your Unity version.
   The bug was introduced in Unity 2022 and fixed in 2023.
2) If the popup list is not showing UI elements, and you also can't use a regular left-click to select them,
   check that Gizmos are enabled in the scene view. If this still doesn't help, try resetting the window layout.
3) If some objects don't show in the Selection Utility popup, check that they aren't locked or hidden by Hierarchy or
   Layer settings. Selection Utility only shows objects that are selectable by Unity's default selection system.

## Support

If you encounter any issues, have questions or feedback, please get in touch:
assetstore@chrisyarbrough.com
