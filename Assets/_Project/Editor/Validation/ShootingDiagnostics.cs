using QFramework;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Unomata.EditorValidation
{
    public sealed class ShootingDiagnostics : EditorWindow
    {
        private float _factor;
        [MenuItem("UNOMATA/Diagnostics/Shooting")]
        public static void Open() { GetWindow<ShootingDiagnostics>("Shooting diagnostics"); }
        private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }
        private void OnGUI()
        {
            if (!EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Enter Play Mode, then select a capsule target.", MessageType.Info); return; }
            var enemy = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                var state = enemy.Snapshot;
                EditorGUILayout.LabelField(enemy.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("HP", state.Hp.ToString("0.###") + " / " + state.MaxHp);
                EditorGUILayout.LabelField("Base reduction / Hack factor", state.BaseDamageReduction + " / " + state.HackFactor);
                _factor = EditorGUILayout.FloatField("New hack factor", _factor);
                if (GUILayout.Button("Apply factor")) GameApp.Interface.SendCommand(new SetEnemyHackFactorCommand(enemy.Id, _factor));
                if (GUILayout.Button("Reset selected target")) { enemy.enabled = false; enemy.enabled = true; }
            }
            else EditorGUILayout.HelpBox("Select a capsule in the Hierarchy to change its factor or reset it.", MessageType.Info);
            var result = GameApp.Interface.GetModel<EnemyModel>().LastDamage;
            if (!result.Accepted) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last damage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("D / R / H", result.RawDamage + " / " + result.BaseDamageReduction + " / " + result.HackFactor);
            EditorGUILayout.LabelField("Remaining reduction", result.RemainingReduction.ToString("0.###"));
            EditorGUILayout.LabelField("Resolved / Applied", result.ResolvedDamage.ToString("0.###") + " / " + result.AppliedDamage.ToString("0.###"));
            EditorGUILayout.LabelField("HP before / after", result.PreviousHp.ToString("0.###") + " / " + result.Hp.ToString("0.###"));
            EditorGUILayout.LabelField("Shot", result.Shot.ToString());
        }
    }
}
