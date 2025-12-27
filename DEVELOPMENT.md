# Relic Development Setup

## Repository Structure

- **Fork**: `k4therin2/Relic` (this repo)
- **Upstream**: `SomewhatRogue/Relic` (Kyle's main repo)
- **Local Path**: `/home/k4therin2/projects/relic/`

## Syncing with Upstream

Before starting work, always sync with Kyle's latest:

```bash
cd /home/k4therin2/projects/relic
git fetch upstream
git checkout main
git merge upstream/main
git push origin main
```

## Workflow

1. **Sync** - Pull latest from upstream (Kyle's repo)
2. **Branch** - Create feature branch: `git checkout -b feature/WP-name`
3. **Implement** - Make changes, test locally
4. **Push** - Push to origin (fork): `git push origin feature/WP-name`
5. **PR** - Create pull request to `SomewhatRogue/Relic`
6. **Notify** - Ping Kyle in #relic-game Slack channel

## Communication

- **Slack Channel**: #relic-game
- **Client**: Kyle (SomewhatRogue)
- Post completion updates to #relic-game when PRs are ready

## Unity Setup

### Current Status (Updated 2025-12-27)

**INSTALLED:**
- ✅ Unity Hub AppImage: `~/UnityHub.AppImage`
- ✅ Unity 6000.3.2f1 (6.3 LTS): `~/Unity/Hub/Editor/6000.3.2f1/`
- ✅ Android Build Support
- ✅ Android SDK & NDK Tools
- ✅ OpenJDK

**BLOCKED:**
- ❌ Unity License needs activation (requires GUI interaction)

### License Activation (User Action Required)

Unity is installed but needs license activation. Since Colby is a headless server,
you need GUI access to activate:

**Option 1: SSH with X11 Forwarding (Recommended)**
```bash
# From your local machine with X11:
ssh -X k4therin2@colby
~/UnityHub.AppImage --no-sandbox
# Sign in with Unity account in the Hub GUI
```

**Option 2: VNC/Remote Desktop**
- Use VNC or RDP to get graphical access to Colby
- Run Unity Hub and sign in

**Option 3: Manual License File**
- Activate Unity on another machine with same Unity ID
- Copy `~/.config/unity3d/Unity/Unity_lic.ulf` to Colby

### After License Activation

Once licensed, verify with:
```bash
~/Unity/Hub/Editor/6000.3.2f1/Editor/Unity -batchmode -quit -nographics
# Should exit cleanly without "No valid Unity Editor license" error
```

Then the agent team can:
1. Create the initial Unity project (Kyle's upstream has only docs, no project yet)
2. Set up AR Foundation and XR Interaction Toolkit
3. Begin Milestone 1 implementation

### Project State

Note: Kyle's upstream repo (`SomewhatRogue/Relic`) currently only contains:
- `README.md` - Project overview
- `milestones.md` - Detailed roadmap and requirements

The actual Unity project (Assets/, ProjectSettings/, etc.) needs to be created
as part of Milestone 1.

## Project Documentation

- `milestones.md` - Kyle's roadmap and requirements
- `DEVELOPMENT.md` - This file (dev setup)

---
*Setup completed by Agent-Dorian, 2025-12-26*
