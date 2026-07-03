# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0] - January 2025

This is a big overhaul of the entire tool. The popup window has been re-implemented in UI Toolkit for increased
performance and to make it easier to support more advanced features.

## Upgrade Notes
- Delete the previous package version before installing the new one, since a lot of files have been removed or renamed.
- In `Unity preferences > Selection Utility`, remember any customized values in _Hidden Icons_, 
  then context-click the property label and use _Reset_ to update the list.

## Added

- An option to show the parent hierarchy of the hovered GameObject in a second pane of the popup list.
- Hover over an item and press the `Delete` or `Backspace` key to delete a GameObject. 
  - An undo action is recorded.
  - If the GameObject is part of a prefab, but not its root, it will prompt to either delete the root or open the prefab for manual editing.
- Hover over an item and press the `p` key to open a prefab for editing in the prefab stage.
- `SelectionPopupExtensions.KeyActions` API to configure custom key actions for hovered items (see examples).
- A user setting to change the ordering of items in the popup (transform hierarchy vs depth from camera).

### Fixed

- Mouse cursor of scene view tool in FPS mode (context click and drag to look around) was sometimes getting stuck.
- Shader error about _ObjectId being a duplicate property name.

## Changed

- Minimum supported Unity version is now `2021.3`.
- The ordering of items in the popup list is now based on the order in the hierarchy (transform sibling index and level) by default.
- Added more types to the default list of _Hidden Icons_ in the user preferences. 
  Revert this setting (context-click) to update the list with the new values.

### Removed

- The following user preferences (now enabled by default):
    - Show search field
    - Show duplicate icons
    - Show missing script icons
    - Show filter toolbar
- Support for pre Unity 2020 API

## [1.2.0]

### Added

- SelectionPopupExtensions class to allow adding a custom filter tab to the toolbar.

### Fixed

- Minor pixel-pushing for improved layout of popup elements.

## [1.1.0]

### Added

- Escape key closes the popup.
- Support contact in Readme.
- User preference to show duplicate icons (disabled by default).
- User preference to show missing script icons (enabled by default).
- User preference to enable and configure the new outline feature.
- When hovering over an item in the popup the GameObject will be highlighted
  with a colored outline in the scene view. Works for 3D objects and 2D Sprites
  and only with the builtin render pipeline. Unity UI objects are not outlined
  and the feature is not supported when using the Universal or High Definition Render Pipeline.
- Filter modes toolbar: [All, 3D, 2D, UI] reduces the shown item list to those that
  contain MeshRenderer, SpriteRenderer, or RectTransform components.

### Changed

- Missing script icons are now shown by default.

## [1.0.0]

### Added

- Initial release of the tool.
