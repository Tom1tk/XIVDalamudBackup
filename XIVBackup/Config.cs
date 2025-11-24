using Dalamud.Configuration;

namespace XIVBackup;

internal class Config : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string BackupPath = "";
    public string TempPath = "";
    public bool DeleteBackups = true;
    public bool DeleteToRecycleBin = true;
    public int DaysToKeep = 7;
    public bool BackupAll = false;
    public bool ExcludeReplays = false;
    public bool UseDefaultZip = false;
    public bool BackupPluginConfigs = true;
    public int BackupsToKeep = 10;
    public bool NoThreadLimit = false;
    public int MaxThreads = 99;
    public List<string> Ignore = new();
    public HashSet<string> TempPathes = new();
    public string OverrideGamePath = "";
    public int MinIntervalBetweenBackups = 0;
    public long LastSuccessfulBackup = 0;
    public int CopyThrottle = 0;

    // GitHub Settings
    public string GitHubToken = "";
    public string GitHubUsername = "";
    public string GitHubRepoName = "ffxiv-dalamud-backup";
    public string GitLocalRepoPath = ""; // Default: %LocalAppData%/XIVDalamudBackup/git-repo
    public bool EnableGitHubSync = false;
    public bool AutoSyncOnStartup = true;
    public bool WarnOnLargeRepo = true;
    public long MaxRepoSizeBytes = 4_000_000_000; // 4GB (below GitHub's 5GB limit)
    public string LastCommitSha = ""; // Track current sync state
}
