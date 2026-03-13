using UnityEngine;
using CaoCao.Camp;
using CaoCao.Data;
using CaoCao.Localization;

namespace CaoCao.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] GameSettingsData gameSettings;

        [Header("Game Data")]
        [SerializeField] GameDataRegistry gameDataRegistry;

        public GameSettingsData Settings => gameSettings;
        public string CurrentLanguage => _localization?.CurrentLanguage ?? "zh";

        LocalizationManager _localization;
        SaveSystem _saveSystem;
        SceneLoader _sceneLoader;
        GameStateManager _gameStateManager;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

            InitializeSystems();
        }

        void InitializeSystems()
        {
            // Save system
            _saveSystem = new SaveSystem();
            _saveSystem.EnsureDataDirectory();
            ServiceLocator.Register(_saveSystem);

            // Localization
            _localization = new LocalizationManager();
            _localization.LoadFromResources("Data/i18n");
            string savedLang = gameSettings != null ? gameSettings.language : "zh";
            _localization.SetLanguage(savedLang);
            ServiceLocator.Register(_localization);

            // Scene loader
            _sceneLoader = GetComponent<SceneLoader>();
            if (_sceneLoader == null)
                _sceneLoader = gameObject.AddComponent<SceneLoader>();
            ServiceLocator.Register(_sceneLoader);

            // Game data registry
            if (gameDataRegistry == null)
                gameDataRegistry = Resources.Load<GameDataRegistry>("Data/GameDataRegistry");
            if (gameDataRegistry != null)
            {
                gameDataRegistry.Initialize();
                ServiceLocator.Register(gameDataRegistry);
            }

            // Game state manager
            _gameStateManager = new GameStateManager(gameDataRegistry, _saveSystem);
            ServiceLocator.Register(_gameStateManager);

            // Camp manager (overlay — listens for story completion)
            if (GetComponent<CampManager>() == null)
                gameObject.AddComponent<CampManager>();
        }

        public void SetLanguage(string lang)
        {
            _localization.SetLanguage(lang);
            if (gameSettings != null)
                gameSettings.language = lang;
            EventBus.LanguageChanged(lang);
        }

        void Start()
        {
            // Boot scene's only job: load MainMenu
            _sceneLoader.LoadScene("MainMenu");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Clear();
                EventBus.Clear();
                Instance = null;
            }
        }
    }
}
