#!/usr/bin/env python3
"""
Relic ScriptableObject Config Validator

Validates YAML/JSON configuration files against expected schemas for
Unity ScriptableObjects. These configs will be converted to ScriptableObjects
when Unity is available.

Supported config types:
- EraConfig: Era definitions (Ancient, Medieval, WWII, Future)
- UnitArchetype: Unit type definitions
- WeaponStats: Weapon configuration
- UpgradeDefinition: Squad upgrade definitions

Usage:
    python config_validator.py validate <config_file>
    python config_validator.py validate-all <directory>
    python config_validator.py schema <type>

Example:
    python config_validator.py validate configs/eras/ancient.yaml
    python config_validator.py validate-all configs/
    python config_validator.py schema UnitArchetype
"""

import argparse
import json
import sys
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Any, Optional

try:
    import yaml
    YAML_AVAILABLE = True
except ImportError:
    YAML_AVAILABLE = False


class ConfigType(Enum):
    """Supported ScriptableObject configuration types."""
    ERA_CONFIG = "EraConfig"
    UNIT_ARCHETYPE = "UnitArchetype"
    WEAPON_STATS = "WeaponStats"
    UPGRADE_DEFINITION = "UpgradeDefinition"


@dataclass
class ValidationError:
    """Represents a validation error."""
    path: str
    message: str
    severity: str = "error"  # error, warning

    def __str__(self) -> str:
        return f"[{self.severity.upper()}] {self.path}: {self.message}"


@dataclass
class ValidationResult:
    """Result of validating a config file."""
    file_path: str
    config_type: Optional[ConfigType]
    errors: list[ValidationError] = field(default_factory=list)
    warnings: list[ValidationError] = field(default_factory=list)

    @property
    def is_valid(self) -> bool:
        return len(self.errors) == 0

    def add_error(self, path: str, message: str) -> None:
        self.errors.append(ValidationError(path, message, "error"))

    def add_warning(self, path: str, message: str) -> None:
        self.warnings.append(ValidationError(path, message, "warning"))


# Schema definitions for each config type
SCHEMAS: dict[ConfigType, dict[str, Any]] = {
    ConfigType.ERA_CONFIG: {
        "required_fields": {
            "name": str,
            "archetypes": list,
        },
        "optional_fields": {
            "description": str,
            "visual_style": str,
            "upgrades": list,
        },
        "nested_schemas": {
            "archetypes": "UnitArchetype",
            "upgrades": "UpgradeDefinition",
        }
    },
    ConfigType.UNIT_ARCHETYPE: {
        "required_fields": {
            "id": str,
            "base_health": (int, float),
            "base_move_speed": (int, float),
        },
        "optional_fields": {
            "name": str,
            "description": str,
            "weapon": str,  # Reference to WeaponStats by name
            "weapon_stats": dict,  # Inline WeaponStats
            "prefab": str,
        },
        "constraints": {
            "base_health": {"min": 0, "max": 10000},
            "base_move_speed": {"min": 0, "max": 100},
        }
    },
    ConfigType.WEAPON_STATS: {
        "required_fields": {
            "name": str,
            "shots_per_burst": int,
            "fire_rate": (int, float),
            "base_hit_chance": (int, float),
            "base_damage": (int, float),
        },
        "optional_fields": {
            "effective_range": (int, float),
            "range_curve": list,  # List of [distance, multiplier] pairs
            "elevation_curve": list,  # List of [height_diff, multiplier] pairs
            "description": str,
        },
        "constraints": {
            "shots_per_burst": {"min": 1, "max": 100},
            "fire_rate": {"min": 0.1, "max": 100},
            "base_hit_chance": {"min": 0, "max": 1},
            "base_damage": {"min": 0, "max": 10000},
            "effective_range": {"min": 0, "max": 1000},
        }
    },
    ConfigType.UPGRADE_DEFINITION: {
        "required_fields": {
            "name": str,
        },
        "optional_fields": {
            "description": str,
            "hit_chance_multiplier": (int, float),
            "damage_multiplier": (int, float),
            "elevation_bonus": (int, float),
            "cost": int,
        },
        "constraints": {
            "hit_chance_multiplier": {"min": 0, "max": 10},
            "damage_multiplier": {"min": 0, "max": 10},
            "elevation_bonus": {"min": -1, "max": 1},
            "cost": {"min": 0, "max": 100000},
        }
    }
}


