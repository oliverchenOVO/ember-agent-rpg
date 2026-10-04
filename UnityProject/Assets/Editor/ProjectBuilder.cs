using System;
using System.IO;
using Ember.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Ember/Build and validate vertical slice")]
        public static void Build()
        {
            Phase2Validation.RunAll();
            Phase3Validation.Unit();
            BuildPlayer(false);
        }
        public static void BuildDevelopment(){BuildPlayer(true);}
        public static void BuildRelocated(){Validation.Run();Phase3Validation.Unit();LocalizationValidation.Run();BuildPlayer(false);}
        public static void BuildReleaseDiagnostics(){BuildPlayer(false);}
        public static void BuildDevelopmentRecovery(){BuildPlayer(true,"../Builds/Phase311/Development/Ember.exe");}
        public static void BuildReleaseRecovery(){BuildPlayer(false,"../Builds/Phase311/Windows/Ember.exe");}
        public static void BuildDevelopmentExperiment(){BuildPlayer(true,"../Builds/Phase311/EXP1/Development/Ember.exe");}
        public static void BuildReleaseExperiment(){BuildPlayer(false,"../Builds/Phase311/EXP1/Windows/Ember.exe");}
        public static void BuildDevelopmentHealing(){BuildPlayer(true,"../Builds/Phase311/EXP2.1/Development/Ember.exe");}
        public static void BuildReleaseHealing(){BuildPlayer(false,"../Builds/Phase311/EXP2.1/Windows/Ember.exe");}
        public static void BuildDevelopmentObserverRecovery(){BuildPlayer(true,"../Builds/Phase311/EXP2.1-R1/Development/Ember.exe");}
        public static void BuildReleaseObserverRecovery(){BuildPlayer(false,"../Builds/Phase311/EXP2.1-R1/Windows/Ember.exe");}
        public static void BuildRestInteraction(){BuildPlayer(true,"../Builds/RestInteraction/Development/Ember.exe");}
        public static void BuildReleaseRestInteraction(){BuildPlayer(false,"../Builds/RestInteraction/Windows/Ember.exe");}
        public static void BuildKnowledgeCodex(){BuildPlayer(true,"../Builds/KnowledgeCodex/Development/Ember.exe");}
        public static void BuildReleaseKnowledgeCodex(){BuildPlayer(false,"../Builds/KnowledgeCodex/Windows/Ember.exe");}
        public static void BuildCombatArt(){BuildPlayer(true,"../Builds/CombatArt/Development/Ember.exe");}
        public static void BuildReleaseCombatArt(){BuildPlayer(false,"../Builds/CombatArt/Windows/Ember.exe");}
        public static void BuildRefuge(){BuildPlayer(true,"../Builds/RefugeExploration/Development/Ember.exe");}
        public static void BuildReleaseRefuge(){RefugeValidation.Run();BuildPlayer(false,"../Builds/RefugeExploration/Windows/Ember.exe");}
        public static void BuildRefugePair(){RefugeValidation.Run();BuildPlayer(true,"../Builds/RefugeExploration/Development/Ember.exe");BuildPlayer(false,"../Builds/RefugeExploration/Windows/Ember.exe");}
        public static void BuildAgentInspectionPair(){LocalizationValidation.Run();BuildPlayer(true,"../Builds/AgentInspection/Development/Ember.exe");BuildPlayer(false,"../Builds/AgentInspection/Windows/Ember.exe");}
        public static void BuildCombatReadabilityPair(){LocalizationValidation.Run();BuildPlayer(true,"../Builds/CombatReadability/Development/Ember.exe");BuildPlayer(false,"../Builds/CombatReadability/Windows/Ember.exe");}
        public static void BuildPressurePair(){PressureValidation.Run();LocalizationValidation.Run();BuildPlayer(true,"../Builds/PressureCores/Development/Ember.exe");BuildPlayer(false,"../Builds/PressureCores/Windows/Ember.exe");}
        public static void BuildTeamBalancePair(){TeamBalanceValidation.Unit();PressureValidation.Run();LocalizationValidation.Run();TeamBalanceValidation.Candidate();BuildPlayer(true,"../Builds/TeamBalance/Development/Ember.exe");BuildPlayer(false,"../Builds/TeamBalance/Windows/Ember.exe");}
        static void BuildPlayer(bool development,string output=null)
        {
            Debug.Log("BUILD START / development="+development+" / UTC="+DateTime.UtcNow.ToString("O"));
            Debug.Log("SOURCE RUNTIME HASH / "+Phase31Validation.RuntimeHash()+" / balance="+Ember.Core.Phase2.TowerSimulation.BalanceVersion);
            Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Witness / runtime bootstrap").AddComponent<WitnessGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Witness.unity");
            PlayerSettings.companyName="WitnessWorks";PlayerSettings.productName="Ember - The Witness Tower";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            // Include shaders referenced by name at runtime, which scene analysis cannot discover.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Standard","Particles/Standard Unlit","Sprites/Default"})
            {
                var shader=Shader.Find(name);bool exists=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
                if(!exists){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            settings.ApplyModifiedProperties();AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Witness.unity",true)};
            PlayerSettings.enableFrameTimingStats=true;
            string path=Path.GetFullPath(output??(development?"../Builds/Development/Ember.exe":"../Builds/Windows/Ember.exe"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Witness.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=development?BuildOptions.Development:BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            Debug.Log("EMBER WINDOWS BUILD PASSED / "+path);
        }
    }
}
