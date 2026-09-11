using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tools
{
    public class GameSessionManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _victoryDelay = 3f;
        private Player.PlayerHealth _health;
        private GameObject _panel;
        private Button _restart;
        private bool _restarting;
        private ArenaEncounterManager _encounter;
        private Text _resultText;

        private void Start()
        {
            _health = GameObject.FindGameObjectWithTag("Player").GetComponent<Player.PlayerHealth>();
            _health.Died += OnDefeat;
            _encounter = GetComponent<ArenaEncounterManager>();
            if (_encounter != null) _encounter.Completed += OnVictory;
            GameObject canvasObject = new GameObject("Session UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            _panel = new GameObject("Defeat", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)_panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            _resultText = MakeText("Has muerto", _panel.transform, new Vector2(0, 60), 36);
            GameObject button = new GameObject("Reintentar", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(_panel.transform, false);
            ((RectTransform)button.transform).sizeDelta = new Vector2(240, 60);
            button.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.5f);
            _restart = button.GetComponent<Button>();
            _restart.targetGraphic = button.GetComponent<Image>();
            _restart.onClick.AddListener(Restart);
            MakeText("Reintentar", button.transform, Vector2.zero, 24);
            _panel.SetActive(false);
        }

        private Text MakeText(string value, Transform parent, Vector2 position, int size)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = position;
            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private void OnDefeat()
        {
            ShowResult("Has muerto");
        }

        private void OnVictory()
        {
            StartCoroutine(ShowVictoryAfterDelay());
        }

        private System.Collections.IEnumerator ShowVictoryAfterDelay()
        {
            // Let the last death animation and loot finish before pausing the game.
            yield return new WaitForSeconds(_victoryDelay);
            if (_health == null || _health.IsDead || _restarting) yield break;
            ShowResult("Victoria - Arena completada");
        }

        private void ShowResult(string result)
        {
            _resultText.text = result;
            _resultText.rectTransform.sizeDelta = new Vector2(800f, 80f);
            Time.timeScale = 0f;
            _panel.SetActive(true);
            Inputs.InputManager.InputActions.asset.Disable();
            Inputs.InputManager.InputActions.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem.current.SetSelectedGameObject(_restart.gameObject);
        }

        public void Restart()
        {
            if (_restarting) return;
            _restarting = true;
            _restart.interactable = false;
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(gameObject.scene.path);
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDefeat;
            if (_encounter != null) _encounter.Completed -= OnVictory;
            Time.timeScale = 1f;
        }
    }
}
