<#
.SYNOPSIS
  Runs the final parity tour (U.INC-T35): Assets/Rea.Incognito/Tests/PlayMode/ParityTourPlayTests.cs plays
  the title, Bleecher Street, 2nd Avenue and the shop through a virtual keyboard and mouse and exercises
  every mechanic in docs/RECONSTRUCTION.md, writing screenshots and a report of the checklist rows it
  proved. Run with the Editor closed. Muted by default, like play.ps1.
.DESCRIPTION
  Unity batch mode with a graphics device (not -nographics), so the tour can screenshot:
    Logs/parity-tour/NN-tag.png   the tour's own PNGs (camera + HUD in one 1920x1080 frame)
    Logs/parity-tour.txt          one line per checklist row the run proved
    (with -Capture) the same moments through MindAttic.Media.Capture, switched on via MINDATTIC_CAPTURE_EDITOR.
    Its grab waits for the end of a rendered frame, which a -batchmode Editor never reaches (it logs
    "Screenshot ... failed: The game is shutting down."), so it is opt-in; the PNGs above are the batch record.
  The run is muted: MINDATTIC_CLI_MUTED=1 and -muted (Unity.Shared's session mute, which
  Incognito.Audio.SessionAudio turns into a silenced AudioListener). -Sound opts out.
.PARAMETER Sound
  Run with audio (no MINDATTIC_CLI_MUTED, no -muted).
.PARAMETER Capture
  Also enable MindAttic.Media.Capture for the tour's TakeScreenshot calls (see above).
.EXAMPLE
  powershell -File tools/verify-parity.ps1
#>
param([switch]$Sound, [switch]$Capture)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$results = Join-Path $root 'TestResults-ParityTour.xml'
Remove-Item -LiteralPath $results -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $root 'Logs/parity-tour') -Recurse -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $root 'Logs/parity-tour.txt') -ErrorAction SilentlyContinue

$previousMute = $env:MINDATTIC_CLI_MUTED
$previousCapture = $env:MINDATTIC_CAPTURE_EDITOR
try {
    if ($Sound) { Remove-Item Env:MINDATTIC_CLI_MUTED -ErrorAction SilentlyContinue } else { $env:MINDATTIC_CLI_MUTED = '1' }
    # "screen 3600": the interval never elapses, so only the tour's own TakeScreenshot calls fire.
    if ($Capture) { $env:MINDATTIC_CAPTURE_EDITOR = 'screen 3600' }
    $extra = '-testFilter Incognito.Tests.ParityTourPlayTests'
    if (-not $Sound) { $extra += ' -muted' }
    & "$PSScriptRoot/unity.ps1" -Log parity-tour -Graphics -TestPlatform PlayMode -TestResults $results -Extra $extra
    $code = $LASTEXITCODE
} finally {
    if ($null -eq $previousMute) { Remove-Item Env:MINDATTIC_CLI_MUTED -ErrorAction SilentlyContinue } else { $env:MINDATTIC_CLI_MUTED = $previousMute }
    if ($null -eq $previousCapture) { Remove-Item Env:MINDATTIC_CAPTURE_EDITOR -ErrorAction SilentlyContinue } else { $env:MINDATTIC_CAPTURE_EDITOR = $previousCapture }
}

if (-not (Test-Path -LiteralPath $results)) { throw "$results was not written; the tour produced no results." }
[xml]$xml = Get-Content -LiteralPath $results -Raw
$run = $xml.'test-run'
Write-Host ("  Parity tour: {0}/{1} passed" -f $run.passed, $run.total) -ForegroundColor Cyan
$rows = Join-Path $root 'Logs/parity-tour.txt'
if (Test-Path $rows) { Write-Host ("  Checklist rows proved: {0}" -f (Get-Content $rows).Count) }
if ($code -ne 0 -or [int]$run.total -le 0 -or [int]$run.failed -gt 0) { exit 1 }
exit 0
