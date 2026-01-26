# Aim Direction Persistence Fix

## Problem
In Twin Stick Shooter mode, when releasing the right stick (or stopping mouse movement), the aim direction would reset to 12 o'clock (Vector2.up) instead of maintaining the last aim direction.

## Root Cause
Two issues were causing this:

1. **InputManager.UpdateAimDirection()** (line 344):
   - Initialized `aimDir = Vector2.up` every frame
   - Always set `AimDirection = aimDir` even when no input
   - Result: Reset to up when stick centered

2. **PlayerController.UpdateAimDirection()** (line 276-280):
   - Had fallback to `_lastMoveDirection` when no aim input
   - Could cause aim to snap to movement direction

## Solution

### 1. InputManager.cs
**Changed**:
```csharp
// OLD: Reset to up every frame
Vector2 aimDir = Vector2.up;
// ... input handling ...
AimDirection = aimDir.normalized; // Always update

// NEW: Maintain last direction
Vector2 aimDir = AimDirection; // Start with current
bool hasNewInput = false;
// ... input handling sets hasNewInput = true ...
// Only update if new input or uninitialized
if (hasNewInput || AimDirection.sqrMagnitude < 0.01f)
{
    AimDirection = aimDir.normalized;
}
```

**Initialization** (in Awake):
```csharp
AimDirection = Vector2.up; // Initialize once
AimInput = Vector2.up;
```

### 2. PlayerController.cs
**Changed**:
```csharp
// OLD: Fall back to move direction
if (inputAim.sqrMagnitude > 0.01f)
{
    targetAim = inputAim.normalized;
}
else if (_lastMoveDirection.sqrMagnitude > 0.01f)
{
    targetAim = _lastMoveDirection; // ❌ Unwanted fallback
}

// NEW: Maintain last aim
switch (ControlScheme)
{
    case ControlScheme.TwinStick:
    case ControlScheme.FaceMovement:
        if (inputAim.sqrMagnitude > 0.01f)
        {
            targetAim = inputAim.normalized;
        }
        // else: keep targetAim = _rawAimDirection (maintain)
        break;
}
```

## Behavior After Fix

### Twin Stick Mode
- Move right stick → Aim updates
- Release right stick → **Aim stays at last position** ✓
- Move left stick → Aim unchanged ✓
- Mouse aim → Always tracks mouse cursor ✓

### Face Movement Mode
- Same as Twin Stick for aiming
- Ship rotates to movement direction
- Aim independent from ship rotation

### Classic Rotate Mode
- Aim locked to ship facing direction
- No independent aiming

### Tank Controls
- Aim independent from ship rotation
- Same persistence as Twin Stick

## Testing

### Test Case 1: Gamepad Right Stick
1. Push right stick in any direction
2. Release stick to center
3. **Expected**: Aim direction stays where it was
4. **Before Fix**: Reset to 12 o'clock ❌
5. **After Fix**: Maintains direction ✓

### Test Case 2: Mouse Aim
1. Move mouse to aim in any direction
2. Stop moving mouse
3. **Expected**: Aim stays at mouse cursor position
4. **Result**: Works correctly ✓

### Test Case 3: Movement Independence
1. Aim right (3 o'clock)
2. Move up (12 o'clock)
3. **Expected**: Aim stays at 3 o'clock while moving up
4. **Result**: Works correctly ✓

## Files Changed
- `Assets/_Project/Scripts/Input/InputManager.cs`
  - Updated `UpdateAimDirection()` to maintain last aim
  - Initialize `AimDirection` in `Awake()`

- `Assets/_Project/Scripts/Entities/Player/PlayerController.cs`
  - Updated `UpdateAimDirection()` to remove move direction fallback
  - Added control scheme-specific aim handling

## Impact
- **Twin Stick Controls**: Now works as expected
- **Gamepad Experience**: Improved (no annoying aim reset)
- **Mouse Aim**: Unchanged (already worked)
- **Other Control Schemes**: Unaffected

---

**Status**: ✅ Fixed - Aim direction now persists when stick/mouse is idle
