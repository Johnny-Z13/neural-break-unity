# UI Builder Architecture

## Before Refactoring

```
UIBuilder.cs (766 LOC)
├── BuildHUD() - 283 LOC
│   ├── BuildHealthDisplay()
│   ├── BuildScoreDisplay()
│   ├── BuildLevelDisplay()
│   └── BuildHeatDisplay()
├── BuildStartScreen() - 49 LOC
├── BuildPauseScreen() - 49 LOC
├── BuildGameOverScreen() - 64 LOC
├── CreateUIObject() - helpers
├── AddTextComponent() - helpers
├── StretchToFill() - helpers
├── SetAnchors() - helpers
├── CreateText() - helpers
├── CreateButton() - helpers
└── CreateStatText() - helpers

PROBLEM: Single massive file, difficult to navigate and maintain
```

## After Refactoring

```
UIBuilder.cs (250 LOC) - COORDINATOR
├── LoadFont() - Font loading logic
├── InitializeBuilders() - Create builder instances
├── BuildAllUI() - Delegate to builders
├── WireReferences() - Connect to UIManager
└── EnsureEventSystem() - Input setup

    ↓ delegates to ↓

Builders/ Namespace
├── UIScreenBuilderBase.cs (202 LOC) - BASE CLASS
│   ├── CreateUIObject()
│   ├── AddTextComponent()
│   ├── StretchToFill()
│   ├── SetAnchors()
│   ├── CreateText()
│   ├── CreateButton()
│   ├── CreateBar()
│   ├── SetPrivateField()
│   └── Color properties (BackgroundColor, PrimaryColor, etc.)
│
├── HUDBuilder.cs (315 LOC) - SPECIALIZED
│   └── BuildHUD()
│       ├── BuildHealthDisplay()
│       ├── BuildScoreDisplay()
│       ├── BuildLevelDisplay()
│       └── BuildHeatDisplay()
│
├── StartScreenBuilder.cs (68 LOC) - SPECIALIZED
│   └── BuildStartScreen()
│
├── PauseMenuBuilder.cs (67 LOC) - SPECIALIZED
│   └── BuildPauseScreen()
│
└── GameOverScreenBuilder.cs (98 LOC) - SPECIALIZED
    └── BuildGameOverScreen()
        └── CreateStatText()

BENEFIT: Modular, focused, maintainable classes
```

## Component Wiring Flow

```
UIBuilder.Awake()
    ↓
LoadFont() - Load TMP_FontAsset
    ↓
InitializeBuilders() - Create all builders with font
    ↓
BuildAllUI()
    ↓
    ├─→ HUDBuilder.BuildHUD()
    │       ↓
    │   Creates: HUDController + 4 display components
    │   Returns: GameObject with HUDController
    │
    ├─→ StartScreenBuilder.BuildStartScreen()
    │       ↓
    │   Creates: StartScreen component + UI elements
    │   Returns: GameObject with StartScreen
    │
    ├─→ PauseMenuBuilder.BuildPauseScreen()
    │       ↓
    │   Creates: PauseScreen component + UI elements
    │   Returns: GameObject with PauseScreen
    │
    └─→ GameOverScreenBuilder.BuildGameOverScreen()
            ↓
        Creates: GameOverScreen component + UI elements
        Returns: GameObject with GameOverScreen
    ↓
WireReferences()
    ↓
Uses reflection to connect screens to UIManager
    ↓
COMPLETE - UI hierarchy built!
```

## Class Responsibilities

### UIBuilder (Coordinator)
**Responsibility**: Orchestrate UI building process
**Does**:
- Manages font loading
- Creates builder instances
- Delegates screen building
- Wires references to UIManager
- Manages EventSystem setup

**Doesn't**:
- Create UI elements directly
- Know layout details
- Handle specific screen logic

### UIScreenBuilderBase (Shared Utilities)
**Responsibility**: Provide common UI building utilities
**Does**:
- Create GameObjects with RectTransform
- Add TextMeshProUGUI components
- Position and anchor UI elements
- Create buttons with standard styling
- Create progress bars
- Wire components via reflection
- Manage theme colors

