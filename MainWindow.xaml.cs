using System;
using System.Windows;
using System.Windows.Controls;

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