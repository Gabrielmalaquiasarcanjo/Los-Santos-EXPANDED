using Rage;
using System;
using System.IO;
using System.Xml.Serialization;
using LosSantosExpanded.lse.Systems.Core;
using LosSantosExpanded.lse.Systems.Menu;

namespace LosSantosExpanded.lse.Systems.Wallet
{
    public class WalletSystem : IBaseSystem
    {
        #region Campos e Propriedades
        private const string SAVE_FILE_NAME = "wallet_data.xml";
        private string _savePath;
        private int _cash;
        private readonly object _lock = new object();
        public event Action<int, int> OnBalanceChanged; // (novoValor, valorAntigo)

        // Propriedade pública segura para leitura
        public int Cash
        {
            get
            {
                lock (_lock) return _cash;
            }
            private set
            {
                // NOTA: Este setter deve ser chamado APENAS de dentro de um lock(_lock) externo
                // para evitar double-lock. Use os métodos internos _SetCashUnsafe() diretamente.
                int oldValue = _cash;
                if (value < 0) value = 0; // Nunca permitir negativo
                if (_cash != value)
                {
                    _cash = value;
                    OnBalanceChanged?.Invoke(_cash, oldValue);
                    SaveData(); // Salva automaticamente a cada alteração
                }
            }
        }
        #endregion

        #region Inicialização e Finalização
        public override void Initialize()
        {
            base.Initialize();
            try
            {
                MenuSystem.OnAddMoneyRequested += OnAddMoneyRequested; // Inscreve-se no evento do menu
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao inscrever-se no evento do MenuSystem: {ex.Message}");
            }

            try
            {
                // Define o caminho do arquivo de save (dentro da pasta do mod)
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string modFolder = Path.Combine(documentsPath, "Rockstar Games", "GTA V", "LosSantosExpanded");


                if (!Directory.Exists(modFolder))
                    Directory.CreateDirectory(modFolder);

                _savePath = Path.Combine(modFolder, SAVE_FILE_NAME);

                // Carrega os dados salvos
                LoadData();

                // Sincroniza com o dinheiro nativo do jogo ao iniciar
                SyncFromGame();

                Game.LogTrivial($"[LSE] WalletSystem: Inicializado com ${Cash}.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO CRÍTICO ao inicializar WalletSystem: {ex.Message}");
                _cash = 0;
            }
        }
        private void OnAddMoneyRequested(int amount)
        {
            AddMoney(amount);
        }

        public override void Shutdown()
        {
            MenuSystem.OnAddMoneyRequested -= OnAddMoneyRequested; // Remove a inscrição do evento
            
            try
            {
                SaveData();
                Game.LogTrivial($"[LSE] WalletSystem: Finalizado. Saldo final: ${Cash}.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao finalizar WalletSystem: {ex.Message}");
            }
            finally
            {
                base.Shutdown();
            }
        }
        #endregion

        #region Update (teclas de atalho)
        public override void Update()
        {
            try
            {
                // F6 → Adiciona $1000
                if (Game.IsKeyDown(System.Windows.Forms.Keys.F6))
                {
                    AddMoney(1000);
                    Game.LogTrivial("[LSE] WalletSystem: Adicionado $1000 via tecla F6.");
                }

                // L → Remove $500
                if (Game.IsKeyDown(System.Windows.Forms.Keys.L))
                {
                    TryRemoveMoney(500);
                    Game.LogTrivial("[LSE] WalletSystem: Tentativa de remover $500 via tecla L.");
                }

                // F7 → Exibe saldo
                if (Game.IsKeyDown(System.Windows.Forms.Keys.J))
                {
                    ShowWallet();
                    Game.LogTrivial("[LSE] WalletSystem: Saldo exibido via tecla F7.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO no Update do WalletSystem: {ex.Message}");
            }
        }
        #endregion

        #region Métodos Públicos (API do Sistema)

        /// <summary>
        /// Adiciona dinheiro à carteira.
        /// </summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Game.LogTrivial("[LSE] WalletSystem: Tentativa de adicionar valor inválido (<=0).");
                return;
            }

            lock (_lock)
            {
                // FIX: Operamos diretamente em _cash para evitar double-lock com a propriedade Cash
                int oldValue = _cash;
                _cash += amount;
                OnBalanceChanged?.Invoke(_cash, oldValue);
                SaveData();
            }

            Game.DisplayNotification($"~g~+${amount:N0}~s~ adicionado à carteira.");
        }

