cd /D "%~dp0..\"

dotnet pack .\ProjectsForSigning.sln -p:PublicRelease=True -p:StableRelease=True -p:OfficialRelease=True -p:Redfield=True --configuration Release --no-restore --no-build || exit /b 1
