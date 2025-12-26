#!/usr/bin/env python3
"""
Relic Project Stats Tool

Generates statistics and metrics about the Relic Unity project.
Works without Unity installed by analyzing file structure and content.

Metrics provided:
- Code statistics (line counts, file counts by type)
- Asset breakdown (textures, models, prefabs, scenes)
- Config analysis (ScriptableObjects, settings)
- Documentation coverage
- Test coverage estimation

Usage:
    python project_stats.py summary
    python project_stats.py code
    python project_stats.py assets
    python project_stats.py docs
    python project_stats.py --json

Example:
    python project_stats.py summary
    python project_stats.py code --verbose
    python project_stats.py assets --by-era
"""

import argparse
import json
import re
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


# File extension categories
EXTENSION_CATEGORIES = {
    "code": [".cs", ".py", ".sh"],
    "config": [".yaml", ".yml", ".json", ".xml", ".asset"],
    "scene": [".unity"],
    "prefab": [".prefab"],
    "material": [".mat"],
    "shader": [".shader", ".shadergraph", ".hlsl"],
    "texture": [".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr"],
    "model": [".fbx", ".obj", ".blend", ".dae"],
    "audio": [".wav", ".mp3", ".ogg", ".aiff"],
    "animation": [".anim", ".controller"],
    "docs": [".md", ".txt", ".rst"],
}


@dataclass
class FileStats:
    """Statistics for a single file."""
    path: Path
    lines: int = 0
    code_lines: int = 0
    comment_lines: int = 0
    blank_lines: int = 0
    size_bytes: int = 0


@dataclass
class CategoryStats:
    """Aggregated statistics for a file category."""
    category: str
    file_count: int = 0
    total_lines: int = 0
    code_lines: int = 0
    comment_lines: int = 0
    blank_lines: int = 0
    total_size_bytes: int = 0
    files: list[FileStats] = field(default_factory=list)


@dataclass
class ProjectStats:
    """Complete project statistics."""
    root_path: Path
    categories: dict[str, CategoryStats] = field(default_factory=dict)
    era_breakdown: dict[str, dict[str, int]] = field(default_factory=dict)
    directory_structure: dict[str, int] = field(default_factory=dict)

    @property
    def total_files(self) -> int:
        return sum(cat.file_count for cat in self.categories.values())

    @property
    def total_lines(self) -> int:
        return sum(cat.total_lines for cat in self.categories.values())

    @property
    def total_size_bytes(self) -> int:
        return sum(cat.total_size_bytes for cat in self.categories.values())

    def to_dict(self) -> dict:
        """Convert to dictionary for JSON output."""
        return {
            "root_path": str(self.root_path),
            "summary": {
                "total_files": self.total_files,
                "total_lines": self.total_lines,
                "total_size_mb": round(self.total_size_bytes / (1024 * 1024), 2),
            },
            "categories": {
                name: {
                    "file_count": cat.file_count,
                    "total_lines": cat.total_lines,
                    "code_lines": cat.code_lines,
                    "total_size_bytes": cat.total_size_bytes,
                }
                for name, cat in self.categories.items()
            },
            "era_breakdown": self.era_breakdown,
            "directory_structure": self.directory_structure,
        }


def get_category(file_path: Path) -> str:
    """Determine the category of a file based on its extension."""
    suffix = file_path.suffix.lower()
    for category, extensions in EXTENSION_CATEGORIES.items():
        if suffix in extensions:
            return category
    return "other"


def count_lines(file_path: Path) -> tuple[int, int, int, int]:
    """
    Count lines in a file.
    Returns (total, code, comments, blank).
    """
    try:
        content = file_path.read_text(encoding="utf-8", errors="ignore")
    except (OSError, UnicodeDecodeError):
        return 0, 0, 0, 0

    lines = content.splitlines()
    total = len(lines)
    blank = sum(1 for line in lines if not line.strip())
    comment = 0
    code = 0

    # Detect language for comment patterns
    suffix = file_path.suffix.lower()
    in_multiline_comment = False

    for line in lines:
        stripped = line.strip()
        if not stripped:
            continue

        if suffix == ".cs":
            # C# comments
            if in_multiline_comment:
                comment += 1
                if "*/" in stripped:
                    in_multiline_comment = False
            elif stripped.startswith("/*"):
                comment += 1
                if "*/" not in stripped:
                    in_multiline_comment = True
            elif stripped.startswith("//"):
                comment += 1
            else:
                code += 1

        elif suffix in (".py", ".sh"):
            # Python/Shell comments
            if stripped.startswith("#"):
                comment += 1
            elif stripped.startswith('"""') or stripped.startswith("'''"):
                # Docstrings - count as comments
                comment += 1
            else:
                code += 1

        elif suffix in (".yaml", ".yml"):
            # YAML comments
            if stripped.startswith("#"):
                comment += 1
            else:
                code += 1

        elif suffix == ".md":
            # Markdown - all is "code" (content)
            code += 1

        else:
            # Default: everything non-blank is code
            code += 1

    return total, code, comment, blank