**Doesn't**:
- Build specific screens
- Know screen layouts
- Handle screen logic

### HUDBuilder (Specialized)
**Responsibility**: Build in-game HUD
**Does**:
- Create HUD layout (top-left, top-right, bottom-center)
- Build health display
- Build score display
- Build level display
- Build heat display
- Wire to HUDController

**Doesn't**:
- Build menu screens
- Handle game logic
- Update display values (HUDController does this)

### StartScreenBuilder (Specialized)
**Responsibility**: Build title screen
**Does**:
- Create title text
- Create subtitle text
- Create play button
- Wire to StartScreen

**Doesn't**:
- Handle button clicks (StartScreen does this)
- Build other screens

### PauseMenuBuilder (Specialized)
**Responsibility**: Build pause overlay
**Does**:
- Create semi-transparent overlay
- Create title text
- Create resume/restart/quit buttons
- Wire to PauseScreen

**Doesn't**:
- Handle button clicks (PauseScreen does this)
- Manage pause state

### GameOverScreenBuilder (Specialized)
**Responsibility**: Build game over screen
**Does**:
- Create title text
- Create stat text lines (6 stats)
- Create restart/menu buttons
- Wire to GameOverScreen

**Doesn't**:
- Calculate stats (GameOverScreen does this)
- Handle button clicks (GameOverScreen does this)

## Data Flow

### Build Time (Awake)
```
UIBuilder
    ↓ (creates)
Builders
    ↓ (build)
GameObjects + Components
    ↓ (wire to)
UIManager
```

### Runtime (Game Playing)
```
Game Events
    ↓
EventBus
    ↓
HUDController
    ↓
Display Components (HealthDisplay, ScoreDisplay, etc.)
    ↓
Update UI Elements
```

## File Organization

```
Assets/_Project/Scripts/UI/
├── UIBuilder.cs (250 LOC) - Main coordinator
├── UIManager.cs - Screen management
├── UITheme.cs - Color palette
├── HUDController.cs - HUD event routing
│
├── Builders/ (New directory)
│   ├── UIScreenBuilderBase.cs (202 LOC) - Base class
│   ├── HUDBuilder.cs (315 LOC) - HUD builder
│   ├── StartScreenBuilder.cs (68 LOC) - Start screen
│   ├── PauseMenuBuilder.cs (67 LOC) - Pause menu
│   ├── GameOverScreenBuilder.cs (98 LOC) - Game over
│   └── README.md - Developer guide
│
├── Screens/
│   ├── ScreenBase.cs
│   ├── StartScreen.cs
│   ├── PauseScreen.cs
│   └── GameOverScreen.cs
│
└── Displays/
    ├── HealthDisplay.cs
    ├── ScoreDisplay.cs
    ├── LevelDisplay.cs
    └── WeaponHeatDisplay.cs
```

## Benefits Summary

### Maintainability
- **Before**: 766 LOC in one file - hard to navigate
- **After**: Largest file is 315 LOC - easy to understand

### Extensibility
- **Before**: Add screen = modify massive file
- **After**: Add screen = new 50-100 LOC builder class

### Testability
- **Before**: Can only test entire UIBuilder
- **After**: Can test each builder independently

### Code Reuse
- **Before**: Duplicated helper methods
- **After**: Shared utilities in base class

### Team Collaboration
- **Before**: Merge conflicts likely
- **After**: Each dev works on separate builder

## Performance Impact

**Zero runtime performance impact** - Same UI generation, just organized differently:
- Same number of GameObjects created
- Same reflection calls for wiring
- Same TextMeshPro font assignment
- Same event system setup

Builds happen once at startup (Awake), so organization doesn't affect frame rate.

## Future Enhancements

1. **Dependency Injection**: Pass builders to UIBuilder constructor
2. **Async Loading**: Load fonts asynchronously
3. **Object Pooling**: Pool UI elements for popups
4. **Factory Pattern**: Create builder factory
5. **Configuration**: Move layouts to ScriptableObjects
6. **Animation**: Add transition helpers to base class
