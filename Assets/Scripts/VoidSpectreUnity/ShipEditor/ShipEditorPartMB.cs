using Sirenix.OdinInspector;
using UnityEngine;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.View;

namespace VoidSpectreUnity.ShipEditor
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShipEditorPartMB : SerializedMonoBehaviour
    {
        [Title("Ship Part")]
        [Required]
        [AssetsOnly]
        [OnValueChanged(nameof(OnArchetypeChanged))]
        [SerializeField]
        private EntityArchetypeSO _archetype;

        [LabelText("Label")]
        [SerializeField]
        private string _displayName;

        private ShipEditorRootMB _root;
        private Vector3 _lastLocalPosition;
        private Quaternion _lastLocalRotation;

        public EntityArchetypeSO Archetype => _archetype;

#if UNITY_EDITOR
        private bool ShowMissingVisualWarning =>
            _archetype != null &&
            EntityVisualUtility.TryGetPrefab(_archetype) == null;

        [InfoBox("Archetype is missing EntityVisualComponent with an assigned prefab.", InfoMessageType.Warning, VisibleIf = nameof(ShowMissingVisualWarning))]
        [SerializeField, HideInInspector]
        private bool _missingVisualInfo;
#endif

        private void OnEnable()
        {
            CacheRoot();
            CacheTransform();
            RefreshVisual();
        }

        private void OnValidate()
        {
            CacheRoot();
            ApplyDisplayName();
            NotifyRootChanged();
        }

        private void Update()
        {
            if (Application.isPlaying)
                return;

            if (!TransformChanged())
                return;

            CacheTransform();
            NotifyRootChanged();
        }

        private void OnDestroy()
        {
            if (_root != null)
                _root.NotifyPartDestroyed(this);
        }

        internal void SetArchetypeFromPlacement(ShipPartPlacement placement)
        {
            if (placement?.Archetype is EntityArchetypeSO archetypeSo)
                _archetype = archetypeSo;
        }

        internal void RefreshVisual()
        {
            ShipEditorVisualBridge.RefreshVisual?.Invoke(this);
        }

        private void OnArchetypeChanged()
        {
            ApplyDisplayName();
            NotifyRootChanged();
            RefreshVisual();
        }

        private void CacheRoot()
        {
            if (_root == null)
                _root = GetComponentInParent<ShipEditorRootMB>();
        }

        private void NotifyRootChanged()
        {
            CacheRoot();
            _root?.NotifyPartChanged(this);
        }

        private void ApplyDisplayName()
        {
            if (string.IsNullOrWhiteSpace(_displayName))
                return;

            gameObject.name = _displayName;
        }

        private bool TransformChanged()
        {
            return transform.localPosition != _lastLocalPosition ||
                   transform.localRotation != _lastLocalRotation;
        }

        private void CacheTransform()
        {
            _lastLocalPosition = transform.localPosition;
            _lastLocalRotation = transform.localRotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            DrawOrientationArrow();
        }

        private void DrawOrientationArrow()
        {
            var origin = transform.position;
            var isThruster = ShipEditorArchetypeUtility.HasThruster(_archetype);
            var length = isThruster ? 2f : 1.25f;
            var direction = transform.forward;

            Gizmos.color = isThruster ? new Color(0.2f, 0.85f, 1f, 0.95f) : new Color(1f, 0.82f, 0.2f, 0.9f);
            DrawArrow(origin, direction, length);
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction, float length)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return;

            direction.Normalize();
            var tip = origin + direction * length;
            Gizmos.DrawLine(origin, tip);

            var headLength = length * 0.2f;
            var headWidth = headLength * 0.45f;
            var right = Vector3.Cross(direction, Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up).normalized;
            var left = -right;

            Gizmos.DrawLine(tip, tip - direction * headLength + right * headWidth);
            Gizmos.DrawLine(tip, tip - direction * headLength - right * headWidth);
            Gizmos.DrawLine(tip, tip - direction * headLength + left * headWidth);
            Gizmos.DrawLine(tip, tip - direction * headLength - left * headWidth);
        }
#endif
    }
}
