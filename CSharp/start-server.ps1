# 启动 MCP Server 脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  启动 MCP Server HTTP 调试程序" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 切换到服务器目录
$serverPath = Join-Path $PSScriptRoot "McpServerConsole"
Set-Location $serverPath

# 检查是否已编译
if (-not (Test-Path "bin\Debug\net8.0\McpServerConsole.dll")) {
    Write-Host "[编译] 正在编译项目..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[错误] 编译失败" -ForegroundColor Red
        exit 1
    }
}

# 启动服务器
Write-Host "[启动] 正在启动 MCP Server..." -ForegroundColor Green
Write-Host ""
dotnet run --no-build

