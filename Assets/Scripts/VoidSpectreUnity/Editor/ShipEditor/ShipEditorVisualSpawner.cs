#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoidSpectreUnity.ShipEditor;
using VoidSpectreUnity.View;

namespace VoidSpectreUnity.Editor.ShipEditor
{
    [InitializeOnLoad]
    public static class ShipEditorVisualSpawner
    {
        static ShipEditorVisualSpawner()
        {
            ShipEditorVisualBridge.RefreshVisual = RefreshVisual;
            ShipEditorVisualBridge.CleanupAllEditorVisuals = CleanupAllEditorVisuals;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                CleanupAllEditorVisuals();
        }

        public static void RefreshVisual(ShipEditorPartMB part)
        {
            if (part == null || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            DestroyEditorVisualChild(part.transform);

            if (part.Archetype == null)
                return;

            var prefab = EntityVisualUtility.TryGetPrefab(part.Archetype);
            if (prefab == null)
                return;

            var instance = PrefabUtility.InstantiatePrefab(prefab, part.transform) as GameObject;
            if (instance == null)
                return;

            instance.name = ShipEditorUtility.EditorVisualChildName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = GeometryVisualScaleUtility.TryGetVisualScale(part.Archetype, out var scale)
                ? scale
                : Vector3.one;
            instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            Undo.RegisterCreatedObjectUndo(instance, "Refresh Ship Part Visual");
        }

        public static void CleanupAllEditorVisuals()
        {
            var parts = Object.FindObjectsOfType<ShipEditorPartMB>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part != null)
                    DestroyEditorVisualChild(part.transform);
            }
        }

        private static void DestroyEditorVisualChild(Transform partTransform)
        {
            if (partTransform == null)
                return;

            for (int i = partTransform.childCount - 1; i >= 0; i--)
            {
                var child = partTransform.GetChild(i);
                if (child.name != ShipEditorUtility.EditorVisualChildName)
                    continue;

                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }
}
#endif
