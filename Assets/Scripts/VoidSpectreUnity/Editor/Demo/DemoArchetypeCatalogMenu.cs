#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.Demo;

namespace VoidSpectreUnity.Editor.Demo
{
    public static class DemoArchetypeCatalogMenu
    {
        private const string CatalogPath = "Assets/Design/DemoArchetypeCatalog.asset";

        [MenuItem("VoidSpectre/Demo/Create Demo Archetype Catalog")]
        public static void CreateDemoArchetypeCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DemoArchetypeCatalogSO>(CatalogPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[VoidSpectre] Demo archetype catalog already exists.", existing);
                return;
            }

            var catalog = ScriptableObject.CreateInstance<DemoArchetypeCatalogSO>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);

            Debug.Log(
                "[VoidSpectre] Created DemoArchetypeCatalog. Assign Planet, Space Rock, Ship, and Crew " +
                "EntityArchetypeSO assets, then add the catalog to GameBootstrapMB Service Assets.",
                catalog);
        }
    }
}
#endif