def detect_config_type(data: dict[str, Any]) -> Optional[ConfigType]:
    """Detect the config type based on the data structure."""
    if "type" in data:
        type_str = data["type"]
        for config_type in ConfigType:
            if config_type.value.lower() == type_str.lower():
                return config_type

    # Heuristics based on required fields
    if "archetypes" in data:
        return ConfigType.ERA_CONFIG
    if "shots_per_burst" in data or "fire_rate" in data:
        return ConfigType.WEAPON_STATS
    if "hit_chance_multiplier" in data or "damage_multiplier" in data:
        return ConfigType.UPGRADE_DEFINITION
    if "base_health" in data or "base_move_speed" in data:
        return ConfigType.UNIT_ARCHETYPE

    return None


def validate_field_type(
    value: Any,
    expected_type: type | tuple[type, ...],
    field_path: str,
    result: ValidationResult
) -> bool:
    """Validate that a field has the expected type."""
    if isinstance(expected_type, tuple):
        if not isinstance(value, expected_type):
            type_names = " or ".join(t.__name__ for t in expected_type)
            result.add_error(
                field_path,
                f"Expected {type_names}, got {type(value).__name__}"
            )
            return False
    else:
        if not isinstance(value, expected_type):
            result.add_error(
                field_path,
                f"Expected {expected_type.__name__}, got {type(value).__name__}"
            )
            return False
    return True


def validate_constraints(
    value: Any,
    constraints: dict[str, Any],
    field_path: str,
    result: ValidationResult
) -> None:
    """Validate numeric constraints on a field."""
    if not isinstance(value, (int, float)):
        return

    if "min" in constraints and value < constraints["min"]:
        result.add_error(
            field_path,
            f"Value {value} is below minimum {constraints['min']}"
        )

    if "max" in constraints and value > constraints["max"]:
        result.add_error(
            field_path,
            f"Value {value} exceeds maximum {constraints['max']}"
        )


def validate_curve(
    curve: list,
    field_path: str,
    result: ValidationResult
) -> None:
    """Validate an animation curve definition (list of [x, y] pairs)."""
    if not isinstance(curve, list):
        result.add_error(field_path, "Curve must be a list of [x, y] pairs")
        return

    for index, point in enumerate(curve):
        point_path = f"{field_path}[{index}]"
        if not isinstance(point, (list, tuple)) or len(point) != 2:
            result.add_error(point_path, "Each curve point must be [x, y]")
            continue

        x_val, y_val = point
        if not isinstance(x_val, (int, float)):
            result.add_error(f"{point_path}[0]", f"X value must be numeric, got {type(x_val).__name__}")
        if not isinstance(y_val, (int, float)):
            result.add_error(f"{point_path}[1]", f"Y value must be numeric, got {type(y_val).__name__}")


