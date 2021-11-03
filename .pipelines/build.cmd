cd /D "%~dp0..\"

dotnet build .\ProjectsForSigning.sln -p:PublicRelease=True -p:StableRelease=True -p:BuildServer=True --configuration Release --no-restore || exit /b 1