def detect_era_from_path(file_path: Path) -> Optional[str]:
    """Detect the era from file path components."""
    era_names = ["ancient", "medieval", "wwii", "future"]
    path_str = str(file_path).lower()

    for era in era_names:
        if era in path_str:
            return era.capitalize()

    return None


def collect_stats(root_path: Path) -> ProjectStats:
    """Collect statistics for the entire project."""
    stats = ProjectStats(root_path=root_path)

    # Initialize categories
    for category in list(EXTENSION_CATEGORIES.keys()) + ["other"]:
        stats.categories[category] = CategoryStats(category=category)

    # Initialize era breakdown
    for era in ["Ancient", "Medieval", "WWII", "Future"]:
        stats.era_breakdown[era] = defaultdict(int)

    # Skip directories
    skip_dirs = {".git", "node_modules", "__pycache__", ".pytest_cache", "Library", "Temp", "Logs", "obj"}

    for file_path in root_path.rglob("*"):
        # Skip non-files
        if not file_path.is_file():
            continue

        # Skip hidden files and directories in skip list
        parts = file_path.parts
        if any(part.startswith(".") for part in parts):
            if ".git" not in parts:  # Allow other hidden files
                continue
        if any(skip_dir in parts for skip_dir in skip_dirs):
            continue

        category = get_category(file_path)
        cat_stats = stats.categories[category]

        # Get file size
        try:
            size = file_path.stat().st_size
        except OSError:
            size = 0

        # Count lines for text files
        total, code, comments, blank = 0, 0, 0, 0
        if category in ("code", "config", "docs"):
            total, code, comments, blank = count_lines(file_path)

        # Create file stats
        file_stats = FileStats(
            path=file_path,
            lines=total,
            code_lines=code,
            comment_lines=comments,
            blank_lines=blank,
            size_bytes=size,
        )

        # Update category stats
        cat_stats.file_count += 1
        cat_stats.total_lines += total
        cat_stats.code_lines += code
        cat_stats.comment_lines += comments
        cat_stats.blank_lines += blank
        cat_stats.total_size_bytes += size
        cat_stats.files.append(file_stats)

        # Update era breakdown
        era = detect_era_from_path(file_path)
        if era:
            stats.era_breakdown[era][category] += 1

        # Update directory structure
        if len(file_path.parts) > len(root_path.parts):
            relative_parts = file_path.relative_to(root_path).parts
            if len(relative_parts) >= 1:
                top_dir = relative_parts[0]
                if top_dir not in skip_dirs:
                    stats.directory_structure[top_dir] = stats.directory_structure.get(top_dir, 0) + 1

    return stats


def print_summary(stats: ProjectStats) -> None:
    """Print a summary of project statistics."""
    print("\n" + "=" * 50)
    print("RELIC PROJECT STATISTICS")
    print("=" * 50)

    print(f"\nRoot: {stats.root_path}")
    print(f"Total Files: {stats.total_files}")
    print(f"Total Lines: {stats.total_lines:,}")
    print(f"Total Size: {stats.total_size_bytes / (1024 * 1024):.2f} MB")

    print("\nFiles by Category:")
    print("-" * 40)
    for name, cat in sorted(stats.categories.items(), key=lambda x: -x[1].file_count):
        if cat.file_count > 0:
            print(f"  {name:15} {cat.file_count:5} files  ({cat.total_lines:,} lines)")


def print_code_stats(stats: ProjectStats, verbose: bool = False) -> None:
    """Print code statistics."""
    print("\n" + "=" * 50)
    print("CODE STATISTICS")
    print("=" * 50)

    code_cat = stats.categories.get("code", CategoryStats("code"))
    print(f"\nTotal Code Files: {code_cat.file_count}")
    print(f"Total Lines: {code_cat.total_lines:,}")
    print(f"  - Code Lines: {code_cat.code_lines:,}")
    print(f"  - Comment Lines: {code_cat.comment_lines:,}")
    print(f"  - Blank Lines: {code_cat.blank_lines:,}")

    if code_cat.code_lines > 0:
        comment_ratio = (code_cat.comment_lines / code_cat.code_lines) * 100
        print(f"\nComment Ratio: {comment_ratio:.1f}%")

    # Group by file extension
    ext_counts: dict[str, int] = defaultdict(int)
    ext_lines: dict[str, int] = defaultdict(int)
    for file_stat in code_cat.files:
        ext = file_stat.path.suffix.lower()
        ext_counts[ext] += 1
        ext_lines[ext] += file_stat.lines

    if ext_counts:
        print("\nBy Extension:")
        print("-" * 40)
        for ext in sorted(ext_counts.keys()):
            print(f"  {ext:10} {ext_counts[ext]:5} files  ({ext_lines[ext]:,} lines)")

    if verbose and code_cat.files:
        print("\nLargest Files:")
        print("-" * 40)
        sorted_files = sorted(code_cat.files, key=lambda x: -x.lines)[:10]
        for file_stat in sorted_files:
            rel_path = file_stat.path.relative_to(stats.root_path)
            print(f"  {file_stat.lines:6} lines  {rel_path}")


