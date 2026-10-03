param([string]$Executable='Builds/Phase311/EXP2.1/Development/Ember.exe',[string]$Rules='Artifacts/Phase311/exp21-H3-player-rules.json',[string]$Prefix='exp21',[string[]]$Only=@())
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskRules=[IO.Path]::GetFullPath((Join-Path $taskRoot $Rules))
$taskExe=[IO.Path]::GetFullPath((Join-Path $taskRoot $Executable))
$taskExpectedExe=(Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash
$taskExpectedAssembly=(Get-FileHash -LiteralPath (Join-Path (Split-Path $taskExe) 'Ember_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
$taskExpectedRules=(Get-FileHash -LiteralPath $taskRules -Algorithm SHA256).Hash
# Every Player is isolated and bounded. B/C are documented no-op controls because
# default mesh embers and Boss-specific ParticleSystems do not exist in this build.
$taskCases=@(
 @{label='baseline-on-native-A';mode='baseline';seconds=600;flags=@()},
 @{label='native-B-mesh-off-noop';mode='baseline';seconds=300;flags=@()},
 @{label='native-C-boss-particles-off-noop';mode='baseline';seconds=300;flags=@()},
 @{label='native-D-ambient-off';mode='baseline';seconds=300;flags=@('--no-particles')},
 @{label='native-E-hitfx-off';mode='baseline';seconds=300;flags=@('--no-hit-fx')},
 @{label='native-F-ui-only';mode='baseline';seconds=300;flags=@('--ui-only')},
 @{label='native-G-no-save';mode='baseline';seconds=300;flags=@('--no-save-load')},
 @{label='native-H-save-load';mode='baseline';seconds=300;flags=@('--native-save-load')},
 @{label='baseline-off';mode='baseline';seconds=600;flags=@('--diagnostics-off')},
 @{label='gameplay-10m';mode='gameplay';seconds=600;flags=@()},
 @{label='extreme-20m';mode='extreme';seconds=1200;flags=@()}
)
foreach($taskName in $Only){if($taskName -notin $taskCases.label){throw "Unknown workload: $taskName"}}
foreach($taskCase in $taskCases){
 if($Only.Count -gt 0 -and $taskCase.label -notin $Only){continue}
 $taskLabel=$Prefix+'-'+$taskCase.label
 $taskFolder=Join-Path $taskRoot ('Artifacts/Phase311/'+$taskLabel)
 if(Test-Path -LiteralPath $taskFolder){
  $taskState=Get-Content -LiteralPath (Join-Path $taskFolder 'process-result.json') -Raw | ConvertFrom-Json
  if($taskState.state -ne 'COMPLETE' -or -not(Test-Path -LiteralPath (Join-Path $taskFolder 'complete.txt'))){throw "Preserve incomplete $taskLabel; choose fresh invocation label"}
  if($taskState.executableHash -ne $taskExpectedExe -or $taskState.managedAssemblyHash -ne $taskExpectedAssembly -or $taskState.configHash -ne $taskExpectedRules){throw "Resume rejected: executable/assembly/rules differ for $taskLabel"}
  Write-Output "RETAIN COMPLETED $taskLabel";continue
 }
 $taskArgs=@('--profile-seconds',([string]$taskCase.seconds),'--profile-mode',$taskCase.mode,'--locale','zh-TW','--balance-config',$taskRules)+$taskCase.flags
 & (Join-Path $PSScriptRoot 'watch-player.ps1') -Label $taskLabel -Executable $Executable -TimeoutSeconds ($taskCase.seconds+90) -PlayerArgs $taskArgs
 if($LASTEXITCODE -ne 0){throw "Player workload failed: $taskLabel; preserve evidence, no escalation"}
}
