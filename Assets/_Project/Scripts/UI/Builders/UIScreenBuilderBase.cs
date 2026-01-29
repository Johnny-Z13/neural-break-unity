using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Base class for all UI screen builders.
    /// Provides common utilities for creating UI elements programmatically.
    /// </summary>
    public abstract class UIScreenBuilderBase
    {
        protected readonly TMP_FontAsset _fontAsset;
        protected readonly bool _useThemeColors;

        // Color accessors
        protected Color BackgroundColor => _useThemeColors ? UITheme.BackgroundDark : _customBackgroundColor;
        protected Color PrimaryColor => _useThemeColors ? UITheme.Primary : _customPrimaryColor;
        protected Color AccentColor => _useThemeColors ? UITheme.Accent : _customAccentColor;
        protected Color TextColor => _useThemeColors ? UITheme.TextPrimary : _customTextColor;

        // Custom colors (fallback)
        private readonly Color _customBackgroundColor;
        private readonly Color _customPrimaryColor;
        private readonly Color _customAccentColor;
        private readonly Color _customTextColor;

        protected UIScreenBuilderBase(
            TMP_FontAsset fontAsset,
            bool useThemeColors = true,
            Color? customBg = null,
            Color? customPrimary = null,
            Color? customAccent = null,
            Color? customText = null)
        {
            _fontAsset = fontAsset;
            _useThemeColors = useThemeColors;
            _customBackgroundColor = customBg ?? new Color(0.05f, 0.05f, 0.1f, 0.9f);
            _customPrimaryColor = customPrimary ?? new Color(0f, 1f, 1f, 1f);
            _customAccentColor = customAccent ?? new Color(1f, 0.3f, 0.5f, 1f);
            _customTextColor = customText ?? Color.white;
        }

        #region UI Creation Helpers

        /// <summary>
        /// Create a new UI GameObject with RectTransform
        /// </summary>
        protected RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Add TextMeshProUGUI component with font
        /// </summary>
        protected TextMeshProUGUI AddTextComponent(GameObject go)
        {
            if (go == null)
            {
                Debug.LogError("[UIScreenBuilderBase] AddTextComponent called with null GameObject!");
                return null;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogError($"[UIScreenBuilderBase] Failed to add TextMeshProUGUI to '{go.name}'! Check for conflicting components.");
                return null;
            }

            if (_fontAsset != null)
            {
                tmp.font = _fontAsset;
            }
            return tmp;
        }

        /// <summary>
        /// Stretch RectTransform to fill parent
        /// </summary>
        protected void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Set anchors for RectTransform
        /// </summary>
        protected void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        /// <summary>
        /// Create text element with standard settings
        /// </summary>
        protected TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            string text,
            int fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)
        {
            var textRect = CreateUIObject(name, parent);
            StretchToFill(textRect);
            var tmp = AddTextComponent(textRect.gameObject);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            return tmp;
        }

        /// <summary>
        /// Create button with standard styling
        /// </summary>
        protected Button CreateButton(
            string name,
            RectTransform parent,
            string text,
            Vector2 position,
            Vector2 size)
        {
            var btnRect = CreateUIObject(name, parent);
            btnRect.anchoredPosition = position;
            btnRect.sizeDelta = size;

            // Button background
            var btnImg = btnRect.gameObject.AddComponent<Image>();
            btnImg.color = UITheme.ButtonNormal;

            // Button component
            var btn = btnRect.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = UITheme.ButtonNormal;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.selectedColor = UITheme.ButtonSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Enable automatic navigation
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;

            // Button text
            CreateText(btnRect, "Text", text, 24, TextColor, TextAlignmentOptions.Center, FontStyles.Bold);

            return btn;
        }

        /// <summary>
        /// Create image-based bar with background and fill
        /// </summary>
        protected (Image background, Image fill) CreateBar(
            RectTransform parent,
            string name,
            Color bgColor,
            Color fillColor)
        {
            // Background
            var bgRect = CreateUIObject($"{name}BG", parent);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = bgColor;

            // Fill
            var fillRect = CreateUIObject($"{name}Fill", bgRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            return (bgImg, fillImg);
        }

        #endregion

        #region Reflection Helpers

        /// <summary>
        /// Set private field on component using reflection
        /// </summary>
        protected void SetPrivateField<T>(T component, string fieldName, object value)
        {
            var type = typeof(T);
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(component, value);
            }
            else
            {
                Debug.LogWarning($"[UIBuilder] Field '{fieldName}' not found on {type.Name}");
            }
        }

        #endregion
    }
}
