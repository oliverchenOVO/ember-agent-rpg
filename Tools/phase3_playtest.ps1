param([int]$Width=1600,[int]$Height=900)
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskDir=Join-Path $taskRoot ("Artifacts\Phase3\qa-$Width-$Height")
New-Item -ItemType Directory -Force $taskDir | Out-Null
$taskArgs=@('--phase3-qa','--showcase','--locale','zh-TW','--artifacts',('"'+$taskDir+'"'),'-screen-width',$Width,'-screen-height',$Height,'-logFile',('"'+(Join-Path $taskDir 'player.log')+'"'),'-diag-job-temp-memory-leak-validation')
$taskProc=Start-Process (Join-Path $taskRoot 'Builds\Windows\Ember.exe') -ArgumentList $taskArgs -WindowStyle Normal -PassThru
$taskProc.WaitForExit()
Get-Content -Encoding utf8 (Join-Path $taskDir 'layout-results.txt')
if($taskProc.ExitCode -ne 0){throw "Phase 3 UI failed: $($taskProc.ExitCode)"}