        /// <summary>
        /// Tenta remover dinheiro da carteira.
        /// </summary>
        /// <returns>True se conseguiu remover, False se saldo insuficiente.</returns>
        public bool TryRemoveMoney(int amount)
        {
            if (amount <= 0)
            {
                Game.LogTrivial("[LSE] WalletSystem: Tentativa de remover valor inválido (<=0).");
                return false;
            }

            lock (_lock)
            {
                if (_cash < amount)
                {
                    Game.DisplayNotification($"~r~Saldo insuficiente!~s~ Necessário: ${amount:N0}, Disponível: ${_cash:N0}.");
                    return false;
                }

                // FIX: Operamos diretamente em _cash para evitar double-lock com a propriedade Cash
                int oldValue = _cash;
                _cash -= amount;
                OnBalanceChanged?.Invoke(_cash, oldValue);
                SaveData();

                Game.DisplayNotification($"~r~-${amount:N0}~s~ removido da carteira.");
                return true;
            }
        }

        /// <summary>
        /// Exibe o saldo atual via notificação na tela.
        /// </summary>
        public void ShowWallet()
        {
            int currentCash = Cash;
            Game.LogTrivial($"[LSE] WalletSystem: Saldo atual: ${currentCash}.");
            Game.DisplayNotification($"~b~Carteira:~s~ ${currentCash:N0}");
        }

        /// <summary>
        /// Define um valor exato para a carteira (útil para carregar saves).
        /// </summary>
        public void SetMoney(int newAmount)
        {
            if (newAmount < 0) newAmount = 0;

            lock (_lock)
            {
                int oldValue = _cash;
                _cash = newAmount;
                OnBalanceChanged?.Invoke(_cash, oldValue);
                SaveData();
            }
        }

        /// <summary>
        /// Verifica se o jogador tem saldo suficiente.
        /// </summary>
        public bool HasEnough(int amount)
        {
            if (amount <= 0) return true;
            lock (_lock) return _cash >= amount;
        }

        /// <summary>
        /// Sincroniza o dinheiro do jogo para a carteira (lê o dinheiro nativo).
        /// </summary>
        public void SyncFromGame()
        {
            try
            {
                if (Game.LocalPlayer.Character.Exists())
                {
                    int gameMoney = Game.LocalPlayer.Character.Money;
                    SetMoney(gameMoney);
                    Game.LogTrivial($"[LSE] WalletSystem: Sincronizado do jogo. Saldo: ${Cash}.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] Erro ao sincronizar do jogo: {ex.Message}");
            }
        }

        /// <summary>
        /// Sincroniza a carteira para o dinheiro do jogo (sobrescreve o dinheiro nativo).
        /// CUIDADO: Isso modifica o dinheiro do personagem no jogo.
        /// </summary>
        public void SyncToGame()
        {
            try
            {
                if (Game.LocalPlayer.Character.Exists())
                {
                    Game.LocalPlayer.Character.Money = Cash;
                    Game.LogTrivial($"[LSE] WalletSystem: Sincronizado para o jogo. Saldo: ${Cash}.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] Erro ao sincronizar para o jogo: {ex.Message}");
            }
        }
        #endregion

        #region Persistência de Dados (Salvar/Carregar)
        private void LoadData()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(WalletData));
                    using (FileStream stream = new FileStream(_savePath, FileMode.Open))
                    {
                        var data = (WalletData)serializer.Deserialize(stream);
                        _cash = data.Cash >= 0 ? data.Cash : 0;
                    }
                    Game.LogTrivial($"[LSE] WalletSystem: Dados carregados com sucesso. Saldo: ${_cash}.");
                }
                else
                {
                    _cash = 0;
                    Game.LogTrivial("[LSE] WalletSystem: Nenhum arquivo de save encontrado. Iniciando com $0.");
                    SaveData(); // Cria o arquivo com valor padrão
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao carregar dados da carteira: {ex.Message}");
                _cash = 0;
            }
        }

        private void SaveData()
        {
            try
            {
                if (string.IsNullOrEmpty(_savePath)) return;

                XmlSerializer serializer = new XmlSerializer(typeof(WalletData));
                using (FileStream stream = new FileStream(_savePath, FileMode.Create))
                {
                    var data = new WalletData { Cash = _cash };
                    serializer.Serialize(stream, data);
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[LSE] ERRO ao salvar dados da carteira: {ex.Message}");
            }
        }

        // Classe auxiliar para serialização XML
        [Serializable]
        public class WalletData
        {
            public int Cash { get; set; }
        }
        #endregion
    }
}