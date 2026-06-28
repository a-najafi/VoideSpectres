#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidSpectreUnity.Editor
{
    public static class PlaceholderVisualPrefabCreator
    {
        private const string VisualsFolder = "Assets/Design/Visuals";

        [InitializeOnLoadMethod]
        private static void EnsurePlaceholderAssetsExist()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                var box = AssetDatabase.LoadAssetAtPath<GameObject>($"{VisualsFolder}/Part_Box.prefab");
                if (box != null)
                    return;

                CreatePlaceholderVisualPrefabs();
            };
        }

        [MenuItem("VoidSpectre/Create Placeholder Visual Prefabs")]
        public static void CreatePlaceholderVisualPrefabs()
        {
            Directory.CreateDirectory(VisualsFolder);

            CreatePrimitivePrefab("Part_Box", PrimitiveType.Cube, new Color(0.55f, 0.55f, 0.6f));
            CreateZForwardCylinderPrefab(new Color(0.7f, 0.45f, 0.2f));

            EnsureResourcesPrefabCopies();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[VoidSpectre] Created placeholder visual prefabs under Assets/Design/Visuals. " +
                "Part_Cylinder is Z-forward (length +Z, radius XY). " +
                "Assign prefabs on EntityVisualComponent in each part EntityArchetypeSO.");
        }

        private static void EnsureResourcesPrefabCopies()
        {
            const string resourcesVisualsFolder = "Assets/Resources/Visuals";
            Directory.CreateDirectory(resourcesVisualsFolder);
            CopyPrefabIfMissing($"{VisualsFolder}/Part_Box.prefab", $"{resourcesVisualsFolder}/Part_Box.prefab");
            CopyPrefabIfMissing($"{VisualsFolder}/Part_Cylinder.prefab", $"{resourcesVisualsFolder}/Part_Cylinder.prefab");
        }

        private static void CopyPrefabIfMissing(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
                return;

            AssetDatabase.CopyAsset(sourcePath, destinationPath);
        }

        private static GameObject CreateZForwardCylinderPrefab(Color color)
        {
            const string name = "Part_Cylinder";
            var path = $"{VisualsFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var root = new GameObject(name);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            mesh.transform.localScale = new Vector3(2f, 0.5f, 2f);

            var renderer = mesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
                AssetDatabase.CreateAsset(material, $"{VisualsFolder}/{name}_Mat.mat");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePrimitivePrefab(string name, PrimitiveType type, Color color)
        {
            var path = $"{VisualsFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var temp = GameObject.CreatePrimitive(type);
            temp.name = name;
            var renderer = temp.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
                AssetDatabase.CreateAsset(material, $"{VisualsFolder}/{name}_Mat.mat");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }
    }
}
#endif
