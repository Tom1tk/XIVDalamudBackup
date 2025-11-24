using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using ECommons.Logging;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Octokit;
using Credentials = LibGit2Sharp.Credentials;
using Repository = LibGit2Sharp.Repository;
using Signature = LibGit2Sharp.Signature;

namespace XIVBackup;

/// <summary>
/// Git and GitHub service for managing backups
/// </summary>
internal class GitService
{
    private readonly GitHubClient _githubClient;
    private string _localRepoPath = "";
    private string _gitHubToken = "";

    public GitService()
    {
        _githubClient = new GitHubClient(new ProductHeaderValue("XIVDalamudBackup"));
    }

    /// <summary>
    /// Get the local repository path, creating directory if needed
    /// </summary>
    private string GetLocalRepoPath()
    {
        if (!string.IsNullOrEmpty(P.config.GitLocalRepoPath))
        {
            _localRepoPath = P.config.GitLocalRepoPath;
        }
        else
        {
            _localRepoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XIVDalamudBackup", "git-repo");
            P.config.GitLocalRepoPath = _localRepoPath;
        }

        if (!Directory.Exists(_localRepoPath))
        {
            Directory.CreateDirectory(_localRepoPath);
        }

        return _localRepoPath;
    }

    /// <summary>
    /// Validate GitHub Personal Access Token
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("XIVDalamudBackup"))
            {
                Credentials = new Octokit.Credentials(token)
            };

            var user = await client.User.Current();
            P.config.GitHubUsername = user.Login;
            _gitHubToken = token;
            PluginLog.Information($"Authenticated as GitHub user: {user.Login}");
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Error($"GitHub token validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create a private GitHub repository
    /// </summary>
    public async Task<bool> CreatePrivateRepositoryAsync(string repoName, string token)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("XIVDalamudBackup"))
            {
                Credentials = new Octokit.Credentials(token)
            };

            var newRepo = new NewRepository(repoName)
            {
                Private = true,
                Description = "FFXIV Dalamud configuration backup",
                AutoInit = true // Initialize with README
            };

            var repo = await client.Repository.Create(newRepo);
            PluginLog.Information($"Created private repository: {repo.FullName}");
            return true;
        }
        catch (RepositoryExistsException)
        {
            PluginLog.Information($"Repository {repoName} already exists");
            return true; // Not an error if it already exists
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to create repository: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Initialize local Git repository and set up remote
    /// </summary>
    public async Task<bool> InitializeRepositoryAsync(string token)
    {
        try
        {
            _gitHubToken = token;
            var localPath = GetLocalRepoPath();
            var repoUrl = $"https://github.com/{P.config.GitHubUsername}/{P.config.GitHubRepoName}.git";

            // Check if repository already exists
            if (Repository.IsValid(localPath))
            {
                PluginLog.Information("Local repository already initialized");
                return true;
            }

            // Create repository on GitHub first
            await CreatePrivateRepositoryAsync(P.config.GitHubRepoName, token);

            // Clone the repository
            PluginLog.Information($"Cloning repository from {repoUrl}");
            
            var cloneOptions = new CloneOptions
            {
                CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                {
                    Username = token,
                    Password = string.Empty
                }
            };

            Repository.Clone(repoUrl, localPath, cloneOptions);
            PluginLog.Information("Repository cloned successfully");

            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to initialize repository: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Check if repository is initialized
    /// </summary>
    public bool IsRepositoryInitialized()
    {
        var localPath = GetLocalRepoPath();
        return Repository.IsValid(localPath);
    }

    /// <summary>
    /// Copy game and plugin files to the git repository
    /// </summary>
    public async Task CopyGameFilesToRepo()
    {
        await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                var ffxivCfg = P.GetFFXIVConfigFolder();

                // Create directory structure
                var gameDir = Path.Combine(repoPath, "game");
                var pluginsDir = Path.Combine(repoPath, "plugins");

                if (Directory.Exists(gameDir))
                    Directory.Delete(gameDir, true);
                if (Directory.Exists(pluginsDir))
                    Directory.Delete(pluginsDir, true);

                Directory.CreateDirectory(gameDir);
                Directory.CreateDirectory(pluginsDir);

                // Copy game configs
                CopyDirectory(ffxivCfg, gameDir, true);

                // Copy plugin configs if enabled
                if (P.config.BackupPluginConfigs)
                {
                    try
                    {
                        var pluginsConfigDir = XIVBackup.GetPluginsConfigDir();
                        CopyDirectory(pluginsConfigDir.FullName, pluginsDir, true);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Error copying plugin configs: {ex.Message}");
                    }
                }

                // Copy Dalamud config files
                try
                {
                    var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");
                    var dalamudConfig = Path.Combine(xivlauncherDir, "dalamudConfig.json");
                    var dalamudUI = Path.Combine(xivlauncherDir, "dalamudUI.ini");

                    if (File.Exists(dalamudConfig))
                        File.Copy(dalamudConfig, Path.Combine(repoPath, "dalamudConfig.json"), true);
                    if (File.Exists(dalamudUI))
                        File.Copy(dalamudUI, Path.Combine(repoPath, "dalamudUI.ini"), true);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error copying Dalamud configs: {ex.Message}");
                }

                PluginLog.Information("Files copied to git repository");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error copying files to repo: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        });
    }

    /// <summary>
    /// Helper method to copy directories recursively
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            return;

        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in dir.GetFiles())
        {
            // Skip our own plugin config to avoid exposing token
            if (file.FullName.Contains("XIVDalamudBackup", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip based on user's ignore list
            if (P.config.Ignore.Any(f => file.FullName.Contains(f, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            try
            {
                var targetPath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetPath, true);
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Failed to copy {file.FullName}: {ex.Message}");
            }
        }

        // Copy subdirectories
        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                // Skip our own plugin folder
                if (subDir.Name.Contains("XIVDalamudBackup", StringComparison.OrdinalIgnoreCase))
                    continue;

                var newDestDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, true);
            }
        }
    }

    /// <summary>
    /// Commit changes and push to GitHub
    /// </summary>
    public async Task<bool> CommitAndPushAsync(string message)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                using var repo = new Repository(repoPath);

                // Stage all changes
                Commands.Stage(repo, "*");

                // Check if there are changes to commit
                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                {
                    PluginLog.Information("No changes to commit");
                    return true; // Not an error, just nothing to do
                }

                // Create signature
                var author = new Signature(P.config.GitHubUsername, $"{P.config.GitHubUsername}@users.noreply.github.com", DateTimeOffset.Now);

                // Commit
                var commit = repo.Commit(message, author, author);
                PluginLog.Information($"Created commit: {commit.Sha}");

                // Push
                var remote = repo.Network.Remotes["origin"];
                var options = new PushOptions
                {
                    CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        Username = _gitHubToken,
                        Password = string.Empty
                    }
                };

                repo.Network.Push(remote, @"refs/heads/main", options);
                PluginLog.Information("Pushed to GitHub successfully");

                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to commit and push: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        });
    }

    /// <summary>
    /// Get commit history from repository
    /// </summary>
    public async Task<List<CommitInfo>> GetCommitHistoryAsync(int maxCount = 50)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                using var repo = new Repository(repoPath);

                // Fetch latest from remote
                try
                {
                    var remote = repo.Network.Remotes["origin"];
                    var fetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                        {
                            Username = _gitHubToken,
                            Password = string.Empty
                        }
                    };
                    Commands.Fetch(repo, remote.Name, new string[0], fetchOptions, "");
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Failed to fetch from remote: {ex.Message}");
                }

                var commits = new List<CommitInfo>();
                foreach (var commit in repo.Commits.Take(maxCount))
                {
                    commits.Add(new CommitInfo
                    {
                        Sha = commit.Sha,
                        Message = commit.MessageShort,
                        Author = commit.Author.Name,
                        Date = commit.Author.When.DateTime
                    });
                }

                return commits;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to get commit history: {ex.Message}");
                return new List<CommitInfo>();
            }
        });
    }

    /// <summary>
    /// Checkout a specific commit
    /// </summary>
    public async Task<bool> CheckoutCommitAsync(string commitSha)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                using var repo = new Repository(repoPath);

                var commit = repo.Lookup<Commit>(commitSha);
                if (commit == null)
                {
                    PluginLog.Error($"Commit {commitSha} not found");
                    return false;
                }

                Commands.Checkout(repo, commit);
                PluginLog.Information($"Checked out commit: {commitSha}");
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to checkout commit: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        });
    }

    /// <summary>
    /// Restore files from repository to game directories
    /// </summary>
    public async Task RestoreFilesFromRepo()
    {
        await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                var ffxivCfg = P.GetFFXIVConfigFolder();

                // Restore game configs
                var gameDir = Path.Combine(repoPath, "game");
                if (Directory.Exists(gameDir))
                {
                    CopyDirectory(gameDir, ffxivCfg, true);
                }

                // Restore plugin configs
                if (P.config.BackupPluginConfigs)
                {
                    var pluginsDir = Path.Combine(repoPath, "plugins");
                    if (Directory.Exists(pluginsDir))
                    {
                        var pluginsConfigDir = XIVBackup.GetPluginsConfigDir();
                        CopyDirectory(pluginsDir, pluginsConfigDir.FullName, true);
                    }
                }

                // Restore Dalamud configs
                try
                {
                    var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

                    var dalamudConfig = Path.Combine(repoPath, "dalamudConfig.json");
                    if (File.Exists(dalamudConfig))
                    {
                        File.Copy(dalamudConfig, Path.Combine(xivlauncherDir, "dalamudConfig.json"), true);
                    }

                    var dalamudUI = Path.Combine(repoPath, "dalamudUI.ini");
                    if (File.Exists(dalamudUI))
                    {
                        File.Copy(dalamudUI, Path.Combine(xivlauncherDir, "dalamudUI.ini"), true);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error restoring Dalamud configs: {ex.Message}");
                }

                PluginLog.Information("Files restored from repository");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error restoring files: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        });
    }

    /// <summary>
    /// Get current commit SHA
    /// </summary>
    public async Task<string> GetCurrentCommitShaAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                using var repo = new Repository(repoPath);
                return repo.Head.Tip?.Sha ?? "";
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to get current commit SHA: {ex.Message}");
                return "";
            }
        });
    }

    /// <summary>
    /// Get repository size in bytes
    /// </summary>
    public async Task<long> GetRepositorySizeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetLocalRepoPath();
                var dirInfo = new DirectoryInfo(repoPath);
                return GetDirectorySize(dirInfo);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to get repository size: {ex.Message}");
                return 0;
            }
        });
    }

    private long GetDirectorySize(DirectoryInfo dir)
    {
        long size = 0;
        
        foreach (var file in dir.GetFiles())
        {
            size += file.Length;
        }

        foreach (var subDir in dir.GetDirectories())
        {
            size += GetDirectorySize(subDir);
        }

        return size;
    }
}

/// <summary>
/// Commit information for UI display
/// </summary>
internal class CommitInfo
{
    public string Sha { get; set; } = "";
    public string Message { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime Date { get; set; }
}
