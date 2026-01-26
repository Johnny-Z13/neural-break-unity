# Smart Bomb Setup Guide

**Status**: Code complete, needs Unity scene setup
**Date**: 2026-01-26

---

## Components Created

### 1. SmartBombSystem.cs ✅
**Location**: `Assets/_Project/Scripts/Combat/SmartBombSystem.cs`
**Purpose**: Core smart bomb logic

**Features**:
- Player starts with 1 bomb (configurable)
- Max 3 bombs (configurable)
- Kills ALL enemies on screen
- Epic particle effects
- Camera shake via MMF_Player
- Unique sound effect
- Cooldown system

**Public Methods**:
- `TryActivateBomb()` - Manual trigger
- `AddBomb()` - Add bomb from pickup/reward
- `Reset()` - Reset on game start

**Events Published**:
- `SmartBombActivatedEvent` - When bomb fires
- `SmartBombCountChangedEvent` - When bomb count changes

---

### 2. SmartBombDisplay.cs ✅
**Location**: `Assets/_Project/Scripts/UI/SmartBombDisplay.cs`
**Purpose**: UI display for bomb count

**Features**:
- Shows "BOMBS:" label + icons
- Gold icons for available bombs
- Gray icons for used bombs
- Pulse animation on available bombs
- Flash effect on activation
- Positioned bottom-left (20, 150)

---

### 3. EventBus Events ✅
**Location**: `Assets/_Project/Scripts/Core/EventBus.cs`

Added:
```csharp
public struct SmartBombActivatedEvent { public Vector3 position; }
public struct SmartBombCountChangedEvent { public int count; public int maxCount; }
```

---

### 4. Input Binding ✅
**Location**: `Assets/_Project/Input/GameInput.inputactions`
**Action**: `Player/SmartBomb`
**Bindings**:
- Keyboard: B key
- Gamepad: L2 (leftTrigger)

**InputManager Integration**: ✅
- Added `OnSmartBombPressed` event
- Subscribed in SmartBombSystem

---

## Unity Scene Setup Required ⚠️

To make Smart Bombs work, you need to add the components to your scene:

### Step 1: Add SmartBombSystem

1. Find or create a **GameSystems** GameObject (or similar manager object)
2. Add Component → Scripts → NeuralBreak.Combat → **SmartBombSystem**
3. Configure settings:
   - **Starting Bombs**: 1
   - **Max Bombs**: 3
   - **Activation Duration**: 0.5s

4. **Assign VFX** (REQUIRED):
   - Create a Particle System GameObject (or use existing)
   - Configure for full-screen explosion:
     - Shape: Sphere, radius ~15
     - Emission: Burst 500 particles
     - Start lifetime: 1s
     - Start speed: 10-20
     - Start size: 0.5-2
   - Drag to **Explosion Particles** field

5. **Assign Feedbacks** (Optional but recommended):
   - Create MMF_Player for activation feedback (flash, sound)
   - Create MMF_Player for camera shake (MMF_CameraShake)
   - Drag to respective fields

6. **Assign Audio** (Optional):
   - Create/import epic explosion sound (short, punchy, 0.5-1s)
   - Drag to **Epic Explosion Sound** field
   - Set volume (0.8 recommended)

---

### Step 2: Add SmartBombDisplay

1. Find or create a **UI** GameObject
2. Add Component → Scripts → NeuralBreak.UI → **SmartBombDisplay**
3. Settings auto-configured:
   - Position: (20, 150) bottom-left
   - Icon size: 45px
   - Gold color for active bombs

No additional setup needed - creates UI at runtime.

---

## Testing Checklist

### Basic Functionality
- [ ] Start game
- [ ] UI shows "BOMBS: [icon]" in bottom-left
- [ ] Icon is gold/yellow (active)
- [ ] Icon pulses gently

### Activation
- [ ] Press B (keyboard) or L2 (gamepad)
- [ ] All enemies die instantly
- [ ] Particle effect plays (full-screen burst)
- [ ] Camera shakes
- [ ] Sound plays
- [ ] Icon turns gray (used)
- [ ] Console shows: `[SmartBombSystem] SMART BOMB ACTIVATED! Bombs remaining: 0`

