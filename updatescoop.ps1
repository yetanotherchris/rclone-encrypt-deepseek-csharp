param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$repo = "yetanotherchris/rclone-encrypt-deepseek-csharp"
$exePath = Join-Path $PSScriptRoot "artifacts" "rclone-encrypt-deepseek-csharp-windows-amd64.exe"

if (-not (Test-Path $exePath)) {
    throw "Unable to locate rclone-encrypt-deepseek-csharp-windows-amd64.exe at $exePath"
}

$hash = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash.ToLower()

Write-Host "Hash: $hash"

$url = "https://github.com/$repo/releases/download/v$Version/rclone-encrypt-deepseek-csharp-windows-amd64.exe"

$manifestPath = Join-Path $PSScriptRoot "rclone-encrypt-deepseek-csharp.json"

$manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json

$manifest.version = $Version
$manifest.architecture."64bit".url = $url
$manifest.architecture."64bit".hash = $hash

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -NoNewline

Write-Host "Updated rclone-encrypt-deepseek-csharp.json to v$Version"
