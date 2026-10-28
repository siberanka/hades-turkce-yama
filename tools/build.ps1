param(
    [string]$Payload = "calisma/build/payload.json",
    [string]$Output = "dist/yerel/Hades-Turkce-Yama-1.0.2.exe",
    [switch]$LocalArtwork,
    [switch]$Release
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# derleyicisi bulunamadı.' }
$payloadPath = [IO.Path]::GetFullPath((Join-Path $project $Payload))
$outputPath = [IO.Path]::GetFullPath((Join-Path $project $Output))
if (-not $outputPath.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Çıktı proje klasöründe olmalı.' }
if (-not (Test-Path -LiteralPath $payloadPath)) { throw 'Önce tools/package.py ile doğrulanmış paket oluşturun.' }
if ($Release -and $LocalArtwork) { throw 'Yayın derlemesi yerel oyun görselleri içeremez.' }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputPath) | Out-Null
$artRoot = Join-Path $project 'calisma/build/artwork'
New-Item -ItemType Directory -Force -Path $artRoot | Out-Null
$generator = Join-Path $artRoot 'ArtworkGenerator.exe'
$iconPath = Join-Path $artRoot 'hades-tr.ico'
& $compiler /nologo /target:exe /reference:System.Drawing.dll ('/out:' + $generator) (Join-Path $project 'installer/Artwork.cs') (Join-Path $project 'tools/ArtworkGenerator.cs')
if ($LASTEXITCODE -ne 0) { throw 'Simge üreticisi derlenemedi.' }
& $generator $iconPath
if ($LASTEXITCODE -ne 0) { throw 'Simge üretilemedi.' }
$arguments = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/utf8output',
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll',
    ('/win32manifest:' + (Join-Path $project 'installer/app.manifest')),
    ('/win32icon:' + $iconPath), ('/resource:' + $iconPath + ',art.icon'),
    ('/resource:' + $payloadPath + ',payload.json'), ('/out:' + $outputPath),
    (Join-Path $project 'installer/Engine.cs'), (Join-Path $project 'installer/TextCatalog.cs'), (Join-Path $project 'installer/Discovery.cs'), (Join-Path $project 'installer/Cleanup.cs'), (Join-Path $project 'installer/Artwork.cs'), (Join-Path $project 'installer/InstallerForm.cs'), (Join-Path $project 'installer/Program.cs'))
$musicPath = Join-Path $project 'music.mp3'
$hasMusic = -not $Release -and (Test-Path -LiteralPath $musicPath -PathType Leaf)
if ($hasMusic) { $arguments += ('/resource:' + $musicPath + ',local.music.mp3') }
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
if ($LocalArtwork -or $hasMusic) {
    [IO.File]::WriteAllText($outputPath + '.LOCAL-ONLY.txt', 'Yalnız yerel kullanım. Yerel müzik veya oyun görselleri içerir; GitHub sürümüne yüklemeyin.', (New-Object Text.UTF8Encoding($false)))
} elseif (Test-Path -LiteralPath ($outputPath + '.LOCAL-ONLY.txt')) {
    Remove-Item -LiteralPath ($outputPath + '.LOCAL-ONLY.txt')
}
Write-Output ('Yerel müzik gömüldü: ' + $hasMusic + '; yayın derlemesi: ' + $Release)
