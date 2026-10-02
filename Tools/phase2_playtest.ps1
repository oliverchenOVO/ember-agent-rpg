param([switch]$Lifecycle,[int]$Width=1600,[int]$Height=900,[switch]$LLM)
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskDir=Join-Path $taskRoot ('Artifacts\Phase2\'+$(if($Lifecycle){if($LLM){'lifecycle-llm'}else{'lifecycle'}}else{"$Width-$Height"}))
New-Item -ItemType Directory -Force -Path $taskDir | Out-Null
$taskMode=if($Lifecycle){'--phase2-smoke'}else{'--phase2-qa'}
$taskArgs=@($taskMode,'--showcase','--locale','zh-TW','--artifacts',('"'+$taskDir+'"'),'-screen-width',$Width,'-screen-height',$Height,'-logFile',('"'+(Join-Path $taskDir 'player.log')+'"'))
if($LLM){$taskArgs+='--llm'}
$taskProc=Start-Process (Join-Path $taskRoot 'Builds\Windows\Ember.exe') -ArgumentList $taskArgs -WindowStyle Normal -PassThru
$taskProc.WaitForExit()
if($taskProc.ExitCode -ne 0){Get-Content (Join-Path $taskDir 'player.log') -Tail 30;throw "Phase 2 Player failed: $($taskProc.ExitCode)"}
if(!$Lifecycle){Get-Content -Encoding utf8 (Join-Path $taskDir 'layout-results.txt')}
Write-Output "Phase 2 player passed: $taskDir"
