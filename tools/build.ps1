param(
    [string]$Payload = "calisma/build/payload.json",
    [string]$Output = "dist/Hades-Turkce-Yama-0.1.0-onizleme.exe",
    [switch]$LocalArtwork
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# derleyicisi bulunamadı.' }
$payloadPath = [IO.Path]::GetFullPath((Join-Path $project $Payload))
$outputPath = [IO.Path]::GetFullPath((Join-Path $project $Output))
if (-not $outputPath.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Çıktı proje klasöründe olmalı.' }
if (-not (Test-Path -LiteralPath $payloadPath)) { throw 'Önce tools/package.py ile doğrulanmış paket oluşturun.' }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputPath) | Out-Null
$arguments = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/utf8output',
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll',
    ('/win32manifest:' + (Join-Path $project 'installer/app.manifest')),
    ('/resource:' + $payloadPath + ',payload.json'), ('/out:' + $outputPath),
    (Join-Path $project 'installer/Engine.cs'), (Join-Path $project 'installer/Discovery.cs'), (Join-Path $project 'installer/Cleanup.cs'), (Join-Path $project 'installer/InstallerForm.cs'), (Join-Path $project 'installer/Program.cs'))
if ($LocalArtwork) {
    foreach ($slide in 1..4) {
        $artPath = Join-Path $project ('calisma/assets/slide' + $slide + '.jpg')
        if (-not (Test-Path -LiteralPath $artPath)) { throw 'Local artwork is missing.' }
        $arguments += ('/resource:' + $artPath + ',art.slide' + $slide + '.jpg')
    }
}
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
$hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($outputPath + '.sha256', $hash + '  ' + [IO.Path]::GetFileName($outputPath) + "`n", (New-Object Text.UTF8Encoding($false)))
Write-Output ('Derlendi: ' + $outputPath)
Write-Output ('SHA256: ' + $hash)
if ($LocalArtwork) {
    [IO.File]::WriteAllText($outputPath + '.LOCAL-ONLY.txt', 'Contains official screenshots for local preview. Not cleared for redistribution.', (New-Object Text.UTF8Encoding($false)))
}
