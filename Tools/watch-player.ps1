param([Parameter(Mandatory=$true)][string]$Label,[string]$Executable='Builds/Phase311/Development/Ember.exe',[string[]]$PlayerArgs=@(),[int]$TimeoutSeconds=660,[int]$Width=1600,[int]$Height=900)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskExe=[IO.Path]::GetFullPath((Join-Path $taskRoot $Executable))
$taskFolder=Join-Path $taskRoot ('Artifacts\Phase311\'+$Label)
if(Test-Path -LiteralPath $taskFolder){throw 'Use a new label; existing artifacts are preserved.'}
if(Get-Process Unity,Ember -ErrorAction SilentlyContinue){throw 'Run Player in isolation; another Unity/Player process is active.'}
New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
$taskLog=Join-Path $taskFolder 'player.log'
$taskArguments=@('--artifacts',$taskFolder,'-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',$taskLog,'-diag-job-temp-memory-leak-validation')+$PlayerArgs
$taskArguments=@($taskArguments | ForEach-Object {'"'+$_.Replace('"','')+'"'})
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Normal -PassThru -RedirectStandardOutput (Join-Path $taskFolder 'stdout.txt') -RedirectStandardError (Join-Path $taskFolder 'stderr.txt')
$taskClock=[Diagnostics.Stopwatch]::StartNew();$taskNext=0
while(-not $taskProcess.HasExited){
    $taskProcess.Refresh()
    if($taskClock.Elapsed.TotalSeconds -ge $taskNext){$taskNext=$taskClock.Elapsed.TotalSeconds+30;$taskLine="elapsed=$($taskClock.Elapsed.TotalSeconds);pid=$($taskProcess.Id);cpu=$($taskProcess.TotalProcessorTime.TotalSeconds);memory=$($taskProcess.WorkingSet64);logBytes=$((Get-Item -LiteralPath $taskLog -ErrorAction SilentlyContinue).Length);utc=$([DateTime]::UtcNow.ToString('O'))";$taskLine | Add-Content -LiteralPath (Join-Path $taskFolder 'process-heartbeat.txt');Write-Output $taskLine}
    if($taskClock.Elapsed.TotalSeconds -ge $TimeoutSeconds){Stop-Process -Id $taskProcess.Id;@{state='FAILED';reason='timeout';elapsed=$taskClock.Elapsed.TotalSeconds} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'process-result.json');exit 124}
    Start-Sleep -Seconds 1
}
$taskProcess.WaitForExit()
@{state=if($taskProcess.ExitCode -eq 0){'COMPLETE'}else{'FAILED'};exitCode=$taskProcess.ExitCode;elapsed=$taskClock.Elapsed.TotalSeconds;executable=$taskExe;executableHash=(Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash;utc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'process-result.json') -Encoding utf8
Write-Output "PLAYER EXIT=$($taskProcess.ExitCode) / $taskFolder"
exit $taskProcess.ExitCode
