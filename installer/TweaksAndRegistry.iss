#define AppName "Tweaks and Registry"
#define AppVersion "0.1.0"
#define AppPublisher "Mogley"
#define AppExe "TweaksAndRegistry.exe"

#ifndef PublishDir
  #define PublishDir "..\bin\Release\net8.0-windows\win-x64\publish"
#endif

[Setup]
AppId={{A7E1B83C-9D62-4F15-AC31-7E92C6B04152}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (C) 2026 Mogley
DefaultDirName={localappdata}\Programs\Tweaks and Registry
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=TweaksAndRegistry-Setup-x64
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CloseApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: postinstall nowait skipifsilent
