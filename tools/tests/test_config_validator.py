"""Tests for the config_validator tool."""

import json
import tempfile
from pathlib import Path

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from config_validator import (
    ConfigType,
    ValidationError,
    ValidationResult,
    detect_config_type,
    validate_config,
    validate_field_type,
    validate_constraints,
    validate_curve,
    validate_file,
    load_config_file,
)


class TestConfigTypeDetection:
    """Tests for config type detection."""

    def test_detect_era_config_by_type_field(self):
        data = {"type": "EraConfig", "name": "Ancient"}
        assert detect_config_type(data) == ConfigType.ERA_CONFIG

    def test_detect_era_config_by_heuristics(self):
        data = {"name": "Ancient", "archetypes": []}
        assert detect_config_type(data) == ConfigType.ERA_CONFIG

    def test_detect_unit_archetype_by_type_field(self):
        data = {"type": "UnitArchetype", "id": "spearman"}
        assert detect_config_type(data) == ConfigType.UNIT_ARCHETYPE

    def test_detect_unit_archetype_by_heuristics(self):
        data = {"id": "spearman", "base_health": 100, "base_move_speed": 5}
        assert detect_config_type(data) == ConfigType.UNIT_ARCHETYPE

    def test_detect_weapon_stats_by_type_field(self):
        data = {"type": "WeaponStats", "name": "sword"}
        assert detect_config_type(data) == ConfigType.WEAPON_STATS

    def test_detect_weapon_stats_by_heuristics(self):
        data = {"name": "sword", "shots_per_burst": 1, "fire_rate": 1.0}
        assert detect_config_type(data) == ConfigType.WEAPON_STATS

    def test_detect_upgrade_definition_by_type_field(self):
        data = {"type": "UpgradeDefinition", "name": "Veterans"}
        assert detect_config_type(data) == ConfigType.UPGRADE_DEFINITION

    def test_detect_upgrade_definition_by_heuristics(self):
        data = {"name": "Veterans", "hit_chance_multiplier": 1.2}
        assert detect_config_type(data) == ConfigType.UPGRADE_DEFINITION

    def test_detect_unknown_returns_none(self):
        data = {"unknown_field": "value"}
        assert detect_config_type(data) is None


