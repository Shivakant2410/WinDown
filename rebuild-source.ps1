param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "ClamDown-Windows")
)
$ErrorActionPreference = "Stop"
$parts = 1..5 | ForEach-Object {
    $path = Join-Path $PSScriptRoot ("source-archive.part{0:D2}.b64" -f $_)
    if (-not (Test-Path $path)) { throw "Missing archive part: $path" }
    (Get-Content -LiteralPath $path -Raw).Trim()
}
$bytes = [Convert]::FromBase64String(($parts -join ""))
$gzipStream = [IO.MemoryStream]::new($bytes)
$decompressor = [IO.Compression.GzipStream]::new($gzipStream, [IO.Compression.CompressionMode]::Decompress)
$zipStream = [IO.MemoryStream]::new()
$decompressor.CopyTo($zipStream)
$decompressor.Dispose()
$gzipStream.Dispose()
$zipPath = Join-Path $PSScriptRoot "ClamDown-Windows-starter.zip"
[IO.File]::WriteAllBytes($zipPath, $zipStream.ToArray())
$zipStream.Dispose()
if (Test-Path $OutputDirectory) { Remove-Item -LiteralPath $OutputDirectory -Recurse -Force }
Expand-Archive -LiteralPath $zipPath -DestinationPath $PSScriptRoot -Force
Write-Host "Extracted project to $OutputDirectory"
