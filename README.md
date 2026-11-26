# Mogley Tweaks & Registry

A lightweight Windows 10/11 customization utility built with C# and Windows Forms.

**Status:** First edition in development  
**Version:** 0.1.0  
**Target:** Windows 10 and Windows 11 (x64)

## Features

- Browse the five standard registry root hives.
- Create and delete registry keys.
- Create, edit, and delete registry values.
- Supports String, ExpandString, Binary, DWORD, QWORD, and MultiString values.
- Search value names and data in the selected key.
- Export selected key values to a `.reg` file.
- Save typed backups before editing or deleting a value, and restore backups.
- Tweaks for accent color, app/system light mode, File Explorer extensions, hidden files, and Explorer startup location.
- Light/dark UI follows the Windows **app mode** preference at launch.

## Build

Install the .NET 8 SDK, then run in the repository directory:

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

Publish a self-contained single-file x64 executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\MogleyTweaks.exe`

## Safety and limitations

- The app does **not** request administrator privileges at startup. Windows permissions still apply; protected locations may reject writes.
- Review every target and value before confirming.
- Value changes are backed up to `%LOCALAPPDATA%\MogleyTweaks\Backups`.
- Recursive key deletion is deliberately not offered in this first edition.
- Backups cover individual values, not complete registry hives.
- Some settings take effect after restarting Explorer or signing out.
- Windows 10 and 11 builds differ. Registry presence alone does not guarantee a tweak is supported.
- This is an early utility, not a replacement for Windows Registry Editor.

## License

No license has been selected yet.
