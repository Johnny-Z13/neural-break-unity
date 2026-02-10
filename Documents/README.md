# Neural Break Unity - Technical Documentation

This folder contains active implementation notes and archived documentation for the Neural Break Unity project.

---

## 📁 Active Documentation

### Implementation Guides
- **SMART_BOMB_IMPLEMENTATION.md** - Smart bomb system with screen-clear mechanics, VFX, and UI
- **CONTROL_SCHEMES_IMPLEMENTATION.md** - Multiple control schemes (Twin Stick, Face Movement, Classic Rotate, Tank)
- **AIM_DIRECTION_FIX.md** - Fix for aim direction persistence in twin-stick mode
- **GAME_MODE_ARCHITECTURE.md** - Game mode system architecture (Arcade, Rogue, Test)

---

## 📦 Archive/ (Completed Work)

Historical documentation for completed refactorings and fixes:

### Refactoring Summaries
- **REFACTORING_COMPLETE.md** - Comprehensive overnight refactoring (Jan 23, 2026)
- **REFACTORING_PROGRESS.md** - Refactoring progress tracking
- **REFACTORING_SUMMARY_EnemyDeathVFX.md** - Enemy death VFX system refactoring
- **REFACTORING_VISUAL_COMPARISON.md** - Visual comparison of refactoring changes
- **SINGLETON_REFACTOR_SUMMARY.md** - Singleton pattern refactoring (29→8 singletons)

### Bug Fixes & Optimization
- **BUG_FIXES_SUMMARY.md** - Bug fix summaries
- **BUG_SWEEP_FIXES.md** - Bug sweep resolutions
- **DEBUG_LOG_OPTIMIZATION.md** - Debug logging optimization guide
- **QUICK_LOG_UPDATE_GUIDE.md** - Quick guide for updating log statements

### Porting & Migration
- **NEURAL_BREAK_PORTING_PLAN.md** - Original TypeScript → Unity port plan (~85% complete when archived)
- **CoplayPlan.md** - Coplay integration planning
- **SMART_BOMB_INPUT_FIXED.md** - Smart bomb input implementation
- **SMART_BOMB_SETUP_COMPLETE.md** - Smart bomb setup completion
- **UI/ARCHITECTURE.md** - UI architecture notes
- **UI/REFACTORING_SUMMARY.md** - UI refactoring summary

---

## 📝 Guidelines for Contributors

### When to Add Documentation Here:
- ✅ Implementation summaries for new features
- ✅ Architecture decisions and design patterns
- ✅ Bug fix summaries with technical details
- ✅ System integration guides

### When to Archive (Move to Archive/):
- ✅ Completed refactorings
- ✅ Resolved bug summaries
- ✅ Obsolete optimization guides
- ✅ Completed porting plans

### What to Keep in Project Root:
- **README.md** - Main project overview for end users
- **CLAUDE.md** - Core development rules and architecture (for AI/developers)
- **CLAUDE_REFERENCE.md** - Extended examples and patterns
- **Z13.Core/README.md** - Shared package documentation

---

## 🔍 Current Project Status (Feb 2026)

### Architecture Highlights
- **Z13.Core Package**: Reusable systems extracted (EventBus, ObjectPool, LogHelper, SaveSystemBase)
- **Event-Driven**: All systems communicate via type-safe EventBus
- **Zero-Allocation**: ObjectPool<T> for runtime spawning
- **Config-Driven**: GameBalanceConfig ScriptableObject for all balance values

### Recent Work
- Twin-stick auto-fire control fix (Feb 9)
- Smart bomb system implementation
- Documentation overhaul and archival (Feb 10)

### Active Development Areas
- Rogue mode card upgrade system
- Boot scene architecture (designed but not implemented)

---

## 📌 Note to Future Contributors

This folder serves as the project's technical knowledge base:

1. **Active docs** = Current systems and recent implementations
2. **Archive/** = Historical context for completed work
3. When adding new docs:
   - Use clear headings: Problem, Solution, Files Changed, Testing, Status
   - Include code examples where helpful
   - Update this README index
4. When work is complete, move to Archive/ and update index

---

**Last Updated**: 2026-02-10
