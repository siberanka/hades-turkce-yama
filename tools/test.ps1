$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Push-Location $project
try {
    py -3 -m unittest discover -s tests -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) { throw 'Python testleri başarısız.' }
    $work = if ($env:HADES_TR_CALISMA) { $env:HADES_TR_CALISMA } else { Join-Path $project 'calisma' }
    $testRoot = Join-Path $work ('tests/' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $testRoot) | Out-Null
    $testExe = Join-Path $work 'tests/InstallerTests.exe'
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
    & $compiler /nologo /utf8output /target:exe /reference:System.Web.Extensions.dll ('/out:' + $testExe) (Join-Path $project 'installer/Engine.cs') (Join-Path $project 'installer/Discovery.cs') (Join-Path $project 'tests/InstallerTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test derlemesi başarısız.' }
    & $testExe $testRoot
    if ($LASTEXITCODE -ne 0) { throw 'Kurulum testleri başarısız.' }
} finally { Pop-Location }