def print_asset_stats(stats: ProjectStats, by_era: bool = False) -> None:
    """Print asset statistics."""
    print("\n" + "=" * 50)
    print("ASSET STATISTICS")
    print("=" * 50)

    asset_categories = ["scene", "prefab", "material", "texture", "model", "audio", "animation", "shader"]

    print("\nAsset Summary:")
    print("-" * 40)
    for category in asset_categories:
        cat = stats.categories.get(category, CategoryStats(category))
        if cat.file_count > 0:
            size_kb = cat.total_size_bytes / 1024
            print(f"  {category:12} {cat.file_count:5} files  ({size_kb:.1f} KB)")

    if by_era:
        print("\nAssets by Era:")
        print("-" * 40)
        for era, era_stats in sorted(stats.era_breakdown.items()):
            if any(era_stats.values()):
                print(f"\n  {era}:")
                for category in asset_categories:
                    count = era_stats.get(category, 0)
                    if count > 0:
                        print(f"    {category:12} {count} files")


def print_docs_stats(stats: ProjectStats) -> None:
    """Print documentation statistics."""
    print("\n" + "=" * 50)
    print("DOCUMENTATION STATISTICS")
    print("=" * 50)

    docs_cat = stats.categories.get("docs", CategoryStats("docs"))
    print(f"\nTotal Doc Files: {docs_cat.file_count}")
    print(f"Total Lines: {docs_cat.total_lines:,}")

    if docs_cat.files:
        print("\nDocument Files:")
        print("-" * 40)
        for file_stat in sorted(docs_cat.files, key=lambda x: -x.lines):
            rel_path = file_stat.path.relative_to(stats.root_path)
            print(f"  {file_stat.lines:6} lines  {rel_path}")

    # Documentation coverage check
    expected_docs = [
        "README.md",
        "DEVELOPMENT.md",
        "IMPLEMENTATION_PLAN.md",
        "docs/milestones.md",
        "docs/ARCHITECTURE.md",
        "docs/CODING_STANDARDS.md",
        "docs/TESTING.md",
    ]

    print("\nDocumentation Coverage:")
    print("-" * 40)
    for doc in expected_docs:
        doc_path = stats.root_path / doc
        status = "✓" if doc_path.exists() else "✗"
        print(f"  {status} {doc}")


def print_directory_structure(stats: ProjectStats) -> None:
    """Print directory structure breakdown."""
    print("\n" + "=" * 50)
    print("DIRECTORY STRUCTURE")
    print("=" * 50)

    print("\nTop-Level Directories:")
    print("-" * 40)
    for dirname, count in sorted(stats.directory_structure.items(), key=lambda x: -x[1]):
        print(f"  {dirname:20} {count} files")


def main() -> int:
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Relic Project Stats Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )

    subparsers = parser.add_subparsers(dest="command", help="Commands")

    # Summary command
    subparsers.add_parser("summary", help="Show project summary")

    # Code command
    code_parser = subparsers.add_parser("code", help="Show code statistics")
    code_parser.add_argument("--verbose", "-v", action="store_true", help="Show detailed info")

    # Assets command
    assets_parser = subparsers.add_parser("assets", help="Show asset statistics")
    assets_parser.add_argument("--by-era", action="store_true", help="Break down by era")

    # Docs command
    subparsers.add_parser("docs", help="Show documentation statistics")

    # Structure command
    subparsers.add_parser("structure", help="Show directory structure")

    # Common arguments
    parser.add_argument("--path", "-p", type=Path, default=None, help="Project root path")
    parser.add_argument("--json", "-j", action="store_true", help="Output as JSON")

    args = parser.parse_args()

    # Determine project root
    if args.path:
        root_path = args.path
    else:
        # Default: assume we're in the tools directory
        root_path = Path(__file__).parent.parent
        if not (root_path / "README.md").exists():
            # Try current directory
            root_path = Path.cwd()

    if not root_path.exists():
        print(f"Error: Path not found: {root_path}")
        return 1

    # Collect statistics
    stats = collect_stats(root_path)

    # Handle JSON output
    if args.json:
        print(json.dumps(stats.to_dict(), indent=2))
        return 0

    # Handle commands
    if args.command == "code":
        print_code_stats(stats, verbose=args.verbose)
    elif args.command == "assets":
        print_asset_stats(stats, by_era=args.by_era)
    elif args.command == "docs":
        print_docs_stats(stats)
    elif args.command == "structure":
        print_directory_structure(stats)
    else:
        # Default: show summary
        print_summary(stats)

    return 0


if __name__ == "__main__":
    sys.exit(main())
