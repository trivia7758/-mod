using UnityEngine;
using UnityEngine.UI;

namespace CaoCao.UI
{
    public class ScreenManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] LogoScreen logoScreen;
        [SerializeField] UpdateScreen updateScreen;
        [SerializeField] AnnouncementScreen announcementScreen;
        [SerializeField] MenuScreen menuScreen;
        [SerializeField] SettingsDialog settingsDialog;
        [SerializeField] LoadDialog loadDialog;

        BaseScreen _current;

        void Awake()
        {
            EnsureCanvasSetup();
        }

        void EnsureCanvasSetup()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        void Start()
        {
            if (logoScreen != null) logoScreen.Initialize();
            if (updateScreen != null) updateScreen.Initialize();
            if (announcementScreen != null) announcementScreen.Initialize();
            if (settingsDialog != null) settingsDialog.Initialize();
            if (loadDialog != null) loadDialog.Initialize();
            if (menuScreen != null) menuScreen.Initialize();

            if (logoScreen != null) logoScreen.OnFinished += OnLogoFinished;
            if (updateScreen != null) updateScreen.OnUpdateCompleted += OnUpdateCompleted;
            if (announcementScreen != null) announcementScreen.OnClosed += OnAnnouncementClosed;
            if (menuScreen != null)
            {
                menuScreen.OnStartGame += OnStartGame;
                menuScreen.OnOpenAnnouncement += OnOpenAnnouncement;
            }
            if (loadDialog != null)
            {
                loadDialog.OnSlotSelected += OnLoadSlotSelected;
            }

            // Go directly to main menu (skip logo/update)
            ShowScreen(menuScreen);
        }

        void ShowScreen(BaseScreen screen)
        {
            if (logoScreen != null) logoScreen.Hide();
            if (updateScreen != null) updateScreen.Hide();
            if (announcementScreen != null) announcementScreen.Hide();
            if (menuScreen != null) menuScreen.Hide();
            if (screen != null) screen.Show();
            _current = screen;
        }

        void OnLogoFinished()
        {
            ShowScreen(updateScreen);
            if (updateScreen != null) updateScreen.StartCheck();
        }

        void OnUpdateCompleted(bool hasUpdate)
        {
            ShowScreen(menuScreen);
            if (hasUpdate)
                ShowAnnouncementOverlay();
        }

        void OnAnnouncementClosed()
        {
            if (announcementScreen != null) announcementScreen.Hide();
        }

        void OnOpenAnnouncement()
        {
            ShowAnnouncementOverlay();
        }

        void ShowAnnouncementOverlay()
        {
            if (menuScreen != null) menuScreen.Show();
            if (announcementScreen != null) announcementScreen.Show();
        }

        void OnStartGame()
        {
            // Reset the loaded flag so story plays normally for new game
            var gsm = Core.ServiceLocator.Get<Core.GameStateManager>();
            if (gsm != null)
            {
                gsm.WasLoadedFromSave = false;
                gsm.InitializeNewGame();
            }

            var loader = Core.ServiceLocator.Get<Core.SceneLoader>();
            if (loader != null)
                loader.LoadScene("StoryScene");
        }

        void OnLoadSlotSelected(int slot)
        {
            var gsm = Core.ServiceLocator.Get<Core.GameStateManager>();
            if (gsm == null)
            {
                Debug.LogWarning("[ScreenManager] GameStateManager not found, cannot load");
                return;
            }

            if (!gsm.LoadFromSlot(slot))
            {
                Debug.Log($"[ScreenManager] Slot {slot} is empty");
                return;
            }

            // Hide load dialog
            if (loadDialog != null) loadDialog.Hide();

            // Navigate to saved scene (defaults to StoryScene)
            string sceneName = gsm.State.currentScene;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "StoryScene";

            Debug.Log($"[ScreenManager] Loading scene: {sceneName}");
            var loader = Core.ServiceLocator.Get<Core.SceneLoader>();
            if (loader != null)
            {
                loader.LoadScene(sceneName);
            }
            else
            {
                // Fallback when SceneLoader doesn't exist (e.g., started from StoryScene)
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        void OnDestroy()
        {
            if (logoScreen != null) logoScreen.OnFinished -= OnLogoFinished;
            if (updateScreen != null) updateScreen.OnUpdateCompleted -= OnUpdateCompleted;
            if (announcementScreen != null) announcementScreen.OnClosed -= OnAnnouncementClosed;
            if (menuScreen != null)
            {
                menuScreen.OnStartGame -= OnStartGame;
                menuScreen.OnOpenAnnouncement -= OnOpenAnnouncement;
            }
            if (loadDialog != null)
            {
                loadDialog.OnSlotSelected -= OnLoadSlotSelected;
            }
        }
    }
}
