# Mogley Tweaks & Registry

A lightweight Windows 10/11 customization utility built with C# WinForms.

**Status:** Early development  
**Target:** Windows 10 and Windows 11 (x64)

## Start here

The app opens to a beginner-friendly home screen:

- **Quick Tweaks** — beginner-friendly switches for dark mode, accent color, transparency, file extensions, hidden files, File Explorer startup, taskbar seconds, and Windows 11 taskbar alignment.
- **Registry Packs** — import a shared `.regimp` file and inspect every proposed change before applying it.
- **Advanced Registry Editor** — manual registry browsing and editing.
- **Backups** — open the folder containing saved value backups.

## Registry packs (.regimp)

A `.regimp` file is a structured JSON-based Mogley pack, not an executable program and not a raw `.reg` script.

- Import supports version 1 packs with up to 200 values and a 1 MB file-size limit.
- Imported entries are restricted to `HKEY_CURRENT_USER\\Software`.
- Pack import does not support deleting keys or values, running commands, loading DLLs, or writing to machine-wide hives.
- Every entry is shown in a review screen before applying.
- Backups are saved before changes.
- The Advanced Registry Editor can export the selected HKCU\\Software key as a shareable `.regimp` pack.

These limits reduce risk but do not guarantee a pack is safe. A malicious or incorrect user-level registry value can still disrupt applications, Windows settings, or your user profile. Only import packs from sources you trust and read the preview carefully.

## Safety

- Registry edits can break Windows, applications, sign-in, networking, or device functionality.
- Backups are helpful but are not a complete system restore and cannot guarantee recovery.
- The app does not request administrator privileges at startup.
- Windows permissions still apply.
- Avoid applying multiple unknown tweaks at once. Create a restore point before experimenting with important system settings.
- Some changes need Explorer restart or sign-out.

## Build

Install the .NET 8 SDK, then run:

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

Publish a self-contained single-file x64 executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output: `bin\\Release\\net8.0-windows\\win-x64\\publish\\MogleyTweaks.exe`

## Limitations

- This is an early utility, not a replacement for Windows Registry Editor.
- Registry availability and behavior can vary between Windows builds.
- Backups cover individual values, not complete registry hives or system state.

## License

No license has been selected yet.
