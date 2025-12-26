#!/usr/bin/env python3
"""
Relic Unity Build Automation

Command-line tool for building the Relic Unity project.
Requires Unity to be installed.

Features:
- Build for Quest 3 (Android APK)
- Build for Debug mode (standalone)
- Configure build options
- Pre-build validation
- Post-build deployment

Usage:
    python build.py quest3                    # Build for Quest 3
    python build.py debug                     # Build for debug/editor
    python build.py quest3 --profile release  # Release build
    python build.py check                     # Pre-build validation only

Environment Variables:
    UNITY_PATH     Path to Unity executable (auto-detected if not set)
    BUILD_PATH     Output directory for builds (default: Builds/)

Example:
    export UNITY_PATH="/path/to/Unity/Hub/Editor/6000.0.3f1/Editor/Unity"
    python build.py quest3 --install
"""

import argparse
import os
import platform
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Optional

# Build profiles
PROFILES = {
    "debug": {
        "development": True,
        "allow_debugging": True,
        "compression": "None",
        "il2cpp": False,
    },
    "release": {
        "development": False,
        "allow_debugging": False,
        "compression": "LZ4HC",
        "il2cpp": True,
    },
    "profile": {
        "development": True,
        "allow_debugging": False,
        "compression": "LZ4",
        "il2cpp": True,
        "profiler": True,
    },
}

# Target platforms
TARGETS = {
    "quest3": {
        "platform": "Android",
        "extension": ".apk",
        "scene": "Assets/Scenes/AR_Battlefield.unity",
        "defines": ["QUEST_3", "XR_ENABLED"],
    },
    "debug": {
        "platform": "StandaloneLinux64" if platform.system() == "Linux" else "StandaloneWindows64",
        "extension": ".x86_64" if platform.system() == "Linux" else ".exe",
        "scene": "Assets/Scenes/Flat_Debug.unity",
        "defines": ["DEBUG_MODE"],
    },
    "android": {
        "platform": "Android",
        "extension": ".apk",
        "scene": "Assets/Scenes/AR_Battlefield.unity",
        "defines": [],
    },
}


def find_unity_executable() -> Optional[Path]:
    """Auto-detect Unity executable path."""
    # Check environment variable first
    if "UNITY_PATH" in os.environ:
        path = Path(os.environ["UNITY_PATH"])
        if path.exists():
            return path

    # Platform-specific default locations
    system = platform.system()

    if system == "Linux":
        possible_paths = [
            Path.home() / "Unity/Hub/Editor",
            Path("/opt/unity"),
        ]
    elif system == "Darwin":  # macOS
        possible_paths = [
            Path("/Applications/Unity/Hub/Editor"),
            Path.home() / "Applications/Unity/Hub/Editor",
        ]
    elif system == "Windows":
        possible_paths = [
            Path("C:/Program Files/Unity/Hub/Editor"),
            Path.home() / "Unity/Hub/Editor",
        ]
    else:
        return None

    # Find Unity 6.x installation
    for base_path in possible_paths:
        if not base_path.exists():
            continue
        for version_dir in sorted(base_path.iterdir(), reverse=True):
            if version_dir.name.startswith("6000"):  # Unity 6
                if system == "Darwin":
                    unity_exe = version_dir / "Unity.app/Contents/MacOS/Unity"
                elif system == "Windows":
                    unity_exe = version_dir / "Editor/Unity.exe"
                else:
                    unity_exe = version_dir / "Editor/Unity"

                if unity_exe.exists():
                    return unity_exe

    return None


def get_project_root() -> Path:
    """Get the Relic project root directory."""
    # Assume we're in the tools directory
    tools_dir = Path(__file__).parent
    project_root = tools_dir.parent

    # Verify it's the right directory
    if (project_root / "docs/milestones.md").exists():
        return project_root

    # Try current directory
    cwd = Path.cwd()
    if (cwd / "docs/milestones.md").exists():
        return cwd

    raise RuntimeError("Cannot find Relic project root. Run from project directory.")


