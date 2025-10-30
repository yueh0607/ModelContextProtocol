# 快速测试脚本 - 使用简单的 HTTP 请求测试服务器

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MCP Server 快速测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:8767"

# 测试服务器是否在线
Write-Host "[1] 测试服务器连接..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $baseUrl -Method GET -TimeoutSec 5
    Write-Host "✓ 服务器在线" -ForegroundColor Green
    Write-Host $response.Content
} catch {
    Write-Host "✗ 无法连接到服务器" -ForegroundColor Red
    Write-Host "请确保服务器正在运行: .\start-server.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# 测试初始化
Write-Host "[2] 测试初始化..." -ForegroundColor Yellow
$initBody = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{
            name = "Quick Test"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $initBody -ContentType "application/json" -TimeoutSec 10
    Write-Host "✓ 初始化成功" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "✗ 初始化失败: $_" -ForegroundColor Red
}

Write-Host ""

# 测试工具列表
Write-Host "[3] 测试工具列表..." -ForegroundColor Yellow
$toolsBody = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/list"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $toolsBody -ContentType "application/json" -TimeoutSec 10
    Write-Host "✓ 获取工具列表成功" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "✗ 获取工具列表失败: $_" -ForegroundColor Red
}

Write-Host ""

# 测试 echo 工具
Write-Host "[4] 测试 echo 工具..." -ForegroundColor Yellow
$echoBody = @{
    jsonrpc = "2.0"
    id = 3
    method = "tools/call"
    params = @{
        name = "echo"
        arguments = @{
            message = "Hello from PowerShell!"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $echoBody -ContentType "application/json" -TimeoutSec 10
    Write-Host "✓ Echo 工具调用成功" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "✗ Echo 工具调用失败: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "测试完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

