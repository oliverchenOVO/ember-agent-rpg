param([switch]$Visual, [switch]$Collapse)
$projectRoot = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $projectRoot 'Builds\Windows\Ember.exe'
$artifacts = Join-Path $projectRoot 'Artifacts'
$log = Join-Path $artifacts $(if ($Collapse) { 'collapse-player.log' } else { 'player.log' })
$mode = if ($Collapse) { '--collapse-smoke' } else { '--smoke' }
$style = if ($Visual) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $exe -ArgumentList @($mode,'--artifacts',('"' + $artifacts + '"'),'-screen-width','1600','-screen-height','900','-logFile',('"' + $log + '"')) -WindowStyle $style -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $log -Tail 50; throw "Player smoke test failed: $($process.ExitCode)" }
if ($Visual) { Write-Output 'Visual player run complete. Inspect screenshots in Artifacts.' }
else { Write-Output 'Hidden player lifecycle test complete. Use -Visual to verify rendered screenshots.' }
