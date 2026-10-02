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
            Validation.Run();
            Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Witness / runtime bootstrap").AddComponent<WitnessGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Witness.unity");
            PlayerSettings.companyName="WitnessWorks";PlayerSettings.productName="Ember - The Witness Tower";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            // Include shaders referenced by name at runtime, which scene analysis cannot discover.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Standard","Particles/Standard Unlit"})
            {
                var shader=Shader.Find(name);bool exists=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
                if(!exists){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            settings.ApplyModifiedProperties();AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Witness.unity",true)};
            string path=Path.GetFullPath("../Builds/Windows/Ember.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Witness.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            Debug.Log("EMBER WINDOWS BUILD PASSED / "+path);
        }
    }
}
