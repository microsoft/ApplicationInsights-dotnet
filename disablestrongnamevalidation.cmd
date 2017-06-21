set VsDevCmd ="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat"
if not exist %VsDevCmd% echo "Error: '%VsDevCmd%' does not exist."
%VsDevCmd% sn -Vr *,31bf3856ad364e35
