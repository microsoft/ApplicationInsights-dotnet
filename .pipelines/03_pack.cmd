cd /D "%~dp0..\"

dotnet pack .\ProjectsForSigning.sln %* --configuration Release --no-restore --no-build || exit /b 1
