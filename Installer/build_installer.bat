@echo off
setlocal enabledelayedexpansion

echo ==================================================
echo    ASUIPP - Build Installer
echo ==================================================
echo.

set SOLUTION_DIR=%~dp0..
set BUILD_CONFIG=Release
set INNO_SCRIPT=%~dp0ASUIPP_Setup.iss
set RELEASE_DIR=%SOLUTION_DIR%\ASUIPP.App\bin\%BUILD_CONFIG%

echo [1/4] Searching MSBuild...
set MSBUILD=

:: Method 1: vswhere (works for all VS installations)
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%VSWHERE% -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        set "MSBUILD=%%i"
    )
)
if defined MSBUILD goto :found_msbuild

:: Method 2: Common paths
for %%p in (
    "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
) do (
    if exist %%~p (
        set "MSBUILD=%%~p"
        goto :found_msbuild
    )
)

:: Method 3: Search entire Program Files
echo    Searching disk...
for /r "%ProgramFiles%" %%f in (MSBuild.exe) do (
    echo    Candidate: %%f
    set "MSBUILD=%%f"
    goto :found_msbuild
)
for /r "%ProgramFiles(x86)%" %%f in (MSBuild.exe) do (
    echo    Candidate: %%f
    set "MSBUILD=%%f"
    goto :found_msbuild
)

:: Method 4: Check PATH
where MSBuild.exe >nul 2>&1
if %errorlevel%==0 (
    for /f "tokens=*" %%i in ('where MSBuild.exe') do (
        set "MSBUILD=%%i"
        goto :found_msbuild
    )
)

echo [ERROR] MSBuild not found!
echo.
echo Please build manually in Visual Studio:
echo   1. Open asuIPP.sln
echo   2. Set Configuration to Release
echo   3. Build - Build Solution
echo   4. Then open ASUIPP_Setup.iss in Inno Setup
echo   5. Build - Compile
echo.
pause
exit /b 1

:found_msbuild
echo    Found: %MSBUILD%

echo.
echo [2/4] Building project (%BUILD_CONFIG%)...
"%MSBUILD%" "%SOLUTION_DIR%\asuIPP.sln" /t:Restore /p:Configuration=%BUILD_CONFIG% /v:quiet /m 2>nul
"%MSBUILD%" "%SOLUTION_DIR%\asuIPP.sln" /t:Build /p:Configuration=%BUILD_CONFIG% /v:minimal /m

if %errorlevel% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo    Build OK

echo.
echo [3/4] Checking files...

if not exist "%RELEASE_DIR%\ASUIPP.App.exe" (
    echo [ERROR] ASUIPP.App.exe not found in %RELEASE_DIR%
    pause
    exit /b 1
)

if not exist "%RELEASE_DIR%\x64\SQLite.Interop.dll" (
    echo    Copying SQLite.Interop.dll...
    for /d %%d in ("%SOLUTION_DIR%\packages\System.Data.SQLite.Core.*") do (
        if exist "%%d\build\net46\x64\SQLite.Interop.dll" (
            if not exist "%RELEASE_DIR%\x64" mkdir "%RELEASE_DIR%\x64"
            copy /y "%%d\build\net46\x64\SQLite.Interop.dll" "%RELEASE_DIR%\x64\" >nul
            if not exist "%RELEASE_DIR%\x86" mkdir "%RELEASE_DIR%\x86"
            copy /y "%%d\build\net46\x86\SQLite.Interop.dll" "%RELEASE_DIR%\x86\" >nul
            echo    SQLite copied OK
        )
    )
)

echo    Files OK

echo.
echo [4/4] Building installer...

set ISCC=
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    goto :found_iscc
)
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
    goto :found_iscc
)
if exist "%ProgramFiles(x86)%\Inno Setup 5\ISCC.exe" (
    set "ISCC=%ProgramFiles(x86)%\Inno Setup 5\ISCC.exe"
    goto :found_iscc
)

echo [ERROR] Inno Setup not found!
echo Download from: https://jrsoftware.org/isdl.php
pause
exit /b 1

:found_iscc
echo    Found: %ISCC%

if not exist "%~dp0Output" mkdir "%~dp0Output"
"%ISCC%" "%INNO_SCRIPT%"

if %errorlevel% neq 0 (
    echo [ERROR] Inno Setup compilation failed!
    pause
    exit /b 1
)

echo.
echo ==================================================
echo    DONE! Installer created in Output folder.
echo ==================================================
echo.
pause
explorer "%~dp0Output"
