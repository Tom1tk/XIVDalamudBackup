using Dalamud.Interface.Components;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Logging;
using System.IO;

namespace XIVBackup;

internal class ConfigWindow : Window
{
    private XIVBackup p;
    private string newIgnoredFile = string.Empty;
    private bool isTestingConnection = false;
    private string connectionStatus = "";

    public ConfigWindow(XIVBackup p) : base("XIV Dalamud Backup configuration")
    {
        this.p = p;
    }

    void Settings()
    {
        ImGuiEx.LineCentered("restore", () =>
        {
            ImGuiEx.WithTextColor(ImGuiColors.DalamudOrange, delegate
            {
                if (ImGui.Button("Read how to restore a backup"))
                {
                    ShellStart("https://github.com/Tom1tk/XIVDalamudBackup/blob/master/README.md#restoring-a-backup");
                }
            });
        });
        ImGuiEx.Text(@"Custom backup path (by default: %localappdata%\XIVDalamudBackup):");
        ImGui.SetNextItemWidth(400f);
        ImGui.InputText("##PathToBkp", ref p.config.BackupPath, 100);
        ImGuiEx.Text(@"Custom temporary files path (by default: %temp%):");
        ImGui.SetNextItemWidth(400f);
        ImGui.InputText("##PathToTmp", ref p.config.TempPath, 100);
        ImGui.Checkbox("Automatically remove old backups", ref p.config.DeleteBackups);
        if (p.config.DeleteBackups)
        {
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Delete backups older than, days", ref p.config.DaysToKeep, 0.1f, 3, 730);
            if (p.config.DaysToKeep < 3) p.config.DaysToKeep = 3;
            if (p.config.DaysToKeep > 730) p.config.DaysToKeep = 730;
            ImGui.Checkbox("Delete to recycle bin, if available.", ref p.config.DeleteToRecycleBin);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Always keep at least this number of backup regardless of their date", ref p.config.BackupsToKeep, 0.1f, 10, 100000);
            if (p.config.BackupsToKeep < 0) p.config.BackupsToKeep = 0;
            ImGui.Separator();
        }
        ImGui.Checkbox("Include plugin configurations", ref p.config.BackupPluginConfigs);
        ImGui.Checkbox("Include ALL files inside FFXIV's data folder into backup", ref p.config.BackupAll);
        ImGuiEx.Text("  (otherwise only config files will be saved, screenshots, logs, etc will be skipped)");
        ImGui.Checkbox($"Exclude replays from backup", ref p.config.ExcludeReplays);
        ImGui.Checkbox("Use built-in zip method instead of 7-zip", ref p.config.UseDefaultZip);
        if (p.config.UseDefaultZip) ImGuiEx.Text(ImGuiColors.DalamudRed, "7-zip archives are taking up to 15 times less space!");
        ImGui.Checkbox("Do not restrict amount of resources 7-zip can use", ref p.config.NoThreadLimit);
        ImGui.SetNextItemWidth(100f);
        ImGui.SliderInt($"Minimal interval between backups, minutes", ref p.config.MinIntervalBetweenBackups, 0, 60);
        ImGuiComponents.HelpMarker("Backup will not be created if previous backup was created less than this amount of minutes. Note that only successfully completed backups will update interval.");
    }

    void Tools()
    {
        if (ImGui.Button("Open backup folder"))
        {
            ShellStart(p.GetBackupPath());
        }
        ImGuiEx.WithTextColor(ImGuiColors.DalamudOrange, delegate
        {
            if (ImGui.Button("Read how to restore a backup"))
            {
                ShellStart("https://github.com/Tom1tk/XIVDalamudBackup/blob/master/README.md#restoring-a-backup");
            }
        });
        if (ImGui.Button("Open FFXIV configuration folder"))
        {
            ShellStart(p.GetFFXIVConfigFolder());
        }
        if (ImGui.Button("Open plugins configuration folder"))
        {
            ShellStart(XIVBackup.GetPluginsConfigDir().FullName);
        }
        if (Svc.ClientState.LocalPlayer != null)
        {
            if (ImGui.Button("Open current character's config directory"))
            {
                ShellStart(Path.Combine(p.GetFFXIVConfigFolder(), $"FFXIV_CHR{Svc.ClientState.LocalContentId:X16}"));
            }
            if (ImGui.Button("Add identification info"))
            {
                Safe(() =>
                {
                    var fname = Path.Combine(p.GetFFXIVConfigFolder(), $"FFXIV_CHR{Svc.ClientState.LocalContentId:X16}",
                        $"_{Player.NameWithWorld}.dat");
                    File.Create(fname).Dispose();
                    Notify.Success("Added identification info for current character");
                }, (e) =>
                {
                    Notify.Error("Error while adding identification info for current character:\n" + e);
                });
            }
            ImGuiEx.Tooltip("Adds an empty file into character's config directory\n" +
                "containing character's name and home world");
        }
    }


