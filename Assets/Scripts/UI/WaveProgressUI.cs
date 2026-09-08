using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Tools.WaveManager))]
    public class WaveProgressUI : MonoBehaviour
    {
        private Tools.WaveManager _waves;
        private Text _label;
        private Image _fill;

        private void Start()
        {
            _waves = GetComponent<Tools.WaveManager>();
            var root = new GameObject("Wave HUD", typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var label = new GameObject("Progress", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(root.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -35f);
            rect.sizeDelta = new Vector2(560f, 55f);
            _label = label.GetComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 20;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.raycastTarget = false;
            var background = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(label.transform, false);
            rect = (RectTransform)background.transform;
            rect.anchoredPosition = new Vector2(0f, -40f);
            rect.sizeDelta = new Vector2(420f, 12f);
            background.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            background.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            rect = (RectTransform)fill.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _fill = fill.GetComponent<Image>();
            _fill.color = new Color(0.2f, 0.8f, 0.7f);
            _fill.raycastTarget = false;
        }

        private void Update()
        {
            _label.text = $"{_waves.Status}\nProgreso: {_waves.Defeated}/{_waves.TotalEnemies}";
            _fill.rectTransform.anchorMax = new Vector2(_waves.Progress, 1f);
        }
    }
}
