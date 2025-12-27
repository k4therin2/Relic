# Relic

A tabletop augmented reality real-time strategy (RTS) sandbox for Meta Quest 3.

## Overview

Relic is an AR RTS game where players command armies on their tabletop, using the Quest 3's mixed reality capabilities to place battlefields on real surfaces. The game features:

- **AR Battlefield Placement** - Tap to place your battlefield on any flat surface
- **Multiple Eras** - Ancient, Medieval, WWII, and Future unit sets
- **RTS Core Mechanics** - Selection, movement, and attack commands
- **Combat System** - Per-bullet hit probability with range and elevation bonuses
- **Squad Upgrades** - Enhance your units with accuracy and damage modifiers
- **Large Battles** - Support for up to ~100 vs 100 unit engagements

## Quick Start

### Prerequisites

- Unity 6.3 LTS (6000.3.0f1) or later
- Unity Hub
- Meta Quest 3 (for AR testing)
- Android Build Support module
- Git

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/k4therin2/Relic.git
   cd Relic
   ```

2. **Install Unity (if not installed):**
   - Download [Unity Hub](https://unity.com/download)
   - Install Unity 6.3 LTS with:
     - Android Build Support
     - Android SDK & NDK Tools
     - OpenJDK

3. **Open the project:**
   - Launch Unity Hub
   - Click "Open" and select the Relic folder
   - Wait for initial import (may take a few minutes)

4. **Choose your development mode:**
   - **AR Mode:** Build to Quest 3 for full AR experience
   - **Debug Mode:** Run in editor with `Flat_Debug.unity` scene

### Running the Game

#### In Editor (Debug Mode)

1. Open `Assets/Scenes/Flat_Debug.unity`
2. Press Play
3. Use mouse to select and command units

#### On Quest 3 (AR Mode)

1. Connect Quest 3 via USB or Air Link
2. Go to `File > Build Settings`
3. Switch platform to Android
4. Build and Run

## Project Structure

```
Relic/
├── Assets/
│   ├── Scripts/
│   │   ├── CoreRTS/      # Platform-independent RTS logic
│   │   ├── ARLayer/      # AR-specific features
│   │   └── UILayer/      # UI components
│   ├── Configs/          # ScriptableObject configurations
│   ├── Art/              # Models, textures, materials
│   ├── Scenes/           # Unity scenes
│   └── Prefabs/          # Reusable prefabs
├── docs/
│   ├── milestones.md     # Project roadmap (source of truth)
│   ├── ARCHITECTURE.md   # System architecture and diagrams
│   ├── CODING_STANDARDS.md # C#/Unity conventions
│   └── TESTING.md        # Test strategy
├── IMPLEMENTATION_PLAN.md # Agent team implementation plan
├── DEVELOPMENT.md        # Development setup guide
└── README.md             # This file
```

## Documentation

| Document | Purpose |
|----------|---------|
| [milestones.md](docs/milestones.md) | Kyle's project roadmap and requirements |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture with diagrams |
| [CODING_STANDARDS.md](docs/CODING_STANDARDS.md) | C# and Unity coding conventions |
| [TESTING.md](docs/TESTING.md) | Testing strategy and guidelines |
| [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) | Agent team work packages |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Development environment setup |

## Controls

### Debug Mode (Mouse/Keyboard)

| Input | Action |
|-------|--------|
| Left Click | Select unit |
| Drag | Multi-select units |
| Right Click (ground) | Move command |
| Right Click (enemy) | Attack command |

### AR Mode (Quest 3)

| Input | Action |
|-------|--------|
| Controller Ray | Aim/hover |
| Trigger | Select |
| Grip | Issue command |
| Tap surface | Place battlefield |

## Development

### For Contributors

See [DEVELOPMENT.md](DEVELOPMENT.md) for:
- Repository setup and sync workflow
- Unity installation guide
- Branch and PR conventions

See [CODING_STANDARDS.md](docs/CODING_STANDARDS.md) for:
- Naming conventions
- Code organization
- Unity best practices

### Running Tests

```bash
# In Unity Editor
# Window > General > Test Runner
# Click "Run All"
```

### Building for Quest 3

1. Enable Developer Mode on Quest 3
2. Connect via USB
3. `File > Build Settings > Build and Run`

## Milestones

| Milestone | Description | Status |
|-----------|-------------|--------|
| M1 | Project and AR Foundation | Not Started |
| M2 | Core RTS Skeleton | Not Started |
| M3 | Combat and Upgrades | Not Started |
| M4 | Performance Optimization | Not Started |
| M5 | Scenarios and Polish | Not Started |

See [milestones.md](docs/milestones.md) for detailed requirements.

## Technology Stack

- **Engine:** Unity 6.3 LTS
- **Rendering:** Universal Render Pipeline (URP)
- **AR:** AR Foundation + Meta XR SDK
- **Input:** XR Interaction Toolkit
- **Platform:** Meta Quest 3, Android
- **Navigation:** Unity NavMesh

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow [CODING_STANDARDS.md](docs/CODING_STANDARDS.md)
4. Write tests for new features
5. Submit a Pull Request
6. Notify in #relic-game Slack channel

## Communication

- **Slack:** #relic-game
- **Upstream:** [SomewhatRogue/Relic](https://github.com/SomewhatRogue/Relic)
- **Fork:** [k4therin2/Relic](https://github.com/k4therin2/Relic)

## License

See LICENSE file for details.

---

*Maintained by the Agent Team*
*Original design by Kyle (SomewhatRogue)*
