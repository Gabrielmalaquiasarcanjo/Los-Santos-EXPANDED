using Rage;
using System;
using System.Collections.Generic;

namespace LosSantosExpanded
{
    internal sealed class ModController
    {
        private static ModController _instance;
        public static ModController Instance
            =>_instance ?? (_instance = new ModController());
        private readonly List<ISystem> _systems = new List<ISystem>();

        private bool _isRunning;

        private ModController()
        {

        }
        public void Initialize()
        {
            Game.LogTrivial("[LSE] Inicialization Sistem.");
            RegisterSystems();
            foreach (var system in _systems)
            {
                system.Initialize();
            }

            _isRunning = true;

            Game.LogTrivial("[LSE] Initialization complete");
        }
        public void Run()
        {
            while (_isRunning)
            {
                GameFiber.Yield();

                foreach (var system in _systems)
                {
                    system.Update();
                }
            }
        }
        public void Shutdown()
        {
            foreach (var system in _systems)
            {
                system.Shutdown();
            }

            _systems.Clear();

            _isRunning = false;
        }

        private void RegisterSystems()
        {
            // Futuramente:
            // _systems.Add(new PlayerSystem());
            // _systems.Add(new EconomySystem());
            // _systems.Add(new VehicleSystem());
            // _systems.Add(new WorldSystem());
        }
    }
}