def validate_config(
    data: dict[str, Any],
    config_type: ConfigType,
    result: ValidationResult,
    path_prefix: str = ""
) -> None:
    """Validate a config dictionary against its schema."""
    schema = SCHEMAS.get(config_type)
    if not schema:
        result.add_error(path_prefix or "root", f"Unknown config type: {config_type}")
        return

    # Check required fields
    for field_name, expected_type in schema["required_fields"].items():
        field_path = f"{path_prefix}.{field_name}" if path_prefix else field_name
        if field_name not in data:
            result.add_error(field_path, f"Required field '{field_name}' is missing")
        else:
            validate_field_type(data[field_name], expected_type, field_path, result)

            # Check constraints if applicable
            if "constraints" in schema and field_name in schema["constraints"]:
                validate_constraints(
                    data[field_name],
                    schema["constraints"][field_name],
                    field_path,
                    result
                )

    # Check optional fields if present
    for field_name, expected_type in schema.get("optional_fields", {}).items():
        if field_name in data and data[field_name] is not None:
            field_path = f"{path_prefix}.{field_name}" if path_prefix else field_name
            validate_field_type(data[field_name], expected_type, field_path, result)

            # Check constraints if applicable
            if "constraints" in schema and field_name in schema["constraints"]:
                validate_constraints(
                    data[field_name],
                    schema["constraints"][field_name],
                    field_path,
                    result
                )

            # Validate curves
            if field_name.endswith("_curve") and isinstance(data[field_name], list):
                validate_curve(data[field_name], field_path, result)

    # Check for unknown fields (warning only)
    known_fields = set(schema["required_fields"].keys()) | set(schema.get("optional_fields", {}).keys())
    known_fields.add("type")  # Always allow type field
    for field_name in data:
        if field_name not in known_fields:
            field_path = f"{path_prefix}.{field_name}" if path_prefix else field_name
            result.add_warning(field_path, f"Unknown field '{field_name}'")


def load_config_file(file_path: Path) -> tuple[Optional[dict], Optional[str]]:
    """Load a config file (YAML or JSON)."""
    try:
        content = file_path.read_text()
        suffix = file_path.suffix.lower()

        if suffix in (".yaml", ".yml"):
            if not YAML_AVAILABLE:
                return None, "PyYAML is not installed. Run: pip install pyyaml"
            return yaml.safe_load(content), None
        elif suffix == ".json":
            return json.loads(content), None
        else:
            return None, f"Unsupported file extension: {suffix}"

    except yaml.YAMLError as error:
        return None, f"YAML parsing error: {error}"
    except json.JSONDecodeError as error:
        return None, f"JSON parsing error: {error}"
    except OSError as error:
        return None, f"File read error: {error}"


def validate_file(file_path: Path) -> ValidationResult:
    """Validate a single config file."""
    result = ValidationResult(file_path=str(file_path), config_type=None)

    # Load the file
    data, error = load_config_file(file_path)
    if error:
        result.add_error("file", error)
        return result

    if not isinstance(data, dict):
        result.add_error("root", f"Expected dictionary, got {type(data).__name__}")
        return result

    # Detect config type
    config_type = detect_config_type(data)
    if not config_type:
        result.add_error("root", "Unable to detect config type. Add 'type' field or use recognizable field names.")
        return result

    result.config_type = config_type

    # Validate against schema
    validate_config(data, config_type, result)

    return result


def validate_directory(directory: Path) -> list[ValidationResult]:
    """Validate all config files in a directory."""
    results = []
    extensions = [".yaml", ".yml", ".json"]

    for ext in extensions:
        for file_path in directory.rglob(f"*{ext}"):
            # Skip test files
            if "test" in file_path.parts or "tests" in file_path.parts:
                continue
            results.append(validate_file(file_path))

    return results


