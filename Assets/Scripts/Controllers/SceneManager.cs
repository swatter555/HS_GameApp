using HammerAndSickle.Services;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Manages scene loading/unloading with fade transitions
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(SceneManager);

        #region Singleton

        private static SceneManager _instance;
        public static SceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SceneManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneManager");
                        _instance = go.AddComponent<SceneManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [SerializeField]
        private float fadeOutDuration = 1.0f;

        [SerializeField]
        private float fadeInDuration = 1.0f;

        #endregion

        #region Private Fields

        private Canvas _fadeCanvas;
        private Image _fadeImage;
        private bool _isTransitioning;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeFadeCanvas();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a scene by name with fade transition
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isTransitioning)
            {
                AppService.CaptureUiMessage("Scene transition already in progress");
                return;
            }

            StartCoroutine(LoadSceneWithFade(sceneName));
        }

        /// <summary>
        /// Loads a scene by build index with fade transition
        /// </summary>
        public void LoadScene(int sceneIndex)
        {
            if (_isTransitioning)
            {
                AppService.CaptureUiMessage("Scene transition already in progress");
                return;
            }

            StartCoroutine(LoadSceneWithFade(sceneIndex));
        }

        /// <summary>
        /// Gets the currently active scene name
        /// </summary>
        public string GetCurrentSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        #endregion

        #region Private Methods

        private void InitializeFadeCanvas()
        {
            try
            {
                GameObject canvasObj = new GameObject("FadeCanvas");
                canvasObj.transform.SetParent(transform);

                _fadeCanvas = canvasObj.AddComponent<Canvas>();
                _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _fadeCanvas.sortingOrder = 9999;

                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                GameObject imageObj = new GameObject("FadeImage");
                imageObj.transform.SetParent(canvasObj.transform);

                _fadeImage = imageObj.AddComponent<Image>();
                _fadeImage.color = new Color(0, 0, 0, 0);

                RectTransform rect = _fadeImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _fadeCanvas.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeFadeCanvas), e);
                throw;
            }
        }

        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            _isTransitioning = true;
            _fadeCanvas.gameObject.SetActive(true);

            // Fade out to black
            yield return StartCoroutine(FadeOut());

            // Load the scene
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            if (asyncLoad == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadSceneWithFade),
                    new InvalidOperationException($"Failed to start loading scene: {sceneName}"));
                AppService.CaptureUiMessage($"Failed to load scene: {sceneName}");
                _isTransitioning = false;
                _fadeCanvas.gameObject.SetActive(false);
                yield break;
            }

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Fade in from black
            yield return StartCoroutine(FadeIn());

            _fadeCanvas.gameObject.SetActive(false);
            _isTransitioning = false;
        }

        private IEnumerator LoadSceneWithFade(int sceneIndex)
        {
            _isTransitioning = true;
            _fadeCanvas.gameObject.SetActive(true);

            // Fade out to black
            yield return StartCoroutine(FadeOut());

            // Load the scene
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneIndex);

            if (asyncLoad == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadSceneWithFade),
                    new InvalidOperationException($"Failed to start loading scene at index: {sceneIndex}"));
                AppService.CaptureUiMessage($"Failed to load scene at index: {sceneIndex}");
                _isTransitioning = false;
                _fadeCanvas.gameObject.SetActive(false);
                yield break;
            }

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Fade in from black
            yield return StartCoroutine(FadeIn());

            _fadeCanvas.gameObject.SetActive(false);
            _isTransitioning = false;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
                _fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            _fadeImage.color = new Color(0, 0, 0, 1);
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
                _fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            _fadeImage.color = new Color(0, 0, 0, 0);
        }

        #endregion
    }
}