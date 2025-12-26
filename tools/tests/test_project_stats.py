"""Tests for the project_stats tool."""

import json
import tempfile
from pathlib import Path

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from project_stats import (
    FileStats,
    CategoryStats,
    ProjectStats,
    get_category,
    count_lines,
    detect_era_from_path,
    collect_stats,
)


class TestGetCategory:
    """Tests for file category detection."""

    def test_code_extensions(self):
        assert get_category(Path("test.cs")) == "code"
        assert get_category(Path("test.py")) == "code"
        assert get_category(Path("test.sh")) == "code"

    def test_config_extensions(self):
        assert get_category(Path("test.yaml")) == "config"
        assert get_category(Path("test.yml")) == "config"
        assert get_category(Path("test.json")) == "config"
        assert get_category(Path("test.asset")) == "config"

    def test_scene_extension(self):
        assert get_category(Path("test.unity")) == "scene"

    def test_prefab_extension(self):
        assert get_category(Path("test.prefab")) == "prefab"

    def test_texture_extensions(self):
        assert get_category(Path("test.png")) == "texture"
        assert get_category(Path("test.jpg")) == "texture"
        assert get_category(Path("test.tga")) == "texture"

    def test_model_extensions(self):
        assert get_category(Path("test.fbx")) == "model"
        assert get_category(Path("test.obj")) == "model"
        assert get_category(Path("test.blend")) == "model"

    def test_audio_extensions(self):
        assert get_category(Path("test.wav")) == "audio"
        assert get_category(Path("test.mp3")) == "audio"
        assert get_category(Path("test.ogg")) == "audio"

    def test_docs_extensions(self):
        assert get_category(Path("test.md")) == "docs"
        assert get_category(Path("test.txt")) == "docs"

    def test_unknown_extension(self):
        assert get_category(Path("test.xyz")) == "other"


class TestCountLines:
    """Tests for line counting."""

    def test_count_python_lines(self):
        content = """# Comment
def hello():
    print("hello")

# Another comment
"""
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".py", delete=False
        ) as tmp_file:
            tmp_file.write(content)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            total, code, comments, blank = count_lines(tmp_path)
            # Content has 5 non-empty lines + 1 blank line
            assert total >= 5
            assert blank >= 1  # At least 1 empty line
            assert comments >= 2  # At least the # comments
            assert code >= 2  # At least the def and print lines
        finally:
            tmp_path.unlink()

    def test_count_csharp_lines(self):
        content = """// Single line comment
public class Test {
    /* Multi-line
       comment */
    public void Method() { }
}
"""
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".cs", delete=False
        ) as tmp_file:
            tmp_file.write(content)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            total, code, comments, blank = count_lines(tmp_path)
            assert total >= 6
            assert comments >= 3  # Single + multi-line comments
            assert code >= 2  # Class, method, closing braces
        finally:
            tmp_path.unlink()

    def test_count_yaml_lines(self):
        content = """# YAML comment
name: test
value: 123

# Another comment
nested:
  key: value
"""
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".yaml", delete=False
        ) as tmp_file:
            tmp_file.write(content)
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            total, code, comments, blank = count_lines(tmp_path)
            assert total >= 7
            assert comments >= 2
            assert blank >= 1
        finally:
            tmp_path.unlink()

    def test_count_empty_file(self):
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".py", delete=False
        ) as tmp_file:
            tmp_file.write("")
            tmp_file.flush()
            tmp_path = Path(tmp_file.name)

        try:
            total, code, comments, blank = count_lines(tmp_path)
            assert total == 0
            assert code == 0
            assert comments == 0
            assert blank == 0
        finally:
            tmp_path.unlink()


class TestDetectEraFromPath:
    """Tests for era detection from file paths."""

    def test_detect_ancient(self):
        assert detect_era_from_path(Path("Assets/Art/Ancient/warrior.fbx")) == "Ancient"

    def test_detect_medieval(self):
        assert detect_era_from_path(Path("Assets/Art/Medieval/knight.fbx")) == "Medieval"

    def test_detect_wwii(self):
        assert detect_era_from_path(Path("Assets/Art/WWII/soldier.fbx")) == "Wwii"

    def test_detect_future(self):
        assert detect_era_from_path(Path("Assets/Art/Future/mech.fbx")) == "Future"

    def test_detect_case_insensitive(self):
        assert detect_era_from_path(Path("Assets/art/ancient/warrior.fbx")) == "Ancient"

    def test_no_era_detected(self):
        assert detect_era_from_path(Path("Assets/Shared/texture.png")) is None


