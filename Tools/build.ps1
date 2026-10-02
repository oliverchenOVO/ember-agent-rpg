param([string]$Unity = 'D:\unity\6000.2.0f1\Editor\Unity.exe', [switch]$TestsOnly)
$projectRoot = Split-Path $PSScriptRoot -Parent
$method = if ($TestsOnly) { 'Ember.Editor.Phase3Validation.Full' } else { 'Ember.Editor.ProjectBuilder.Build' }
$log = Join-Path $projectRoot 'Artifacts\build.log'
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Artifacts') | Out-Null
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + (Join-Path $projectRoot 'UnityProject') + '"'),'-executeMethod',$method,'-logFile',('"' + $log + '"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -WorkingDirectory (Join-Path $projectRoot 'UnityProject')
$process.WaitForExit()
if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $log -Tail 60; throw "Unity failed with exit code $($process.ExitCode)" }
Get-Content -LiteralPath (Join-Path $projectRoot 'Artifacts\core-tests.txt') -TotalCount 1
Write-Output "Unity completed. Log: $log"
