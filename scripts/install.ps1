# Compile Calibre en Release et copie la DLL dans le dossier des rulesets de lazer.
# Usage : powershell -ExecutionPolicy Bypass -File scripts\install.ps1
# Redémarrer lazer ensuite : les rulesets ne sont chargés qu'au démarrage.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'osu.Game.Rulesets.Calibre\osu.Game.Rulesets.Calibre.csproj'
$rulesets = Join-Path $env:APPDATA 'osu\rulesets'

# Utilise le SDK du PATH s'il existe, sinon une installation par utilisateur (dotnet-install.ps1).
$dotnet = 'dotnet'
$userDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (& dotnet --list-sdks 2>$null) -and (Test-Path $userDotnet)) { $dotnet = $userDotnet }

& $dotnet build $project -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "La compilation a échoué." }

$dll = Join-Path $root 'osu.Game.Rulesets.Calibre\bin\Release\net10.0\osu.Game.Rulesets.Calibre.dll'

New-Item -ItemType Directory -Force $rulesets | Out-Null

# lazer renomme en .dll.broken un ruleset qui l'a fait planter : on retire l'ancienne copie désactivée.
$broken = Join-Path $rulesets 'osu.Game.Rulesets.Calibre.dll.broken'
if (Test-Path $broken) { Remove-Item $broken }

Copy-Item $dll $rulesets -Force
Write-Host "Calibre installé dans $rulesets"
