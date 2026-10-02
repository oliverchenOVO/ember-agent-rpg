param([int]$Width=1600,[int]$Height=900)
$projectRoot=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $projectRoot "Artifacts\Localization\$Width-$Height"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$exe=Join-Path $projectRoot 'Builds\Windows\Ember.exe'
$log=Join-Path $folder 'player.log'
# This is a visible visual QA run, explicitly requested for localization verification.
$proc=Start-Process -FilePath $exe -ArgumentList @('--localization-smoke','--locale','zh-TW','--artifacts',('"'+$folder+'"'),'-screen-width',$Width,'-screen-height',$Height,'-logFile',('"'+$log+'"')) -WindowStyle Normal -PassThru
$proc.WaitForExit()
Get-Content -LiteralPath (Join-Path $folder 'layout-results.txt') -Encoding UTF8 -TotalCount 20
if($proc.ExitCode -ne 0){throw "Localization QA failed with code $($proc.ExitCode). See $log"}