class TestFieldValidation:
    """Tests for field type and constraint validation."""

    def test_validate_field_type_string_success(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        assert validate_field_type("hello", str, "name", result) is True
        assert len(result.errors) == 0

    def test_validate_field_type_string_failure(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        assert validate_field_type(123, str, "name", result) is False
        assert len(result.errors) == 1
        assert "Expected str" in result.errors[0].message

    def test_validate_field_type_number_union_success(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        assert validate_field_type(100, (int, float), "health", result) is True
        assert validate_field_type(99.5, (int, float), "health", result) is True
        assert len(result.errors) == 0

    def test_validate_field_type_number_union_failure(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        assert validate_field_type("100", (int, float), "health", result) is False
        assert len(result.errors) == 1

    def test_validate_constraints_min_max_success(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        validate_constraints(50, {"min": 0, "max": 100}, "health", result)
        assert len(result.errors) == 0

    def test_validate_constraints_below_min(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        validate_constraints(-10, {"min": 0, "max": 100}, "health", result)
        assert len(result.errors) == 1
        assert "below minimum" in result.errors[0].message

    def test_validate_constraints_above_max(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        validate_constraints(150, {"min": 0, "max": 100}, "health", result)
        assert len(result.errors) == 1
        assert "exceeds maximum" in result.errors[0].message


class TestCurveValidation:
    """Tests for animation curve validation."""

    def test_validate_curve_valid(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        curve = [[0, 1.0], [1, 0.8], [2, 0.5]]
        validate_curve(curve, "range_curve", result)
        assert len(result.errors) == 0

    def test_validate_curve_invalid_point_format(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        curve = [[0, 1.0], [1, 0.8, 0.5]]  # Too many values
        validate_curve(curve, "range_curve", result)
        assert len(result.errors) == 1

    def test_validate_curve_invalid_value_type(self):
        result = ValidationResult(file_path="test.yaml", config_type=None)
        curve = [[0, "one"], [1, 0.8]]
        validate_curve(curve, "range_curve", result)
        assert len(result.errors) == 1
        assert "must be numeric" in result.errors[0].message


class TestUnitArchetypeValidation:
    """Tests for UnitArchetype validation."""

    def test_valid_unit_archetype(self):
        data = {
            "type": "UnitArchetype",
            "id": "spearman",
            "base_health": 100,
            "base_move_speed": 5.0,
            "weapon": "bronze_spear",
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UNIT_ARCHETYPE)
        validate_config(data, ConfigType.UNIT_ARCHETYPE, result)
        assert result.is_valid

    def test_missing_required_field(self):
        data = {
            "type": "UnitArchetype",
            "id": "spearman",
            # Missing base_health and base_move_speed
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UNIT_ARCHETYPE)
        validate_config(data, ConfigType.UNIT_ARCHETYPE, result)
        assert not result.is_valid
        assert len(result.errors) >= 2

    def test_invalid_health_value(self):
        data = {
            "type": "UnitArchetype",
            "id": "spearman",
            "base_health": -100,  # Below minimum
            "base_move_speed": 5.0,
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UNIT_ARCHETYPE)
        validate_config(data, ConfigType.UNIT_ARCHETYPE, result)
        assert not result.is_valid
        assert any("below minimum" in e.message for e in result.errors)

    def test_unknown_field_warning(self):
        data = {
            "type": "UnitArchetype",
            "id": "spearman",
            "base_health": 100,
            "base_move_speed": 5.0,
            "unknown_field": "value",
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UNIT_ARCHETYPE)
        validate_config(data, ConfigType.UNIT_ARCHETYPE, result)
        assert result.is_valid  # Warnings don't fail validation
        assert len(result.warnings) == 1
        assert "Unknown field" in result.warnings[0].message


class TestWeaponStatsValidation:
    """Tests for WeaponStats validation."""

    def test_valid_weapon_stats(self):
        data = {
            "type": "WeaponStats",
            "name": "bronze_spear",
            "shots_per_burst": 1,
            "fire_rate": 1.0,
            "base_hit_chance": 0.8,
            "base_damage": 25,
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.WEAPON_STATS)
        validate_config(data, ConfigType.WEAPON_STATS, result)
        assert result.is_valid

    def test_valid_weapon_with_curves(self):
        data = {
            "type": "WeaponStats",
            "name": "rifle",
            "shots_per_burst": 3,
            "fire_rate": 5.0,
            "base_hit_chance": 0.7,
            "base_damage": 15,
            "range_curve": [[0, 1.0], [50, 0.9], [100, 0.5]],
            "elevation_curve": [[0, 1.0], [10, 1.2]],
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.WEAPON_STATS)
        validate_config(data, ConfigType.WEAPON_STATS, result)
        assert result.is_valid

    def test_invalid_hit_chance(self):
        data = {
            "type": "WeaponStats",
            "name": "sword",
            "shots_per_burst": 1,
            "fire_rate": 1.0,
            "base_hit_chance": 1.5,  # Above maximum (1)
            "base_damage": 30,
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.WEAPON_STATS)
        validate_config(data, ConfigType.WEAPON_STATS, result)
        assert not result.is_valid
        assert any("exceeds maximum" in e.message for e in result.errors)


class TestUpgradeDefinitionValidation:
    """Tests for UpgradeDefinition validation."""

    def test_valid_upgrade(self):
        data = {
            "type": "UpgradeDefinition",
            "name": "Veterans",
            "hit_chance_multiplier": 1.2,
            "damage_multiplier": 1.1,
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UPGRADE_DEFINITION)
        validate_config(data, ConfigType.UPGRADE_DEFINITION, result)
        assert result.is_valid

    def test_minimal_upgrade(self):
        data = {
            "type": "UpgradeDefinition",
            "name": "Basic Training",
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UPGRADE_DEFINITION)
        validate_config(data, ConfigType.UPGRADE_DEFINITION, result)
        assert result.is_valid

    def test_invalid_multiplier(self):
        data = {
            "type": "UpgradeDefinition",
            "name": "OP Buff",
            "damage_multiplier": 15.0,  # Above max (10)
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.UPGRADE_DEFINITION)
        validate_config(data, ConfigType.UPGRADE_DEFINITION, result)
        assert not result.is_valid


class TestEraConfigValidation:
    """Tests for EraConfig validation."""

    def test_valid_era_config(self):
        data = {
            "type": "EraConfig",
            "name": "Ancient",
            "archetypes": [
                {"id": "spearman", "base_health": 100, "base_move_speed": 5}
            ],
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.ERA_CONFIG)
        validate_config(data, ConfigType.ERA_CONFIG, result)
        assert result.is_valid

    def test_missing_archetypes(self):
        data = {
            "type": "EraConfig",
            "name": "Ancient",
        }
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.ERA_CONFIG)
        validate_config(data, ConfigType.ERA_CONFIG, result)
        assert not result.is_valid
        assert any("archetypes" in e.message for e in result.errors)


class TestFileLoading:
    """Tests for config file loading."""

    def test_load_json_file(self):
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False
        ) as tmp_file:
            json.dump({"name": "test"}, tmp_file)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            data, error = load_config_file(tmp_path)
            assert error is None
            assert data == {"name": "test"}
        finally:
            tmp_path.unlink()

    def test_load_invalid_json(self):
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False
        ) as tmp_file:
            tmp_file.write("{invalid json}")
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            data, error = load_config_file(tmp_path)
            assert data is None
            assert "JSON parsing error" in error
        finally:
            tmp_path.unlink()

    def test_load_unsupported_extension(self):
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".txt", delete=False
        ) as tmp_file:
            tmp_file.write("hello")
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            data, error = load_config_file(tmp_path)
            assert data is None
            assert "Unsupported file extension" in error
        finally:
            tmp_path.unlink()


class TestFileValidation:
    """Tests for complete file validation."""

    def test_validate_valid_file(self):
        data = {
            "type": "UnitArchetype",
            "id": "spearman",
            "base_health": 100,
            "base_move_speed": 5.0,
        }
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False
        ) as tmp_file:
            json.dump(data, tmp_file)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            result = validate_file(tmp_path)
            assert result.is_valid
            assert result.config_type == ConfigType.UNIT_ARCHETYPE
        finally:
            tmp_path.unlink()

    def test_validate_non_dict_file(self):
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False
        ) as tmp_file:
            json.dump(["list", "not", "dict"], tmp_file)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            result = validate_file(tmp_path)
            assert not result.is_valid
            assert any("Expected dictionary" in e.message for e in result.errors)
        finally:
            tmp_path.unlink()


class TestValidationResult:
    """Tests for ValidationResult class."""

    def test_is_valid_with_no_errors(self):
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.ERA_CONFIG)
        assert result.is_valid

    def test_is_valid_with_errors(self):
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.ERA_CONFIG)
        result.add_error("field", "error message")
        assert not result.is_valid

    def test_is_valid_with_warnings_only(self):
        result = ValidationResult(file_path="test.yaml", config_type=ConfigType.ERA_CONFIG)
        result.add_warning("field", "warning message")
        assert result.is_valid  # Warnings don't affect validity

    def test_validation_error_string(self):
        error = ValidationError("field.name", "invalid value", "error")
        assert "[ERROR] field.name: invalid value" == str(error)
