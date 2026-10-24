param(
    [Parameter(Mandatory = $true)]
    [string]$OyunKlasoru,
    [string]$Surum = "1.0.0"
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Push-Location $project
try {
    $python = Get-Command py -ErrorAction SilentlyContinue
    if ($python) { $pythonArgs = @('-3', '-X', 'utf8') } else { $python = Get-Command python -ErrorAction Stop; $pythonArgs = @('-X', 'utf8') }

    function Invoke-Adim([string]$Ad, [string[]]$Arguments) {
        Write-Output ('==> ' + $Ad)
        & $python.Source @pythonArgs @Arguments
        if ($LASTEXITCODE -ne 0) { throw ($Ad + ' başarısız.') }
    }

    Invoke-Adim 'Oyun metinlerinin envanteri' @('tools/catalog.py', $OyunKlasoru)
    Invoke-Adim 'Türkçe karakter font eşlemesi' @('tools/font_patch.py', $OyunKlasoru)
    Invoke-Adim 'Çeviri doğrulaması' @('tools/validate.py')
    Invoke-Adim 'Yama paketi' @('tools/package.py', '--version', $Surum)
    Invoke-Adim 'Paket doğrulaması' @('tools/verify_payload.py')
    Write-Output '==> Kurulum aracı derlemesi'
    & (Join-Path $PSScriptRoot 'build.ps1') -Output ('dist/Hades-Turkce-Yama-' + $Surum + '.exe')
} finally { Pop-Location }
