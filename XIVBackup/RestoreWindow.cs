using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ECommons.Logging;
using ImGuiNET;

namespace XIVBackup;

internal class RestoreWindow : Window
{
    private List<CommitInfo> commits = new();
    private string selectedCommitSha = "";
    private bool isLoading = false;
    private bool showConfirmation = false;

    public RestoreWindow() : base("Restore from GitHub##XIVDalamudBackup")
    {
        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        LoadCommitsAsync();
    }

    private async void LoadCommitsAsync()
    {
        isLoading = true;
        try
        {
            commits = await P.gitService.GetCommitHistoryAsync();
            if (commits.Count > 0)
            {
                selectedCommitSha = commits[0].Sha; // Select latest by default
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to load commits: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    public override void Draw()
    {
        if (!P.config.EnableGitHubSync || !P.gitService.IsRepositoryInitialized())
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "GitHub sync is not configured.");
            ImGui.TextWrapped("Please configure GitHub settings in the Settings tab first.");
            
            if (ImGui.Button("Close"))
            {
                IsOpen = false;
            }
            return;
        }

        if (isLoading)
        {
            ImGui.Text("Loading backups from GitHub...");
            return;
        }

        if (commits.Count == 0)
        {
            ImGui.TextWrapped("No backups found in GitHub repository.");
            
            if (ImGui.Button("Refresh"))
            {
                LoadCommitsAsync();
            }
            ImGui.SameLine();
            if (ImGui.Button("Close"))
            {
                IsOpen = false;
            }
            return;
        }

        if (!showConfirmation)
        {
            DrawCommitSelection();
        }
        else
        {
            DrawConfirmation();
        }
    }

    private void DrawCommitSelection()
    {
        ImGui.TextWrapped("Select a backup to restore:");
        ImGui.Spacing();

        ImGui.BeginChild("CommitList", new Vector2(0, -40));
        
        foreach (var commit in commits)
        {
            var isSelected = selectedCommitSha == commit.Sha;
            
            if (ImGui.Selectable($"{commit.Date:yyyy-MM-dd HH:mm:ss}##{commit.Sha}", isSelected))
            {
                selectedCommitSha = commit.Sha;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Commit: {commit.Sha[..8]}");
                ImGui.Text($"Message: {commit.Message}");
                ImGui.Text($"Author: {commit.Author}");
                ImGui.EndTooltip();
            }

            // Show message indented
            ImGui.Indent();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), commit.Message);
            ImGui.Unindent();
            ImGui.Spacing();
        }

        ImGui.EndChild();

        ImGui.Separator();

        if (ImGui.Button("Refresh"))
        {
            LoadCommitsAsync();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            IsOpen = false;
        }
        ImGui.SameLine();
        
        if (string.IsNullOrEmpty(selectedCommitSha))
        {
            ImGui.BeginDisabled();
            ImGui.Button("Restore");
            ImGui.EndDisabled();
        }
        else
        {
            if (ImGui.Button("Restore"))
            {
                showConfirmation = true;
            }
        }
    }

    private void DrawConfirmation()
    {
        var selectedCommit = commits.FirstOrDefault(c => c.Sha == selectedCommitSha);
        
        ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "⚠ Warning");
        ImGui.Spacing();
        ImGui.TextWrapped("This will overwrite your current game and plugin configurations!");
        ImGui.Spacing();
        
        if (selectedCommit != null)
        {
            ImGui.Text($"Restoring backup from:");
            ImGui.Indent();
            ImGui.Text($"Date: {selectedCommit.Date:yyyy-MM-dd HH:mm:ss}");
            ImGui.Text($"Message: {selectedCommit.Message}");
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("You will need to restart Final Fantasy XIV after restoration completes.");
        ImGui.Spacing();

        if (ImGui.Button("Back"))
        {
            showConfirmation = false;
        }
        ImGui.SameLine();
        
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1));
        if (ImGui.Button("Confirm Restore"))
        {
            _ = Task.Run(async () =>
            {
                var success = await P.RestoreFromGitHubAsync(selectedCommitSha);
                if (success)
                {
                    // Close window on success
                    new ECommons.Schedulers.TickScheduler(() =>
                    {
                        IsOpen = false;
                        showConfirmation = false;
                    });
                }
            });
        }
        ImGui.PopStyleColor();
    }
}
