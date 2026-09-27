using UnityEngine;
using UnityEngine.UI;

namespace SkinAI
{
    /// <summary>
    /// A world-anchored panel that shows the diagnosis in front of the user.
    /// Built at runtime. Uses a dynamic OS font (Roboto on Quest includes Cyrillic),
    /// so the Ukrainian result text renders correctly without any TMP font setup.
    /// </summary>
    public class ResultPanelUI : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Distance in front of the user where the panel appears, in meters.")]
        public float distance = 0.7f;
        [Tooltip("Drop the panel slightly below eye line so it doesn't cover the scope.")]
        public float verticalOffset = -0.1f;

        [Header("Size")]
        public Vector2 panelPixelSize = new Vector2(900f, 640f);
        public float metersPerPixel = 0.0006f;

        [Header("Colors")]
        public Color panelColor = new Color(0f, 0f, 0f, 0.8f);
        public Color titleColor = new Color(0.5f, 1f, 0.7f, 1f);
        public Color bodyColor = Color.white;
        public Color errorColor = new Color(1f, 0.5f, 0.5f, 1f);

        private GameObject _root;
        private Text _title;
        private Text _body;
        private Font _font;

        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont(
                new[] { "Roboto", "Noto Sans", "Droid Sans", "Arial", "sans-serif" }, 32);
            Build();
            Hide();
        }

        private void Build()
        {
            _root = new GameObject("ResultPanel");
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = panelPixelSize;
            _root.transform.localScale = Vector3.one * metersPerPixel;

            // Background
            var bg = new GameObject("BG");
            bg.transform.SetParent(_root.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = panelColor;
            var bgrt = bgImg.rectTransform;
            bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

            // Title
            _title = MakeText("Title", 54, FontStyle.Bold, titleColor, TextAnchor.UpperCenter);
            var trt = _title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(30f, -150f);
            trt.offsetMax = new Vector2(-30f, -20f);

            // Body
            _body = MakeText("Body", 30, FontStyle.Normal, bodyColor, TextAnchor.UpperLeft);
            var brt = _body.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(30f, 30f);
            brt.offsetMax = new Vector2(-30f, -160f);
        }

        private Text MakeText(string name, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            return t;
        }

        public void ShowResult(string title, string body)
        {
            _title.color = titleColor;
            _title.text = string.IsNullOrEmpty(title) ? "—" : title;
            _body.text = body ?? "";
            PlaceInFront();
            _root.SetActive(true);
        }

        public void ShowError(string message)
        {
            _title.color = errorColor;
            _title.text = "Помилка";
            _body.text = message ?? "";
            PlaceInFront();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void PlaceInFront()
        {
            Camera cam = Camera.main;
            if (cam == null)
                foreach (var c in Camera.allCameras) { cam = c; break; }
            if (cam == null) return;

            Transform h = cam.transform;
            // World-anchored: placed once, then it stays put (not parented to the head).
            _root.transform.position = h.position + h.forward * distance + Vector3.up * verticalOffset;
            // Canvas forward aligned with camera forward => readable to the user.
            _root.transform.rotation = Quaternion.LookRotation(h.forward, h.up);
        }
    }
}
