# UI Builders

This directory contains specialized UI builder classes that construct game UI at runtime.

## Architecture

### Builder Pattern
Each screen has its own dedicated builder class that extends `UIScreenBuilderBase`:

```
UIScreenBuilderBase (abstract)
├── HUDBuilder
├── StartScreenBuilder
├── PauseMenuBuilder
└── GameOverScreenBuilder
```

## Base Class: UIScreenBuilderBase

Provides common utilities for all builders:

### Core Methods
- `CreateUIObject(name, parent)` - Create GameObject with RectTransform
- `AddTextComponent(gameObject)` - Add TextMeshProUGUI with font
- `StretchToFill(rect)` - Anchor to fill parent
- `SetAnchors(rect, min, max)` - Set anchor points
- `CreateText(...)` - Create text element with styling
- `CreateButton(...)` - Create button with standard styling
- `CreateBar(...)` - Create progress bar (background + fill)
- `SetPrivateField(component, fieldName, value)` - Wire via reflection

### Color Properties
All builders access colors through base class:
- `BackgroundColor` - Panel backgrounds
- `PrimaryColor` - Main UI elements (cyan)
- `AccentColor` - Highlights (magenta/pink)
- `TextColor` - Text elements (white)

Colors automatically use `UITheme` if `_useThemeColors` is true.

## Builder Classes

### HUDBuilder.cs (315 LOC)
**Purpose**: Builds in-game HUD components

**Main Method**: `BuildHUD(canvasTransform)`

**Sections**:
1. **Health Display** - Health bar, text, shield icons
2. **Score Display** - Score, combo, multiplier, milestone text
3. **Level Display** - Current level indicator
4. **Heat Display** - Weapon heat bar, overheat warning, power level

**Components Created**:
- `HealthDisplay` - Top left
- `ScoreDisplay` - Top right
- `LevelDisplay` - Top right (below score)
- `WeaponHeatDisplay` - Bottom center

### StartScreenBuilder.cs (68 LOC)
**Purpose**: Builds title/start screen

**Main Method**: `BuildStartScreen(canvasTransform)`

**Elements**:
- Title text ("NEURAL BREAK")
- Subtitle text ("SURVIVE THE DIGITAL SWARM")
- Play button
- Version text (optional)

**Component**: `StartScreen`

### PauseMenuBuilder.cs (67 LOC)
**Purpose**: Builds pause overlay

**Main Method**: `BuildPauseScreen(canvasTransform)`

**Elements**:
- Semi-transparent overlay
- "PAUSED" title
- Resume button
- Restart button
- Quit to Main Menu button

**Component**: `PauseScreen`

### GameOverScreenBuilder.cs (98 LOC)
**Purpose**: Builds game over screen with stats

**Main Method**: `BuildGameOverScreen(canvasTransform)`

**Elements**:
- "GAME OVER" title
- Final Score
- Time Survived
- Enemies Killed
- Level Reached
- Highest Combo
- Best Multiplier
- Restart button
- Main Menu button

**Component**: `GameOverScreen`

## Usage Example

### Adding a New Screen Builder

1. **Create builder class**:
```csharp
using UnityEngine;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    public class SettingsScreenBuilder : UIScreenBuilderBase
    {
        public SettingsScreenBuilder(TMP_FontAsset font, bool useTheme = true)
            : base(font, useTheme) { }

        public GameObject BuildSettingsScreen(Transform parent)
        {
            var screen = CreateUIObject("SettingsScreen", parent);
            StretchToFill(screen);

            // Add components...
            var settingsScreen = screen.gameObject.AddComponent<SettingsScreen>();

            // Build UI elements...

            // Wire references...
            SetPrivateField(settingsScreen, "_backButton", backBtn);

            return screen.gameObject;
        }
    }
}
```

2. **Register in UIBuilder**:
```csharp
// In UIBuilder.cs InitializeBuilders()
_settingsScreenBuilder = new SettingsScreenBuilder(_fontAsset, _useThemeColors);

// In UIBuilder.cs BuildAllUI()
var settingsScreenGO = _settingsScreenBuilder.BuildSettingsScreen(transform);
_settingsScreen = settingsScreenGO.GetComponent<SettingsScreen>();
```

## Wiring Pattern

All builders use reflection to wire private fields on components:

```csharp
// Create component
var display = root.gameObject.AddComponent<HealthDisplay>();

// Build UI elements
var healthBar = CreateBar(...);
var healthText = AddTextComponent(...);

// Wire to component
SetPrivateField(display, "_healthFill", healthBar.fill);
SetPrivateField(display, "_healthText", healthText);
```

This matches the component's `[SerializeField] private` fields.

## Best Practices

1. **Keep builders focused** - One screen per builder
2. **Use base class methods** - Don't duplicate utility code
3. **Follow naming conventions** - Use descriptive GameObject names
4. **Wire all references** - Use `SetPrivateField()` for component fields
5. **Set active state** - Screens start inactive except StartScreen
6. **Use UITheme colors** - Don't hardcode colors
7. **Document sections** - Use regions for clarity

## Testing

Each builder can be tested independently:

```csharp
[Test]
public void HUDBuilder_CreatesHealthDisplay()
{
    var canvas = new GameObject().AddComponent<Canvas>();
    var builder = new HUDBuilder(null, true);

    var hud = builder.BuildHUD(canvas.transform);

    Assert.IsNotNull(hud);
    Assert.IsNotNull(hud.GetComponent<HUDController>());
}
```

## Dependencies

- **UnityEngine.UI** - Image, Button components
- **TMPro** - TextMeshProUGUI for text rendering
- **NeuralBreak.UI** - Screen components (StartScreen, PauseScreen, etc.)
- **UITheme** - Color and style constants

## Related Files

- `Assets/_Project/Scripts/UI/UIBuilder.cs` - Main coordinator
- `Assets/_Project/Scripts/UI/UITheme.cs` - Color palette and constants
- `Assets/_Project/Scripts/UI/UIManager.cs` - Screen management
- `Assets/_Project/Scripts/UI/HUDController.cs` - HUD event routing
