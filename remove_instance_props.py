#!/usr/bin/env python3
"""
Remove Instance property and Awake/OnDestroy singleton code from removed singletons.
"""

import os
import re
from pathlib import Path

# Classes that should have their Instance property removed
REMOVED_SINGLETONS = [
    'AchievementSystem',
    'ArenaManager',
    'GamepadRumble',
    'MusicManager',
    'EnvironmentParticles',
    'FeedbackManager',
    'PostProcessManager',
    'ShipCustomization',
    'DamageNumberPopup',
    'Minimap',
    'StarfieldController',
    'UIManager',
]

def remove_singleton_code(filepath, classname):
    """Remove singleton Instance property and related code."""
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    new_lines = []
    skip_until = None
    removed_something = False

    i = 0
    while i < len(lines):
        line = lines[i]

        # Skip Instance property declaration
        if re.search(r'public\s+static\s+\w+\s+Instance\s*{', line):
            # Skip until we find the closing brace
            skip_until = '}'
            removed_something = True
            i += 1
            continue

        # Skip Awake singleton check
        if 'if (Instance != null && Instance != this)' in line or 'if (Instance != null' in line:
            # Skip this block (usually 4-5 lines)
            while i < len(lines) and '}' not in lines[i]:
                i += 1
            i += 1  # Skip the closing brace
            removed_something = True
            continue

        # Skip Instance = this/null lines
        if re.search(r'^\s+Instance\s*=\s*(this|null);', line):
            removed_something = True
            i += 1
            continue

        # Skip OnDestroy Instance cleanup
        if re.search(r'if\s*\(\s*Instance\s*==\s*this\s*\)', line):
            # Skip this block
            while i < len(lines) and '}' not in lines[i]:
                i += 1
            i += 1  # Skip the closing brace
            removed_something = True
            continue

        if skip_until:
            if skip_until in line:
                skip_until = None
            i += 1
            continue

        new_lines.append(line)
        i += 1

    if removed_something:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.writelines(new_lines)
        return True
    return False

def main():
    scripts_dir = Path(r'D:\Projects\Unity\neural-break-unity\Assets\_Project\Scripts')

    for singleton_name in REMOVED_SINGLETONS:
        # Find the file
        files = list(scripts_dir.rglob(f'{singleton_name}.cs'))
        if files:
            filepath = files[0]
            if remove_singleton_code(filepath, singleton_name):
                print(f"Removed singleton code from {singleton_name}.cs")

if __name__ == '__main__':
    main()
