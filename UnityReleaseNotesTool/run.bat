@echo off
setlocal

set CurrentPath=%~dp0
set ReleaseNotesFolder=E:\Workspace\_MyRepos\UnityReleaseNotes
set Exe=%CurrentPath%bin\Debug\net9.0\UnityReleaseNotesTool.exe

echo === Sync Unity release notes into %ReleaseNotesFolder% ===
echo.

rem --archive-html moves the original HTML into Backup\ (main tree keeps Markdown only).
"%Exe%" "%ReleaseNotesFolder%" --archive-html
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo FAILED: grabber returned a non-zero exit code.
    goto End
)

echo.
echo === Commit and push ===
echo.

cd /d "%ReleaseNotesFolder%"

git add -A

git status --short

git commit -m "Update Unity release notes (%DATE%)" --quiet
if %ERRORLEVEL% NEQ 0 (
    echo Nothing to commit, or commit failed.
    goto End
)

git push

:End
echo.
cd /d "%CurrentPath%"
pause
