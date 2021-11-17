cd /D "%~dp0..\"

dotnet pack .\ProjectsForSigning.sln -p:OfficialRelease=True --configuration Release --no-restore --no-build || exit /b 1
