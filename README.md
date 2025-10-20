# Flow UI System

<div align="center">

**A comprehensive fluent UI builder system for Unity**

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![UPM Package](https://img.shields.io/badge/UPM-1.0.0-blue.svg)](https://docs.unity3d.com/Manual/upm-ui.html)

[Installation](#installation) • [Features](#features) • [Quick Start](#quick-start) • [Documentation](#documentation)

</div>

---

## Overview

Flow UI System is a powerful Unity package that revolutionizes UI development through a fluent interface pattern. Build complex UI hierarchies with intuitive method chaining, centralized management, and comprehensive editor tools.

## Features

### 🎨 Fluent Interface Pattern
Build UI elements with elegant, readable code:
```csharp
uiManager.Button("MainMenu/PlayButton")
    .SetText("Play Game")
    .SetColor(Color.green)
    .SetSize(200, 50)
    .OnClick(() => StartGame())
    .Build();
```

### 🗂️ Centralized Management
- Single UIManager component manages all UI elements
- Path-based hierarchical organization
- Category system for logical grouping
- Automatic lifecycle management

### 🛠️ Powerful Editor Integration
- **Search & Filter**: Quickly find any UI element
- **Hierarchy View**: Visual tree representation
- **Smart Naming Assistant**: Automated naming conventions
- **Missing Reference Detection**: Auto-detect and repair broken references
- **Quick Start Window**: Guided setup for new users

### 📦 Comprehensive UI Builders
Support for all essential UI components:
- ButtonBuilder
- TextBuilder (TextMeshPro)
- ImageBuilder & RawImageBuilder
- InputFieldBuilder
- ToggleBuilder
- SliderBuilder
- DropdownBuilder
- ScrollRectBuilder
- PanelBuilder

### 🎭 Animation System
Built-in UIAnimationController for smooth transitions and effects.

### 🛡️ Error Handling
Robust SafeExecute wrappers ensure graceful error recovery.

## Installation

### Option 1: Via Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button → "Add package from git URL"
3. Enter:
   ```
   https://github.com/nimritagames/Unity-FlowUI.git
   ```

### Option 2: Via manifest.json

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.nimrita.flowui": "https://github.com/nimritagames/Unity-FlowUI.git"
  }
}
```

### Option 3: Local Installation

1. Clone this repository
2. Open Unity Package Manager (Window > Package Manager)
3. Click `+` → "Add package from disk"
4. Select `Packages/com.nimrita.flowui/package.json`

## Quick Start

### 1. Add UIManager to Your Scene

```csharp
using Nimrita.FlowUI;

// Create UIManager (or add via Unity Editor)
GameObject uiManagerObject = new GameObject("UIManager");
UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
```

### 2. Build Your First UI

```csharp
// Create a panel
uiManager.Panel("MainMenu")
    .SetSize(400, 600)
    .Build();

// Add a title
uiManager.Text("MainMenu/Title")
    .SetText("Welcome!")
    .SetFontSize(32)
    .Build();

// Add a button
uiManager.Button("MainMenu/PlayButton")
    .SetText("Play")
    .OnClick(() => Debug.Log("Game Started!"))
    .Build();
```

### 3. Organize with Categories

```csharp
uiManager.AddCategory("MainMenu", Color.blue);
uiManager.AddCategory("Settings", Color.green);
uiManager.AddCategory("HUD", Color.yellow);
```

## Documentation

### Package Structure
```
Packages/com.nimrita.flowui/
├── Runtime/              # Core runtime code
│   ├── UIManager.cs     # Main manager component
│   ├── UIReference.cs   # Reference system
│   └── UI_Builder/      # Builder implementations
├── Editor/              # Unity Editor tools
│   ├── UIManagerEditor.cs
│   ├── SmartNamingAssistant.cs
│   └── ...
├── package.json         # UPM manifest
├── README.md           # Package documentation
├── CHANGELOG.md        # Version history
└── LICENSE.md          # MIT License
```

### Core Concepts

#### Path-Based References
UI elements use hierarchical paths for organization:
```csharp
"Panel/SubPanel/Button"  // Parent/Child/GrandChild
```

#### Builder Pattern
All elements follow a consistent fluent interface:
```csharp
uiManager.ElementType("Path")
    .ConfigMethod1(value)
    .ConfigMethod2(value)
    .Build();
```

#### Category System
Group and color-code related UI elements:
```csharp
uiManager.AddCategory("CategoryName", Color.blue);
```

## Requirements

- Unity 2022.3 or later
- TextMeshPro 3.0.7+
- Unity UI (UGUI) 1.0.0+

## Examples

Check the [package README](Packages/com.nimrita.flowui/README.md) for detailed examples and API documentation.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

### Development Workflow
- `development` branch: Active development
- `main` branch: Stable releases only

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/nimritagames/Unity-FlowUI/issues)
- **Discussions**: [GitHub Discussions](https://github.com/nimritagames/Unity-FlowUI/discussions)

## Changelog

See [CHANGELOG.md](Packages/com.nimrita.flowui/CHANGELOG.md) for version history and release notes.

---

<div align="center">

**Made with ❤️ by Nimrita Games**

[GitHub](https://github.com/nimritagames) • [Website](https://nimrita.com)

</div>
