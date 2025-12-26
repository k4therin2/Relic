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

See WP-EXT-1.3 for Unity environment requirements. Project uses:
- Unity LTS with URP
- AR Foundation + XR Interaction Toolkit
- Target: Meta Quest 3

## Project Documentation

- `milestones.md` - Kyle's roadmap and requirements
- `DEVELOPMENT.md` - This file (dev setup)

---
*Setup completed by Agent-Dorian, 2025-12-26*
