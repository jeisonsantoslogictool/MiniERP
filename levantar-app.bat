@echo off
cd /d E:\repos\MiniERP
echo Levantando el Mini ERP... (no cierres esta ventana mientras se hacen las pruebas de roles)
dotnet run --project src\MiniERP.Web
pause
