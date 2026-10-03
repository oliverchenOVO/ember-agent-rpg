param([Parameter(Mandatory=$true)][string]$Method,[Parameter(Mandatory=$true)][string]$Label,[int]$LimitSeconds=900,[switch]$Graphics)
$ErrorActionPreference='Stop'
if($LimitSeconds -lt 1 -or $LimitSeconds -gt 900){throw 'LimitSeconds must be 1..900.'}
if($Label.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $Label.Contains('..')){throw 'Invalid artifact label.'}
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskProject=Join-Path $taskRoot 'UnityProject'
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -like ('*'+$taskProject+'*') }){throw 'This Unity project still has an Editor process; wait for shutdown before starting another.'}
$taskOutput=Join-Path $taskRoot ('Artifacts\Phase31\'+$Label)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskLog=Join-Path $taskOutput 'editor.log'
$taskArguments=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',$Method,'-logFile',('"'+$taskLog+'"'))
if(-not $Graphics){$taskArguments=@('-nographics')+$taskArguments}
$taskProcess=Start-Process -FilePath 'D:\unity\6000.2.0f1\Editor\Unity.exe' -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -WorkingDirectory $taskProject
$taskClock=[Diagnostics.Stopwatch]::StartNew()
while(-not $taskProcess.HasExited){
    $taskProcess.Refresh()
    $taskChildren=Get-CimInstance Win32_Process -Filter "ParentProcessId=$($taskProcess.Id)" | Where-Object Name -eq 'Unity.ILPP.Runner.exe'
    foreach($taskChild in $taskChildren){$taskWorker=Get-Process -Id $taskChild.ProcessId -ErrorAction SilentlyContinue;if($taskWorker -and $taskWorker.PriorityClass -eq 'BelowNormal'){$taskWorker.PriorityClass='Normal'}}
    "utc=$([DateTime]::UtcNow.ToString('O'));pid=$($taskProcess.Id);elapsed=$($taskClock.Elapsed.TotalSeconds);cpu=$($taskProcess.TotalProcessorTime.TotalSeconds);logBytes=$((Get-Item -LiteralPath $taskLog -ErrorAction SilentlyContinue).Length)" | Set-Content -LiteralPath (Join-Path $taskOutput 'process-heartbeat.txt') -Encoding utf8
    if($taskClock.Elapsed.TotalSeconds -ge $LimitSeconds){
        Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue
        foreach($taskChild in $taskChildren){Stop-Process -Id $taskChild.ProcessId -ErrorAction SilentlyContinue}
        'TIMED OUT; outputs preserved; do not restart from seed 1' | Set-Content -LiteralPath (Join-Path $taskOutput 'process-result.txt')
        exit 124
    }
    Start-Sleep -Seconds 5
}
$taskProcess.WaitForExit()
"exit=$($taskProcess.ExitCode);elapsed=$($taskClock.Elapsed.TotalSeconds);utc=$([DateTime]::UtcNow.ToString('O'))" | Set-Content -LiteralPath (Join-Path $taskOutput 'process-result.txt') -Encoding utf8
exit $taskProcess.ExitCode
