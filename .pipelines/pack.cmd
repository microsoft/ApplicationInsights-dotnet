cd /D "%~dp0..\"

dotnet pack .\ProjectsForSigning.sln -p:PublicRelease=True -p:StableRelease=True -p:BuildServer=True --configuration Release --no-restore --no-build || exit /b 1
