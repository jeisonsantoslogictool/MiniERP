@echo off
chcp 65001 >nul
cd /d E:\repos\MiniERP
if not exist evidencia mkdir evidencia
echo === Evidencia de pruebas - %DATE% %TIME% === > evidencia\dotnet-test.txt
git rev-parse HEAD >> evidencia\dotnet-test.txt
git describe --tags >> evidencia\dotnet-test.txt 2>&1
dotnet --version >> evidencia\dotnet-test.txt
echo. >> evidencia\dotnet-test.txt
echo Comando: dotnet test --collect:"XPlat Code Coverage" >> evidencia\dotnet-test.txt
dotnet test --collect:"XPlat Code Coverage" --results-directory evidencia\TestResults --logger "trx;LogFileName=resultados.trx" >> evidencia\dotnet-test.txt 2>&1
type evidencia\dotnet-test.txt
echo.
echo === LISTO. Deja esta ventana abierta y avisa en el chat. ===
pause
