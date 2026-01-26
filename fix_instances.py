#!/usr/bin/env python3
"""
Fix all .Instance references for removed singletons.
Replace with FindObjectOfType or EventBus patterns.
"""

import os
import re
from pathlib import Path

# Classes that were converted from singletons (should NOT have .Instance)
REMOVED_SINGLETONS = [
    'AchievementSystem',
    'ArenaManager',
    'PlayerLevelSystem',
    'WeaponUpgradeManager',
    'GamepadRumble',
    'MusicManager',
    'UIFeedbacks',
    'ScreenFlash',
    'VFXManager',
    'EnemyDeathVFX',
    'StarfieldController',
    'EnvironmentParticles',
    'FeedbackManager',
    'PostProcessManager',
    'HighScoreManager',
    'ShipCustomization',
    'WaveAnnouncement',
    'ControlsOverlay',
    'DamageNumberPopup',
    'Minimap',
    'FeedbackSetup',
    'UIManager',
]

# Classes that should KEEP .Instance (valid singletons)
KEPT_SINGLETONS = [
    'GameManager',
    'LevelManager',
    'InputManager',
    'AudioManager',
    'SaveSystem',
    'AccessibilityManager',
    'EnemyProjectilePool',
]

def fix_file(filepath):
    """Fix Instance references in a single file."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original_content = content
    changes = []

    # Replace each removed singleton's Instance with FindObjectOfType
    for singleton in REMOVED_SINGLETONS:
        pattern = rf'\b{singleton}\.Instance\b'
        if re.search(pattern, content):
            # Simple replacement: ClassName.Instance → FindObjectOfType<ClassName>()
            replacement = f'FindObjectOfType<{singleton}>()'
            content = re.sub(pattern, replacement, content)
            changes.append(singleton)

    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return changes
    return None

def main():
    scripts_dir = Path(r'D:\Projects\Unity\neural-break-unity\Assets\_Project\Scripts')

    total_files = 0
    total_changes = 0

    for cs_file in scripts_dir.rglob('*.cs'):
        changes = fix_file(cs_file)
        if changes:
            total_files += 1
            total_changes += len(changes)
            print(f"Fixed {cs_file.name}: {', '.join(changes)}")

    print(f"\nTotal: Fixed {total_changes} Instance references in {total_files} files")

if __name__ == '__main__':
    main()
