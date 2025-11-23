# 🔥 Live UI Builder Playground

## What is This?

The **Live UI Builder Playground** is a revolutionary tool that lets you write UI builder code and see changes **INSTANTLY** in your Unity scene - no Execute button needed!

Think of it like a **React/Vue hot-reload dev server**, but for Unity UI!

## 🎯 The Experience

```
Type this:
    uiManager.CreateButton("Test")

↓ [300ms pause]
↓ [Auto-compile]
↓ [Auto-execute]

Scene: *Button appears! ⚡*

Change to:
    .WithText("Hello World")

↓ [300ms pause]
↓ [Auto-compile]
↓ [Auto-execute]

Scene: *Text updates! ⚡*

Delete the line:

↓ [300ms pause]

Scene: *Button vanishes! ⚡*
```

## 🚀 How It Works

### Architecture

```
User Types Code
    ↓
LiveExecutionEngine (detects change)
    ↓
SmartDebouncer (waits 300ms)
    ↓
FastPlaygroundCompiler (compiles code)
    ↓
UIStateTracker (cleans up old UI)
    ↓
Execute (creates new UI)
    ↓
Scene Updates LIVE! ⚡
```

### Key Components

#### 1. **UIBuilderPlaygroundWindow**
- Main editor window
- Handles GUI rendering
- Manages user input
- Coordinates all systems

#### 2. **LiveExecutionEngine**
- Detects code changes
- Debounces input (300ms delay)
- Coordinates compilation and execution
- Provides status updates
- Tracks statistics

#### 3. **FastPlaygroundCompiler** (IPlaygroundCompiler)
- Compiles C# code to assembly
- **Caches compilation results** for speed
- Wraps user code in executable class
- Adjusts error line numbers
- Uses CodeDOM (will be replaced with Roslyn)

#### 4. **UIStateTracker**
- Tracks all created GameObjects
- Enables smart cleanup
- Prevents memory leaks
- Allows "delete code = delete UI"

## 🎨 Features

### Live Mode (Default ON)
- **Auto-execution** after typing pause
- **No Execute button** needed
- **Real-time feedback** in scene
- **Debounced** to prevent spam

### Manual Mode
- Traditional Execute button
- More control
- Good for complex changes

### Smart Caching
- **First compile**: ~100-200ms
- **Cached compile**: <5ms (almost instant!)
- Only recompiles when code changes

### Visual Feedback
- **Status bar** shows current state
- **Color-coded** (green = success, red = error)
- **Statistics** (execution count, success rate)
- **Compilation time** display