class TestFileStats:
    """Tests for FileStats dataclass."""

    def test_file_stats_defaults(self):
        stats = FileStats(path=Path("test.cs"))
        assert stats.lines == 0
        assert stats.code_lines == 0
        assert stats.comment_lines == 0
        assert stats.blank_lines == 0
        assert stats.size_bytes == 0


class TestCategoryStats:
    """Tests for CategoryStats dataclass."""

    def test_category_stats_defaults(self):
        stats = CategoryStats(category="code")
        assert stats.file_count == 0
        assert stats.total_lines == 0
        assert stats.files == []


class TestProjectStats:
    """Tests for ProjectStats dataclass."""

    def test_total_files(self):
        stats = ProjectStats(root_path=Path("/test"))
        stats.categories["code"] = CategoryStats(category="code", file_count=10)
        stats.categories["docs"] = CategoryStats(category="docs", file_count=5)
        assert stats.total_files == 15

    def test_total_lines(self):
        stats = ProjectStats(root_path=Path("/test"))
        stats.categories["code"] = CategoryStats(category="code", total_lines=1000)
        stats.categories["docs"] = CategoryStats(category="docs", total_lines=500)
        assert stats.total_lines == 1500

    def test_total_size(self):
        stats = ProjectStats(root_path=Path("/test"))
        stats.categories["code"] = CategoryStats(category="code", total_size_bytes=1024)
        stats.categories["texture"] = CategoryStats(category="texture", total_size_bytes=2048)
        assert stats.total_size_bytes == 3072

    def test_to_dict(self):
        stats = ProjectStats(root_path=Path("/test"))
        stats.categories["code"] = CategoryStats(
            category="code",
            file_count=10,
            total_lines=1000,
            code_lines=800,
            total_size_bytes=5120,
        )

        result = stats.to_dict()
        assert result["root_path"] == "/test"
        assert result["summary"]["total_files"] == 10
        assert result["summary"]["total_lines"] == 1000
        assert result["categories"]["code"]["file_count"] == 10


class TestCollectStats:
    """Tests for collecting project statistics."""

    def test_collect_stats_from_temp_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)

            # Create some test files
            (tmp_path / "test.py").write_text("# Comment\nprint('hello')\n")
            (tmp_path / "README.md").write_text("# Title\n\nDescription\n")
            (tmp_path / "config.json").write_text('{"key": "value"}')

            stats = collect_stats(tmp_path)

            assert stats.total_files == 3
            assert stats.categories["code"].file_count == 1
            assert stats.categories["docs"].file_count == 1
            assert stats.categories["config"].file_count == 1

    def test_collect_stats_ignores_git_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)

            # Create .git directory with files
            git_dir = tmp_path / ".git"
            git_dir.mkdir()
            (git_dir / "config").write_text("[core]")

            # Create a regular file
            (tmp_path / "test.py").write_text("print('hello')")

            stats = collect_stats(tmp_path)

            # .git files should be ignored
            assert stats.total_files == 1

    def test_collect_stats_ignores_pycache(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)

            # Create __pycache__ directory with files
            cache_dir = tmp_path / "__pycache__"
            cache_dir.mkdir()
            (cache_dir / "test.cpython-310.pyc").write_bytes(b"\x00\x00")

            # Create a regular file
            (tmp_path / "test.py").write_text("print('hello')")

            stats = collect_stats(tmp_path)

            # __pycache__ files should be ignored
            assert stats.total_files == 1

    def test_collect_stats_directory_breakdown(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)

            # Create files in subdirectories
            (tmp_path / "src").mkdir()
            (tmp_path / "src" / "main.py").write_text("print('main')")
            (tmp_path / "src" / "utils.py").write_text("print('utils')")

            (tmp_path / "docs").mkdir()
            (tmp_path / "docs" / "README.md").write_text("# Docs")

            stats = collect_stats(tmp_path)

            assert "src" in stats.directory_structure
            assert stats.directory_structure["src"] == 2
            assert "docs" in stats.directory_structure
            assert stats.directory_structure["docs"] == 1

    def test_collect_stats_era_breakdown(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            tmp_path = Path(tmp_dir)

            # Create era-specific directories
            ancient_dir = tmp_path / "Art" / "Ancient"
            ancient_dir.mkdir(parents=True)
            (ancient_dir / "warrior.fbx").write_bytes(b"FBX")
            (ancient_dir / "texture.png").write_bytes(b"PNG")

            stats = collect_stats(tmp_path)

            assert "Ancient" in stats.era_breakdown
            assert stats.era_breakdown["Ancient"]["model"] == 1
            assert stats.era_breakdown["Ancient"]["texture"] == 1
