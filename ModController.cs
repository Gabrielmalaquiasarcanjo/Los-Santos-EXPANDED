using Rage;
using System;
using System.Collections.Generic;
using LosSantosExpanded.lse.Systems.Core;
using LosSantosExpanded.lse.Systems.Wallet;
using LosSantosExpanded.lse.Systems.Menu;

namespace LosSantosExpanded
{
    internal sealed class ModController
    {
        private static ModController _instance;
        public static ModController Instance
            => _instance ?? (_instance = new ModController());

        // FIX: Lista agora usa IBaseSystem (namespace correto do projeto)
        private readonly List<IBaseSystem> _systems = new List<IBaseSystem>();

        private bool _isRunning;

        private ModController() { }

        public void Initialize()
        {
            Game.LogTrivial("[LSE] Inicializando sistemas...");
            RegisterSystems();

            foreach (var system in _systems)
            {
                system.Initialize();
            }

            _isRunning = true;
            Game.LogTrivial("[LSE] Inicialização completa.");
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

        /// <summary>
        /// FIX: Método adicionado para que outros sistemas possam buscar referências entre si.
        /// Ex: MenuSystem busca WalletSystem via GetSystem<WalletSystem>()
        /// </summary>
        public T GetSystem<T>() where T : IBaseSystem
        {
            foreach (var system in _systems)
            {
                if (system is T match)
                    return match;
            }
            return null;
        }

        private void RegisterSystems()
        {
            // FIX: Sistemas agora são registrados corretamente.
            // A ordem importa: WalletSystem deve vir antes do MenuSystem,
            // pois o MenuSystem depende do WalletSystem.
            _systems.Add(new WalletSystem());
            _systems.Add(new MenuSystem());

            Game.LogTrivial($"[LSE] {_systems.Count} sistema(s) registrado(s).");
        }
    }
}