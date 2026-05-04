@echo off
cd /d "%~dp0"
echo Publishing TransFund Inventory...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true --output ./publish
copy /Y "TransFundInventory.db" "publish\TransFundInventory.db"
echo Zipping files...
powershell -Command "Compress-Archive -Path './publish/*' -DestinationPath './TransFundInventory_Final_V6_Totals.zip' -Force"
echo Done!
pause
