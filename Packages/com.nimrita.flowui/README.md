# Flow UI System

A comprehensive fluent UI builder system for Unity with centralized management, powerful editor integration, and built-in animation capabilities.

## Features

- **Fluent Interface Pattern**: Build complex UI hierarchies with intuitive method chaining
- **Centralized UIManager**: Manage all UI elements from a single component
- **Path-Based References**: Hierarchical organization using slash-separated paths
- **Category System**: Organize UI elements into logical groups
- **Smart Naming Assistant**: Automated naming convention enforcement
- **Custom Editor Integration**: Rich Unity Editor experience with search, hierarchy view, and management tools
- **Animation System**: Built-in UI animation controller
- **Error Handling**: SafeExecute wrappers for robust error management
- **Comprehensive Builders**: Support for Button, Text, Image, InputField, Toggle, Slider, Dropdown, ScrollRect, Panel, and RawImage

## Installation

### Via Git URL (Recommended)

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.nimrita.flowui": "https://github.com/nimritagames/Unity-FlowUI.git"
  }
}
```

### Via Package Manager

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button
3. Select "Add package from git URL"
4. Enter: `https://github.com/nimritagames/Unity-FlowUI.git`

### Local Installation

1. Clone this repository
2. Open Unity Package Manager (Window > Package Manager)
3. Click the `+` button
4. Select "Add package from disk"
5. Navigate to the cloned folder and select `package.json`

## Quick Start

### 1. Add UIManager to Your Scene

```csharp
// Attach UIManager component to a GameObject in your scene
GameObject uiManagerObject = new GameObject("UIManager");
UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
```

### 2. Build Your First UI Element

```csharp
using Nimrita.FlowUI;

// Create a button with fluent interface
uiManager.Button("MainMenu/PlayButton")
    .SetText("Play Game")
    .SetColor(Color.green)
    .SetSize(200, 50)
    .OnClick(() => Debug.Log("Game Started!"))
    .Build();
```

### 3. Create Complex UI Hierarchies

```csharp
// Create a panel with nested elements
uiManager.Panel("SettingsPanel")
    .SetSize(400, 600)
    .Build();

uiManager.Text("SettingsPanel/Title")
    .SetText("Settings")
    .SetFontSize(24)
    .Build();

uiManager.Slider("SettingsPanel/VolumeSlider")
    .SetMinMax(0, 100)
    .SetValue(75)
    .OnValueChanged(value => Debug.Log($"Volume: {value}"))
    .Build();
```

## Core Concepts

### UIReference System

UI elements are referenced using hierarchical paths:

```csharp
// Parent/Child/GrandChild structure
"MainPanel/HeaderPanel/TitleText"
```

### Category Organization

Organize related UI elements into categories:

```csharp
uiManager.AddCategory("MainMenu", Color.blue);
uiManager.AddCategory("Settings", Color.green);
```

### Builder Pattern

All UI elements use a consistent builder pattern:

```csharp
uiManager.ElementType("Path/To/Element")
    .ConfigurationMethod1(value)
    .ConfigurationMethod2(value)
    .Build();
```

## Available Builders

- **ButtonBuilder**: Interactive buttons with onClick events
- **TextBuilder**: Text display with TextMeshPro support
- **ImageBuilder**: Image rendering with sprite support
- **InputFieldBuilder**: Text input fields
- **ToggleBuilder**: Boolean toggle controls
- **SliderBuilder**: Value sliders with min/max ranges
- **DropdownBuilder**: Dropdown selection menus
- **ScrollRectBuilder**: Scrollable content areas
- **PanelBuilder**: Container panels for organizing UI
- **RawImageBuilder**: Raw texture display

## Editor Features

The Flow UI System includes a powerful custom editor:

- **Search Functionality**: Quickly find UI elements
- **Hierarchy View**: Visual tree representation of UI structure
- **Category Management**: Organize and color-code UI groups
- **Missing Reference Detection**: Automatic detection and repair of broken references
- **Smart Naming Assistant**: Enforce naming conventions across your UI
- **Quick Start Window**: Guided setup for new users
- **Standardization Tools**: Apply consistent styling across elements

## Requirements

- Unity 2022.3 or later
- TextMeshPro 3.0.7 or later
- Unity UI (UGUI) 1.0.0 or later

## Documentation

For detailed documentation, tutorials, and API reference, visit the [GitHub repository](https://github.com/nimritagames/Unity-FlowUI).

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/nimritagames/Unity-FlowUI/issues)
- **Discussions**: [GitHub Discussions](https://github.com/nimritagames/Unity-FlowUI/discussions)

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting pull requests.

---

Made with ❤️ by Nimrita Games