def check_prerequisites(unity_path: Optional[Path], project_root: Path) -> list[str]:
    """Check build prerequisites and return list of issues."""
    issues = []

    # Check Unity
    if unity_path is None:
        issues.append("Unity executable not found. Set UNITY_PATH environment variable.")
    elif not unity_path.exists():
        issues.append(f"Unity executable not found at: {unity_path}")

    # Check project structure
    if not (project_root / "Assets").exists():
        issues.append("Assets/ directory not found. Unity project may not be initialized.")

    if not (project_root / "ProjectSettings").exists():
        issues.append("ProjectSettings/ directory not found. Unity project may not be initialized.")

    # Check for required scenes
    for target_name, target_config in TARGETS.items():
        scene_path = project_root / target_config["scene"]
        if not scene_path.exists():
            issues.append(f"Scene not found for {target_name}: {target_config['scene']}")

    return issues


def generate_build_script(
    project_root: Path,
    target: str,
    profile: str,
    output_path: Path,
) -> str:
    """Generate C# build script for Unity's batchmode."""
    target_config = TARGETS[target]
    profile_config = PROFILES[profile]

    defines = target_config["defines"] + (["DEVELOPMENT"] if profile_config["development"] else [])
    defines_str = ";".join(defines)

    script = f'''
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;

public class RelicBuildScript
{{
    public static void Build()
    {{
        string[] scenes = new string[] {{ "{target_config["scene"]}" }};
        string outputPath = "{output_path.as_posix()}";

        BuildPlayerOptions buildOptions = new BuildPlayerOptions();
        buildOptions.scenes = scenes;
        buildOptions.locationPathName = outputPath;
        buildOptions.target = BuildTarget.{target_config["platform"]};

        BuildOptions options = BuildOptions.None;
        {"options |= BuildOptions.Development;" if profile_config["development"] else ""}
        {"options |= BuildOptions.AllowDebugging;" if profile_config.get("allow_debugging") else ""}
        {"options |= BuildOptions.EnableDeepProfilingSupport;" if profile_config.get("profiler") else ""}

        buildOptions.options = options;

        // Set scripting defines
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            BuildTargetGroup.{target_config["platform"].replace("Standalone", "Standalone")},
            "{defines_str}"
        );

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {{
            Console.WriteLine("Build succeeded: " + summary.totalSize + " bytes");
            EditorApplication.Exit(0);
        }}
        else
        {{
            Console.WriteLine("Build failed");
            EditorApplication.Exit(1);
        }}
    }}
}}
'''
    return script


def run_unity_build(
    unity_path: Path,
    project_root: Path,
    target: str,
    profile: str,
    output_dir: Path,
) -> int:
    """Run Unity build in batchmode."""
    target_config = TARGETS[target]

    # Generate output path
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    build_name = f"Relic_{target}_{profile}_{timestamp}{target_config['extension']}"
    output_path = output_dir / build_name

    # Create build directory
    output_dir.mkdir(parents=True, exist_ok=True)

    # Generate build script
    build_script = generate_build_script(project_root, target, profile, output_path)
    script_path = project_root / "Assets/Editor/RelicBuildScript.cs"
    script_path.parent.mkdir(parents=True, exist_ok=True)
    script_path.write_text(build_script)

    print(f"Building Relic for {target} ({profile} profile)...")
    print(f"Output: {output_path}")

    # Build Unity command
    cmd = [
        str(unity_path),
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", str(project_root),
        "-executeMethod", "RelicBuildScript.Build",
        "-logFile", str(output_dir / "build.log"),
    ]

    print(f"Running: {' '.join(cmd)}")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=1800)  # 30 min timeout
        print(f"\nUnity exit code: {result.returncode}")

        if result.returncode == 0:
            print(f"\n✓ Build successful: {output_path}")
            if output_path.exists():
                size_mb = output_path.stat().st_size / (1024 * 1024)
                print(f"  Size: {size_mb:.2f} MB")
            return 0
        else:
            print("\n✗ Build failed")
            print(f"See log: {output_dir / 'build.log'}")
            return 1

    except subprocess.TimeoutExpired:
        print("\n✗ Build timed out (30 minutes)")
        return 1
    except Exception as error:
        print(f"\n✗ Build error: {error}")
        return 1
    finally:
        # Cleanup build script
        if script_path.exists():
            script_path.unlink()


