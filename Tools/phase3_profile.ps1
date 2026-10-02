param([int]$Seconds=7200,[string]$Label='soak',[string]$Probe='', [switch]$Development)
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskDir=Join-Path $taskRoot ("Artifacts\Phase3\$Label")
New-Item -ItemType Directory -Force $taskDir | Out-Null
$taskArgs=@('--showcase','--locale','zh-TW','--artifacts',('"'+$taskDir+'"'),'-screen-width','1600','-screen-height','900','-logFile',('"'+(Join-Path $taskDir 'player.log')+'"'),'-diag-job-temp-memory-leak-validation')
if($Probe){$taskArgs+=@('--native-diag',$Probe)}else{$taskArgs+=@('--profile-seconds',$Seconds)}
$taskBuild=if($Development){'Development'}else{'Windows'}
$taskProc=Start-Process (Join-Path $taskRoot "Builds\$taskBuild\Ember.exe") -ArgumentList $taskArgs -WindowStyle Normal -PassThru
$taskProc.WaitForExit()
if($taskProc.ExitCode -ne 0){throw "Phase 3 Player failed: $($taskProc.ExitCode)"}
Write-Output "Phase 3 session finished: $taskDir"
