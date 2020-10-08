# The `FROM` instruction specifies the base image. You are
# extending the `microsoft/aspnet` image.

FROM microsoft/aspnet:4.6.2

#Download status monitor
ADD http://go.microsoft.com/fwlink/?LinkID=522371&clcid=0x409 ApplicationInsightsAgent.msi

#Check status of msiserver
RUN powershell -NoProfile -Command \
	start-service -Name msiserver;

#Check status of SM install
RUN powershell -NoProfile -Command \	
	"Get-ItemProperty 'hklm:\\SYSTEM\\CurrentControlSet\\Services\\W3SVC'"
	
RUN ["powershell", "Start-Process", "msiexec.exe", "-ArgumentList", "'/i C:\\ApplicationInsightsAgent.msi /qn /LIME C:\\sminstall.txt'", "-NoNewWindow", "-Wait"]
RUN ["powershell","Get-Content", "C:\\sminstall.txt"]

#Restart iis to take status monitor effect
RUN iisreset /restart

#Check status of SM install
RUN powershell -NoProfile -Command \	
	"Get-ItemProperty 'hklm:\\SYSTEM\\CurrentControlSet\\Services\\W3SVC'"


COPY . /inetpub/wwwroot	