def install_apk(apk_path: Path) -> int:
    """Install APK to connected Quest device via ADB."""
    print(f"\nInstalling APK: {apk_path}")

    # Check ADB
    adb_path = shutil.which("adb")
    if not adb_path:
        print("Error: ADB not found in PATH")
        return 1

    # Check device
    result = subprocess.run([adb_path, "devices"], capture_output=True, text=True)
    if "device" not in result.stdout.split("\n")[1:]:
        print("Error: No ADB device connected")
        return 1

    # Install
    result = subprocess.run(
        [adb_path, "install", "-r", str(apk_path)],
        capture_output=True,
        text=True
    )

    if result.returncode == 0:
        print("✓ APK installed successfully")
        return 0
    else:
        print(f"✗ Install failed: {result.stderr}")
        return 1


def main() -> int:
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Relic Unity Build Automation",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )

    subparsers = parser.add_subparsers(dest="target", help="Build target")

    # Quest 3 build
    quest3_parser = subparsers.add_parser("quest3", help="Build for Quest 3")
    quest3_parser.add_argument("--profile", choices=PROFILES.keys(), default="debug")
    quest3_parser.add_argument("--install", action="store_true", help="Install to connected device")
    quest3_parser.add_argument("--output", type=Path, help="Output directory")

    # Debug build
    debug_parser = subparsers.add_parser("debug", help="Build for debug/standalone")
    debug_parser.add_argument("--profile", choices=PROFILES.keys(), default="debug")
    debug_parser.add_argument("--output", type=Path, help="Output directory")

    # Android build
    android_parser = subparsers.add_parser("android", help="Build generic Android APK")
    android_parser.add_argument("--profile", choices=PROFILES.keys(), default="debug")
    android_parser.add_argument("--install", action="store_true")
    android_parser.add_argument("--output", type=Path, help="Output directory")

    # Check command
    subparsers.add_parser("check", help="Check prerequisites only")

    args = parser.parse_args()

    if not args.target:
        parser.print_help()
        return 0

    try:
        project_root = get_project_root()
    except RuntimeError as error:
        print(f"Error: {error}")
        return 1

    unity_path = find_unity_executable()

    # Check prerequisites
    issues = check_prerequisites(unity_path, project_root)

    if args.target == "check":
        print("\n=== Build Prerequisites Check ===\n")
        print(f"Project Root: {project_root}")
        print(f"Unity Path: {unity_path or 'NOT FOUND'}")

        if issues:
            print("\nIssues found:")
            for issue in issues:
                print(f"  ✗ {issue}")
            return 1
        else:
            print("\n✓ All prerequisites met")
            return 0

    # For actual builds, fail on issues
    if issues:
        print("\nBuild prerequisites not met:")
        for issue in issues:
            print(f"  ✗ {issue}")
        return 1

    # Run build
    output_dir = args.output or (project_root / "Builds")
    result = run_unity_build(unity_path, project_root, args.target, args.profile, output_dir)

    # Install if requested
    if result == 0 and hasattr(args, "install") and args.install:
        # Find the most recent APK
        apks = list(output_dir.glob("*.apk"))
        if apks:
            latest_apk = max(apks, key=lambda p: p.stat().st_mtime)
            result = install_apk(latest_apk)

    return result


if __name__ == "__main__":
    sys.exit(main())