### Error Handling
- **Inline errors** with correct line numbers
- **Expandable error panel**
- **Non-intrusive** (doesn't break flow)

## 📁 File Structure

```
Playground/
├── UIBuilderPlaygroundWindow.cs     ← Main window
│
├── Compilation/
│   ├── IPlaygroundCompiler.cs       ← Interface
│   ├── FastPlaygroundCompiler.cs    ← CodeDOM implementation
│   └── CompilationResult.cs         ← Result data structure
│
├── Execution/
│   ├── LiveExecutionEngine.cs       ← Live execution + debouncing
│   ├── UIStateTracker.cs            ← GameObject tracking
│   ├── ExecutionResult.cs           ← Result data
│   └── ExecutionStatus.cs           ← Status updates
│
└── README.md                         ← This file
```

## 🔧 How to Use

### Basic Usage

1. Open: `Tools > UI System > UI Builder Playground 🔥`
2. Assign a **UIManager** in the scene
3. **Start typing** code!
4. **Wait 300ms** after you stop typing
5. **Watch** the UI appear/update/vanish automatically!

### Tips

- **Live Mode ON** = Automatic execution (recommended!)
- **Live Mode OFF** = Manual Execute button
- **Ctrl+E** = Manual execute (works in either mode)
- **Clear Cache** = Forces full recompilation

### Example Code

```csharp
// Get canvas
var canvas = GameObject.Find("Canvas");

// Create button (appears instantly!)
uiManager.CreateButton("TestButton")
    .WithText("Click Me!")
    .WithSize(200, 50)
    .WithPosition(Vector2.zero)
    .OnClick(() => Debug.Log("Clicked!"))
    .AddTo(canvas.transform);

// Try changing text above - updates live!
// Try deleting button - vanishes live!
```

## ⚙️ Configuration

### Debounce Delay
Default: **300ms** (configurable in code)
- Lower = Faster response, more CPU
- Higher = Less responsive, less CPU

### Caching
- Enabled by default
- Use "Clear Cache" if stuck
- Automatically invalidates on code change

## 🎯 Design Principles

### 1. **Separation of Concerns**
Each class has ONE job:
- Window = GUI only
- Compiler = Compilation only
- Engine = Execution orchestration
- Tracker = GameObject management

### 2. **Interface-Based**
- `IPlaygroundCompiler` = Swappable compilers
- Easy to add Roslyn later
- Easy to test

### 3. **Smart Caching**
- Don't recompile identical code
- Cache assembly in memory
- Invalidate on change

### 4. **Debouncing**
- Don't spam compilation
- Wait for typing pause
- Configurable delay

### 5. **Clean Slate**
- Each execution starts fresh
- Old UI is destroyed
- No leftover garbage

## 🚀 Future Enhancements

### Phase 1: Roslyn Integration ✨
Replace CodeDOM with Roslyn for:
- **10x faster** compilation
- **Incremental compilation** (only recompile changes)
- **Better errors** (more context)
- **Modern C#** features

### Phase 2: Advanced Features
- **Snippet library** (save/load common patterns)
- **Code history** (undo/redo for code)
- **Diff view** (see what changed)
- **Multi-file support** (organize complex UIs)

### Phase 3: Hot-Reload 2.0
- **Partial updates** (don't destroy unchanged UI)
- **State preservation** (keep button click handlers)
- **Animation retention** (don't restart animations)
- **Smart diffing** (only update what changed)

## 🧪 Technical Details

### Compilation Process

1. **Wrap Code**: User code → Full C# class
2. **Compile**: C# → Assembly (in-memory)
3. **Extract**: Get `GeneratedUIBuilder.Execute` method
4. **Invoke**: Call method with UIManager

### Execution Process

1. **Validate**: Check UIManager exists
2. **Clean**: Destroy previous UI
3. **Track**: Begin tracking new UI
4. **Execute**: Run compiled code
5. **Mark Dirty**: Update Unity scene

### Caching Strategy

```csharp
if (code == lastCompiledCode)
    return cachedAssembly; // FAST!
else
    compile(code); // SLOW (but only once)
```

## 📊 Performance

**Cold Compile** (first time):
- Simple UI: ~100-150ms
- Complex UI: ~200-300ms

**Hot Compile** (cached):
- Identical code: ~2-5ms ⚡
- Changed code: Back to cold

**Execution**:
- Minimal overhead (~1-2ms)
- Depends on UI complexity

## 🐛 Troubleshooting

**Problem**: UI not updating
- **Solution**: Check Live Mode is ON
- **Solution**: Check UIManager is assigned
- **Solution**: Wait full 300ms after typing

**Problem**: Compilation errors
- **Solution**: Check error panel
- **Solution**: Line numbers are adjusted
- **Solution**: Remember code is wrapped

**Problem**: Slow compilation
- **Solution**: Clear cache if stuck
- **Solution**: Simplify code temporarily
- **Solution**: Wait for Roslyn upgrade

**Problem**: Old UI not clearing
- **Solution**: Manually delete GameObjects
- **Solution**: Clear cache and re-execute
- **Solution**: Check console for errors

## 💡 Best Practices

1. **Use Live Mode** - It's the whole point!
2. **Small iterations** - Make small changes
3. **Test incrementally** - Don't write 100 lines at once
4. **Use templates** - Start with working examples
5. **Check errors** - Read error panel carefully

## 🎓 Learning Resources

**Getting Started**:
1. Open playground
2. Read default template
3. Make small changes
4. Watch the magic!

**Advanced Usage**:
1. Create complex UIs
2. Use loops for repetition
3. Extract to methods
4. Save as snippets (coming soon!)

---

**Last Updated**: November 23, 2025
**Version**: 2.0 (Live Mode)
**Status**: Production Ready 🚀
