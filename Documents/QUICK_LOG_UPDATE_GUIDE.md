# Quick Guide: Update Debug Logs to LogHelper

## TL;DR
Replace `Debug.Log` with `LogHelper.Log` for zero-cost logging in production builds.

## 3-Step Process

### Step 1: Add Using Statement
```csharp
using NeuralBreak.Utils;  // Add this at top with other usings
```

### Step 2: Replace Debug Calls
| Before | After | Reason |
|--------|-------|--------|
| `Debug.Log(...)` | `LogHelper.Log(...)` | Stripped from production |
| `Debug.LogWarning(...)` | `LogHelper.LogWarning(...)` | Stripped from production |
| `Debug.LogError(...)` | `LogHelper.LogError(...)` | Always logs (errors important) |

### Step 3: Test
- ✅ Editor: Logs still work
- ✅ Build: Logs removed (check player.log)
- ✅ Errors: Still logged

## Example

### Before ❌
```csharp
using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Combat
{
    public class MySystem : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("[MySystem] Starting up...");
            Debug.LogWarning("[MySystem] Config missing!");
            Debug.LogError("[MySystem] Critical error!");
        }
    }
}
```

### After ✅
```csharp
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Utils;  // ADD THIS

namespace NeuralBreak.Combat
{
    public class MySystem : MonoBehaviour
    {
        void Start()
        {
            LogHelper.Log("[MySystem] Starting up...");
            LogHelper.LogWarning("[MySystem] Config missing!");
            LogHelper.LogError("[MySystem] Critical error!");
        }
    }
}
```

## Why?

### Performance Impact
```
Before: Debug.Log("[System] Message");
- String allocation
- String formatting
- Unity API call
- Console rendering
= BAD for performance in production

After: LogHelper.Log("[System] Message");
- [Conditional("UNITY_EDITOR")] attribute
- Compiler strips entire call from builds
- Zero runtime cost
= GOOD for performance
```

### Build Size Comparison
- **Editor**: Same behavior, all logs work
- **Production**:
  - ❌ Debug.Log/LogWarning stripped (0 cost)
  - ✅ LogError kept (important for debugging)

## Batch Update Script

Run this to update all files automatically:
```bash
python update_debug_logs.py
```

The script:
1. Finds all .cs files with Debug.Log
2. Adds `using NeuralBreak.Utils;`
3. Replaces Debug calls with LogHelper
4. Skips Editor/ and Debug/ folders
5. Keeps Debug.LogError as LogHelper.LogError

## Files Updated So Far (6/49)

✅ **High Priority Complete**:
1. GameManager.cs (8 logs)
2. LevelManager.cs (33 logs)
3. EnemySpawner.cs (10 logs)
4. StartScreen.cs (7 logs)
5. WeaponSystem.cs (11 logs)
6. PlayerHealth.cs (14 logs)

⏳ **Remaining**: 43 files (use batch script)

## Testing Checklist

- [ ] Editor: Open Unity Console, verify logs still appear
- [ ] Editor: Check warnings still highlighted yellow
- [ ] Editor: Check errors still highlighted red
- [ ] Build: Create Release build
- [ ] Build: Check player.log - no Debug.Log messages
- [ ] Build: Verify errors still logged
- [ ] Performance: Profile frame time improvement

## Common Mistakes

### ❌ Forgot Using Statement
```csharp
// ERROR: LogHelper not found
LogHelper.Log("Message");
```
**Fix**: Add `using NeuralBreak.Utils;` at top

### ❌ Only Replaced Some Calls
```csharp
// INCONSISTENT
LogHelper.Log("Starting...");
Debug.Log("Still using old way");  // BAD
```
**Fix**: Replace ALL Debug.Log calls in file

### ❌ Replaced Context Overloads
```csharp
// PROBLEM: LogHelper doesn't have this overload yet
LogHelper.Log("Message", gameObject);
```
**Fix**: Either:
- Add overload to LogHelper class
- Or use: `LogHelper.Log($"Message: {gameObject.name}")`

## Need Help?

See `DEBUG_LOG_OPTIMIZATION.md` for full documentation.
