# Z13.Core

Core utilities for Z13 Labs Unity projects.

## Installation

This package is included as a local package. To use in another project, copy the `Z13.Core` folder to your project's `Packages` folder.

## Features

### EventBus

Type-safe pub/sub event system for decoupled communication.

```csharp
using Z13.Core;

// Define event structs in your project
public struct PlayerDamagedEvent
{
    public int damage;
    public int currentHealth;
}

// Subscribe
EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);

// Publish
EventBus.Publish(new PlayerDamagedEvent { damage = 10, currentHealth = 90 });

// Unsubscribe (important for cleanup!)
EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);

// Clear all subscriptions (call on scene unload)
EventBus.Clear();
```

### ObjectPool

Zero-allocation object pooling for runtime spawning.

```csharp
using Z13.Core;

// Create pool
var pool = new ObjectPool<Projectile>(
    prefab: projectilePrefab,
    parent: transform,
    initialSize: 100,
    onGet: p => p.Reset(),
    onReturn: p => p.Cleanup()
);

// Get from pool
var projectile = pool.Get();
var projectile2 = pool.Get(position, rotation);

// Return to pool
pool.Return(projectile);
```

### LogHelper

Performance-optimized logging that strips from production builds.

```csharp
using Z13.Core;

// These are stripped in production (zero overhead)
LogHelper.Log("Debug message");
LogHelper.LogWarning("Warning message");

// Errors are always included
LogHelper.LogError("Error message");
```

### SaveSystemBase

Generic base class for save systems with JSON file I/O.

```csharp
using Z13.Core;

[Serializable]
public class MySaveData
{
    public int highScore;
    public List<string> unlockedItems;
}

public class MySaveSystem : SaveSystemBase<MySaveData>
{
    protected override string SaveFileName => "my_game_save.json";
    protected override bool ShouldAutoSave => IsPlaying;

    // Override hooks as needed
    protected override void OnBeforeSave() { }
    protected override void OnAfterLoad() { }
}
```

## Requirements

- Unity 6000.0+
- No external dependencies

## License

Copyright (c) Z13 Labs. All rights reserved.
