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

namespace CrocMadame;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process? _crocProcess;
    private readonly object _outputLock = new object();

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        BrowseButton.Click += BrowseButton_Click;
        DownloadButton.Click += DownloadButton_Click;
        CancelButton.Click += CancelButton_Click;
        CodeTextBox.TextChanged += CodeTextBox_TextChanged;
    }

    private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDownloadButtonState();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "Select destination directory";
        dialog.ShowNewFolderButton = true;
        dialog.UseDescriptionForTitle = true;
        dialog.InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DirectoryTextBox.Text = dialog.SelectedPath;
            UpdateDownloadButtonState();
        }
    }

    private void UpdateDownloadButtonState()
    {
        bool isValid = !string.IsNullOrWhiteSpace(CodeTextBox.Text) &&
                      !string.IsNullOrWhiteSpace(DirectoryTextBox.Text);
        DownloadButton.IsEnabled = isValid;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeTextBox.Text) ||
            string.IsNullOrWhiteSpace(DirectoryTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a secret code and select a destination directory.",
                          "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string code = CodeTextBox.Text.Trim();
        string directory = DirectoryTextBox.Text.Trim();

        if (!Directory.Exists(directory))
        {
            System.Windows.MessageBox.Show("The selected directory does not exist.",
                          "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await StartCrocProcess(code, directory);
    }

    private async Task StartCrocProcess(string code, string directory)
    {
        try
        {
            // Disable controls during execution
            SetControlsEnabled(false);
            ClearOutput();

            // Start the croc process
            _crocProcess = new Process();
            _crocProcess.StartInfo.FileName = "croc";
            _crocProcess.StartInfo.Arguments = $"--yes --out \"{directory}\" {code}";
            _crocProcess.StartInfo.UseShellExecute = false;
            _crocProcess.StartInfo.RedirectStandardOutput = true;
            _crocProcess.StartInfo.RedirectStandardError = true;
            _crocProcess.StartInfo.CreateNoWindow = true;

            _crocProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppendOutput(e.Data);
                }
            };

            _crocProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains('%'))
                    {
                        // Parse progress percentage
                        var percentIndex = e.Data.IndexOf(" % ");
                        if (percentIndex > 0)
                        {
                            var start = percentIndex;
                            while (start > 0 && (char.IsDigit(e.Data[start - 1]) || e.Data[start - 1] == '.'))
                                start--;
                            var percentStr = e.Data.Substring(start, percentIndex - start);
                            if (double.TryParse(percentStr, out double percent))
                            {
                                Dispatcher.Invoke(() => ProgressBar.Value = percent);
                            }
                        }
                        // Parse filename from progress line
                        var trimmed = e.Data.TrimStart();
                        var spaceIndex = trimmed.IndexOf("   ");
                        if (spaceIndex > 0)
                        {
                            var filename = trimmed.Substring(0, spaceIndex).Trim();
                            if (!string.IsNullOrWhiteSpace(filename))
                            {
                                Dispatcher.Invoke(() => ProgressTextBlock.Text = $"Downloading: {filename}");
                            }
                        }
                    }
                    else if (e.Data.Contains("Receiving '"))
                    {
                        // Parse filename from receiving message
                        var start = e.Data.IndexOf("Receiving '") + 11;
                        var end = e.Data.IndexOf("'", start);
                        if (end > start)
                        {
                            var filename = e.Data.Substring(start, end - start);
                            Dispatcher.Invoke(() => ProgressTextBlock.Text = $"Downloading: {filename}");
                        }
                    }
                    else
                    {
                        AppendOutput(e.Data);
                    }
                }
            };

            AppendOutput($"Starting croc with output directory: {directory}");
            AppendOutput($"Command: croc --yes --out \"{directory}\" {code}");
            AppendOutput("");

            _crocProcess.Start();
            _crocProcess.BeginOutputReadLine();
            _crocProcess.BeginErrorReadLine();

            // Wait for process to complete
            await Task.Run(() => _crocProcess.WaitForExit());

            AppendOutput("");
            AppendOutput("Process completed.");
            AppendOutput($"Exit code: {_crocProcess.ExitCode}");

            // Re-enable controls
            SetControlsEnabled(true);
        }
        catch (Exception ex)
        {
            AppendOutput($"Error: {ex.Message}");
            SetControlsEnabled(true);
        }
        finally
        {
            _crocProcess = null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_crocProcess != null && !_crocProcess.HasExited)
        {
            AppendOutput("Stopping croc process...");
            _crocProcess.Kill();
            _crocProcess = null;
            SetControlsEnabled(true);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        CodeTextBox.IsEnabled = enabled;
        DirectoryTextBox.IsEnabled = enabled;
        BrowseButton.IsEnabled = enabled;
        DownloadButton.IsEnabled = enabled && !string.IsNullOrWhiteSpace(CodeTextBox.Text) &&
                                 !string.IsNullOrWhiteSpace(DirectoryTextBox.Text);
        CancelButton.IsEnabled = !enabled;
    }

    private void ClearOutput()
    {
        OutputTextBox.Text = "";
        ProgressBar.Value = 0;
        ProgressTextBlock.Text = "";
    }

    private void AppendOutput(string text)
    {
        lock (_outputLock)
        {
            OutputTextBox.Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text + Environment.NewLine);
                OutputTextBox.ScrollToEnd();
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
}