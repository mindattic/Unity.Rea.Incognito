<#
.SYNOPSIS
    Launches a playable Incognito for development, muted.
.DESCRIPTION
    Starts Builds/Windows/Incognito.exe (building it with tools/build.ps1 first when it is missing or
    -Build is given) with MINDATTIC_CLI_MUTED=1 and -muted, so a CLI or agent launching the game never
    plays sound. This is not tools/build.ps1 or tools/test.ps1's batch -batchmode runs: it starts a real,
    on-screen player. The mute is Unity.Shared's session mute: it lasts for this run only and never changes
    the player's saved settings.json (Incognito.Audio.SessionAudio turns it into a silenced AudioListener,
    U.INC-T40). Pass -Sound to launch with audio. Any other arguments go to the player (for example
    -screen-fullscreen 0). The player's log is Logs/play.log.
.PARAMETER Build
    Rebuild the player with tools/build.ps1 before launching.
.PARAMETER Sound
    Launch with audio (no MINDATTIC_CLI_MUTED, no -muted).
.PARAMETER Wait
    Wait for the player to exit and return its exit code.
.PARAMETER Online
    Open the online scene (Host, Join by IP, Join by code) instead of the title (passes -online, U.INC-T43).
.PARAMETER Headless
    Run with no window through ..\Unity.Shared\tools\Start-Headless.ps1 (Unity.Shared's --headless mode, U.INC-T54):
    muted, in the background, straight into play, and it waits for the MINDATTIC_HEADLESS_READY log line.
    The player keeps running afterwards; stop it yourself.
#>
param(
    [switch]$Build,
    [switch]$Sound,
    [switch]$Wait,
    [switch]$Headless,
    [switch]$Online,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$PlayerArgs
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$exe = Join-Path $root 'Builds/Windows/Incognito.exe'
$log = Join-Path $root 'Logs/play.log'

if ($Build -or -not (Test-Path $exe)) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tools/build.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null

if ($Headless) {
    # U.INC-T54: the shared launcher adds --headless -batchmode -nographics and the -logFile.
    $hArgs = @(); if ($Online) { $hArgs += '-online' }; if ($PlayerArgs) { $hArgs += $PlayerArgs }
    & (Join-Path $root '..\Unity.Shared\tools\Start-Headless.ps1') -Exe $exe -LogFile $log -Wait @hArgs
    exit $LASTEXITCODE
}

$arguments = @('-logFile', "`"$log`"")
if (-not $Sound) { $arguments += '-muted' }
if ($Online) { $arguments += '-online' }
if ($PlayerArgs) { $arguments += $PlayerArgs }

# The child inherits this process's environment; set the mute only around the launch.
$previous = $env:MINDATTIC_CLI_MUTED
if ($Sound) { Remove-Item Env:MINDATTIC_CLI_MUTED -ErrorAction SilentlyContinue } else { $env:MINDATTIC_CLI_MUTED = '1' }
try {
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
} finally {
    if ($null -eq $previous) { Remove-Item Env:MINDATTIC_CLI_MUTED -ErrorAction SilentlyContinue } else { $env:MINDATTIC_CLI_MUTED = $previous }
}

Write-Host ("Launched Incognito (pid {0}, {1}); log {2}" -f $process.Id, $(if ($Sound) { 'with sound' } else { 'muted' }), $log)
if ($Wait) {
    $process.WaitForExit()
    exit $process.ExitCode
}
exit 0
