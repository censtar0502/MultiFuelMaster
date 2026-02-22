; MultiFuelMaster Installer Script for Inno Setup 6
; ==================================================

#define MyAppName "MultiFuelMaster"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Azerhud"
#define MyAppURL "mailto:azerhud@gmail.com"
#define MyAppExeName "MultiFuelMaster.exe"

; Path to build output (Debug) - change to Release for production
#define BuildOutput "..\MultiFuelMaster.UI\bin\x64\Debug\net10.0-windows\win-x64"

[Setup]
AppId={{B7E2F1A3-9C4D-5E6F-8A7B-1C2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
; License agreement page
LicenseFile=license.txt
; Trial/licensing info page (shown after license)
InfoAfterFile=trial_info.txt
OutputDir=output
OutputBaseFilename=MultiFuelMasterSetup_{#MyAppVersion}
SetupIconFile=..\MultiFuelMaster.UI\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; 64-bit Windows required
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Minimum Windows version
MinVersion=10.0

; Admin privileges
PrivilegesRequired=admin

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main exe
Source: "{#BuildOutput}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; All DLLs and dependencies
Source: "{#BuildOutput}\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; Runtime config and other files
Source: "{#BuildOutput}\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Native libraries in subfolders
Source: "{#BuildOutput}\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "{#BuildOutput}\createdump.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
