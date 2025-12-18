using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace CrocMadame;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly CrocProcessManager _processManager;
    private readonly ReceiveHandler _receiveHandler;
    private readonly SendHandler _sendHandler;
    private readonly Settings _settings;
    private NotifyIcon? _notifyIcon;
    private bool _isMinimizedToTray;

    public MainWindow()
    {
        InitializeComponent();

        _processManager = new CrocProcessManager();
        _settings = Settings.Load();

        _receiveHandler = new ReceiveHandler(ReceiveCodeTextBox, ReceiveDirectoryTextBox, ReceiveBrowseButton,
                                          ReceiveDownloadButton, ReceiveCancelButton, ReceiveProgressBar,
                                          ReceiveOutputTextBox, ReceiveRelayTextBox, _processManager);
        _sendHandler = new SendHandler(SendCodeTextBox, SendFileRadio, SendDirectoryRadio, SendPathTextBox,
                                    SendBrowseButton, SendButton, SendCancelButton, SendProgressBar,
                                    SendOutputTextBox, SendRelayTextBox, _processManager);

        SetupEventHandlers();

        // Load settings into UI
        ReceiveRelayTextBox.Text = _settings.ReceiveRelay;
        SendRelayTextBox.Text = _settings.SendRelay;

        // Prefill the download destination with the user's Downloads folder
        ReceiveDirectoryTextBox.Text = _receiveHandler.GetDownloadsFolder();
        _receiveHandler.UpdateButtonState();

        // Handle window state changes for minimize to tray
        StateChanged += MainWindow_StateChanged;
        
        // Initialize tray icon after window is loaded
        Loaded += MainWindow_Loaded;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            InitializeTrayIcon();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to initialize tray icon: {ex.Message}");
        }
    }
    
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            MinimizeToTray();
        }
    }

    private void SetupEventHandlers()
    {
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

        private void InitializeTrayIcon()
        {
            try
            {
                // Create the tray icon
                _notifyIcon = new NotifyIcon();

                // Try to load the custom icon from WPF resource, fallback to default if it fails
            _notifyIcon.Icon = SystemIcons.Application; // Default fallback
            try
            {
                var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico", UriKind.Absolute));
                if (resource != null)
                {
                    _notifyIcon.Icon = new Icon(resource.Stream);
                }
            }
            catch
            {
                // Keep default icon if loading fails
            }
            
            _notifyIcon.Text = "CrocMadame";
            _notifyIcon.Visible = true;
            
            // Add click event to restore the window
            _notifyIcon.DoubleClick += (sender, args) =>
            {
                RestoreFromTray();
            };
            
            // Add context menu with restore and exit options
            var contextMenu = new ContextMenuStrip();
            var restoreMenuItem = new ToolStripMenuItem("Restore");
            restoreMenuItem.Click += (sender, args) => RestoreFromTray();
            contextMenu.Items.Add(restoreMenuItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (sender, args) =>
            {
                _notifyIcon?.Dispose();
                Application.Current.Shutdown();
            };
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tray icon initialization failed: {ex.Message}");
            _notifyIcon = null;
        }
    }
    
    private void RestoreFromTray()
    {
        if (_isMinimizedToTray)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _isMinimizedToTray = false;
        }
    }
    
    private void MinimizeToTray()
    {
        if (_notifyIcon != null)
        {
            Hide();
            _isMinimizedToTray = true;
            _notifyIcon.BalloonTipTitle = "CrocMadame";
            _notifyIcon.BalloonTipText = "CrocMadame is running in the background";
            _notifyIcon.ShowBalloonTip(1000);
        }
        else
        {
            // If tray icon is not available, just minimize normally
            WindowState = WindowState.Minimized;
        }
    }

    private void ReceiveCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _receiveHandler.UpdateButtonState();
    }

    private void SaveSettings()
    {
        _settings.ReceiveRelay = ReceiveRelayTextBox.Text;
        _settings.SendRelay = SendRelayTextBox.Text;
        _settings.Save();
    }

    private void ReceiveBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _receiveHandler.BrowseDirectory();
    }

    private async void ReceiveDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        await _receiveHandler.StartReceive();
    }

    private void ReceiveCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _receiveHandler.Cancel();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Save settings before closing
        SaveSettings();

        // Clean up process if window is closed during execution
        _processManager.KillProcess();
        
        // Dispose of the tray icon
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }


    // Send tab methods
    private void SendBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _sendHandler.BrowsePath();
    }

    private void SendTypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        _sendHandler.OnTypeRadioChecked();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await _sendHandler.StartSend();
    }

    private void SendCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _sendHandler.Cancel();
    }
}