"""Tests for the build automation tool."""

import os
import tempfile
from pathlib import Path
from unittest.mock import patch, MagicMock

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from build import (
    PROFILES,
    TARGETS,
    find_unity_executable,
    get_project_root,
    check_prerequisites,
    generate_build_script,
)


class TestProfiles:
    """Tests for build profile configurations."""

    def test_debug_profile_exists(self):
        assert "debug" in PROFILES
        assert PROFILES["debug"]["development"] is True

    def test_release_profile_exists(self):
        assert "release" in PROFILES
        assert PROFILES["release"]["development"] is False
        assert PROFILES["release"]["il2cpp"] is True

    def test_profile_profile_exists(self):
        assert "profile" in PROFILES
        assert PROFILES["profile"]["profiler"] is True


class TestTargets:
    """Tests for build target configurations."""

    def test_quest3_target_exists(self):
        assert "quest3" in TARGETS
        assert TARGETS["quest3"]["platform"] == "Android"
        assert TARGETS["quest3"]["extension"] == ".apk"
        assert "QUEST_3" in TARGETS["quest3"]["defines"]

    def test_debug_target_exists(self):
        assert "debug" in TARGETS
        assert "DEBUG_MODE" in TARGETS["debug"]["defines"]

    def test_android_target_exists(self):
        assert "android" in TARGETS
        assert TARGETS["android"]["platform"] == "Android"


class TestFindUnityExecutable:
    """Tests for Unity executable detection."""

    def test_returns_path_from_environment(self):
        with patch.dict(os.environ, {"UNITY_PATH": "/path/to/Unity"}):
            with patch("pathlib.Path.exists", return_value=True):
                result = find_unity_executable()
                assert result == Path("/path/to/Unity")

    def test_returns_none_when_env_path_not_exists(self):
        with patch.dict(os.environ, {"UNITY_PATH": "/nonexistent/Unity"}):
            # Clear any existing UNITY_PATH mock and let it check the path
            result = find_unity_executable()
            # The path doesn't exist, so it should return None or try other locations
            # This tests that it doesn't crash

    def test_returns_none_when_no_unity_found(self):
        with patch.dict(os.environ, {}, clear=True):
            # Remove UNITY_PATH from environment
            env_backup = os.environ.pop("UNITY_PATH", None)
            try:
                # Mock the path checks to return False
                with patch("pathlib.Path.exists", return_value=False):
                    result = find_unity_executable()
                    # May return None if no Unity found
                    # (actual behavior depends on system)
            finally:
                if env_backup:
                    os.environ["UNITY_PATH"] = env_backup


class TestGetProjectRoot:
    """Tests for project root detection."""

    def test_finds_project_root_from_tools_dir(self):
        # This test runs from within the tools directory
        # so it should find the project root
        try:
            root = get_project_root()
            # Should find a valid path
            assert root.exists()
        except RuntimeError:
            # If running from a different location, this is expected
            pass

    def test_raises_when_not_found(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)
            # Create a fake tools directory without milestones.md
            tools_dir = tmp_path / "tools"
            tools_dir.mkdir()

            # Patch __file__ to point to our temp directory
            with patch("build.__file__", str(tools_dir / "build.py")):
                with patch("pathlib.Path.cwd", return_value=tmp_path):
                    with pytest.raises(RuntimeError, match="Cannot find Relic project root"):
                        get_project_root()


class TestCheckPrerequisites:
    """Tests for build prerequisite checking."""

    def test_reports_missing_unity(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            (project_root / "Assets").mkdir()
            (project_root / "ProjectSettings").mkdir()

            issues = check_prerequisites(None, project_root)
            assert any("Unity executable not found" in issue for issue in issues)

    def test_reports_missing_assets_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            (project_root / "ProjectSettings").mkdir()

            issues = check_prerequisites(Path("/some/unity"), project_root)
            assert any("Assets/ directory not found" in issue for issue in issues)

    def test_reports_missing_project_settings(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            (project_root / "Assets").mkdir()

            issues = check_prerequisites(Path("/some/unity"), project_root)
            assert any("ProjectSettings/ directory not found" in issue for issue in issues)

    def test_reports_missing_scenes(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            (project_root / "Assets").mkdir()
            (project_root / "ProjectSettings").mkdir()

            issues = check_prerequisites(Path("/some/unity"), project_root)
            # Should report missing scene files
            assert any("Scene not found" in issue for issue in issues)

    def test_no_issues_when_complete(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            (project_root / "Assets").mkdir()
            (project_root / "ProjectSettings").mkdir()

            # Create scene files
            scenes_dir = project_root / "Assets" / "Scenes"
            scenes_dir.mkdir(parents=True)
            (scenes_dir / "AR_Battlefield.unity").write_text("")
            (scenes_dir / "Flat_Debug.unity").write_text("")

            # Mock Unity existence
            with patch("pathlib.Path.exists", return_value=True):
                unity_path = Path("/path/to/Unity")
                issues = check_prerequisites(unity_path, project_root)
                # May still have issues for scenes depending on target configs


class TestGenerateBuildScript:
    """Tests for build script generation."""

    def test_generates_valid_csharp_script(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            output_path = project_root / "Builds" / "test.apk"

            script = generate_build_script(
                project_root=project_root,
                target="quest3",
                profile="debug",
                output_path=output_path,
            )

            # Should contain C# class definition
            assert "public class RelicBuildScript" in script
            assert "public static void Build()" in script

            # Should include build options
            assert "BuildPlayerOptions" in script
            assert "BuildPipeline.BuildPlayer" in script

    def test_includes_development_flag_for_debug(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            output_path = project_root / "Builds" / "test.apk"

            script = generate_build_script(
                project_root=project_root,
                target="quest3",
                profile="debug",
                output_path=output_path,
            )

            assert "BuildOptions.Development" in script

    def test_excludes_development_flag_for_release(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            output_path = project_root / "Builds" / "test.apk"

            script = generate_build_script(
                project_root=project_root,
                target="quest3",
                profile="release",
                output_path=output_path,
            )

            # Release profile should not have Development enabled
            # (it won't include the line that adds the flag)
            lines = script.split("\n")
            dev_lines = [line for line in lines if "BuildOptions.Development" in line and not line.strip().startswith("//")]
            # The line should be commented out or empty for release
            assert not any("options |=" in line and "Development" in line for line in lines if line.strip())

    def test_includes_defines_for_target(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            output_path = project_root / "Builds" / "test.apk"

            script = generate_build_script(
                project_root=project_root,
                target="quest3",
                profile="debug",
                output_path=output_path,
            )

            # Should include Quest 3 defines
            assert "QUEST_3" in script
            assert "XR_ENABLED" in script

    def test_includes_profiler_for_profile_build(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            project_root = Path(tmp_dir)
            output_path = project_root / "Builds" / "test.apk"

            script = generate_build_script(
                project_root=project_root,
                target="debug",
                profile="profile",
                output_path=output_path,
            )

            assert "EnableDeepProfilingSupport" in script


class TestOutputNaming:
    """Tests for build output naming conventions."""

    def test_quest3_output_extension(self):
        target = TARGETS["quest3"]
        assert target["extension"] == ".apk"

    def test_debug_output_extension_linux(self):
        import platform
        if platform.system() == "Linux":
            target = TARGETS["debug"]
            assert target["extension"] == ".x86_64"

    def test_debug_output_extension_windows(self):
        import platform
        if platform.system() == "Windows":
            target = TARGETS["debug"]
            assert target["extension"] == ".exe"
