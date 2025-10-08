using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CrocMadame;

public class SendHandler
{
    private readonly System.Windows.Controls.TextBox _codeTextBox;
    private readonly System.Windows.Controls.RadioButton _fileRadio;
    private readonly System.Windows.Controls.RadioButton _directoryRadio;
    private readonly System.Windows.Controls.TextBox _pathTextBox;
    private readonly System.Windows.Controls.Button _browseButton;
    private readonly System.Windows.Controls.Button _sendButton;
    private readonly System.Windows.Controls.Button _cancelButton;
    private readonly System.Windows.Controls.ProgressBar _progressBar;
    private readonly System.Windows.Controls.TextBox _outputTextBox;
    private readonly System.Windows.Controls.TextBox _relayTextBox;
    private readonly CrocProcessManager _processManager;

    public SendHandler(System.Windows.Controls.TextBox codeTextBox, System.Windows.Controls.RadioButton fileRadio, System.Windows.Controls.RadioButton directoryRadio,
                       System.Windows.Controls.TextBox pathTextBox, System.Windows.Controls.Button browseButton, System.Windows.Controls.Button sendButton, System.Windows.Controls.Button cancelButton,
                       System.Windows.Controls.ProgressBar progressBar, System.Windows.Controls.TextBox outputTextBox, System.Windows.Controls.TextBox relayTextBox, CrocProcessManager processManager)
    {
        _codeTextBox = codeTextBox;
        _fileRadio = fileRadio;
        _directoryRadio = directoryRadio;
        _pathTextBox = pathTextBox;
        _browseButton = browseButton;
        _sendButton = sendButton;
        _cancelButton = cancelButton;
        _progressBar = progressBar;
        _outputTextBox = outputTextBox;
        _relayTextBox = relayTextBox;
        _processManager = processManager;
    }

    public void BrowsePath()
    {
        if (_fileRadio.IsChecked == true)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Select file to send";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;

            if (dialog.ShowDialog() == true)
            {
                _pathTextBox.Text = dialog.FileName;
                UpdateButtonState();
            }
        }
        else // Directory
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select directory to send";
            dialog.ShowNewFolderButton = false;
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _pathTextBox.Text = dialog.SelectedPath;
                UpdateButtonState();
            }
        }
    }

    public void OnTypeRadioChecked()
    {
        // Clear the path when switching between file/directory
        _pathTextBox.Text = "";
        UpdateButtonState();
    }

    public void UpdateButtonState()
    {
        bool isValid = !string.IsNullOrWhiteSpace(_pathTextBox.Text) &&
                       (File.Exists(_pathTextBox.Text) || Directory.Exists(_pathTextBox.Text));
        _sendButton.IsEnabled = isValid;
    }

    public async Task StartSend()
    {
        string path = _pathTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(path))
        {
            System.Windows.MessageBox.Show("Please select a file or directory to send.",
                            "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            System.Windows.MessageBox.Show("The selected file or directory does not exist.",
                            "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await StartSendProcess(path);
    }

    private async Task StartSendProcess(string path)
    {
        SetControlsEnabled(false);
        ClearOutput();
        ResetProgress();
        _codeTextBox.Text = ""; // Clear previous code

        string relayInput = _relayTextBox.Text.Trim();

        string globalArgs = "";
        if (!string.IsNullOrEmpty(relayInput))
        {
            globalArgs += $" --relay {relayInput}";
        }

        string commandArgs = $"send \"{path}\"";

        string arguments = globalArgs + " " + commandArgs;

        _outputTextBox.AppendText($"Starting croc send for: {path}" + Environment.NewLine);
        _outputTextBox.AppendText($"Command: croc{globalArgs} {commandArgs}" + Environment.NewLine);
        _outputTextBox.AppendText(Environment.NewLine);

        string extractedCode = "";
        Action<string> processOutput = (string text) =>
        {
            // Try to extract code from "Code is: <code>" line
            if (string.IsNullOrWhiteSpace(extractedCode))
            {
                var match = Regex.Match(text.Trim(), @"^Code is:\s*([^\s]+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    extractedCode = match.Groups[1].Value;
                    _codeTextBox.Dispatcher.Invoke(() =>
                    {
                        _codeTextBox.Text = extractedCode;
                    });
                }
            }
            AppendOutput(text);
        };

        int exitCode = await _processManager.StartProcess(arguments, processOutput, UpdateProgress);

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
        _fileRadio.IsEnabled = enabled;
        _directoryRadio.IsEnabled = enabled;
        _pathTextBox.IsEnabled = enabled;
        _browseButton.IsEnabled = enabled;
        _sendButton.IsEnabled = enabled && !string.IsNullOrWhiteSpace(_pathTextBox.Text) &&
                               (File.Exists(_pathTextBox.Text) || Directory.Exists(_pathTextBox.Text));
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