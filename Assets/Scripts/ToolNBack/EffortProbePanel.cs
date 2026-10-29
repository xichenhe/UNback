using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToolNBack
{
    /// <summary>
    /// World-space rating scale for the effort probe: the question, one box per number,
    /// labels under the two ends and a controls hint, on a dark backdrop at the panel
    /// centre facing the user. The manager creates it once and shows it after each block.
    /// </summary>
    public class EffortProbePanel : MonoBehaviour
    {
        public const string ObjectName = "Effort Probe Canvas";

        private const float CanvasWidth = 1000f;
        private const float CanvasHeight = 460f;
        private const float RowWidth = 900f;
        private const float BoxGap = 12f;

        private static readonly Color BoxColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color SelectedBoxColor = new Color(0.2f, 0.7f, 1f, 0.95f);

        private Font _font;
        private Text _question;
        private Text _lowLabel;
        private Text _highLabel;
        private RectTransform _row;
        private Image[] _boxes = Array.Empty<Image>();
        private int _scaleMin;

        /// <summary>The highlighted number, or null when nothing is selected.</summary>
        public int? HighlightedValue { get; private set; }

        /// <summary>Builds the hidden panel as a child of <paramref name="parent"/> at the given world pose.</summary>
        public static EffortProbePanel Create(Transform parent, Vector3 position, Quaternion rotation, Color backdropColor)
        {
            var go = new GameObject(ObjectName);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            go.transform.localScale = Vector3.one * 0.001f;
            go.transform.SetPositionAndRotation(position, rotation);

            var panel = go.AddComponent<EffortProbePanel>();
            panel.Build(backdropColor);
            go.SetActive(false);
            return panel;
        }

        /// <summary>Shows the scale with nothing selected.</summary>
        public void Show(string question, string lowLabel, string highLabel, int scaleMin, int scaleMax)
        {
            _question.text = question;
            _lowLabel.text = lowLabel;
            _highLabel.text = highLabel;
            BuildBoxes(scaleMin, scaleMax);
            SetSelection(null);
            gameObject.SetActive(true);
        }

        public void SetSelection(int? value)
        {
            HighlightedValue = value;
            for (int i = 0; i < _boxes.Length; i++)
                _boxes[i].color = value == _scaleMin + i ? SelectedBoxColor : BoxColor;
        }

        public void Hide() => gameObject.SetActive(false);

        private void Build(Color backdropColor)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Hierarchy order is draw order in world-space UI: backdrop first.
            var backdrop = NewChild<Image>("Backdrop");
            backdrop.color = backdropColor;
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);

            _question = NewText("Question", 42, TextAnchor.MiddleCenter);
            Place(_question.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -20f));

            _row = NewChild<RectTransform>("Scale");
            _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 0.5f);
            _row.sizeDelta = new Vector2(RowWidth, 110f);
            _row.anchoredPosition = new Vector2(0f, 10f);

            _lowLabel = NewText("Low Label", 26, TextAnchor.UpperLeft);
            Place(_lowLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(50f, 80f), new Vector2(0f, 150f));

            _highLabel = NewText("High Label", 26, TextAnchor.UpperRight);
            Place(_highLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0f, 80f), new Vector2(-50f, 150f));

            Text hint = NewText("Hint", 24, TextAnchor.MiddleCenter);
            hint.color = new Color(0.8f, 0.8f, 0.8f);
            // Index buttons may not arrive in recording mode; the experimenter then types
            // the number the participant says.
            hint.text = "Choose with the stick, confirm with A or the trigger, or say your number aloud";
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 15f), new Vector2(-20f, 65f));
        }

        private void BuildBoxes(int scaleMin, int scaleMax)
        {
            int count = scaleMax - scaleMin + 1;
            if (_boxes.Length == count && _scaleMin == scaleMin)
                return;

            foreach (Image box in _boxes)
            {
                if (Application.isPlaying)
                    Destroy(box.gameObject);
                else
                    DestroyImmediate(box.gameObject);
            }

            _scaleMin = scaleMin;
            _boxes = new Image[count];
            float width = (RowWidth - BoxGap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                var boxGo = new GameObject($"Box {scaleMin + i}", typeof(RectTransform));
                boxGo.transform.SetParent(_row, false);
                var box = boxGo.AddComponent<Image>();
                RectTransform rect = box.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(width, 100f);
                rect.anchoredPosition = new Vector2(i * (width + BoxGap) + width * 0.5f, 0f);

                var numberGo = new GameObject("Number", typeof(RectTransform));
                numberGo.transform.SetParent(rect, false);
                var number = numberGo.AddComponent<Text>();
                number.font = _font;
                number.fontSize = 52;
                number.alignment = TextAnchor.MiddleCenter;
                number.color = Color.white;
                number.text = (scaleMin + i).ToString();
                Stretch(number.rectTransform, Vector2.zero, Vector2.zero);

                _boxes[i] = box;
            }
        }

        private T NewChild<T>(string childName) where T : Component
        {
            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(transform, false);
            return typeof(T) == typeof(RectTransform)
                ? child.GetComponent<T>()
                : child.AddComponent<T>();
        }

        private Text NewText(string childName, int fontSize, TextAnchor alignment)
        {
            var text = NewChild<Text>(childName);
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax) =>
            Place(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
