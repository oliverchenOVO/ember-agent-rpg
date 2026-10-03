param([Parameter(Mandatory=$true)][string]$Label,[string]$Executable='Builds/Phase311/Development/Ember.exe',[string[]]$PlayerArgs=@(),[int]$TimeoutSeconds=660,[int]$Width=1600,[int]$Height=900,[int]$MaxLogMegabytes=64)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskExe=[IO.Path]::GetFullPath((Join-Path $taskRoot $Executable))
$taskFolder=Join-Path $taskRoot ('Artifacts\Phase311\'+$Label)
if($TimeoutSeconds -lt 1 -or $TimeoutSeconds -gt 2100 -or $MaxLogMegabytes -lt 1 -or $MaxLogMegabytes -gt 256){throw 'Invalid watchdog bounds.'}
if($Label.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $Label.Contains('..')){throw 'Invalid label.'}
if(-not (Test-Path -LiteralPath $taskExe)){throw 'Player executable is missing.'}
$taskConfigHash=''
$taskConfigIndex=[Array]::IndexOf($PlayerArgs,'--balance-config')
if($taskConfigIndex -ge 0){
    if($taskConfigIndex+1 -ge $PlayerArgs.Count){throw 'Missing balance config argument.'}
    $taskConfigPath=$PlayerArgs[$taskConfigIndex+1].Trim('"')
    if(-not (Test-Path -LiteralPath $taskConfigPath)){throw 'Balance config is missing; do not launch Player.'}
    $null=Get-Content -LiteralPath $taskConfigPath -Raw | ConvertFrom-Json
    $taskConfigHash=(Get-FileHash -LiteralPath $taskConfigPath -Algorithm SHA256).Hash
}
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
    $taskLogInfo=Get-Item -LiteralPath $taskLog -ErrorAction SilentlyContinue
    $taskFatal=''
    if($taskLogInfo -and $taskLogInfo.Length -gt 0){
        # Read a bounded snapshot, never follow a rapidly growing exception log.
        $taskStream=[IO.File]::Open($taskLog,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
        try{$taskBuffer=[byte[]]::new(65536);$taskRead=$taskStream.Read($taskBuffer,0,$taskBuffer.Length);$taskHead=[Text.Encoding]::UTF8.GetString($taskBuffer,0,$taskRead)}finally{$taskStream.Dispose()}
        if($taskHead -match '(?:FileNotFound|NullReference|Argument|InvalidOperation|Unhandled)Exception:'){$taskFatal='startup/runtime exception'}
        if($taskLogInfo.Length -gt $MaxLogMegabytes*1MB){$taskFatal='log volume limit'}
    }
    if($taskFatal){Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue;@{state='FAILED';reason=$taskFatal;elapsed=$taskClock.Elapsed.TotalSeconds;pid=$taskProcess.Id;logBytes=$taskLogInfo.Length} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'process-result.json');Write-Output "PLAYER FAILED: $taskFatal; artifacts preserved";exit 125}
    if($taskClock.Elapsed.TotalSeconds -ge $taskNext){$taskNext=$taskClock.Elapsed.TotalSeconds+30;$taskLine="elapsed=$($taskClock.Elapsed.TotalSeconds);pid=$($taskProcess.Id);cpu=$($taskProcess.TotalProcessorTime.TotalSeconds);memory=$($taskProcess.WorkingSet64);logBytes=$((Get-Item -LiteralPath $taskLog -ErrorAction SilentlyContinue).Length);utc=$([DateTime]::UtcNow.ToString('O'))";$taskLine | Add-Content -LiteralPath (Join-Path $taskFolder 'process-heartbeat.txt');Write-Output $taskLine}
    if($taskClock.Elapsed.TotalSeconds -ge $TimeoutSeconds){Stop-Process -Id $taskProcess.Id;@{state='FAILED';reason='timeout';elapsed=$taskClock.Elapsed.TotalSeconds} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'process-result.json');exit 124}
    Start-Sleep -Seconds 1
}
$taskProcess.WaitForExit()
$taskExit=$taskProcess.ExitCode
if($taskExit -eq 0 -and $PlayerArgs -contains '--profile-seconds' -and -not (Test-Path -LiteralPath (Join-Path $taskFolder 'complete.txt'))){$taskExit=1}
@{state=if($taskExit -eq 0){'COMPLETE'}else{'FAILED'};exitCode=$taskExit;elapsed=$taskClock.Elapsed.TotalSeconds;executable=$taskExe;executableHash=(Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash;managedAssemblyHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $taskExe) 'Ember_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash;configHash=$taskConfigHash;utc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskFolder 'process-result.json') -Encoding utf8
Write-Output "PLAYER EXIT=$($taskProcess.ExitCode) / $taskFolder"
exit $taskExit
