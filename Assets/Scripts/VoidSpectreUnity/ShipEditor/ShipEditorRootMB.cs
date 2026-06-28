using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.Conversion;

namespace VoidSpectreUnity.ShipEditor
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShipEditorRootMB : SerializedMonoBehaviour
    {
        [Title("Ship Design")]
        [FoldoutGroup("Ship Design")]
        [Required]
        [AssetsOnly]
        [SerializeField]
        private EntityArchetypeSO _shipArchetype;

        [FoldoutGroup("Ship Design")]
        [ShowInInspector]
        [ReadOnly]
        private int PartCount => GetPartComponents().Count;

        public EntityArchetypeSO ShipArchetype => _shipArchetype;

        private bool _isLoadingFromAsset;

        [FoldoutGroup("Ship Design")]
        [Button(ButtonSizes.Medium)]
        private void LoadFromShipAsset()
        {
#if UNITY_EDITOR
            if (_shipArchetype == null)
            {
                Debug.LogWarning("[ShipEditor] Assign a Ship Archetype asset before loading.", this);
                return;
            }

            var config = ShipEditorUtility.TryGetShipPartsConfig(_shipArchetype);
            if (config == null || config.Parts == null)
            {
                Debug.LogWarning("[ShipEditor] Ship asset has no ShipPartsConfigComponent.", this);
                return;
            }

            _isLoadingFromAsset = true;
            try
            {
                ClearPartChildren();

                for (int i = 0; i < config.Parts.Count; i++)
                {
                    var placement = config.Parts[i];
                    if (placement == null)
                        continue;

                    var part = CreatePartChild($"Part_{i + 1}");
                    part.SetArchetypeFromPlacement(placement);
                    part.transform.localPosition = placement.LocalPosition.ToUnity();
                    part.transform.localRotation = placement.LocalOrientation.ToUnity();
                    part.RefreshVisual();
                }

                RebuildPartsList(recordUndo: false);
            }
            finally
            {
                _isLoadingFromAsset = false;
            }
#endif
        }

        [FoldoutGroup("Ship Design")]
        [Button(ButtonSizes.Medium)]
        private void ApplyToShipAsset()
        {
            RebuildPartsList(recordUndo: true);
        }

        [FoldoutGroup("Ship Design")]
        [Button(ButtonSizes.Medium)]
        private void AddPart()
        {
#if UNITY_EDITOR
            var part = CreatePartChild("Part");
            UnityEditor.Undo.RegisterCreatedObjectUndo(part.gameObject, "Add Ship Part");
            UnityEditor.Selection.activeGameObject = part.gameObject;
            RebuildPartsList(recordUndo: true);
            part.RefreshVisual();
#endif
        }

        internal void NotifyPartChanged(ShipEditorPartMB part)
        {
            if (_isLoadingFromAsset || part == null)
                return;

            RebuildPartsList(recordUndo: true);
        }

        internal void NotifyPartDestroyed(ShipEditorPartMB part)
        {
            if (_isLoadingFromAsset || part == null)
                return;

            RebuildPartsList(recordUndo: true);
        }

        internal void RebuildPartsList(bool recordUndo)
        {
            if (_shipArchetype == null)
                return;

#if UNITY_EDITOR
            if (recordUndo)
                UnityEditor.Undo.RecordObject(_shipArchetype, "Update Ship Parts");
#endif

            var config = ShipEditorUtility.GetOrCreateShipPartsConfig(_shipArchetype);
            config.Parts ??= new List<ShipPartPlacement>();
            config.Parts.Clear();

            var parts = GetPartComponents();
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part == null || part.Archetype == null)
                    continue;

                config.Parts.Add(new ShipPartPlacement
                {
                    Archetype = part.Archetype,
                    LocalPosition = part.transform.localPosition.ToFloat3(),
                    LocalOrientation = part.transform.localRotation.ToFloatQuaternion(),
                });
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_shipArchetype);
#endif
        }

        private List<ShipEditorPartMB> GetPartComponents()
        {
            var parts = new List<ShipEditorPartMB>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.TryGetComponent(out ShipEditorPartMB part))
                    parts.Add(part);
            }

            return parts;
        }

        private void ClearPartChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (!child.TryGetComponent<ShipEditorPartMB>(out _))
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        private ShipEditorPartMB CreatePartChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<ShipEditorPartMB>();
        }

        private bool ShowMissingArchetypeWarning => _shipArchetype == null;

#if UNITY_EDITOR
        [InfoBox("Assign a Ship Archetype asset to begin editing.", InfoMessageType.Warning, VisibleIf = nameof(ShowMissingArchetypeWarning))]
        [SerializeField, HideInInspector]
        private bool _missingArchetypeInfo;
#endif
    }
}
