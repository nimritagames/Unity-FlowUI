# Contributing to Flow UI System

Thank you for your interest in contributing to Flow UI System! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment. Please be respectful and constructive in all interactions.

### Expected Behavior

- Be respectful and considerate
- Provide constructive feedback
- Focus on what is best for the community
- Show empathy towards others

## Getting Started

### Prerequisites

- **Unity 2022.3 or later**
- **Git** installed and configured
- **GitHub account**
- **Text editor or IDE** (Visual Studio, Rider, VSCode)

### Initial Setup

1. **Fork the repository**
   ```bash
   # Go to https://github.com/nimritagames/Unity-FlowUI
   # Click "Fork" button
   ```

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/Unity-FlowUI.git
   cd Unity-FlowUI
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/nimritagames/Unity-FlowUI.git
   ```

4. **Open in Unity**
   - Open Unity Hub
   - Click "Open" → Select the cloned directory
   - Unity will import the project

## Development Workflow

### Branch Strategy

**IMPORTANT: We NEVER work directly on the `main` branch!**

- **`main`** - Production-ready code, stable releases only
- **`development`** - Active development branch (DEFAULT working branch)
- **`feature/*`** - New features (branch from `development`)
- **`fix/*`** - Bug fixes (branch from `development`)

### Daily Workflow

1. **Always start from `development`**
   ```bash
   git checkout development
   git pull upstream development
   ```

2. **Create a feature branch**
   ```bash
   # For new features
   git checkout -b feature/my-new-feature

   # For bug fixes
   git checkout -b fix/issue-123
   ```

3. **Make your changes**
   - Write code
   - Add tests
   - Update documentation

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: Add new feature description"
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/my-new-feature
   ```

6. **Create Pull Request**
   - Go to GitHub
   - Create PR from your branch → `development` (NOT `main`!)
   - Fill out the PR template

### Keeping Your Fork Updated

```bash
# Fetch upstream changes
git fetch upstream

# Update your development branch
git checkout development
git merge upstream/development

# Update your feature branch (if needed)
git checkout feature/my-feature
git rebase development
```

## Coding Standards

### C# Coding Style

Follow Unity's C# coding conventions:

#### Naming Conventions

```csharp
// PascalCase for classes, methods, properties, and public fields
public class UIManager { }
public void BuildElement() { }
public string DisplayName { get; set; }

// camelCase for private fields with underscore prefix
private UIReference _currentReference;
private bool _isInitialized;

// PascalCase for constants
private const int MaxElements = 100;

// PascalCase for enum values
public enum ElementType
{
    Button,
    Text,
    Image
}
```

#### Code Organization

```csharp
// File structure order:
public class MyClass
{
    // 1. Constants
    private const int DefaultSize = 10;

    // 2. Static fields
    private static MyClass _instance;

    // 3. Serialized fields
    [SerializeField] private string _myField;

    // 4. Private fields
    private List<UIElement> _elements;

    // 5. Properties
    public string Name { get; set; }

    // 6. Unity lifecycle methods
    private void Awake() { }
    private void Start() { }
    private void Update() { }

    // 7. Public methods
    public void Initialize() { }

    // 8. Private methods
    private void InternalMethod() { }
}
```

#### Documentation Comments

```csharp
/// <summary>
/// Creates a new UI button with the specified path.
/// </summary>
/// <param name="path">Hierarchical path for the button (e.g., "Panel/SubPanel/Button")</param>
/// <returns>A ButtonBuilder instance for fluent configuration</returns>
public ButtonBuilder Button(string path)
{
    // Implementation
}
```

### File Organization

```
Packages/com.nimrita.flowui/
├── Runtime/
│   ├── Core/              # Core classes (UIManager, UIReference, etc.)
│   ├── Builders/          # UI element builders
│   ├── Animation/         # Animation system
│   └── Utilities/         # Helper classes
├── Editor/
│   ├── Windows/           # Editor windows
│   ├── Inspectors/        # Custom inspectors
│   └── Utilities/         # Editor utilities
└── Tests/
    ├── Runtime/           # Runtime tests
    └── Editor/            # Editor tests
```

### Code Quality Rules

- ❌ **NO** `Debug.Log` in production code (use proper logging)
- ❌ **NO** hardcoded paths or magic numbers
- ❌ **NO** public fields (use properties or `[SerializeField]` private fields)
- ✅ **YES** to XML documentation for public APIs
- ✅ **YES** to meaningful variable names
- ✅ **YES** to error handling with try-catch where appropriate

## Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) specification.

### Format

```
<type>: <short description>

<optional detailed description>

<optional footer>
```

### Types

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, no logic changes)
- `refactor:` - Code refactoring (no functional changes)
- `perf:` - Performance improvements
- `test:` - Adding or updating tests
- `chore:` - Build process, dependencies, tooling

### Examples

```bash
# Good commit messages
git commit -m "feat: Add slider builder with value range support"

git commit -m "fix: Resolve null reference in UIManager.GetElement

- Added null check before accessing element
- Added unit test to prevent regression
- Fixes #123"

git commit -m "docs: Update README with installation instructions"

git commit -m "refactor: Simplify button builder initialization"

# Bad commit messages (avoid these!)
git commit -m "fix stuff"
git commit -m "WIP"
git commit -m "asdfasdf"
git commit -m "Update UIManager.cs"
```

### Rules

- Keep the first line under 72 characters
- Use present tense ("Add feature" not "Added feature")
- Don't capitalize the first letter after the type
- No period at the end of the subject line
- Separate subject from body with a blank line
- Use the body to explain **what** and **why**, not **how**

## Pull Request Process

### Before Creating a PR

1. ✅ **Update your branch**
   ```bash
   git checkout development
   git pull upstream development
   git checkout your-feature-branch
   git rebase development
   ```

2. ✅ **Run tests locally**
   - Open Unity Test Runner (Window → General → Test Runner)
   - Run all tests (EditMode + PlayMode)
   - Ensure all tests pass

3. ✅ **Check for compilation errors**
   - Ensure no compiler errors or warnings
   - Test in Unity Editor

4. ✅ **Update documentation**
   - Update README if needed
   - Update CHANGELOG.md
   - Add XML comments to public APIs

### Creating the PR

1. **Push your branch**
   ```bash
   git push origin feature/my-feature
   ```

2. **Create PR on GitHub**
   - Go to your fork on GitHub
   - Click "New Pull Request"
   - **IMPORTANT**: Set base branch to `development` (NOT `main`!)
   - Fill out the PR template completely

3. **PR Checklist**
   - [ ] All tests pass
   - [ ] No compilation errors/warnings
   - [ ] Code follows style guidelines
   - [ ] Documentation updated
   - [ ] CHANGELOG.md updated
   - [ ] Commits follow conventional format
   - [ ] PR title is descriptive

### Code Review Process

1. **Automated Checks**
   - GitHub Actions will run automated tests
   - All checks must pass before merge

2. **Human Review**
   - A maintainer will review your code
   - Address any feedback or requested changes

3. **Making Changes**
   ```bash
   # Make changes based on feedback
   git add .
   git commit -m "fix: Address review feedback"
   git push origin feature/my-feature
   ```

4. **Merge**
   - Once approved, a maintainer will merge your PR
   - Your branch will be deleted automatically

## Testing Guidelines

### Unit Tests

Create tests for all new functionality:

```csharp
using NUnit.Framework;
using Nimrita.FlowUI;

public class UIManagerTests
{
    [Test]
    public void Button_CreatesButtonWithCorrectPath()
    {
        // Arrange
        var uiManager = CreateUIManager();

        // Act
        var button = uiManager.Button("TestButton").Build();

        // Assert
        Assert.IsNotNull(button);
        Assert.AreEqual("TestButton", button.name);
    }
}
```

### Test Coverage

- Aim for 80%+ code coverage
- Test edge cases and error conditions
- Test public APIs thoroughly

### Running Tests

```bash
# In Unity Editor
Window → General → Test Runner → Run All

# Via command line (example)
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testResults results.xml
```

## Documentation

### When to Update Documentation

- ✅ Adding new public API
- ✅ Changing existing behavior
- ✅ Adding new feature
- ✅ Fixing significant bugs

### Documentation Requirements

1. **XML Comments** - All public APIs must have XML documentation
2. **README.md** - Update if adding major features
3. **CHANGELOG.md** - Document all changes
4. **Code Examples** - Provide usage examples for new features

### Example Documentation

```csharp
/// <summary>
/// Creates a button UI element at the specified hierarchical path.
/// </summary>
/// <remarks>
/// The button is created as a child of the parent specified in the path.
/// If the parent doesn't exist, it will be created automatically.
/// </remarks>
/// <param name="path">
/// Hierarchical path in format "Parent/Child/ButtonName".
/// Example: "MainMenu/PlayButton"
/// </param>
/// <returns>
/// A <see cref="ButtonBuilder"/> for fluent configuration.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="path"/> is null or empty.
/// </exception>
/// <example>
/// <code>
/// uiManager.Button("MainMenu/PlayButton")
///     .SetText("Play")
///     .SetColor(Color.green)
///     .OnClick(() => StartGame())
///     .Build();
/// </code>
/// </example>
public ButtonBuilder Button(string path)
{
    // Implementation
}
```

## Questions?

- 💬 **Discussions**: [GitHub Discussions](https://github.com/nimritagames/Unity-FlowUI/discussions)
- 🐛 **Issues**: [GitHub Issues](https://github.com/nimritagames/Unity-FlowUI/issues)
- 📧 **Email**: Contact us through GitHub

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to Flow UI System! 🎉
