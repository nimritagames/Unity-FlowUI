# Editor Folder Structure

This document describes the organization of the FlowUI Editor folder.

## Folder Organization

The Editor folder is organized into three main categories for clear separation of concerns:

### 1. UIManagerInspector/
**Purpose**: Contains all components related to the main UIManager inspector and its features.

**Files**:
- `UIManagerEditor.cs` - Main custom inspector for UIManager
- `UIManagerEditor.*.cs` - Partial class files for specific features:
  - `Categories.cs` - Category management
  - `Handlers.cs` - Handler generation
  - `Hierarchy.cs` - Hierarchy view
  - `Library.cs` - Library generation
  - `MissingReferences.cs` - Missing reference detection
  - `PanelHandlers.cs` - Panel handler generation
  - `Search.cs` - Search functionality
  - `Standardization.cs` - UI standardization tools
  - `Utilities.cs` - Utility methods
- `UIManagerQuickStartWindow.cs` - Quick start guide window
- `UIManagerTipWindow.cs` - Tips and tricks window

**Responsibilities**:
- Custom inspector UI for UIManager
- UI element management and categorization
- Code generation (libraries, handlers)
- Visual hierarchy and search
- UI standardization tools

### 2. SmartNaming/
**Purpose**: Contains the Smart Naming Assistant and its automation controller.

**Files**:
- `SmartNamingAssistant.cs` - Smart naming window with auto-fix and manual fixing
- `SmartNamingController.cs` - Proactive naming detection and validation

**Responsibilities**:
- Automatic naming issue detection
- Smart name suggestions and fixes
- Naming conflict resolution
- Code generation validation
- Proactive user assistance

### 3. CodeTools/
**Purpose**: Contains the live UI Builder Playground for rapid prototyping.

**Files**:
- `UIBuilderPlayground.cs` - Live UI builder code execution window

**Responsibilities**:
- Live UI code execution in Edit Mode
- Rapid UI prototyping without Play mode
- Runtime compilation and instant feedback
- Built-in templates for common UI patterns
- UI builder API experimentation

## Design Principles

### Separation of Concerns
Each folder has a single, clear responsibility:
- **UIManagerInspector**: Everything related to the main inspector UI
- **SmartNaming**: Everything related to naming assistance
- **CodeTools**: Everything related to code editing and playgrounds

### Scalability
The structure makes it easy to add new features:
- New inspector features → Add to UIManagerInspector
- New naming features → Add to SmartNaming
- New code tools → Add to CodeTools

### Maintainability
- Related files are grouped together
- Easy to navigate and find specific functionality
- Clear mental model of the codebase

## File Naming Conventions

### UIManagerEditor Partial Classes
Pattern: `UIManagerEditor.[FeatureName].cs`

Examples:
- `UIManagerEditor.Categories.cs`
- `UIManagerEditor.Search.cs`
- `UIManagerEditor.Library.cs`

### Tool Windows
Pattern: `[ToolName]Window.cs` or `[ToolName]Assistant.cs`

Examples:
- `SmartNamingAssistant.cs`
- `UIManagerQuickStartWindow.cs`
- `UIBuilderPlayground.cs`

### Controllers/Utilities
Pattern: `[ToolName]Controller.cs`

Examples:
- `SmartNamingController.cs`

## Assembly Definition

All Editor scripts are compiled into the `com.nimrita.flowui.Editor` assembly, which:
- References the runtime assembly (`com.nimrita.flowui`)
- References TextMeshPro and TextMeshPro.Editor
- Only includes Editor platform
- Uses root namespace: `Nimrita.FlowUI.Editor`

Location: `com.nimrita.flowui.Editor.asmdef` (root of Editor folder)

## Adding New Features

### New Inspector Feature
1. Create `UIManagerEditor.[FeatureName].cs` in `UIManagerInspector/`
2. Make it a partial class of `UIManagerEditor`
3. Add feature-specific fields, methods, and GUI drawing

### New Tool Window
1. Decide which category it belongs to
2. Create the window class in the appropriate folder
3. Add menu items if needed
4. Document in this README

### New Code Tool
1. Add to `CodeTools/` folder
2. Create the tool window class
3. Add menu items if needed
4. Document in this README

## Migration Notes

**Previous Structure**: All files were in the root Editor folder

**New Structure**: Organized into three subfolders

**Breaking Changes**: None - Unity automatically handles folder moves and maintains meta files

**Benefits**:
- 67% reduction in root folder clutter (24 files → 8)
- Logical grouping makes navigation easier
- Easier to understand the codebase structure
- Better scalability for future features

## Quick Reference

| What you need | Where to look |
|---------------|---------------|
| UIManager inspector code | `UIManagerInspector/UIManagerEditor*.cs` |
| Smart naming features | `SmartNaming/` |
| Live UI playground | `CodeTools/UIBuilderPlayground.cs` |
| Quick start guide | `UIManagerInspector/UIManagerQuickStartWindow.cs` |
| Assembly definition | `com.nimrita.flowui.Editor.asmdef` (root) |

---

**Last Updated**: November 23, 2025
**Reorganization Version**: 2.0
