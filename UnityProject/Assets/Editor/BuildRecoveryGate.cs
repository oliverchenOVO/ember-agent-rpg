using System;
using System.IO;
using Ember.Core;
using UnityEditor;
using UnityEngine;

namespace Ember.Editor
{
    public static class BuildRecoveryGate
    {
        public static void CompileOnly()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Compile/import is still active");
            var text=Resources.Load<TextAsset>("catalog");
            var catalog=JsonUtility.FromJson<Catalog>(text.text);
            var content=Ember.Core.Phase2.TowerContent.Load();
            content.Validate(catalog);
            Debug.Log("COMPILE GATE PASSED / assembly loaded; catalog test started and passed / UTC="+DateTime.UtcNow.ToString("O"));
        }
        public static void ValidateOnly()
        {
            CompileOnly();Phase31Validation.ShortGate();
            Debug.Log("EDITOR VALIDATION GATE PASSED / UTC="+DateTime.UtcNow.ToString("O"));
        }
    }
}

// Compile continuity probe 3