### Edge Cases
- [ ] Try to use bomb when count = 0 (should show warning)
- [ ] Die and restart - bomb count resets to 1
- [ ] Pause game - bomb input ignored (handled by GameManager)

---

## Input Bindings Reference

**Keyboard**: B key
**Gamepad**: L2 (leftTrigger)

Already configured in:
- `GameInput.inputactions` - Input System asset
- `InputManager.cs` - Event subscription
- `SmartBombSystem.cs` - Event handler

---

## VFX Recommendations

### Particle System Settings
```
Main Module:
  Duration: 1.0
  Looping: OFF
  Start Lifetime: 1.0
  Start Speed: 10-20
  Start Size: 0.5-2
  Start Color: Gold → Orange (gradient)
  Max Particles: 500

Emission:
  Rate Over Time: 0
  Bursts: 1 burst at time 0.0, count 500

Shape:
  Shape: Sphere
  Radius: 15 (covers full screen at typical zoom)

Color Over Lifetime:
  Gradient: Gold (1,1) → Orange (0.5,0.5) → Transparent (0,0)

Size Over Lifetime:
  Curve: Start 1.0 → End 0.0 (shrink)
```

---

## Camera Shake Setup (MMF_Player)

1. Create GameObject → "SmartBomb_CameraShake"
2. Add Component → More Mountains → Feedbacks → MMF Player
3. Add Feedback → Camera → **MMF Camera Shake**
4. Settings:
   - Duration: 0.5s
   - Amplitude: 2-3
   - Frequency: 30-40 Hz

---

## Audio Recommendations

**Sound Type**: Short explosive burst
**Duration**: 0.5-1 second
**Style**: Deep bass + bright treble (epic impact)
**Volume**: 0.7-0.9 (loud but not clipping)

**Suggested Layers**:
1. Deep bass thump (100-200 Hz)
2. Mid-range crunch (1-3 kHz)
3. High-frequency sparkle (8-12 kHz with reverb)

---

## Known Issues

### ❌ Component Not in Scene
**Problem**: SmartBombSystem not added to scene yet
**Solution**: Follow Step 1 above

### ⚠️ No VFX Assigned
**Problem**: Particles don't play
**Solution**: Assign particle system to SmartBombSystem

### ⚠️ No Audio
**Problem**: Silent activation
**Solution**: Assign audio clip to SmartBombSystem

---

## Integration Notes

### EventBus Integration ✅
- Publishes `SmartBombActivatedEvent` for other systems to react
- Publishes `SmartBombCountChangedEvent` for UI updates
- Subscribes to `GameStartedEvent` for reset

### InputManager Integration ✅
- Uses `OnSmartBombPressed` event
- Respects pause state via GameManager check
- No polling - event-driven

### Enemy Integration ✅
- Calls `EnemyBase.Kill()` on all enemies
- Works with object pools
- No special enemy code needed

---

## Future Enhancements

Potential additions (not implemented):
- [ ] Smart Bomb pickup item
- [ ] Screen flash effect (full-screen white flash)
- [ ] Slow-motion effect during activation
- [ ] Score multiplier for smart bomb kills
- [ ] Achievement: "Clear Screen" (kill 50+ enemies with one bomb)
- [ ] Sound effect variation (randomize pitch/volume)

---

## Troubleshooting

### "Cannot activate - no bombs available"
**Cause**: Bomb count = 0
**Fix**: Check bomb count in UI, use AddBomb() for testing

### No enemies die
**Cause**: SmartBombSystem not in scene OR enemies not using EnemyBase
**Fix**: Add component to scene, verify enemies inherit EnemyBase

### UI doesn't show
**Cause**: SmartBombDisplay not in scene OR Canvas render mode wrong
**Fix**: Add component to scene, check console for errors

### Input doesn't work
**Cause**: InputManager not initialized OR GameInput.inputactions not assigned
**Fix**: Check InputManager.Instance != null, verify input asset

---

**Setup Status**: Code ✅ | Scene ❌ | Testing ⏳
