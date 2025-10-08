using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CrocMadame;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process? _crocProcess;
    private readonly object _outputLock = new object();

    private string GetDownloadsFolder()
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

        // Fallback to default
        return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();

        // Prefill the download destination with the user's Downloads folder
        ReceiveDirectoryTextBox.Text = GetDownloadsFolder();
        UpdateReceiveDownloadButtonState();
    }

    private void SetupEventHandlers()
    {
        MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

        // Receive tab handlers
        ReceiveBrowseButton.Click += ReceiveBrowseButton_Click;
        ReceiveDownloadButton.Click += ReceiveDownloadButton_Click;
        ReceiveCancelButton.Click += ReceiveCancelButton_Click;
        ReceiveCodeTextBox.TextChanged += ReceiveCodeTextBox_TextChanged;

        // Send tab handlers
        SendBrowseButton.Click += SendBrowseButton_Click;
        SendButton.Click += SendButton_Click;
        SendCancelButton.Click += SendCancelButton_Click;
        SendFileRadio.Checked += SendTypeRadio_Checked;
        SendDirectoryRadio.Checked += SendTypeRadio_Checked;
    }

    private void ReceiveCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateReceiveDownloadButtonState();
    }

    private void ReceiveBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "Select destination directory";
        dialog.ShowNewFolderButton = true;
        dialog.UseDescriptionForTitle = true;
        dialog.InitialDirectory = GetDownloadsFolder();

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ReceiveDirectoryTextBox.Text = dialog.SelectedPath;
            UpdateReceiveDownloadButtonState();
        }
    }

    private void UpdateReceiveDownloadButtonState()
    {
        bool isValid = !string.IsNullOrWhiteSpace(ReceiveCodeTextBox.Text) &&
                      !string.IsNullOrWhiteSpace(ReceiveDirectoryTextBox.Text);
        ReceiveDownloadButton.IsEnabled = isValid;
    }

    private async void ReceiveDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReceiveCodeTextBox.Text) ||
            string.IsNullOrWhiteSpace(ReceiveDirectoryTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a secret code and select a destination directory.",
                          "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string code = ReceiveCodeTextBox.Text.Trim();
        string directory = ReceiveDirectoryTextBox.Text.Trim();

        if (!Directory.Exists(directory))
        {
            System.Windows.MessageBox.Show("The selected directory does not exist.",
                          "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await StartReceiveCrocProcess(code, directory);
    }

    private async Task StartReceiveCrocProcess(string code, string directory)
    {
        try
        {
            // Disable controls during execution
            SetReceiveControlsEnabled(false);
            ClearReceiveOutput();
            ResetReceiveProgress();

            // Start the croc process
            _crocProcess = new Process();
            _crocProcess.StartInfo.FileName = "croc";
            _crocProcess.StartInfo.Arguments = $"--yes --overwrite --out \"{directory}\" {code}";
            _crocProcess.StartInfo.UseShellExecute = false;
            _crocProcess.StartInfo.RedirectStandardOutput = true;
            _crocProcess.StartInfo.RedirectStandardError = true;
            _crocProcess.StartInfo.CreateNoWindow = true;

            _crocProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (TryParseProgress(e.Data, out double progress))
                    {
                        UpdateReceiveProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        AppendReceiveOutput(e.Data);
                    }
                    // Skip empty/whitespace-only lines
                }
            };

            _crocProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (TryParseProgress(e.Data, out double progress))
                    {
                        UpdateReceiveProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        AppendReceiveOutput(e.Data);
                    }
                    // Skip empty/whitespace-only lines
                }
            };

            AppendReceiveOutput($"Starting croc with output directory: {directory}");
            AppendReceiveOutput($"Command: croc --yes --out \"{directory}\" {code}");
            AppendReceiveOutput("");

            _crocProcess.Start();
            _crocProcess.BeginOutputReadLine();
            _crocProcess.BeginErrorReadLine();

            // Wait for process to complete
            await Task.Run(() => _crocProcess.WaitForExit());

            AppendReceiveOutput("");
            AppendReceiveOutput("Process completed.");
            AppendReceiveOutput($"Exit code: {_crocProcess.ExitCode}");

            // Re-enable controls
            SetReceiveControlsEnabled(true);
        }
        catch (Exception ex)
        {
            AppendReceiveOutput($"Error: {ex.Message}");
            SetReceiveControlsEnabled(true);
        }
        finally
        {
            _crocProcess = null;
        }
    }

    private void ReceiveCancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_crocProcess != null && !_crocProcess.HasExited)
        {
            AppendReceiveOutput("Stopping croc process...");
            _crocProcess.Kill();
            _crocProcess = null;
            SetReceiveControlsEnabled(true);
        }
    }

    private void SetReceiveControlsEnabled(bool enabled)
    {
        ReceiveCodeTextBox.IsEnabled = enabled;
        ReceiveDirectoryTextBox.IsEnabled = enabled;
        ReceiveBrowseButton.IsEnabled = enabled;
        ReceiveDownloadButton.IsEnabled = enabled && !string.IsNullOrWhiteSpace(ReceiveCodeTextBox.Text) &&
                                  !string.IsNullOrWhiteSpace(ReceiveDirectoryTextBox.Text);
        ReceiveCancelButton.IsEnabled = !enabled;
    }

    private void ClearReceiveOutput()
    {
        ReceiveOutputTextBox.Text = "";
    }

    private void AppendReceiveOutput(string text)
    {
        lock (_outputLock)
        {
            ReceiveOutputTextBox.Dispatcher.Invoke(() =>
            {
                ReceiveOutputTextBox.AppendText(text + Environment.NewLine);
                ReceiveOutputTextBox.ScrollToEnd();
            });
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Clean up process if window is closed during execution
        if (_crocProcess != null && !_crocProcess.HasExited)
        {
            _crocProcess.Kill();
        }
        base.OnClosed(e);
    }

    private bool TryParseProgress(string line, out double progress)
    {
        progress = 0;
        // Look for pattern like: "filename   45% |==========          | (size) [time]"
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)%\s*\|\s*.*\|\s*\(");
        if (match.Success && double.TryParse(match.Groups[1].Value, out progress))
        {
            return true;
        }
        return false;
    }

    private void ResetReceiveProgress()
    {
        ReceiveProgressBar.Dispatcher.Invoke(() =>
        {
            ReceiveProgressBar.Value = 0;
        });
    }

    private void UpdateReceiveProgress(double progress)
    {
        ReceiveProgressBar.Dispatcher.Invoke(() =>
        {
            ReceiveProgressBar.Value = progress;
        });
    }

    // Tab selection changed
    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Tab changed - could add logic here if needed for different modes
    }

    // Send tab methods
    private void SendBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (SendFileRadio.IsChecked == true)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Select file to send";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;

            if (dialog.ShowDialog() == true)
            {
                SendPathTextBox.Text = dialog.FileName;
                UpdateSendButtonState();
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
                SendPathTextBox.Text = dialog.SelectedPath;
                UpdateSendButtonState();
            }
        }
    }

    private void SendTypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Clear the path when switching between file/directory
        SendPathTextBox.Text = "";
        UpdateSendButtonState();
    }

    private void UpdateSendButtonState()
    {
        bool isValid = !string.IsNullOrWhiteSpace(SendPathTextBox.Text) &&
                      (File.Exists(SendPathTextBox.Text) || Directory.Exists(SendPathTextBox.Text));
        SendButton.IsEnabled = isValid;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        string path = SendPathTextBox.Text.Trim();

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

        await StartSendCrocProcess(path);
    }

    private async Task StartSendCrocProcess(string path)
    {
        try
        {
            // Disable controls during execution
            SetSendControlsEnabled(false);
            ClearSendOutput();
            ResetSendProgress();
            SendCodeTextBox.Text = ""; // Clear previous code

            // Start the croc process
            _crocProcess = new Process();
            _crocProcess.StartInfo.FileName = "croc";
            _crocProcess.StartInfo.Arguments = $"send \"{path}\"";
            _crocProcess.StartInfo.UseShellExecute = false;
            _crocProcess.StartInfo.RedirectStandardOutput = true;
            _crocProcess.StartInfo.RedirectStandardError = true;
            _crocProcess.StartInfo.CreateNoWindow = true;

            string extractedCode = "";

            _crocProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Try to extract code from "Code is: <code>" line
                    if (string.IsNullOrWhiteSpace(extractedCode))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(e.Data.Trim(), @"^Code is:\s*([^\s]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            extractedCode = match.Groups[1].Value;
                            SendCodeTextBox.Dispatcher.Invoke(() =>
                            {
                                SendCodeTextBox.Text = extractedCode;
                            });
                        }
                    }

                    if (TryParseProgress(e.Data, out double progress))
                    {
                        UpdateSendProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        AppendSendOutput(e.Data);
                    }
                    // Skip empty/whitespace-only lines
                }
            };

            _crocProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Try to extract code from "Code is: <code>" line (also check stderr)
                    if (string.IsNullOrWhiteSpace(extractedCode))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(e.Data.Trim(), @"^Code is:\s*([^\s]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            extractedCode = match.Groups[1].Value;
                            SendCodeTextBox.Dispatcher.Invoke(() =>
                            {
                                SendCodeTextBox.Text = extractedCode;
                            });
                        }
                    }

                    if (TryParseProgress(e.Data, out double progress))
                    {
                        UpdateSendProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        AppendSendOutput(e.Data);
                    }
                    // Skip empty/whitespace-only lines
                }
            };

            AppendSendOutput($"Starting croc send for: {path}");
            AppendSendOutput($"Command: croc send \"{path}\"");
            AppendSendOutput("");

            _crocProcess.Start();
            _crocProcess.BeginOutputReadLine();
            _crocProcess.BeginErrorReadLine();

            // Wait for process to complete
            await Task.Run(() => _crocProcess.WaitForExit());

            AppendSendOutput("");
            AppendSendOutput("Process completed.");
            AppendSendOutput($"Exit code: {_crocProcess.ExitCode}");

            // Re-enable controls
            SetSendControlsEnabled(true);
        }
        catch (Exception ex)
        {
            AppendSendOutput($"Error: {ex.Message}");
            SetSendControlsEnabled(true);
        }
        finally
        {
            _crocProcess = null;
        }
    }

    private void SendCancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_crocProcess != null && !_crocProcess.HasExited)
        {
            AppendSendOutput("Stopping croc process...");
            _crocProcess.Kill();
            _crocProcess = null;
            SetSendControlsEnabled(true);
        }
    }

    private void SetSendControlsEnabled(bool enabled)
    {
        SendFileRadio.IsEnabled = enabled;
        SendDirectoryRadio.IsEnabled = enabled;
        SendPathTextBox.IsEnabled = enabled;
        SendBrowseButton.IsEnabled = enabled;
        SendButton.IsEnabled = enabled && !string.IsNullOrWhiteSpace(SendPathTextBox.Text) &&
                              (File.Exists(SendPathTextBox.Text) || Directory.Exists(SendPathTextBox.Text));
        SendCancelButton.IsEnabled = !enabled;
    }

    private void ClearSendOutput()
    {
        SendOutputTextBox.Text = "";
    }

    private void AppendSendOutput(string text)
    {
        lock (_outputLock)
        {
            SendOutputTextBox.Dispatcher.Invoke(() =>
            {
                SendOutputTextBox.AppendText(text + Environment.NewLine);
                SendOutputTextBox.ScrollToEnd();
            });
        }
    }

    private void ResetSendProgress()
    {
        SendProgressBar.Dispatcher.Invoke(() =>
        {
            SendProgressBar.Value = 0;
        });
    }

    private void UpdateSendProgress(double progress)
    {
        SendProgressBar.Dispatcher.Invoke(() =>
        {
            SendProgressBar.Value = progress;
        });
    }
}