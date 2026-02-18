; ══════════════════════════════════════════════════════════════
; АСУИПП — Установщик
; Inno Setup Script
; ══════════════════════════════════════════════════════════════

#define MyAppName "АСУИПП"
#define MyAppNameEn "ASUIPP"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "СГУПС"
#define MyAppExeName "ASUIPP.App.exe"
#define MyAppDescription "Автоматизированная система учёта индивидуальных показателей работы преподавателя"
#define BuildOutput "..\ASUIPP.App\bin\Release"

[Setup]
AppId={{A5D1F9E2-8B3C-4A7E-9F12-ASUIPP000001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=ASUIPP_Setup_{#MyAppVersion}
SetupIconFile=setup_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=wizard_banner.bmp
WizardSmallImageFile=wizard_small.bmp
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DisableWelcomePage=no
ShowLanguageDialog=no
MinVersion=6.1sp1

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
russian.LaunchApp=Запустить {#MyAppName}
russian.CreateDesktopIcon=Создать ярлык на рабочем столе
russian.CreateStartup=Запускать при входе в Windows (напоминания)
russian.DotNetRequired={#MyAppName} требует .NET Framework 4.7.2 или новее.%n%nПожалуйста, установите его и повторите установку.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:CreateStartup}"; GroupDescription: "Автозагрузка:"

[Files]
Source: "{#BuildOutput}\*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\*.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\*.xml"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\x86\*.dll"; DestDir: "{app}\x86"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\x64\*.dll"; DestDir: "{app}\x64"; Flags: ignoreversion skipifsourcedoesntexist
Source: "app_icon.ico"; DestDir: "{app}"; DestName: "ASUIPP.ico"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ASUIPP.ico"; Comment: "{#MyAppDescription}"
Name: "{group}\Удалить {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{app}\ASUIPP.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ASUIPP.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ASUIPP"; ValueData: """{app}\{#MyAppExeName}"" --tray"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\ASUIPP"

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden skipifdoesntexist

[Code]
function IsDotNetInstalled(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    Result := (Release >= 461808);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNetInstalled() then
  begin
    MsgBox(ExpandConstant('{cm:DotNetRequired}'), mbCriticalError, MB_OK);
    Result := False;
  end;
end;
