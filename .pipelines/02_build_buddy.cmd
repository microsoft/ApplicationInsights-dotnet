cd /D "%~dp0..\"

dotnet build .\ProjectsForSigning.sln -p:OfficialRelease=True --configuration Release --no-restore || exit /b 1
