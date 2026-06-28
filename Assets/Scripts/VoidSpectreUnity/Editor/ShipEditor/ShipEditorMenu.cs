#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoidSpectreUnity.ShipEditor;

namespace VoidSpectreUnity.Editor.ShipEditor
{
    public static class ShipEditorMenu
    {
        [MenuItem("VoidSpectre/Ship Editor/Create Ship Root")]
        public static void CreateShipRoot()
        {
            var rootGo = new GameObject("ShipEditor");
            Undo.RegisterCreatedObjectUndo(rootGo, "Create Ship Root");
            rootGo.AddComponent<ShipEditorRootMB>();

            Selection.activeGameObject = rootGo;
            EditorGUIUtility.PingObject(rootGo);
        }

        [MenuItem("VoidSpectre/Ship Editor/Add Part To Selected Root")]
        public static void AddPartToSelectedRoot()
        {
            var root = Selection.activeGameObject?.GetComponent<ShipEditorRootMB>();
            if (root == null)
            {
                Debug.LogWarning("[ShipEditor] Select a GameObject with ShipEditorRootMB.");
                return;
            }

            var partGo = new GameObject("Part");
            Undo.RegisterCreatedObjectUndo(partGo, "Add Ship Part");
            partGo.transform.SetParent(root.transform, false);
            var part = partGo.AddComponent<ShipEditorPartMB>();

            Selection.activeGameObject = partGo;
            root.NotifyPartChanged(part);
            part.RefreshVisual();
        }

        [MenuItem("VoidSpectre/Ship Editor/Add Part To Selected Root", true)]
        private static bool AddPartToSelectedRootValidate()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<ShipEditorRootMB>() != null;
        }
    }
}
#endif
