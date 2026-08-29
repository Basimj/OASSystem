param(
    [string]$BootstrapVersion = "5.3.8",
    [string]$FontAwesomeVersion = "7.3.1"
)

$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$UiLibRoot = Split-Path -Parent $ScriptDirectory
$WwwRoot = Join-Path $UiLibRoot "wwwroot"
$LibRoot = Join-Path $WwwRoot "lib"
$TempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("oas-ui-libs-" + [Guid]::NewGuid().ToString("N"))

$BootstrapRoot = Join-Path $LibRoot "bootstrap"
$FontAwesomeRoot = Join-Path $LibRoot "font-awesome"

function Assert-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file was not created: $Path"
    }
}

try {
    New-Item -ItemType Directory -Path $TempRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $LibRoot -Force | Out-Null

    Write-Host "Downloading Bootstrap $BootstrapVersion..." -ForegroundColor Cyan
    $bootstrapZip = Join-Path $TempRoot "bootstrap.zip"
    $bootstrapUrl = "https://github.com/twbs/bootstrap/releases/download/v$BootstrapVersion/bootstrap-$BootstrapVersion-dist.zip"
    Invoke-WebRequest -Uri $bootstrapUrl -OutFile $bootstrapZip
    $bootstrapExtract = Join-Path $TempRoot "bootstrap"
    Expand-Archive -Path $bootstrapZip -DestinationPath $bootstrapExtract -Force
    $bootstrapPackage = Join-Path $bootstrapExtract ("bootstrap-$BootstrapVersion-dist")

    Remove-Item $BootstrapRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Join-Path $BootstrapRoot "css") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $BootstrapRoot "js") -Force | Out-Null
    Copy-Item (Join-Path $bootstrapPackage "css/bootstrap.min.css") (Join-Path $BootstrapRoot "css/bootstrap.min.css")
    Copy-Item (Join-Path $bootstrapPackage "css/bootstrap.rtl.min.css") (Join-Path $BootstrapRoot "css/bootstrap.rtl.min.css")
    Copy-Item (Join-Path $bootstrapPackage "js/bootstrap.bundle.min.js") (Join-Path $BootstrapRoot "js/bootstrap.bundle.min.js")

    # The dist archive does not always contain the root LICENSE file, so fetch it explicitly.
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/twbs/bootstrap/v$BootstrapVersion/LICENSE" -OutFile (Join-Path $BootstrapRoot "LICENSE")

    Write-Host "Downloading Font Awesome Free $FontAwesomeVersion..." -ForegroundColor Cyan
    $faZip = Join-Path $TempRoot "fontawesome.zip"
    $faUrl = "https://github.com/FortAwesome/Font-Awesome/releases/download/$FontAwesomeVersion/fontawesome-free-$FontAwesomeVersion-web.zip"
    Invoke-WebRequest -Uri $faUrl -OutFile $faZip
    $faExtract = Join-Path $TempRoot "fontawesome"
    Expand-Archive -Path $faZip -DestinationPath $faExtract -Force
    $faPackage = Join-Path $faExtract ("fontawesome-free-$FontAwesomeVersion-web")

    Remove-Item $FontAwesomeRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Join-Path $FontAwesomeRoot "css") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $FontAwesomeRoot "webfonts") -Force | Out-Null
    Copy-Item (Join-Path $faPackage "css/all.min.css") (Join-Path $FontAwesomeRoot "css/all.min.css")
    Copy-Item (Join-Path $faPackage "webfonts/*") (Join-Path $FontAwesomeRoot "webfonts") -Recurse
    Copy-Item (Join-Path $faPackage "LICENSE.txt") (Join-Path $FontAwesomeRoot "LICENSE.txt")

    Assert-File (Join-Path $BootstrapRoot "css/bootstrap.min.css")
    Assert-File (Join-Path $BootstrapRoot "js/bootstrap.bundle.min.js")
    Assert-File (Join-Path $FontAwesomeRoot "css/all.min.css")

    $fontCount = @(Get-ChildItem (Join-Path $FontAwesomeRoot "webfonts") -File).Count
    if ($fontCount -lt 1) { throw "Font Awesome webfonts were not installed." }

    Write-Host "Installed Bootstrap $BootstrapVersion and Font Awesome Free $FontAwesomeVersion." -ForegroundColor Green
    Write-Host "Font files: $fontCount" -ForegroundColor Green
    Write-Host "App.razor only needs: _content/OAS.UiLib/css/oas-ui-bundle.css" -ForegroundColor Green
}
finally {
    Remove-Item $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
