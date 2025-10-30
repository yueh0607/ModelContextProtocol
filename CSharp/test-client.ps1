# 测试 MCP Server 脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MCP Server HTTP 测试客户端" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 切换到测试客户端目录
$clientPath = Join-Path $PSScriptRoot "McpTestClient"
Set-Location $clientPath

# 检查是否已编译
if (-not (Test-Path "bin\Debug\net8.0\McpTestClient.dll")) {
    Write-Host "[编译] 正在编译项目..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[错误] 编译失败" -ForegroundColor Red
        exit 1
    }
}

# 运行测试
Write-Host "[测试] 正在运行测试..." -ForegroundColor Green
Write-Host ""

if ($args.Count -gt 0 -and ($args[0] -eq "-i" -or $args[0] -eq "--interactive")) {
    dotnet run --no-build -- --interactive
} else {
    dotnet run --no-build
}