    void Ignored()
    {
        var id = 0;
        foreach (var file in p.config.Ignore.ToArray())
        {
            if (ImGui.SmallButton($"x##{id++}"))
            {
                p.config.Ignore.Remove(file);
            }
            ImGui.SameLine();
            ImGui.Text(file);
        }

        if (ImGui.SmallButton("+"))
        {
            if (!p.config.Ignore.Contains(newIgnoredFile, StringComparer.InvariantCultureIgnoreCase))
            {
                p.config.Ignore.Add(newIgnoredFile);
                newIgnoredFile = string.Empty;
            }
        }
        ImGui.SameLine();

        ImGui.InputText("Ignored (partial) Path", ref newIgnoredFile, 512);
    }

    void Expert()
    {
        ImGuiEx.Text($"Override game configuration folder path:");
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText($"##pathGame", ref p.config.OverrideGamePath, 2000);
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("Maximum threads", ref p.config.MaxThreads.ValidateRange(1, 99), 1, 99);
        ImGui.SetNextItemWidth(100f);
        ImGui.SliderInt($"Throttle copying, ms", ref p.config.CopyThrottle.ValidateRange(0, 50), 0, 5);
        ImGuiComponents.HelpMarker("The higher this value, the longer backup creation will take but the less loaded your SSD/HDD will be. Increase this value if you're experiencing lag during backup process.");
    }

