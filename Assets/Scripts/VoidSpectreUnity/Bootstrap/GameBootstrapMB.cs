using System.Collections.Generic;
using System.Linq;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Services;
using VoidSpectre.Gameplay.Bootstrap;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.Diagnostics;
using VoidSpectreUnity.View;
using UnityEngine;
using VoidSpectreUnity.Services;

namespace VoidSpectreUnity.Bootstrap
{
    public sealed class GameBootstrapMB : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SystemOrderConfigSO systemOrderConfig;
        [SerializeField] private SerializedScriptableObjectServiceProvider[] serviceAssets;

        [Header("Demo")]
        [SerializeField] private bool runDemoOnStart = true;
        [SerializeField] private bool runNavigationContractTests;
        [SerializeField] private bool focusSectorOnStart = true;
        [SerializeField] private bool logTickDeltas;

        private readonly GameBootstrap _bootstrap = new();
        private InteractionFocusServiceMB _focusService;

        public GameBootstrap Bootstrap => _bootstrap;
        public SimulationUniverse Universe => _bootstrap.Universe;

        private void Awake()
        {
            VoidSpectreLogBridge.Install();
            EntityVisualRootMB.EnsureExists(transform);

            var providers = new List<ISceneServiceProvider>();
            providers.AddRange(FindObjectsOfType<MonoBehaviour>(true).OfType<ISceneServiceProvider>());
            if (serviceAssets != null)
            {
                foreach (var asset in serviceAssets)
                {
                    if (asset != null) providers.Add(asset);
                }
            }

            _focusService = GetComponent<InteractionFocusServiceMB>();
            if (_focusService == null)
                _focusService = gameObject.AddComponent<InteractionFocusServiceMB>();

            providers.Add(_focusService);

            _bootstrap.Initialize(new GameBootstrapOptions
            {
                SystemOrderConfig = systemOrderConfig != null ? systemOrderConfig.ToData() : null,
                ServiceProviders = providers,
                FocusProvider = _focusService,
                FocusSectorOnStart = focusSectorOnStart,
                RunDemoOnStart = runDemoOnStart,
                RunNavigationContractTests = runNavigationContractTests,
                LogTickDeltas = logTickDeltas
            });

            _focusService.Initialize(_bootstrap.Universe);
            _focusService.Register(_bootstrap.Universe.Services);

            Debug.Log(
                "[VoidSpectre] Bootstrap complete. Default focus: Sector (space). " +
                "1=Ship interior, 2=Sector | Pilot: W thrust, A/D yaw, S/X pitch | Demo: S shoot, M migrate");
        }

        private void Update() => _bootstrap.Tick(Time.deltaTime);
    }
}
