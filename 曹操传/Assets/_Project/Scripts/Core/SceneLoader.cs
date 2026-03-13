using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CaoCao.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] float fadeDuration = 0.4f;

        CanvasGroup _fadeOverlay;
        Canvas _fadeCanvas;

        void Awake()
        {
            EnsureFadeOverlay();
        }

        void EnsureFadeOverlay()
        {
            if (_fadeCanvas != null) return;

            var go = new GameObject("FadeOverlay");
            go.transform.SetParent(transform);
            _fadeCanvas = go.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 9999;

            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            _fadeOverlay = go.AddComponent<CanvasGroup>();
            _fadeOverlay.alpha = 0f;
            _fadeOverlay.blocksRaycasts = false;
            _fadeOverlay.interactable = false;

            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void LoadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
        }

        IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
        {
            EnsureFadeOverlay();

            // Fade out
            yield return FadeRoutine(0f, 1f, fadeDuration);

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;

            // Fade in
            yield return FadeRoutine(1f, 0f, fadeDuration);

            onComplete?.Invoke();
        }

        IEnumerator FadeRoutine(float from, float to, float duration)
        {
            _fadeOverlay.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _fadeOverlay.alpha = to;
            _fadeOverlay.blocksRaycasts = to > 0.5f;
        }
    }
}