    void GitHubSync()
    {
        if (!p.config.EnableGitHubSync)
        {
            // Setup Section
            ImGuiEx.TextWrapped("GitHub sync allows you to automatically backup your game and plugin configurations to a private GitHub repository. You can restore from any previous backup.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGuiEx.TextWrapped("To use GitHub sync, you need:");
            ImGui.Indent();
            ImGui.Text("1. A GitHub account");
            ImGui.Text("2. A Personal Access Token (PAT)");
            ImGui.Unindent();
            ImGui.Spacing();

            if (ImGui.Button("How to create a Personal Access Token"))
            {
                ShellStart("https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token");
            }
            ImGuiComponents.HelpMarker("You'll need:
• Fine-grained token
• Repository permissions: Contents (Read & Write)
• Only for your backup repository");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("GitHub Username:");
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText("##GitHubUsername", ref p.config.GitHubUsername, 100);

            ImGui.Text("Repository Name:");
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText("##GitHubRepoName", ref p.config.GitHubRepoName, 100);

            ImGui.Text("Personal Access Token:");
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText("##GitHubToken", ref p.config.GitHubToken, 200, ImGuiInputTextFlags.Password);

            ImGui.Spacing();

            if (!string.IsNullOrEmpty(connectionStatus))
            {
                var color = connectionStatus.Contains("Success") ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
                ImGuiEx.Text(color, connectionStatus);
                ImGui.Spacing();
            }

            var canInitialize = !string.IsNullOrEmpty(p.config.GitHubUsername) &&
                              !string.IsNullOrEmpty(p.config.GitHubRepoName) &&
                              !string.IsNullOrEmpty(p.config.GitHubToken);

            if (!canInitialize)
            {
                ImGui.BeginDisabled();
            }

            if (isTestingConnection)
            {
                ImGui.Text("Testing connection...");
            }
            else
            {
                if (ImGui.Button("Test Connection"))
                {
                    _ = Task.Run(async () =>
                    {
                        isTestingConnection = true;
                        var success = await p.gitService.ValidateTokenAsync(p.config.GitHubToken);
                        connectionStatus = success ? "✓ Connection successful!" : "✗ Connection failed. Check your token and username.";
                        isTestingConnection = false;
                    });
                }
                ImGui.SameLine();

                if (ImGui.Button("Create \u0026 Initialize Repository"))
                {
                    _ = Task.Run(async () =>
                    {
                        var success = await p.gitService.InitializeRepositoryAsync(p.config.GitHubToken);
                        if (success)
                        {
                            p.config.EnableGitHubSync = true;
                            Svc.PluginInterface.SavePluginConfig(p.config);
                            Notify.Success("GitHub sync initialized successfully!");
                        }
                        else
                        {
                            Notify.Error("Failed to initialize GitHub sync");
                        }
                    });
                }
            }

            if (!canInitialize)
            {
                ImGui.EndDisabled();
            }
        }
        else
        {
            // Control Section (when configured)
            var repoUrl = $"https://github.com/{p.config.GitHubUsername}/{p.config.GitHubRepoName}";
            ImGuiEx.Text(ImGuiColors.HealerGreen, $"✓ Connected to: {p.config.GitHubUsername}/{p.config.GitHubRepoName}");

            if (p.config.LastSuccessfulBackup > 0)
            {
                var lastBackup = DateTimeOffset.FromUnixTimeMilliseconds(p.config.LastSuccessfulBackup);
                var timeSince = DateTimeOffset.Now - lastBackup;
                string timeStr;
                if (timeSince.TotalMinutes < 60)
                    timeStr = $"{(int)timeSince.TotalMinutes} minutes ago";
                else if (timeSince.TotalHours < 24)
                    timeStr = $"{(int)timeSince.TotalHours} hours ago";
                else
                    timeStr = $"{(int)timeSince.TotalDays} days ago";

                ImGui.Text($"Last local backup: {timeStr}");
            }

            // Show repository size if available
            _ = Task.Run(async () =>
            {
                var repoSize = await p.gitService.GetRepositorySizeAsync();
                var sizeMB = repoSize / 1024.0 / 1024.0;
                var maxSizeGB = p.config.MaxRepoSizeBytes / 1024.0 / 1024.0 / 1024.0;
                ImGui.Text($"Repository size: {sizeMB:F1} MB / {maxSizeGB:F1} GB");

                if (p.config.WarnOnLargeRepo && repoSize > p.config.MaxRepoSizeBytes)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "⚠ Repository size exceeds limit!");
                }
            });

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Checkbox("Auto-sync on game startup", ref p.config.AutoSyncOnStartup);
            ImGuiComponents.HelpMarker("Automatically push a backup to GitHub when the game starts");

            ImGui.Checkbox("Warn when repo size > 4GB", ref p.config.WarnOnLargeRepo);
            ImGuiComponents.HelpMarker("Show warning when approaching GitHub's 5GB repository limit");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Push Backup Now"))
            {
                _ = Task.Run(async () => await p.SyncToGitHubAsync());
            }
            ImGuiComponents.HelpMarker("Manually push current configurations to GitHub");

            ImGui.SameLine();
            if (ImGui.Button("Restore from GitHub..."))
            {
                p.restoreWindow.IsOpen = true;
            }
            ImGuiComponents.HelpMarker("Open restore window to select and restore from a previous backup");

            ImGui.SameLine();
            if (ImGui.Button("Open Repository in Browser"))
            {
                ShellStart(repoUrl);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGuiEx.Text(ImGuiColors.DalamudOrange, "Advanced");
            if (ImGui.Button("Disconnect GitHub"))
            {
                p.config.EnableGitHubSync = false;
                p.config.GitHubToken = "";
                Svc.PluginInterface.SavePluginConfig(p.config);
                Notify.Info("GitHub sync disconnected");
            }
            ImGuiComponents.HelpMarker("Disable GitHub sync and clear stored credentials");
        }
    }

    public override void Draw()
    {
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("default", PatreonBanner.Text,
            ("Settings", Settings, null, true),
            ("Tools", Tools, null, true),
            ("GitHub Sync", GitHubSync, null, true),
            ("Ignored pathes (beta)", Ignored, null, true),
            ("Expert options", Expert, null, true),
            InternalLog.ImGuiTab()
            );
                   
    }

    public override void OnClose()
    {
        Svc.PluginInterface.SavePluginConfig(p.config);
        Notify.Success("Configuration saved");
        base.OnClose();
    }
}
