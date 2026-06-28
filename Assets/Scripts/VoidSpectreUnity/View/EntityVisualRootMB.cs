using UnityEngine;

namespace VoidSpectreUnity.View
{
    public sealed class EntityVisualRootMB : MonoBehaviour
    {
        public static Transform Root { get; private set; }

        public static EntityVisualRootMB EnsureExists(Transform parent)
        {
            var existing = parent.GetComponentInChildren<EntityVisualRootMB>(true);
            if (existing != null)
            {
                Root = existing.transform;
                return existing;
            }

            var go = new GameObject("EntityVisuals");
            go.transform.SetParent(parent, false);
            var root = go.AddComponent<EntityVisualRootMB>();
            Root = root.transform;
            return root;
        }

        private void OnDestroy()
        {
            if (Root == transform)
                Root = null;
        }
    }
}
