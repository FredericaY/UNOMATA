using UnityEditor;
using UnityEngine;
using Unomata.Gameplay;

namespace Unomata.Editor.Validation
{
    /// <summary>Release external playable jobs before domain serialization drops their managed owners.</summary>
    [InitializeOnLoad]
    public static class AimRepairDomainGuard
    {
        static AimRepairDomainGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }
        private static void BeforeReload()
        {
            if (!EditorApplication.isPlaying) return;
            foreach (var view in Resources.FindObjectsOfTypeAll<PlayerAimPresentation>())
                if (view != null && view.gameObject.scene.IsValid())
                    view.ReleaseRuntimeResources();
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) BeforeReload();
        }
    }
}