#Requires -Version 7
$ErrorActionPreference = 'Stop'
$Tml = 'D:\SteamLibrary\steamapps\common\tModLoader'
$Repo = Split-Path $PSScriptRoot -Parent
$Root = 'F:\Temp\tshockloader-p5'
$TimeoutSec = 180
$Port = 7779
$RestPort = 7879
$RestToken = 'p5verify'

function Stop-Tml {
    Get-Process tModLoader, dotnet -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*tModLoader*' -or $_.Path -like '*tModLoader*' } |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep 1
}

function Wait-Listen([int]$Port, [int]$Seconds, [Diagnostics.Process]$Process) {
    $until = [datetime]::UtcNow.AddSeconds($Seconds)
    while ([datetime]::UtcNow -lt $until) {
        if ($Process.HasExited) { throw "tModLoader exited $($Process.ExitCode) before port $Port" }
        try {
            $c = [Net.Sockets.TcpClient]::new()
            $c.Connect('127.0.0.1', $Port)
            $c.Dispose()
            return
        }
        catch { Start-Sleep -Milliseconds 400 }
    }
    throw "timeout ${Seconds}s waiting for port $Port"
}

function Start-Dedicated {
    $arg = "tModLoader.dll -server -config `"$Root\serverconfig.txt`" -tmlsavedirectory `"$Root`" -instancepath `"$Root\instance`" -port $Port"
    return Start-Process dotnet -ArgumentList $arg -WorkingDirectory $Tml -PassThru -WindowStyle Hidden
}

function Invoke-Tshock([string]$Path) {
    return Invoke-RestMethod "http://127.0.0.1:$RestPort$Path`?token=$RestToken"
}

function Stop-Dedicated([Diagnostics.Process]$Process) {
    Invoke-RestMethod "http://127.0.0.1:$RestPort/v2/server/off?token=$RestToken&confirm=true" | Out-Null
    if (-not $Process.WaitForExit(60000)) { throw 'off did not exit in 60s' }
}

Stop-Tml
dotnet build "$Repo\src\tShockLoader\tShockLoader.csproj" -p:BuildMod=true | Out-Host

$tmod = Join-Path $env:USERPROFILE 'Documents\My Games\Terraria\tModLoader\Mods\tShockLoader.tmod'
if (-not (Test-Path $tmod)) {
    $tmod = Join-Path $env:USERPROFILE 'Documents\My games\Terraria\tModLoader\Mods\tShockLoader.tmod'
}
if (-not (Test-Path $tmod)) { throw 'packed tmod not found' }

$dist = Join-Path $Root 'dist\tShockLoader-0.1.0'
$data = Join-Path $Root 'instance\tshock'
$plugins = Join-Path $Root 'instance\ServerPlugins'
if (Test-Path $Root) { Remove-Item $Root -Recurse -Force }
New-Item -ItemType Directory -Path $dist, "$Root\Mods", "$Root\saves\Worlds", $data | Out-Null
Copy-Item $tmod "$dist\tShockLoader.tmod"
Copy-Item "$Repo\LICENSE", "$Repo\NOTICE", "$Repo\docs\install.md", "$Repo\docs\versions.md", "$Repo\docs\compatibility.md" $dist
Copy-Item "$dist\tShockLoader.tmod" "$Root\Mods\tShockLoader.tmod"
Set-Content "$Root\Mods\enabled.json" '["tShockLoader"]' -NoNewline
Copy-Item 'F:\Temp\tshockloader-p0\saves\Worlds\p0.wld', 'F:\Temp\tshockloader-p0\saves\Worlds\p0.twld' "$Root\saves\Worlds\"
@"
maxplayers=2
worldname=p0
world=$Root\saves\Worlds\p0.wld
autocreate=1
difficulty=0
port=$Port
upnp=0
"@ | Set-Content "$Root\serverconfig.txt"
@'
{
  "Settings": {
    "RestApiEnabled": true,
    "RestApiPort": 7879,
    "ApplicationRestTokens": {
      "p5verify": { "Username": "ServerAdmin", "UserGroupName": "superadmin" }
    }
  }
}
'@ | Set-Content "$data\config.json"

$p = Start-Dedicated
try {
    Wait-Listen $Port $TimeoutSec $p
    Wait-Listen $RestPort 30 $p
    $status = Invoke-Tshock '/v2/server/status'
    if ("$($status.tshockversion)" -notmatch '5\.2\.3') { throw "status tshockversion=$($status.tshockversion)" }
    Stop-Dedicated $p
}
finally {
    if (-not $p.HasExited) { Stop-Tml }
}

if (-not (Test-Path "$data\tshock.sqlite")) { throw 'tshock.sqlite missing' }
if (Test-Path "$plugins\TShockAPI.dll") { throw 'core dll copied into ServerPlugins' }
$tshockLog = Get-ChildItem "$data\logs\*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$logText = Get-Content $tshockLog.FullName -Raw
if ($logText -notmatch 'TShock 5\.2\.3') { throw 'TShock 5.2.3 not in log' }
if ($logText.Contains($RestToken)) { throw 'REST token leaked into TShock log' }
Set-Content "$data\p5-marker.txt" 'keep'
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
Set-Content "$plugins\keep-me.txt" 'keep'

Copy-Item "$dist\tShockLoader.tmod" "$Root\Mods\tShockLoader.tmod" -Force
$p = Start-Dedicated
try {
    Wait-Listen $Port $TimeoutSec $p
    if (-not (Test-Path "$data\p5-marker.txt")) { throw 'update overwrote tshock data' }
    if (-not (Test-Path "$plugins\keep-me.txt")) { throw 'update overwrote ServerPlugins' }
    Invoke-Tshock '/v2/server/status' | Out-Null
    Stop-Dedicated $p
}
finally {
    if (-not $p.HasExited) { Stop-Tml }
}

Compress-Archive -Path "$dist\*" -DestinationPath "$Root\dist\tShockLoader-0.1.0.zip" -Force
Write-Host "P5 ok zip=$Root\dist\tShockLoader-0.1.0.zip sqlite=$data\tshock.sqlite"
