# Code Writer - Visual Studio Style Code Editor

## Overview

The Code Writer is a built-in code editing window that provides a Visual Studio-style interface for creating and editing C# scripts directly within Unity Editor.

## Features

✨ **Visual Studio-Style Interface**
- Dark theme matching VS Code/Visual Studio
- Line numbers with adjustable margin
- Status bar showing line/character count
- Unsaved changes indicator

📝 **Full Editing Capabilities**
- Multi-line code editing
- File path browsing
- Save and Save As functionality
- Unsaved changes warnings

🎨 **Integrated with UI System**
- Edit Library code directly
- Edit Handler code directly
- Quick access from Library/Handler generation tabs

## How to Use

### Method 1: Menu Access

1. Go to `Tools > UI System > Code Writer` in Unity menu
2. Or use `Tools > UI System > Code Writer (With Template)` for a pre-filled template

### Method 2: From Library Generation

1. Open UIManager Inspector
2. Go to "Library Generation" tab
3. Click "Edit Code" button
4. The Code Writer opens with your library code

### Method 3: From Handler Generation

1. Open UIManager Inspector
2. Go to "Handler Generation" tab
3. Click "Edit Code" button
4. The Code Writer opens with your handler code

## Interface Guide

### Toolbar (Top)
- **File Name**: Edit the filename directly
- **Path**: Shows where the file will be saved
- **Browse**: Select save location
- **Show Line Numbers**: Toggle line numbers on/off
- **Save**: Save file (highlighted when unsaved changes exist)
- **Save As**: Save to a new location
- **Close**: Close window (warns if unsaved changes)

### Editor Area (Middle)
- **Line Numbers**: Left margin with draggable resize
- **Code Area**: Full multi-line text editing
- Scroll vertically and horizontally as needed

### Status Bar (Bottom)
- **Lines/Characters**: Count of lines and total characters
- **Status Indicator**:
  - "● Unsaved changes" (orange) when modifications exist
  - "✓ Saved" (green) after successful save

## Keyboard Shortcuts

While the Code Writer is focused:
- Standard text editing shortcuts work (Ctrl+C, Ctrl+V, etc.)
- Ctrl+S triggers save (if implemented)
- Alt+F4 or ESC closes the window (with unsaved check)

## Tips

1. **Auto-refresh**: When saving inside Assets folder, Unity automatically refreshes and compiles
2. **Line number margin**: Drag the vertical divider to adjust line number width
3. **Template usage**: Use "Code Writer (With Template)" for quick C# MonoBehaviour scaffolding
4. **Integration**: Always use "Edit Code" from Library/Handler tabs for generated code editing

## Integration Points

The Code Writer is integrated into:
- Library Generation → "Edit Code" button
- Handler Generation → "Edit Code" button
- Tools menu → Direct access

## File Management

- **New Files**: Use "Browse" to select save location
- **Existing Files**: Opened files remember their path
- **Unsaved Changes**: Prompts before closing/overwriting
- **Backup**: When overwriting, consider using "Save As" first

## Technical Details

- Based on the same Visual Studio styling as CodePreviewWindow
- Supports all C# file editing
- Automatically refreshes Unity Asset Database after save
- Validates save paths (must be in Assets or project folder)

---

**Note**: This is a built-in editor tool. For complex refactoring or advanced features, consider using Visual Studio or Rider as your primary IDE. The Code Writer is perfect for quick edits and generated code review/modification.
