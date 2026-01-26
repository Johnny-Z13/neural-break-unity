#!/usr/bin/env python3
"""
Batch update Debug.Log statements to use LogHelper for performance optimization.
This script processes all C# files and wraps Debug.Log/LogWarning with conditional compilation.
"""

import os
import re
from pathlib import Path

# Configuration
SCRIPTS_DIR = Path(r"D:\Projects\Unity\neural-break-unity\Assets\_Project\Scripts")
USING_STATEMENT = "using NeuralBreak.Utils;"

# Patterns
DEBUG_LOG_PATTERN = re.compile(r'(\s+)Debug\.Log\(')
DEBUG_LOG_WARNING_PATTERN = re.compile(r'(\s+)Debug\.LogWarning\(')
USING_SECTION_PATTERN = re.compile(r'(using [^;]+;[\r\n]+)+', re.MULTILINE)

def should_process_file(filepath):
    """Determine if file should be processed."""
    # Skip editor scripts (they need logs in editor anyway)
    if 'Editor' in str(filepath):
        return False
    # Skip debug scripts (they're for debugging)
    if 'Debug' in str(filepath):
        return False
    return True

def add_using_statement(content):
    """Add using NeuralBreak.Utils if not present."""
    if USING_STATEMENT in content:
        return content

    # Find the last using statement
    matches = list(USING_SECTION_PATTERN.finditer(content))
    if matches:
        last_match = matches[-1]
        insert_pos = last_match.end()
        return content[:insert_pos] + USING_STATEMENT + '\n' + content[insert_pos:]

    # If no using statements, add after namespace or at start
    namespace_match = re.search(r'namespace\s+[\w\.]+\s*\{', content)
    if namespace_match:
        insert_pos = namespace_match.start()
        return content[:insert_pos] + USING_STATEMENT + '\n\n' + content[insert_pos:]

    return USING_STATEMENT + '\n\n' + content

def replace_debug_calls(content):
    """Replace Debug.Log/LogWarning with LogHelper equivalents."""
    # Replace Debug.Log( with LogHelper.Log(
    content = DEBUG_LOG_PATTERN.sub(r'\1LogHelper.Log(', content)

    # Replace Debug.LogWarning( with LogHelper.LogWarning(
    content = DEBUG_LOG_WARNING_PATTERN.sub(r'\1LogHelper.LogWarning(', content)

    # Keep Debug.LogError as-is (always log errors)

    return content

def process_file(filepath):
    """Process a single file."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        # Check if file has Debug.Log statements
        if 'Debug.Log(' not in content and 'Debug.LogWarning(' not in content:
            return False

        original_content = content

        # Add using statement
        content = add_using_statement(content)

        # Replace Debug calls
        content = replace_debug_calls(content)

        # Only write if changed
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            return True

        return False
    except Exception as e:
        print(f"Error processing {filepath}: {e}")
        return False

def main():
    """Main function."""
    print(f"Processing C# files in {SCRIPTS_DIR}...")

    processed_count = 0
    skipped_count = 0

    for filepath in SCRIPTS_DIR.rglob('*.cs'):
        if should_process_file(filepath):
            if process_file(filepath):
                print(f"✓ Updated: {filepath.relative_to(SCRIPTS_DIR)}")
                processed_count += 1
        else:
            skipped_count += 1

    print(f"\nSummary:")
    print(f"  Processed: {processed_count} files")
    print(f"  Skipped: {skipped_count} files (Editor/Debug)")
    print(f"\nDone! All Debug.Log/LogWarning calls have been wrapped with LogHelper.")
    print(f"Debug.LogError calls were kept as-is (always important in production).")

if __name__ == '__main__':
    main()
