@echo off
set CurrentPath=%~dp0

set DownloadFolder="D:\Unity\Workspace\UnityReleaseNotes-Tool\download"
set ReleaseNotesFolder="d:\Unity\Workspace\UnityReleaseNotes"
set WhatsNewUrl="https://unity3d.com/unity/whats-new/2020.1.3"

echo bin\UnityReleaseNotesTool.exe %DownloadFolder% %ReleaseNotesFolder% %WhatsNewUrl%
bin\UnityReleaseNotesTool.exe %DownloadFolder% %ReleaseNotesFolder% %WhatsNewUrl%

if %ERRORLEVEL% NEQ 0 (
    echo 提取版本信息失败
	goto Failed
)

echo.
echo.

:Git

cd %ReleaseNotesFolder%

git status
git ls-files --others> %CurrentPath%other-files.txt
git add *
git commit --file=%CurrentPath%other-files.txt
git push

:Failed

:Success

cd %CurrentPath%

pause