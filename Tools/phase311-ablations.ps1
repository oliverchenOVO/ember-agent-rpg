param([string]$CohortRoot='Artifacts/Phase311/exp1-ablations')
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskCohorts=Join-Path $taskRoot $CohortRoot
foreach($taskConfig in Get-ChildItem -LiteralPath $taskCohorts -Filter '*-config.json' | Sort-Object Name){
    $taskConfiguration=Get-Content -LiteralPath $taskConfig.FullName -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath $taskConfig.FullName -Destination (Join-Path $taskCohorts 'run-config.json')
    # Fresh invocation label preserves earlier watchdog output; cohort itself resumes.
    $taskLabel='ablation-'+$taskConfiguration.label+'-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    & (Join-Path $PSScriptRoot 'watch-unity.ps1') -Method Ember.Editor.Phase31Validation.Run -Label $taskLabel -DiagnosticRoot $taskCohorts -TimeoutSeconds 960
    if($LASTEXITCODE -ne 0){throw "Ablation failed: $taskLabel; preserve completed prefix"}
    $taskSummary=Get-Content -LiteralPath (Join-Path $taskCohorts ($taskConfiguration.label+'/summary.txt')) -Raw
    Write-Output ($taskConfiguration.label+': '+$taskSummary.Trim())
    if($taskSummary -notmatch 'complete=True' -or $taskSummary -notmatch 'nonterminal=0'){throw 'Incomplete/unsafe cohort; no escalation'}
}
