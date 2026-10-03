param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$Label,
    [int]$TimeoutSeconds=900,[int]$IdleWarningSeconds=120,[int]$StallSeconds=420,
    [switch]$Graphics,[string]$DiagnosticRoot=''
)
$ErrorActionPreference='Stop'
if($TimeoutSeconds -lt 1 -or $TimeoutSeconds -gt 1800){throw 'Timeout must be 1..1800 seconds.'}
if($StallSeconds -le $IdleWarningSeconds){throw 'Stall threshold must exceed warning threshold.'}
if($Label.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $Label.Contains('..')){throw 'Invalid label.'}
$taskRoot=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskProject=Join-Path $taskRoot 'UnityProject'
$taskFolder=Join-Path $taskRoot ('Artifacts\Phase311\'+$Label)
if(Test-Path -LiteralPath $taskFolder){throw 'Use a fresh label; preserve prior logs.'}
$taskExisting=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('/','\').Contains($taskProject)}
if($taskExisting){throw 'Project already has an Editor process.'}
New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
$taskLog=Join-Path $taskFolder 'Editor.log'
$taskArguments=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',$Method,'-logFile',('"'+$taskLog+'"'))
if(-not $DiagnosticRoot){$DiagnosticRoot=Join-Path $taskFolder 'diagnostics'}
$taskArguments+=@('--phase31-root',('"'+[IO.Path]::GetFullPath($DiagnosticRoot)+'"'))
if(-not $Graphics){$taskArguments=@('-nographics')+$taskArguments}
$taskProcess=Start-Process -FilePath 'D:\unity\6000.2.0f1\Editor\Unity.exe' -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -WorkingDirectory $taskProject -RedirectStandardOutput (Join-Path $taskFolder 'stdout.txt') -RedirectStandardError (Join-Path $taskFolder 'stderr.txt')
$taskClock=[Diagnostics.Stopwatch]::StartNew()
$taskSample=Join-Path $taskFolder 'samples.csv'
'utc,elapsed,state,pid,cpu_delta,log_growth,log_bytes,log_timestamp,stage,idle_seconds,working_set,ilpp_pid,ilpp_cpu,ilpp_memory,ilpp_priority,pipe_present' | Set-Content -LiteralPath $taskSample
$taskBeforeCpu=0;$taskBeforeBytes=0;$taskProgress=0;$taskNext=0;$taskStage='STARTUP';$taskSeenStage='';$taskResult='FAILED';$taskCode=1;$taskObserved=@{}
while($true){
    $taskProcess.Refresh()
    if($taskProcess.HasExited){$taskProcess.WaitForExit();$taskCode=$taskProcess.ExitCode;$taskResult=if($taskCode -eq 0){'COMPLETE'}else{'FAILED'};break}
    $taskElapsed=$taskClock.Elapsed.TotalSeconds
    $taskCpu=$taskProcess.TotalProcessorTime.TotalSeconds
    $taskInfo=Get-Item -LiteralPath $taskLog -ErrorAction SilentlyContinue
    $taskBytes=if($taskInfo){$taskInfo.Length}else{0}
    $taskTail=if($taskInfo){Get-Content -LiteralPath $taskLog -Tail 35 -ErrorAction SilentlyContinue}else{@()}
    $taskText=$taskTail -join "`n"
    if($taskText -match 'Exiting batchmode|Cleanup mono|shutting down'){ $taskStage='SHUTDOWN' }
    elseif($taskText -match 'BUILD START|Building Player|BuildPlayer|Shader compiler|shader compiler|Building (?:scene|asset)|Build pipeline'){ $taskStage='BUILD' }
    elseif($taskText -match 'GATE PASSED|VALIDATION|SAVE REPLAY|UNIT PASSED'){ $taskStage='VALIDATION' }
    elseif($taskText -match 'ReloadAssembly|Reload Profiling|reloaded assembly'){ $taskStage='RELOAD' }
    elseif($taskText -match 'Compiling Scripts|Csc Library|script compilation|IL Post Processor'){ $taskStage='COMPILE' }
    $taskCpuDelta=$taskCpu-$taskBeforeCpu;$taskGrowth=$taskBytes-$taskBeforeBytes
    if($taskCpuDelta -ge .02 -or $taskGrowth -gt 0 -or $taskStage -ne $taskSeenStage){$taskProgress=$taskElapsed}
    $taskIdle=$taskElapsed-$taskProgress
    $taskState=if($taskIdle -ge $StallSeconds){'STALLED'}elseif($taskIdle -ge $IdleWarningSeconds){'IDLE'}else{'ACTIVE'}
    $taskChildren=Get-CimInstance Win32_Process -Filter "ParentProcessId=$($taskProcess.Id)"
    foreach($taskChild in $taskChildren){$taskObserved[[string]$taskChild.ProcessId]=$taskChild}
    $taskWorkerInfo=$taskChildren | Where-Object Name -eq 'Unity.ILPP.Runner.exe' | Select-Object -First 1
    $taskWorker=if($taskWorkerInfo){Get-Process -Id $taskWorkerInfo.ProcessId -ErrorAction SilentlyContinue}else{$null}
    $taskPipe='NOT_OBSERVED'
    if($taskWorkerInfo -and $taskWorkerInfo.CommandLine -match 'unity-ilpp-[a-zA-Z0-9-]+'){
        $taskPipeName=$Matches[0]
        try{$taskPipe=if([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object {$_ -like ('*'+$taskPipeName+'*')}){'PRESENT'}else{'ABSENT'}}catch{$taskPipe='UNAVAILABLE'}
    }
    $taskTimestamp=if($taskInfo){$taskInfo.LastWriteTimeUtc.ToString('O')}else{''}
    $taskWorkerPid=if($taskWorker){$taskWorker.Id}else{0};$taskWorkerCpu=if($taskWorker){$taskWorker.CPU}else{0};$taskWorkerMemory=if($taskWorker){$taskWorker.WorkingSet64}else{0};$taskWorkerPriority=if($taskWorker){$taskWorker.PriorityClass}else{''}
    "$([DateTime]::UtcNow.ToString('O')),$taskElapsed,$taskState,$($taskProcess.Id),$taskCpuDelta,$taskGrowth,$taskBytes,$taskTimestamp,$taskStage,$taskIdle,$($taskProcess.WorkingSet64),$taskWorkerPid,$taskWorkerCpu,$taskWorkerMemory,$taskWorkerPriority,$taskPipe" | Add-Content -LiteralPath $taskSample
    $taskMetadata=@{utc=[DateTime]::UtcNow.ToString('O');pid=$taskProcess.Id;method=$Method;stage=$taskStage;state=$taskState;children=@($taskChildren | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CommandLine)}
    $taskMetadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskFolder 'process-metadata.json') -Encoding utf8
    if($taskElapsed -ge $taskNext){$taskNext=$taskElapsed+30;Write-Output ("{0} elapsed={1:F1}s PID={2} CPU+={3:F3}s log+={4} stage={5} idle={6:F1}s ILPP={7} IPC={8} logUTC={9}" -f $taskState,$taskElapsed,$taskProcess.Id,$taskCpuDelta,$taskGrowth,$taskStage,$taskIdle,$taskWorkerPid,$taskPipe,$taskTimestamp)}
    if($taskState -eq 'STALLED' -or $taskElapsed -ge $TimeoutSeconds){
        $taskResult=if($taskState -eq 'STALLED'){'STALLED'}else{'FAILED'};$taskCode=124
        $taskTail | Set-Content -LiteralPath (Join-Path $taskFolder 'stopped-log-tail.txt') -Encoding utf8
        Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue
        foreach($taskChild in $taskObserved.Values){$taskAlive=Get-CimInstance Win32_Process -Filter "ProcessId=$($taskChild.ProcessId)";if($taskAlive -and $taskAlive.ParentProcessId -eq $taskProcess.Id -and $taskAlive.Name -eq $taskChild.Name -and $taskChild.Name -ne 'Unity.Licensing.Client.exe'){Stop-Process -Id $taskChild.ProcessId -ErrorAction SilentlyContinue}}
        break
    }
    $taskBeforeCpu=$taskCpu;$taskBeforeBytes=$taskBytes;$taskSeenStage=$taskStage
    Start-Sleep -Seconds 1
}
if($taskResult -eq 'COMPLETE'){
    $taskFinalLog=if(Test-Path -LiteralPath $taskLog){[IO.File]::ReadAllText($taskLog)}else{''}
    $taskExpected=if($Method.EndsWith('.CompileOnly')){'COMPILE GATE PASSED'}elseif($Method.EndsWith('.ValidateOnly')){'EDITOR VALIDATION GATE PASSED'}elseif($Method -match 'ProjectBuilder.Build'){'EMBER WINDOWS BUILD PASSED'}else{''}
    if(($taskExpected -and -not $taskFinalLog.Contains($taskExpected)) -or $taskFinalLog -match 'error CS\d+|executeMethod method .* threw an exception'){$taskResult='FAILED';$taskCode=1}
}
@{state=$taskResult;exitCode=$taskCode;elapsed=$taskClock.Elapsed.TotalSeconds;utc=[DateTime]::UtcNow.ToString('O');method=$Method;stage=$taskStage;pid=$taskProcess.Id} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'result.json') -Encoding utf8
Write-Output "$taskResult exit=$taskCode; logs preserved at $taskFolder"
exit $taskCode
