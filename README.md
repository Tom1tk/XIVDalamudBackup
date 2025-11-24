# XIV Dalamud Backup

Dalamud plugin that automatically backs up your FFXIV game and plugin configurations to a **private GitHub repository** with full version history. Restore from any previous backup with one click.

> **Note**: This plugin is a fork of [JustBackup](https://github.com/NightmareXIV/JustBackup) by NightmareXIV, extended with GitHub integration for cloud-based backup and multi-machine sync.

## Features

- **Automatic Local Backups** - Creates backups when you start the game
- **GitHub Sync** - Automatically pushes backups to your private GitHub repository
- **Version History** - Every backup is a Git commit, allowing you to restore from any point in time
- **Multi-Machine Sync** - Access your configurations from any PC
- **One-Click Restore** - Browse backup history and restore with a single click
- **Selective Backup** - Choose what to backup (game config, plugins, Dalamud settings)

## What Gets Backed Up

- **Game Configuration**: HUD layouts, keybinds, character settings, gearsets
- **Plugin Configurations**: All your Dalamud plugin settings
- **Dalamud Settings**: ImGui styles, third-party repos, developer settings

## Setup

### Prerequisites

1. A GitHub account
2. A Personal Access Token (PAT) from GitHub

### Creating a GitHub Personal Access Token

1. Go to GitHub Settings → Developer Settings → Personal Access Tokens → [Fine-grained tokens](https://github.com/settings/tokens?type=beta)
2. Click "Generate new token"
3. Configure the token:
   - **Token name**: `XIV Dalamud Backup`
   - **Expiration**: Choose your preference (recommend: 1 year or No expiration)
   - **Repository access**: Choose "Only select repositories"
   - Click "Create a new repository" or select an existing private repository
   - **Permissions**: 
     - Repository → Contents: **Read and write**
4. Generate and copy your token (save it somewhere safe!)

### Plugin Configuration

1. Install **XIV Dalamud Backup** from the Dalamud plugin installer
2. Open the plugin settings (`/xlplugins` → XIV Dalamud Backup)
3. Navigate to the **"GitHub Sync"** tab
4. Fill in your details:
   - **GitHub Username**: Your GitHub username
   - **Repository Name**: Name for your backup repo (e.g., `ffxiv-dalamud-backup`)
   - **Personal Access Token**: Paste the token you created
5. Click **"Test Connection"** to verify
6. Click **"Create & Initialize Repository"** to set up your backup repo
7. Enable **"Auto-sync on game startup"** if desired

## Usage

### Automatic Backups

Once configured, the plugin will:
- Create a local backup when you start the game
- Push the backup to GitHub automatically (if auto-sync is enabled)
- Keep your configurations safe and versioned

### Manual Backup

- Click **"Push Backup Now"** in the GitHub Sync tab
- Or use the command: `/xivbackup-push`

### Restoring from GitHub

1. Open plugin settings → **GitHub Sync** tab
2. Click **"Restore from GitHub..."**
3. Select the backup you want to restore (shows date and time)
4. Click **"Confirm Restore"**
5. **Restart FFXIV** to apply the restored settings

> ⚠️ **Warning**: Restoring will overwrite your current configuration files. Make sure you have a recent backup before restoring.

## Commands

- `/justbackup` - Create a manual local backup
- `/xivbackup-push` - Push current backup to GitHub
- `/xivbackup-restore` - Open the restore window

## Local Backups

Local backups are still created and stored in `%localappdata%\XIVDalamudBackup` by default. These can be used as an offline fallback if GitHub is unavailable.

### Manual Restore from Local Backup

If you need to restore from a local backup file:

1. Close FFXIV completely
2. Navigate to `%localappdata%\XIVDalamudBackup`
3. Find the backup you want (filename shows date/time)
4. Extract the `.7z` archive
5. Copy the contents to their respective locations:
   - **game/** folder → `%userprofile%\Documents\My Games\FINAL FANTASY XIV - A Realm Reborn`
   - **plugins/** folder → `%appdata%\XIVLauncher\pluginConfigs`
   - **dalamudConfig.json** → `%appdata%\XIVLauncher\`
   - **dalamudUI.ini** → `%appdata%\XIVLauncher\`
6. Restart FFXIV

## Configuration Options

### Settings Tab
- Backup path customization
- Automatic cleanup of old backups
- Include/exclude replays
- Backup frequency limits

### GitHub Sync Tab
- GitHub authentication
- Repository setup
- Auto-sync toggle
- Manual sync controls
- Restore interface
- Repository size warnings

### Tools Tab
- Open backup folder
- Open game config folder
- Open plugins config folder
- Add character identification

## FAQ

**Q: Is my data safe?**  
A: Your GitHub repository is **private by default**. Only you can access it. The Personal Access Token is stored locally on your machine.

**Q: Can I use this on multiple PCs?**  
A: Yes! Set up the plugin on each PC with the same GitHub repository. All machines will sync to the same backup.

**Q: What if I hit GitHub's repository size limit?**  
A: The plugin will warn you when approaching 4GB. Plugin configs are typically small (~50-200MB), so this shouldn't be an issue for most users.

**Q: Do I need to keep local backups if I'm using GitHub?**  
A: Local backups provide an offline fallback. The plugin creates both by default, which is recommended.

**Q: What happens if GitHub is down?**  
A: The plugin will continue creating local backups. GitHub sync will resume when connectivity is restored.

## Troubleshooting

### "Connection failed" error
- Verify your GitHub token is correct
- Ensure the token has "Contents: Read and Write" permissions
- Check your internet connection

### "Repository not initialized" warning
- Make sure you clicked "Create & Initialize Repository"
- Check that the repository was created on GitHub
- Try disconnecting and reconnecting GitHub sync

### Restore doesn't work
- Make sure you've restarted FFXIV after restoring
- Check that the repository has commits (backups)
- Verify you have write permissions to game/plugin folders

## Credits

- **Original Plugin**: [JustBackup](https://github.com/NightmareXIV/JustBackup) by [NightmareXIV](https://github.com/NightmareXIV)
- **GitHub Integration**: [Tom1tk](https://github.com/Tom1tk)

## License

This project inherits the GPL-3.0 license from JustBackup.

## Support

For issues or questions:
- Open an issue on [GitHub](https://github.com/Tom1tk/XIVDalamudBackup/issues)
- Join the discussion in the issues section
