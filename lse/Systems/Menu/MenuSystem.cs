using System;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using LosSantosExpanded.lse.Systems.Core;

namespace LosSantosExpanded.lse.Systems.Menu
{
    internal class MenuSystem : IBaseSystem
    {
        private MenuPool _menuPool;
        private UIMenu _mainMenu;
        private bool _isInitialized = false;
        public static event Action<int> OnAddMoneyRequested; // Evento para notificar quando um item do menu é selecionado
        public override void Initialize()
        {
            Game.LogTrivial("[LSE] Inicializando MenuSystem...");

            try
            {
                // Cria o pool de menus
                _menuPool = new MenuPool();

                // Cria o menu principal
                _mainMenu = new UIMenu("Los Santos EXPANDED", "~b~Menu Principal");
                _menuPool.Add(_mainMenu);

                // Adiciona alguns itens de exemplo
                var itemSair = new UIMenuItem("Sair", "Fecha o menu.");
                _mainMenu.AddItem(itemSair);

                var AdicionarDinheiroItem = new UIMenuItem("Adicionar Dinheiro", "Adiciona $10000 à carteira.");
                _mainMenu.AddItem(AdicionarDinheiroItem);

                // Evento: quando um item é selecionado
                _mainMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == itemSair)
                    {
                        _mainMenu.Visible = false;
                        Game.DisplayNotification("~g~Menu fechado.");
                    }
                    else if (item == AdicionarDinheiroItem)
                    {
                        OnAddMoneyRequested?.Invoke(10000);
                        _mainMenu.Visible = false;
                        Game.DisplayNotification("~g~$10000 adicionado à carteira.");
                    }
                };

                // Inicia a fiber que processa o menu
                GameFiber.StartNew(ProcessMenus);

                _isInitialized = true;
                Game.LogTrivial("[LSE] MenuSystem inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao inicializar MenuSystem: {ex.Message}");
            }
        }

        public override void Update()
        {
            // O processamento contínuo é feito na fiber separada
            // Este método é chamado a cada tick pelo ModController
        }

        public override void Shutdown()
        {
            Game.LogTrivial("[LSE] Desligando MenuSystem...");
            OnAddMoneyRequested = null; // Remove todas as inscrições do evento
            
            _mainMenu?.Visible = false;
            _menuPool = null;
            _mainMenu = null;
            _isInitialized = false;
        }

        private void ProcessMenus()
        {
            while (_isInitialized)
            {
                GameFiber.Yield();

                // Processa todos os menus do pool
                _menuPool?.ProcessMenus();

                // Tecla para abrir/fechar o menu (ex: F6)
                if (Game.IsKeyDown(System.Windows.Forms.Keys.F7))
                {
                    if (_mainMenu.Visible)
                    {
                        _mainMenu.Visible = false;
                    }
                    else if (!UIMenu.IsAnyMenuVisible)
                    {
                        _mainMenu.Visible = true;
                    }
                }
            }
        }
    }
}