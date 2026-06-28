using System;
using Sirenix.OdinInspector;
using UnityEngine;
using VoidSpectre.Core.Config;
using VoidSpectre.Core.Services;
using VoidSpectre.Gameplay.Demo;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.Services;

namespace VoidSpectreUnity.Demo
{
    [CreateAssetMenu(menuName = "VoidSpectre/Demo Archetype Catalog")]
    public sealed class DemoArchetypeCatalogSO
        : SerializedScriptableObjectServiceProvider, IDemoArchetypeProvider
    {
        public override Type ContractType => typeof(IDemoArchetypeProvider);

        [Title("Demo Entity Archetypes")]
        [Required, AssetsOnly, SerializeField]
        private EntityArchetypeSO _planetArchetype;

        [Required, AssetsOnly, SerializeField]
        private EntityArchetypeSO _spaceRockArchetype;

        [Required, AssetsOnly, SerializeField]
        private EntityArchetypeSO _shipArchetype;

        [Required, AssetsOnly, SerializeField]
        private EntityArchetypeSO _crewArchetype;

        public EntityArchetypeSO PlanetArchetypeAsset => _planetArchetype;
        public EntityArchetypeSO SpaceRockArchetypeAsset => _spaceRockArchetype;
        public EntityArchetypeSO ShipArchetypeAsset => _shipArchetype;
        public EntityArchetypeSO CrewArchetypeAsset => _crewArchetype;

        public IEntityArchetype PlanetArchetype => _planetArchetype;
        public IEntityArchetype SpaceRockArchetype => _spaceRockArchetype;
        public IEntityArchetype ShipArchetype => _shipArchetype;
        public IEntityArchetype CrewArchetype => _crewArchetype;

        public override void Register(ServiceLocator locator)
        {
            DemoArchetypeValidator.ValidateOrThrow(this);
            base.Register(locator);
        }

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        private void ValidateCatalog()
        {
            if (DemoArchetypeValidator.TryValidate(this, out var message))
            {
                Debug.Log("[VoidSpectre] Demo archetype catalog validation passed.", this);
                return;
            }

            Debug.LogError(message, this);
        }

        private void OnValidate()
        {
            if (DemoArchetypeValidator.TryValidate(this, out var message))
                return;

            Debug.LogWarning(message, this);
        }
#endif
    }
}
