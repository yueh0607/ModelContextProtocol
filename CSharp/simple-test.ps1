# Simple MCP Server Test

$baseUrl = "http://localhost:8767"

Write-Host "Testing MCP Server at $baseUrl"
Write-Host ""

# Test 1: Server Connection
Write-Host "[1] Testing server connection..."
try {
    $response = Invoke-WebRequest -Uri $baseUrl -Method GET -TimeoutSec 5
    Write-Host "SUCCESS: Server is online" -ForegroundColor Green
    Write-Host $response.Content
} catch {
    Write-Host "FAILED: Cannot connect to server" -ForegroundColor Red
    Write-Host "Error: $_"
    exit 1
}

Write-Host ""

# Test 2: Initialize
Write-Host "[2] Testing initialize..."
$initBody = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{
            name = "Simple Test"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $initBody -ContentType "application/json" -TimeoutSec 10
    Write-Host "SUCCESS: Initialize" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "FAILED: Initialize" -ForegroundColor Red
    Write-Host "Error: $_"
}

Write-Host ""

# Test 3: List Tools
Write-Host "[3] Testing tools/list..."
$toolsBody = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/list"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $toolsBody -ContentType "application/json" -TimeoutSec 10
    Write-Host "SUCCESS: List tools" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "FAILED: List tools" -ForegroundColor Red
    Write-Host "Error: $_"
}

Write-Host ""

# Test 4: Call Echo Tool
Write-Host "[4] Testing echo tool..."
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
    Write-Host "SUCCESS: Echo tool" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
} catch {
    Write-Host "FAILED: Echo tool" -ForegroundColor Red
    Write-Host "Error: $_"
}

Write-Host ""
Write-Host "Test completed" -ForegroundColor Cyan