def print_schema(config_type_name: str) -> None:
    """Print the schema for a config type."""
    # Find matching config type
    config_type = None
    for cfg_type in ConfigType:
        if cfg_type.value.lower() == config_type_name.lower():
            config_type = cfg_type
            break

    if not config_type:
        print(f"Unknown config type: {config_type_name}")
        print("Available types:", ", ".join(ct.value for ct in ConfigType))
        return

    schema = SCHEMAS[config_type]
    print(f"\n{config_type.value} Schema")
    print("=" * 40)

    print("\nRequired Fields:")
    for field_name, field_type in schema["required_fields"].items():
        type_name = field_type.__name__ if isinstance(field_type, type) else " | ".join(t.__name__ for t in field_type)
        constraints = ""
        if "constraints" in schema and field_name in schema["constraints"]:
            cons = schema["constraints"][field_name]
            constraints = f" (range: {cons.get('min', '∞')} to {cons.get('max', '∞')})"
        print(f"  - {field_name}: {type_name}{constraints}")

    if schema.get("optional_fields"):
        print("\nOptional Fields:")
        for field_name, field_type in schema["optional_fields"].items():
            type_name = field_type.__name__ if isinstance(field_type, type) else " | ".join(t.__name__ for t in field_type)
            constraints = ""
            if "constraints" in schema and field_name in schema["constraints"]:
                cons = schema["constraints"][field_name]
                constraints = f" (range: {cons.get('min', '∞')} to {cons.get('max', '∞')})"
            print(f"  - {field_name}: {type_name}{constraints}")

    # Print example
    print("\nExample (YAML):")
    if config_type == ConfigType.ERA_CONFIG:
        print("""
type: EraConfig
name: Ancient
description: Bronze age warriors
archetypes:
  - id: spearman
    base_health: 100
    base_move_speed: 5
    weapon: bronze_spear
upgrades:
  - name: Veterans
    hit_chance_multiplier: 1.2
""")
    elif config_type == ConfigType.UNIT_ARCHETYPE:
        print("""
type: UnitArchetype
id: spearman
name: Spearman
base_health: 100
base_move_speed: 5.0
weapon: bronze_spear
prefab: Units/Ancient/Spearman
""")
    elif config_type == ConfigType.WEAPON_STATS:
        print("""
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
""")
    elif config_type == ConfigType.UPGRADE_DEFINITION:
        print("""
type: UpgradeDefinition
name: Veterans
description: Experienced soldiers with better accuracy
hit_chance_multiplier: 1.2
damage_multiplier: 1.1
cost: 100
""")


def main() -> int:
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Relic ScriptableObject Config Validator",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )

    subparsers = parser.add_subparsers(dest="command", help="Commands")

    # Validate command
    validate_parser = subparsers.add_parser("validate", help="Validate a config file")
    validate_parser.add_argument("file", type=Path, help="Config file to validate")

    # Validate-all command
    validate_all_parser = subparsers.add_parser("validate-all", help="Validate all configs in directory")
    validate_all_parser.add_argument("directory", type=Path, help="Directory containing configs")

    # Schema command
    schema_parser = subparsers.add_parser("schema", help="Print schema for a config type")
    schema_parser.add_argument("type", help="Config type (EraConfig, UnitArchetype, WeaponStats, UpgradeDefinition)")

    args = parser.parse_args()

    if args.command == "validate":
        if not args.file.exists():
            print(f"Error: File not found: {args.file}")
            return 1

        result = validate_file(args.file)
        print(f"\nValidating: {result.file_path}")
        print(f"Detected type: {result.config_type.value if result.config_type else 'Unknown'}")

        for error in result.errors:
            print(f"  {error}")
        for warning in result.warnings:
            print(f"  {warning}")

        if result.is_valid:
            print("\n✓ Validation passed")
            if result.warnings:
                print(f"  ({len(result.warnings)} warnings)")
            return 0
        else:
            print(f"\n✗ Validation failed ({len(result.errors)} errors)")
            return 1

    elif args.command == "validate-all":
        if not args.directory.exists():
            print(f"Error: Directory not found: {args.directory}")
            return 1

        results = validate_directory(args.directory)
        if not results:
            print("No config files found")
            return 0

        total_errors = 0
        total_warnings = 0
        failed_files = []

        for result in results:
            print(f"\n{result.file_path}: ", end="")
            if result.is_valid:
                print("✓ OK", end="")
                if result.warnings:
                    print(f" ({len(result.warnings)} warnings)")
                else:
                    print()
            else:
                print(f"✗ FAILED ({len(result.errors)} errors)")
                failed_files.append(result.file_path)
                for error in result.errors:
                    print(f"    {error}")

            total_errors += len(result.errors)
            total_warnings += len(result.warnings)

        print(f"\n{'=' * 40}")
        print(f"Total: {len(results)} files, {total_errors} errors, {total_warnings} warnings")

        if failed_files:
            print(f"\nFailed files:")
            for file_path in failed_files:
                print(f"  - {file_path}")
            return 1

        return 0

    elif args.command == "schema":
        print_schema(args.type)
        return 0

    else:
        parser.print_help()
        return 0


if __name__ == "__main__":
    sys.exit(main())
