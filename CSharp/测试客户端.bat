@echo off
chcp 65001 >nul
echo ========================================
echo      MCP 测试客户端
echo ========================================
echo.

cd /d "%~dp0McpTestClient"

echo [编译] 正在编译项目...
dotnet build
if errorlevel 1 (
    echo.
    echo [错误] 编译失败
    pause
    exit /b 1
)

echo.
echo [测试] 正在运行测试...
echo.
dotnet run --no-build

echo.
pause

