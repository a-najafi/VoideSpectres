using System.Collections.Generic;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Priority;
using VoidSpectre.Core.Services;
using VoidSpectre.Gameplay.Demo;

namespace VoidSpectre.Gameplay.Bootstrap
{
    public sealed class GameBootstrapOptions
    {
        public SystemOrderConfigData SystemOrderConfig;
        public IReadOnlyList<ISceneServiceProvider> ServiceProviders;
        public IInteractionFocusProvider FocusProvider;
        public bool FocusSectorOnStart = true;
        public bool RunDemoOnStart = true;
        public bool RunNavigationContractTests;
        public bool LogTickDeltas;
    }

    public sealed class GameBootstrap
    {
        public SimulationUniverse Universe { get; private set; }
        public SimulationContext Sector { get; private set; }
        public SimulationContext ShipInterior { get; private set; }

        public void Initialize(GameBootstrapOptions options)
        {
            Universe = new SimulationUniverse(options?.SystemOrderConfig);
            Universe.RegisterEntityTransfer(new DemoEntityTransfer());

            var locator = Universe.Services;
            if (options?.ServiceProviders != null)
            {
                foreach (var provider in options.ServiceProviders)
                    provider?.Register(locator);
            }

            Sector = Universe.CreateContext(ContextKind.Volume, parent: null, displayName: "Sector");
            ShipInterior = Universe.CreateContext(ContextKind.Interior, parent: Sector, displayName: "ShipInterior");

            Universe.InjectServicesIntoContext(Sector, locator);
            Universe.InjectServicesIntoContext(ShipInterior, locator);

            if (options?.RunNavigationContractTests == true)
                Ship.Navigation.Tests.ShipStepSimulationContractTests.RunAll();

            if (options?.RunDemoOnStart != false)
            {
                if (!locator.TryGet<IDemoArchetypeProvider>(out var demoArchetypes))
                {
                    throw new System.InvalidOperationException(
                        "[VoidSpectre] Demo spawn requires IDemoArchetypeProvider. " +
                        "Assign a DemoArchetypeCatalogSO to GameBootstrapMB Service Assets.");
                }

                DemoHierarchySetup.Spawn(Sector, ShipInterior, demoArchetypes);
            }

            if (options?.LogTickDeltas == true)
            {
                Sector.RegisterCoreSystem(new DemoTickLoggerSystem());
                ShipInterior.RegisterCoreSystem(new DemoTickLoggerSystem());
            }

            if (options?.FocusProvider != null)
            {
                Universe.Scheduler.SetFocusProvider(options.FocusProvider);

                if (options.FocusSectorOnStart != false &&
                    options.FocusProvider is ISettableInteractionFocus settable)
                {
                    settable.SetFocus(Sector);
                }
            }
        }

        public void Tick(float deltaTime) => Universe?.Scheduler.Update(deltaTime);
    }
}
