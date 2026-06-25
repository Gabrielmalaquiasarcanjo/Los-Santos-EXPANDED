using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using LosSantosExpanded.lse.Systems.Core;
using LosSantosExpanded.lse.Systems.Wallet;
using System;
using System.Windows.Forms;

namespace LosSantosExpanded.Systems.Menu
{
    public class MenuSystem : IBaseSystem
    {
        private MenuPool _menuPool;
        private UIMenu _mainMenu;
        private UIMenu _moneySubmenu;
        
        private WalletSystem _wallet;
        private bool _menuKeyWasDown = false;
        private bool _isInitialized = false;

        public override void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                // Obtém referência do WalletSystem
                _wallet = ModController.Instance?.GetSystem<WalletSystem>();
                if (_wallet == null)
                {
                    Game.LogTrivial("[LSE] MenuSystem: AVISO - WalletSystem não encontrado. Algumas funções do menu não estarão disponíveis.");
                }

                // Cria o pool de menus
                _menuPool = new MenuPool();

                // Cria o menu principal
                _mainMenu = new UIMenu("Los Santos EXPANDED", "~b~Menu Principal");
                _menuPool.Add(_mainMenu);

                // Cria o submenu de dinheiro
                _moneySubmenu = new UIMenu("Gerenciar Dinheiro", "~g~Adicione ou remova dinheiro");
                _menuPool.Add(_moneySubmenu);

                // Constrói os itens do menu
                BuildMainMenu();
                BuildMoneySubmenu();

                // Inicia a fiber que processa o menu
                GameFiber.StartNew(ProcessMenuLoop);

                _isInitialized = true;
                Game.LogTrivial("[LSE] MenuSystem: Inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao inicializar MenuSystem: {ex.Message}");
            }
        }

        public override void Shutdown()
        {
            try
            {
                if (_mainMenu != null) 
                    _mainMenu.Visible = false;
                
                Game.LogTrivial("[LSE] MenuSystem: Finalizado.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao finalizar MenuSystem: {ex.Message}");
            }
            finally
            {
                base.Shutdown();
            }
        }

        #region Construção do Menu

        private void BuildMainMenu()
        {
            // --- Item: Exibir Saldo ---
            var balanceItem = new UIMenuItem("Exibir Saldo", "Mostra o saldo atual da sua carteira.");
            balanceItem.Activated += (sender, item) =>
            {
                if (_wallet != null)
                {
                    _wallet.ShowWallet();
                }
                else
                {
                    Game.DisplayNotification("~r~Erro: Sistema de carteira não disponível.");
                }
            };
            _mainMenu.AddItem(balanceItem);

            // --- Item: Submenu Dinheiro ---
            var moneySubmenuItem = new UIMenuItem("Gerenciar Dinheiro", "Acesse opções para adicionar ou remover dinheiro.");
            moneySubmenuItem.SetRightLabel("→");
            moneySubmenuItem.Activated += (sender, item) =>
            {
                _moneySubmenu.Visible = true;
            };
            _mainMenu.AddItem(moneySubmenuItem);

            // --- Item: Sincronizar com o Jogo (Checkbox) ---
            var syncItem = new UIMenuCheckboxItem(
                "Sincronizar com o Jogo", 
                false, 
                "Ao ativar, o saldo do jogo será sincronizado com a sua carteira."
            );
            syncItem.CheckboxEvent += (sender, item, isChecked) =>
            {
                if (_wallet != null)
                {
                    if (isChecked)
                    {
                        _wallet.SyncFromGame();
                        Game.DisplayNotification("~g~Sincronizado do jogo para a carteira.");
                    }
                    else
                    {
                        _wallet.SyncToGame();
                        Game.DisplayNotification("~g~Sincronizado da carteira para o jogo.");
                    }
                }
            };
            _mainMenu.AddItem(syncItem);

            // --- Item: Fechar Menu ---
            var closeItem = new UIMenuItem("Fechar Menu", "Fecha o menu.");
            closeItem.Activated += (sender, item) =>
            {
                _mainMenu.Visible = false;
            };
            _mainMenu.AddItem(closeItem);
        }

        private void BuildMoneySubmenu()
        {
            // --- Item: Adicionar $1000 ---
            var addMoneyItem = new UIMenuItem("Adicionar $1000", "Adiciona mil reais à sua carteira.");
            addMoneyItem.Activated += (sender, item) =>
            {
                _wallet?.AddMoney(1000);
            };
            _moneySubmenu.AddItem(addMoneyItem);

            // --- Item: Adicionar $5000 ---
            var addMoney5kItem = new UIMenuItem("Adicionar $5000", "Adiciona cinco mil reais à sua carteira.");
            addMoney5kItem.Activated += (sender, item) =>
            {
                _wallet?.AddMoney(5000);
            };
            _moneySubmenu.AddItem(addMoney5kItem);

            // --- Item: Remover $500 ---
            var removeMoneyItem = new UIMenuItem("Remover $500", "Remove quinhentos reais da sua carteira.");
            removeMoneyItem.Activated += (sender, item) =>
            {
                if (_wallet != null)
                {
                    _wallet.TryRemoveMoney(500);
                }
            };
            _moneySubmenu.AddItem(removeMoneyItem);

            // --- Item: Remover $1000 ---
            var removeMoney1kItem = new UIMenuItem("Remover $1000", "Remove mil reais da sua carteira.");
            removeMoney1kItem.Activated += (sender, item) =>
            {
                if (_wallet != null)
                {
                    _wallet.TryRemoveMoney(1000);
                }
            };
            _moneySubmenu.AddItem(removeMoney1kItem);

            // --- Item: Voltar ---
            var backItem = new UIMenuItem("Voltar", "Retorna ao menu principal.");
            backItem.Activated += (sender, item) =>
            {
                _moneySubmenu.Visible = false;
                _mainMenu.Visible = true;
            };
            _moneySubmenu.AddItem(backItem);
        }

        #endregion

        #region Loop de Processamento do Menu

        private void ProcessMenuLoop()
        {
            while (true)
            {
                try
                {
                    // Processa entrada e desenha o menu (recomendado pela documentação do RAGENativeUI)[reference:15]
                    _menuPool.ProcessMenus();

                    // Detecta a tecla F5 para abrir/fechar (com detecção de borda)
                    bool menuKeyDown = Game.IsKeyDown(Keys.F5);
                    if (menuKeyDown && !_menuKeyWasDown)
                    {
                        if (_mainMenu.Visible)
                        {
                            _mainMenu.Visible = false;
                        }
                        else
                        {
                            // Verifica se nenhum outro menu está visível[reference:16]
                            if (!UIMenu.IsAnyMenuVisible)
                            {
                                _mainMenu.Visible = true;
                            }
                        }
                        _menuKeyWasDown = true;
                    }
                    else if (!menuKeyDown)
                    {
                        _menuKeyWasDown = false;
                    }

                    GameFiber.Yield();
                }
                catch (Exception ex)
                {
                    Game.LogTrivial($"[LSE] ERRO no loop do menu: {ex.Message}");
                    GameFiber.Yield();
                }
            }
        }

        #endregion
    }
}