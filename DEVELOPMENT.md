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

### Current Status
**Unity is NOT yet installed on Colby.** Setup requires user action (see below).

### Requirements
- Unity 6.3 LTS (Unity 6000.3.0f1) - latest LTS as of Dec 2025
- Unity Hub for installation management
- Ubuntu 22.04 or 24.04 (Colby has Ubuntu 24.04)
- AR Foundation package
- XR Interaction Toolkit package
- Android build support (for Quest 3)

### Installation Steps (User Action Required)

1. **Download Unity Hub:**
   ```bash
   # Download the AppImage from https://unity.com/download
   wget -O ~/UnityHub.AppImage "https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage"
   chmod +x ~/UnityHub.AppImage
   ~/UnityHub.AppImage
   ```

2. **Login to Unity Hub:**
   - Launch Unity Hub
   - Sign in with Unity account (or create one - Personal is free for <$200k revenue)

3. **Install Unity 6.3 LTS:**
   - In Unity Hub, go to Installs
   - Click "Install Editor"
   - Select Unity 6000.3.0f1 (LTS)
   - Add these modules:
     - Android Build Support
     - Android SDK & NDK Tools
     - OpenJDK

4. **Verify Installation:**
   ```bash
   # Check Unity is accessible
   ls ~/Unity/Hub/Editor/*/Editor/Unity
   ```

### After Installation

Once Unity is installed, the agent team can:
1. Open the Relic project
2. Verify it loads without errors
3. Run existing tests (if any)
4. Begin RELIC-1 implementation batch

## Project Documentation

- `milestones.md` - Kyle's roadmap and requirements
- `DEVELOPMENT.md` - This file (dev setup)

---
*Setup completed by Agent-Dorian, 2025-12-26*
