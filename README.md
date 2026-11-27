# Tweaks and Registry

**A lightweight Windows utility for registry management, system information, and desktop customization.**

Tweaks and Registry brings common Windows configuration tools into one application. It includes searchable tweaks, a registry editor, registry-pack import and export, backups, system inventories, and personalization tools.

> **Project status:** Early development (0.1.0)  
> **Supported platform:** Windows 10 and Windows 11, 64-bit (x64)  
> **Technology:** C# · Windows Forms · .NET 8

## Download

Get the available builds from the [official download page](https://apps.ereyada.com/tweaks-and-reg/download.html).

| Download | Description |
| --- | --- |
| **Windows Installer** | Installs the application in your user profile, creates a Start Menu shortcut, and registers an uninstaller. |
| **Portable** | A standalone executable that can be run without using the installer. |

Build artifacts are also available from [GitHub Actions](https://github.com/mogley-co/Tweaks-and-Regesitry/actions). Select a successful workflow run and download the installer or portable artifact. GitHub Actions artifacts are ZIP archives; extract the executable before running or distributing it.

## Features

### Quick Tweaks

Browse more than 50 searchable tweak options covering areas such as appearance, File Explorer, taskbar, privacy, gaming, desktop behavior, notifications, and clipboard settings.

- Apply a tweak individually.
- Review and apply toggles changed during the current session with **Apply all**.
- Export supported managed settings to a .regimp pack.
- Back up registry values before changes are written.

**Apply all does not turn every unchecked option off.** It processes only toggles changed during the current session.

### Registry Editor

Browse supported registry hives, navigate keys, search values, and create, edit, or delete registry values. Supported value types include String, ExpandString, Binary, DWORD, QWORD, and MultiString.

The editor also supports exporting a selected key as a .reg file and creating or restoring individual value backups.

Registry editing is powerful and can affect Windows or installed applications. Review every change carefully.

### Registry Packs

Import and export .regimp packs to share supported user-level registry settings.

- Version 1 packs support up to 200 values and a maximum file size of 1 MB.
- Imported entries are restricted to HKEY_CURRENT_USER\Software.
- The app displays proposed changes for review before applying them.
- A .regbu backup is saved for each value before it is changed.
- Pack imports do not execute commands or support deleting registry keys or values.

These restrictions reduce the scope of an imported pack; they do **not** guarantee that its contents are safe. Incorrect registry values can still disrupt applications or Windows settings. Only import packs from sources you trust.

### Backups

Create and restore supported registry-value backups. Registry packs also create backups before applying their proposed changes.

Backups are not a full registry, disk, or Windows system image. They cannot guarantee recovery from every problem.

### System Tools

View read-only information about:

- Windows version, build, edition, architecture, and runtime.
- Startup entries from standard Run registry keys and Startup folders.
- Installed applications listed in standard uninstall registry locations.
- Running processes and their memory usage.
- Local drive capacity and free space.
- Network adapters and IP information.

The app also provides shortcuts to built-in Windows utilities such as Task Manager, Services, Device Manager, Disk Management, Event Viewer, System Information, and Resource Monitor.

System inventories are informational. Use Windows management interfaces to change startup settings, uninstall applications, or manage processes and services.

### Personalization

- Set the desktop wallpaper.
- Assign a custom .ico file to a folder.
- Open Windows desktop icon settings.

The application saves a backup of the wallpaper registry value and copies an existing folder desktop.ini before modifying it.

### Activation Diagnostics

Check Windows activation status and view license details using Windows built-in licensing diagnostics. The app can open the official Windows Activation settings page.

This feature is diagnostic only. It does not activate Windows, provide product keys, or bypass licensing.

## Safety and limitations

Tweaks and Registry is a configuration utility, not a replacement for Windows Settings, Registry Editor, or system recovery tools.

- Registry changes can affect Windows, applications, sign-in, networking, and device behavior.
- Create a Windows restore point before experimenting with important system settings.
- Review pack previews and understand changes before applying them.
- The application does not request administrator privileges at startup. Windows permissions still apply, and some operations may be unavailable without appropriate permissions.
- Some settings require restarting File Explorer or signing out before their effects appear.
- Registry behavior can vary between Windows versions and builds. Some legacy or optional settings may be ignored by Windows.
- If a multi-value operation fails partway through, earlier writes may already have succeeded. Use the created backups to restore supported values if necessary.
- Backups are not a substitute for a full system backup.

The application intentionally keeps several system-management actions read-only and opens the corresponding Windows tools instead of silently changing or removing system components.

## System requirements

- Windows 10 or Windows 11.
- 64-bit (x64) Windows.
- The published installer and portable builds are self-contained; users do not normally need to install the .NET runtime separately.

## Building from source

### Prerequisites

- Windows 10 or Windows 11, x64.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) if you also want to compile the Windows installer.

### Build and run

From the repository root, open PowerShell:

~~~powershell
dotnet restore
dotnet build -c Release
dotnet run
~~~

### Publish the portable executable

~~~powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
~~~

The published executable is located at:

~~~text
bin/Release/net8.0-windows/win-x64/publish/TweaksAndRegistry.exe
~~~

### Build the installer

After publishing the application and installing Inno Setup 6, run:

~~~powershell
$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
$publishDir = (Resolve-Path "bin\Release\net8.0-windows\win-x64\publish").Path
& $iscc "/DPublishDir=$publishDir" "installer\TweaksAndRegistry.iss"
~~~

The installer output is written to:

~~~text
installer/Output/TweaksAndRegistry-Setup-x64.exe
~~~

The installer uses a per-user installation under %LOCALAPPDATA%\Programs\Tweaks and Registry. It creates a Start Menu shortcut, offers an optional Desktop shortcut, and registers an uninstaller.

## Continuous integration

The GitHub Actions workflow builds the self-contained x64 application and compiles the installer. Successful runs publish two separate artifacts:

- Tweaks-and-Registry-portable-win-x64
- Tweaks-and-Registry-installer-win-x64

See [Actions](https://github.com/mogley-co/Tweaks-and-Regesitry/actions) for build history and available artifacts.

## Contributing and reporting issues

Bug reports, reproducible problems, and practical feature suggestions are welcome.

When reporting an issue, include:

- Your Windows edition, version, and build.
- The application version.
- The steps needed to reproduce the problem.
- Any relevant error message or screenshot.
- Whether the issue occurs with the installer or portable build.

Please do not include passwords, product keys, personal files, or other sensitive information in issue reports.

## Project links

- **Website:** https://apps.ereyada.com/tweaks-and-reg/
- **Downloads:** https://apps.ereyada.com/tweaks-and-reg/download.html
- **Source code and issues:** https://github.com/mogley-co/Tweaks-and-Regesitry
- **Builds:** https://github.com/mogley-co/Tweaks-and-Regesitry/actions

## License

No open-source license has been selected for this repository yet. Until a license is added, the source code is publicly viewable, but no general permission to reuse, modify, or redistribute it is granted.
