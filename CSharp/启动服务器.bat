@echo off
chcp 65001 >nul
echo ========================================
echo      MCP Server 启动程序
echo ========================================
echo.

cd /d "%~dp0McpServerConsole"

echo [编译] 正在编译项目...
dotnet build
if errorlevel 1 (
    echo.
    echo [错误] 编译失败
    pause
    exit /b 1
)

echo.
echo [启动] 正在启动 MCP Server...
echo.
dotnet run --no-build

pause

