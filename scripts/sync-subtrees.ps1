#Requires -Version 7
param([switch]$Push)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
git config core.hooksPath .githooks

$pairs = @(
    @{ Prefix = 'src/TShockAPI'; Remote = 'tshock'; Ref = 'tml' }
    @{ Prefix = 'src/TerrariaApi.Server'; Remote = 'tsapi'; Ref = 'tml' }
)

$drift = @()
foreach ($p in $pairs) {
    git fetch $p.Remote $p.Ref
    $hostTree = git rev-parse "HEAD:$($p.Prefix)"
    $remoteTree = git rev-parse "$($p.Remote)/$($p.Ref)^{tree}"
    if ($hostTree -eq $remoteTree) {
        Write-Host "ok $($p.Prefix)"
        continue
    }

    Write-Host "drift $($p.Prefix) host=$hostTree remote=$remoteTree"
    $drift += $p
}

if (-not $Push) {
    if ($drift.Count -gt 0) { exit 1 }
    exit 0
}

foreach ($p in $drift) {
    git subtree push --prefix=$($p.Prefix) $p.Remote $p.Ref
}

foreach ($p in $drift) {
    git fetch $p.Remote $p.Ref
    $hostTree = git rev-parse "HEAD:$($p.Prefix)"
    $remoteTree = git rev-parse "$($p.Remote)/$($p.Ref)^{tree}"
    if ($hostTree -ne $remoteTree) {
        throw "push did not sync $($p.Prefix)"
    }
    Write-Host "ok $($p.Prefix)"
}
