$ErrorActionPreference='Stop'
foreach($taskResolution in @(@(1280,720),@(1600,900))){
    foreach($taskLocale in @('zh-TW','en')){
        foreach($taskSuite in @('phase2','phase3')){
            $taskLabel="ui-A-$($taskResolution[0])-$($taskResolution[1])-$taskLocale-$taskSuite"
            & (Join-Path $PSScriptRoot 'watch-player.ps1') -Label $taskLabel -Executable 'Builds/Phase311/Windows/Ember.exe' -Width $taskResolution[0] -Height $taskResolution[1] -TimeoutSeconds 90 -PlayerArgs @("--$taskSuite-qa",'--showcase','--qa-locale',$taskLocale,'--glyph-trace')
            if($LASTEXITCODE -ne 0){throw "UI matrix failed: $taskLabel; preserved images/logs"}
        }
    }
}
