# CrocMadame

A modern, user-friendly Windows desktop application for secure peer-to-peer file transfer using the [croc](https://github.com/schollz/croc) command-line tool.

![CrocMadame](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-1.2.0-orange)

## Overview

CrocMadame provides a graphical interface for the powerful `croc` file transfer tool, making it easy for Windows users to securely send and receive files without needing to use the command line. The application features a clean, tabbed interface with real-time progress tracking and comprehensive output logging.

### Key Features

- **📤 Send Files & Directories**: Easily send individual files or entire directories
- **📥 Receive Files**: Download files using secret codes provided by the sender
- **🎯 Real-time Progress**: Visual progress bar with percentage tracking
- **🔒 Secure Transfers**: Leverages croc's end-to-end encryption
- **⚙️ Custom Relay Support**: Configure custom relay servers
- **📝 Detailed Logging**: Real-time output display showing transfer status
- **💾 Persistent Settings**: Saves relay preferences between sessions
- **🎨 Modern UI**: Clean, intuitive Windows desktop interface

## Screenshots

### Receive Tab
The Receive tab allows you to download files by entering a secret code and selecting a destination directory.

### Send Tab
The Send tab enables you to send files or directories, with the generated secret code displayed for sharing.

## Prerequisites

Before using CrocMadame, you need to have `croc` installed on your system.

### Installing croc

**Using Scoop (Recommended for Windows):**
```powershell
scoop install croc
```

**Using Go:**
```powershell
go install github.com/schollz/croc/v9@latest
```

**Manual Installation:**
1. Visit the [croc releases page](https://github.com/schollz/croc/releases)
2. Download the latest Windows executable
3. Add it to your system PATH

**Verify Installation:**
```powershell
croc --version
```

## Installation

### Option 1: Download Pre-built Binary (Recommended)

1. Visit the [Releases page](https://github.com/yourusername/CrocMadame/releases)
2. Download the latest `CrocMadame.exe` file
3. Run the executable

### Option 2: Build from Source

**Requirements:**
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11

**Build Steps:**
```bash
# Clone the repository
git clone https://github.com/yourusername/CrocMadame.git
cd CrocMadame

# Build the application
dotnet build -c Release

# Or create a self-contained executable
./build.sh
```

The built executable will be in `bin/Release/net9.0-windows/`.

## Usage

### Sending Files

1. Open CrocMadame
2. Navigate to the **Send** tab
3. Choose whether to send a **File** or **Directory**
4. Click **Browse...** to select your file or folder
5. (Optional) Configure a custom relay server
6. Click **Send** to start the transfer
7. **Share the generated secret code** with the recipient
8. Wait for the transfer to complete

### Receiving Files

1. Open CrocMadame
2. Navigate to the **Receive** tab
3. Enter the **secret code** provided by the sender
4. Click **Browse...** to select the destination directory
5. (Optional) Configure a custom relay server
6. Click **Download** to start receiving
7. Monitor progress in the progress bar and output log

### Relay Configuration

By default, CrocMadame uses croc's public relay servers. You can configure custom relay servers for enhanced privacy or performance:

- **Format**: `host:port` (e.g., `relay.example.com:9009`)
- **Receive Tab**: Configure relay for receiving files
- **Send Tab**: Configure relay for sending files
- Settings are automatically saved and restored between sessions

## Architecture

CrocMadame is built with modern .NET 9.0 and WPF, featuring a clean MVVM-inspired architecture:

```
CrocMadame/
├── MainWindow.xaml          # Main UI layout and tabs
├── MainWindow.xaml.cs       # Window event handlers
├── CrocProcessManager.cs    # Manages croc process execution
├── ReceiveHandler.cs        # Handles file receiving logic
├── SendHandler.cs           # Handles file sending logic
├── Settings.cs              # Application settings management
└── CrocMadame.csproj        # Project configuration
```

### Key Components

- **CrocProcessManager**: Wraps croc CLI execution with progress parsing and output capture
- **ReceiveHandler**: Manages the receive workflow, directory selection, and validation
- **SendHandler**: Manages the send workflow, file/directory selection, and code extraction
- **Settings**: Persistent configuration using JSON serialization

## Development

### Project Structure

```bash
CrocMadame/
├── CrocMadame.csproj        # Project file (version 1.2.0)
├── CrocMadame.sln           # Solution file
├── App.xaml                 # Application resources
├── App.xaml.cs              # Application entry point
├── MainWindow.xaml          # Main window UI
├── MainWindow.xaml.cs       # Main window logic
├── CrocProcessManager.cs    # Process management
├── ReceiveHandler.cs        # Receive functionality
├── SendHandler.cs           # Send functionality
├── Settings.cs              # Settings management
├── build.sh                 # Build script for releases
├── tag_release.sh           # Automated version tagging
└── README.md                # This file
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained executable (Windows x64)
dotnet publish -c Release --self-contained true --runtime win-x64 \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

### Version Management

The project uses automated version tagging:

```bash
# Update version in CrocMadame.csproj
# Then run:
./tag_release.sh
```

This script:
1. Reads the version from `CrocMadame.csproj`
2. Compares with the latest Git tag
3. Creates and pushes a new tag if the version is newer
4. Ensures the main branch is up-to-date

## Troubleshooting

### Common Issues

**"croc is not recognized"**
- Ensure croc is installed and available in your PATH
- Verify installation: `croc --version`

**Transfer Fails**
- Check your internet connection
- Verify the secret code is correct
- Try using a different relay server
- Check the output log for detailed error messages

**Permission Errors**
- Ensure you have write permissions for the destination directory
- Try running as administrator if needed

**Firewall Issues**
- Ensure croc can communicate through your firewall
- Ports 9009-9013 may need to be open for relay communication

### Getting Help

1. Check the **Output** section in the application for detailed logs
2. Review the [croc documentation](https://github.com/schollz/croc)
3. [Open an issue](https://github.com/yourusername/CrocMadame/issues) with:
   - Windows version
   - CrocMadame version
   - croc version
   - Error messages from the output log

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/CrocMadame.git`
3. Create a feature branch: `git checkout -b feature/amazing-feature`
4. Make your changes
5. Test thoroughly
6. Commit your changes: `git commit -m 'Add amazing feature'`
7. Push to the branch: `git push origin feature/amazing-feature`
8. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

CrocMadame is a graphical interface for croc, which is also [MIT licensed](https://github.com/schollz/croc/blob/master/LICENSE).

## Acknowledgments

- **[croc](https://github.com/schollz/croc)** - The powerful file transfer tool that makes this possible
- **[.NET](https://dotnet.microsoft.com/)** - The modern development platform
- **[WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)** - Windows Presentation Foundation for the UI

## Changelog

### Version 1.2.0
- Initial public release
- Full send and receive functionality
- Custom relay server support
- Real-time progress tracking
- Detailed output logging
- Persistent settings
- Modern WPF interface

---

**Made with ❤️ for easy and secure file sharing**