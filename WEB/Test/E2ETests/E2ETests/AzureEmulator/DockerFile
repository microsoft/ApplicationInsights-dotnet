FROM microsoft/windowsservercore:latest

RUN powershell -NoProfile -Command \
        mkdir nginx;

# Get SQLLocalDb
ADD https://download.microsoft.com/download/9/0/7/907AD35F-9F9C-43A5-9789-52470555DB90/ENU/SqlLocalDB.msi SqlLocalDB.msi
RUN msiexec /i SqlLocalDB.msi /qn /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES

# Get AzureEmulator
ADD https://download.microsoft.com/download/F/3/8/F3857A38-D344-43B4-8E5B-2D03489909B9/MicrosoftAzureStorageEmulator.msi MicrosoftAzureStorageEmulator.msi
RUN msiexec /i MicrosoftAzureStorageEmulator.msi /qn

#Delete the downloaded installers.
RUN powershell -NoProfile -Command \
        Remove-Item -Force *.msi;

RUN setx /M AZURE_STORAGE_EMULATOR_HOME "%ProgramFiles(x86)%\Microsoft SDKs\Azure\Storage Emulator"
RUN setx /M PATH "%PATH%;%AZURE_STORAGE_EMULATOR_HOME%"

WORKDIR "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator"

# let azure emulator listen on 20000,20001,20002. Nginx will redirect requests to these ports.
RUN powershell -NoProfile -Command \				
        "(Get-Content .\AzureStorageEmulator.exe.config) -replace ':10000/',':20000/' | Out-File -Encoding utf8 .\AzureStorageEmulator.exe.config"; \
        "(Get-Content .\AzureStorageEmulator.exe.config) -replace ':10001/',':20001/' | Out-File -Encoding utf8 .\AzureStorageEmulator.exe.config"; \
        "(Get-Content .\AzureStorageEmulator.exe.config) -replace ':10002/',':20002/' | Out-File -Encoding utf8 .\AzureStorageEmulator.exe.config";

# initialize will discover and initialize with the local db installed in previous step		
RUN AzureStorageEmulator.exe init

# These are ports exposed via Nginx
EXPOSE 10000 10001 10002

#Nginx - In windows nginx cannot be run as a daemon. so start nginx as entrypoint.

WORKDIR "C:\nginx"
ADD nginx-1.12.0.zip .
RUN powershell -NoProfile -Command \
	Expand-Archive nginx-1.12.0.zip . ;
ADD entry.cmd 'C:\entry.cmd'

WORKDIR "C:\nginx\nginx-1.12.0"
ENTRYPOINT C:\entry.cmd

