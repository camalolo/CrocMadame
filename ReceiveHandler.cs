using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace CrocMadame;

public class ReceiveHandler
{
    private readonly System.Windows.Controls.TextBox _codeTextBox;
    private readonly System.Windows.Controls.TextBox _directoryTextBox;
    private readonly System.Windows.Controls.Button _browseButton;
    private readonly System.Windows.Controls.Button _downloadButton;
    private readonly System.Windows.Controls.Button _cancelButton;
    private readonly System.Windows.Controls.ProgressBar _progressBar;
    private readonly System.Windows.Controls.TextBox _outputTextBox;
    private readonly System.Windows.Controls.TextBox _relayTextBox;
    private readonly CrocProcessManager _processManager;

    public ReceiveHandler(System.Windows.Controls.TextBox codeTextBox, System.Windows.Controls.TextBox directoryTextBox, System.Windows.Controls.Button browseButton,
                          System.Windows.Controls.Button downloadButton, System.Windows.Controls.Button cancelButton, System.Windows.Controls.ProgressBar progressBar,
                          System.Windows.Controls.TextBox outputTextBox, System.Windows.Controls.TextBox relayTextBox, CrocProcessManager processManager)
    {
        _codeTextBox = codeTextBox;
        _directoryTextBox = directoryTextBox;
        _browseButton = browseButton;
        _downloadButton = downloadButton;
        _cancelButton = cancelButton;
        _progressBar = progressBar;
        _outputTextBox = outputTextBox;
        _relayTextBox = relayTextBox;
        _processManager = processManager;
    }

    public string GetDownloadsFolder()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
            {
                if (key != null)
                {
                    var value = key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}");
                    if (value != null)
                    {
                        string path = value.ToString()!;
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("%"))
                        {
                            path = Environment.ExpandEnvironmentVariables(path);
                        }
                        return path;
                    }
                }
            }
        }
        catch { }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public void BrowseDirectory()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "Select destination directory";
        dialog.ShowNewFolderButton = true;
        dialog.UseDescriptionForTitle = true;
        dialog.InitialDirectory = GetDownloadsFolder();

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _directoryTextBox.Text = dialog.SelectedPath;
            UpdateButtonState();
        }
    }

    public void UpdateButtonState()
    {
        bool isValid = !string.IsNullOrWhiteSpace(_codeTextBox.Text) &&
                       !string.IsNullOrWhiteSpace(_directoryTextBox.Text);
        _downloadButton.IsEnabled = isValid;
    }

    public async Task StartReceive()
    {
        string code = _codeTextBox.Text.Trim();
        string directory = _directoryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(directory))
        {
            System.Windows.MessageBox.Show("Please enter a secret code and select a destination directory.",
                            "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(directory))
        {
            System.Windows.MessageBox.Show("The selected directory does not exist.",
                            "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await StartReceiveProcess(code, directory);
    }

    private async Task StartReceiveProcess(string code, string directory)
    {
        SetControlsEnabled(false);
        ClearOutput();
        ResetProgress();

        string relayInput = _relayTextBox.Text.Trim();

        string globalArgs = "";
        if (!string.IsNullOrEmpty(relayInput))
        {
            globalArgs += $" --relay {relayInput}";
        }

        string commandArgs = $"--yes --overwrite --out \"{directory}\" {code}";

        string arguments = globalArgs + " " + commandArgs;

        _outputTextBox.AppendText($"Starting croc with output directory: {directory}" + Environment.NewLine);
        _outputTextBox.AppendText($"Command: croc{globalArgs} {commandArgs.Trim()}" + Environment.NewLine);
        _outputTextBox.AppendText(Environment.NewLine);

        int exitCode = await _processManager.StartProcess(arguments, AppendOutput, UpdateProgress);

        _outputTextBox.AppendText(Environment.NewLine);
        _outputTextBox.AppendText("Process completed." + Environment.NewLine);
        _outputTextBox.AppendText($"Exit code: {exitCode}" + Environment.NewLine);

        SetControlsEnabled(true);
    }

    public void Cancel()
    {
        _processManager.KillProcess();
        AppendOutput("Stopping croc process...");
        SetControlsEnabled(true);
    }

    private void SetControlsEnabled(bool enabled)
    {
        _codeTextBox.IsEnabled = enabled;
        _directoryTextBox.IsEnabled = enabled;
        _browseButton.IsEnabled = enabled;
        _downloadButton.IsEnabled = enabled && !string.IsNullOrWhiteSpace(_codeTextBox.Text) &&
                                                      !string.IsNullOrWhiteSpace(_directoryTextBox.Text);
        _cancelButton.IsEnabled = !enabled;
    }

    private void ClearOutput()
    {
        _outputTextBox.Text = "";
    }

    private void AppendOutput(string text)
    {
        _outputTextBox.Dispatcher.Invoke(() =>
        {
            _outputTextBox.AppendText(text + Environment.NewLine);
            _outputTextBox.ScrollToEnd();
        });
    }

    private void ResetProgress()
    {
        _progressBar.Dispatcher.Invoke(() =>
        {
            _progressBar.Value = 0;
        });
    }

    private void UpdateProgress(double progress)
    {
        _progressBar.Dispatcher.Invoke(() =>
        {
            _progressBar.Value = progress;
        });
    }
}