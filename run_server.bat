@echo off
dotnet "D:\Wizard101_client_04_2019\wizard101\Greyrose\bin\Release\net8.0\win-x64\Greyrose.dll" --console --full-player-blob --fix-prop-count 1 > "D:\Wizard101_client_04_2019\server_run.log" 2> "D:\Wizard101_client_04_2019\server_err.log"
