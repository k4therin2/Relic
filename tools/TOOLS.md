# Relic Development Tools

Command-line utilities for developing and maintaining the Relic Unity project.

## Quick Reference

| Tool | Purpose | Unity Required? |
|------|---------|-----------------|
| `config_validator.py` | Validate ScriptableObject configs | No |
| `project_stats.py` | Project metrics and analysis | No |
| `build.py` | Build automation | Yes |

## Installation

These tools require Python 3.10+. Optional dependencies:

```bash
# For YAML config support
pip install pyyaml

# For running tests
pip install pytest
```

---

## Config Validator

Validates YAML/JSON configuration files against expected schemas for Unity ScriptableObjects. This allows you to validate configs before Unity is even installed.

### Usage

```bash
# Validate a single config file
python config_validator.py validate configs/eras/ancient.yaml

# Validate all configs in a directory
python config_validator.py validate-all configs/

# Print schema for a config type
python config_validator.py schema UnitArchetype
```

### Supported Config Types

| Type | Description | Key Fields |
|------|-------------|------------|
| `EraConfig` | Era definition (Ancient, Medieval, WWII, Future) | `name`, `archetypes`, `upgrades` |
| `UnitArchetype` | Unit type definition | `id`, `base_health`, `base_move_speed`, `weapon` |
| `WeaponStats` | Weapon configuration | `name`, `shots_per_burst`, `fire_rate`, `base_hit_chance`, `base_damage` |
| `UpgradeDefinition` | Squad upgrade | `name`, `hit_chance_multiplier`, `damage_multiplier` |

### Example Configs

**EraConfig (configs/eras/ancient.yaml):**
```yaml
type: EraConfig
name: Ancient
description: Bronze age warriors and archers
archetypes:
  - id: spearman
    base_health: 100
    base_move_speed: 5
    weapon: bronze_spear
  - id: archer
    base_health: 60
    base_move_speed: 6
    weapon: short_bow
upgrades:
  - name: Veterans
    hit_chance_multiplier: 1.2
    damage_multiplier: 1.1
```

**WeaponStats (configs/weapons/bronze_spear.yaml):**
```yaml
type: WeaponStats
name: bronze_spear
shots_per_burst: 1
fire_rate: 1.0
base_hit_chance: 0.8
base_damage: 25
effective_range: 2.0
range_curve:
  - [0, 1.0]
  - [2, 1.0]
  - [3, 0.5]
```

### Validation Constraints

The validator enforces these constraints:

| Field | Min | Max |
|-------|-----|-----|
| `base_health` | 0 | 10000 |
| `base_move_speed` | 0 | 100 |
| `shots_per_burst` | 1 | 100 |
| `fire_rate` | 0.1 | 100 |
| `base_hit_chance` | 0 | 1 |
| `base_damage` | 0 | 10000 |
| `hit_chance_multiplier` | 0 | 10 |
| `damage_multiplier` | 0 | 10 |

---

## Project Stats

Generates statistics and metrics about the Relic project. Works without Unity installed.

### Usage

```bash
# Show project summary
python project_stats.py summary

# Show code statistics
python project_stats.py code
python project_stats.py code --verbose  # Include largest files

# Show asset breakdown
python project_stats.py assets
python project_stats.py assets --by-era  # Break down by era

# Show documentation coverage
python project_stats.py docs

# Show directory structure
python project_stats.py structure

# Output as JSON (for automation)
python project_stats.py --json
```

### Metrics Provided

**Code Statistics:**
- Total lines of code
- Lines by type (code, comments, blank)
- Comment ratio
- Breakdown by file extension (.cs, .py, etc.)
- Largest files

**Asset Statistics:**
- File counts by type (textures, models, prefabs, etc.)
- Size breakdown
- Era-specific asset counts

**Documentation Coverage:**
- Checks for expected documentation files
- Line counts per document

### Example Output

```
==================================================
RELIC PROJECT STATISTICS
==================================================

Root: /home/user/projects/relic
Total Files: 42
Total Lines: 3,250
Total Size: 1.24 MB

Files by Category:
------------------------------------------
  docs             7 files  (1,200 lines)
  code             3 files  (450 lines)
  config           2 files  (85 lines)
```

---

## Build Automation

Automates Unity builds for different targets. **Requires Unity to be installed.**

### Setup

Set the Unity executable path:

```bash
# Linux
export UNITY_PATH="$HOME/Unity/Hub/Editor/6000.0.3f1/Editor/Unity"

# macOS
export UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.3f1/Unity.app/Contents/MacOS/Unity"

# Windows (PowerShell)
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\6000.0.3f1\Editor\Unity.exe"
```

Or let the tool auto-detect (looks for Unity 6.x in standard locations).

### Usage

```bash
# Check prerequisites
python build.py check

# Build for Quest 3
python build.py quest3
python build.py quest3 --profile release    # Release build
python build.py quest3 --install            # Build and install to device

# Build for debug/standalone
python build.py debug
python build.py debug --profile profile     # With profiler support

# Specify output directory
python build.py quest3 --output ./builds/test/
```

### Build Profiles

| Profile | Development | Debugging | Compression | IL2CPP |
|---------|------------|-----------|-------------|--------|
| `debug` | Yes | Yes | None | No |
| `release` | No | No | LZ4HC | Yes |
| `profile` | Yes | No | LZ4 | Yes |

### Build Targets

| Target | Platform | Scene | Description |
|--------|----------|-------|-------------|
| `quest3` | Android | AR_Battlefield | Quest 3 with XR enabled |
| `debug` | Standalone | Flat_Debug | Desktop debug mode |
| `android` | Android | AR_Battlefield | Generic Android |

### Output

Builds are placed in `Builds/` by default with naming convention:
```
Relic_{target}_{profile}_{timestamp}.{ext}
```

Example: `Relic_quest3_debug_20251226_143000.apk`

### Device Installation

For Quest 3 builds, use `--install` to automatically install via ADB:

```bash
python build.py quest3 --install
```

Requirements:
- ADB installed and in PATH
- Quest 3 connected via USB
- Developer mode enabled on device

---

## Creating New Tools

To add a new tool to this suite:

1. Create the Python script in `tools/`
2. Follow the existing patterns:
   - Use argparse for CLI
   - Include docstring with usage examples
   - Return 0 for success, non-zero for failure
3. Add tests in `tools/tests/`
4. Document in this file

### Code Style

- Python 3.10+ features (type hints, match statements)
- Use dataclasses for structured data
- Follow existing patterns for CLI structure
- Include docstrings with examples

---

## Testing

Run the tool test suite:

```bash
# Run all tool tests
pytest tools/tests/

# Run specific test file
pytest tools/tests/test_config_validator.py

# With verbose output
pytest tools/tests/ -v
```

---

## Troubleshooting

### Config Validator

**"PyYAML is not installed"**
```bash
pip install pyyaml
```

**"Unable to detect config type"**
Add a `type` field to your config:
```yaml
type: UnitArchetype
id: spearman
...
```

### Build Tool

**"Unity executable not found"**
Set the `UNITY_PATH` environment variable to your Unity installation.

**"Assets/ directory not found"**
Run from the Relic project root, or use `--path` to specify it.

**Build hangs**
Check `Builds/build.log` for Unity output. Common causes:
- License not activated
- Missing Android SDK
- Script compilation errors

### Project Stats

**"Cannot find Relic project root"**
Run from the project directory or specify with `--path`.

---

*Created by Agent-Nadia, 2025-12-26*
*WP-EXT-1.7: Relic CLI Tools & Scripts*
