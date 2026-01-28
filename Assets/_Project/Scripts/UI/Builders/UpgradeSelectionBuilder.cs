using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builder for upgrade selection screen.
    /// Creates card layout programmatically.
    /// </summary>
    public class UpgradeSelectionBuilder : UIScreenBuilderBase
    {
        public UpgradeSelectionBuilder(TMP_FontAsset fontAsset) : base(fontAsset, useThemeColors: true)
        {
        }

        /// <summary>
        /// Build complete upgrade selection screen.
        /// </summary>
        public UpgradeSelectionScreen Build(Transform parent)
        {
            // Create screen root
            var screenRoot = CreateUIObject("UpgradeSelectionScreen", parent);
            StretchToFill(screenRoot);

            // Add canvas group for fade effects
            var canvasGroup = screenRoot.gameObject.AddComponent<CanvasGroup>();

            // Create background overlay
            CreateBackgroundOverlay(screenRoot);

            // Create content panel
            var contentPanel = CreateContentPanel(screenRoot);

            // Create title
            CreateTitle(contentPanel);

            // Create subtitle
            CreateSubtitle(contentPanel);

            // Create card container
            var cardContainer = CreateCardContainer(contentPanel);

            // Add screen component
            var screen = screenRoot.gameObject.AddComponent<UpgradeSelectionScreen>();
            SetPrivateField(screen, "_screenRoot", screenRoot.gameObject);
            SetPrivateField(screen, "_cardContainer", cardContainer);

            // Find and set text references
            var titleText = contentPanel.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            var subtitleText = contentPanel.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
            SetPrivateField(screen, "_titleText", titleText);
            SetPrivateField(screen, "_subtitleText", subtitleText);

            // Start hidden
            screenRoot.gameObject.SetActive(false);

            return screen;
        }

        private void CreateBackgroundOverlay(RectTransform parent)
        {
            var overlayRect = CreateUIObject("Overlay", parent);
            StretchToFill(overlayRect);

            var overlayImg = overlayRect.gameObject.AddComponent<Image>();
            overlayImg.color = UITheme.BackgroundOverlay;
        }

        private RectTransform CreateContentPanel(RectTransform parent)
        {
            var panelRect = CreateUIObject("ContentPanel", parent);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1200f, 700f);

            // Background
            var panelImg = panelRect.gameObject.AddComponent<Image>();
            panelImg.color = UITheme.BackgroundDark;

            // Border
            var outline = panelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = UITheme.Primary;
            outline.effectDistance = new Vector2(2f, -2f);

            // Layout
            var vlg = panelRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            return panelRect;
        }

        private void CreateTitle(RectTransform parent)
        {
            var titleRect = CreateUIObject("TitleText", parent);
            titleRect.sizeDelta = new Vector2(1100f, 60f);

            var titleText = AddTextComponent(titleRect.gameObject);
            titleText.text = "LEVEL COMPLETE";
            titleText.fontSize = UITheme.FontSize.Headline;
            titleText.color = UITheme.Primary;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
        }

        private void CreateSubtitle(RectTransform parent)
        {
            var subtitleRect = CreateUIObject("SubtitleText", parent);
            subtitleRect.sizeDelta = new Vector2(1100f, 40f);

            var subtitleText = AddTextComponent(subtitleRect.gameObject);
            subtitleText.text = "CHOOSE YOUR UPGRADE";
            subtitleText.fontSize = UITheme.FontSize.Medium;
            subtitleText.color = UITheme.TextSecondary;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontStyle = FontStyles.Normal;
        }

        private Transform CreateCardContainer(RectTransform parent)
        {
            var containerRect = CreateUIObject("CardContainer", parent);
            containerRect.sizeDelta = new Vector2(1100f, 500f);

            // Horizontal layout for cards
            var hlg = containerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            return containerRect;
        }

        /// <summary>
        /// Build a single upgrade card (can be used as prefab or instantiated).
        /// </summary>
        public UpgradeCard BuildCard(Transform parent)
        {
            var cardRect = CreateUIObject("UpgradeCard", parent);
            cardRect.sizeDelta = new Vector2(340f, 480f);

            // Background with glow
            var bgImg = cardRect.gameObject.AddComponent<Image>();
            bgImg.color = UITheme.BackgroundMedium;
            bgImg.raycastTarget = true; // Ensure clicks are detected

            // Outline
            var outline = cardRect.gameObject.AddComponent<Outline>();
            outline.effectColor = UITheme.Primary.WithAlpha(0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Button
            var button = cardRect.gameObject.AddComponent<Button>();
            button.targetGraphic = bgImg; // IMPORTANT: Set target graphic for click detection
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UITheme.Primary.WithAlpha(0.3f);
            colors.pressedColor = UITheme.Primary.WithAlpha(0.5f);
            colors.selectedColor = UITheme.Primary.WithAlpha(0.4f);
            colors.fadeDuration = 0.15f;
            button.colors = colors;

            // Navigation
            var nav = button.navigation;
            nav.mode = Navigation.Mode.Horizontal;
            button.navigation = nav;

            // Vertical layout
            var vlg = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15f;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Icon
            CreateCardIcon(cardRect);

            // Name
            CreateCardName(cardRect);

            // Description
            CreateCardDescription(cardRect);

            // Spacer
            var spacerRect = CreateUIObject("Spacer", cardRect);
            spacerRect.sizeDelta = new Vector2(0f, 20f);
            var layoutElement = spacerRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1f;

            // Tier
            CreateCardTier(cardRect);

            // Add card component
            var card = cardRect.gameObject.AddComponent<UpgradeCard>();
            SetPrivateField(card, "_background", bgImg);
            SetPrivateField(card, "_button", button);

            // Find and set child references
            var iconImg = cardRect.Find("Icon")?.GetComponent<Image>();
            var nameText = cardRect.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = cardRect.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var tierText = cardRect.Find("TierText")?.GetComponent<TextMeshProUGUI>();

            SetPrivateField(card, "_icon", iconImg);
            SetPrivateField(card, "_nameText", nameText);
            SetPrivateField(card, "_descriptionText", descText);
            SetPrivateField(card, "_tierText", tierText);

            // Add canvas group for animations BEFORE adding animator
            // (animator's Awake will find it instead of adding a duplicate)
            var canvasGroup = cardRect.gameObject.AddComponent<CanvasGroup>();

            // Add animator component for polish
            var animator = cardRect.gameObject.AddComponent<UpgradeCardAnimator>();
            SetPrivateField(animator, "_background", bgImg);
            SetPrivateField(animator, "_canvasGroup", canvasGroup);

            return card;
        }

        private void CreateCardIcon(RectTransform parent)
        {
            var iconRect = CreateUIObject("Icon", parent);
            iconRect.sizeDelta = new Vector2(120f, 120f);

            var iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.color = UITheme.Primary;
            iconImg.preserveAspect = true;

            // Circle background
            var bgRect = CreateUIObject("IconBG", iconRect);
            StretchToFill(bgRect);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = UITheme.BackgroundLight;
            bgRect.SetAsFirstSibling();
        }

        private void CreateCardName(RectTransform parent)
        {
            var nameRect = CreateUIObject("NameText", parent);
            nameRect.sizeDelta = new Vector2(300f, 50f);

            var nameText = AddTextComponent(nameRect.gameObject);
            nameText.text = "UPGRADE NAME";
            nameText.fontSize = UITheme.FontSize.Large;
            nameText.color = UITheme.Primary;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            nameText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        }

        private void CreateCardDescription(RectTransform parent)
        {
            var descRect = CreateUIObject("DescriptionText", parent);
            descRect.sizeDelta = new Vector2(300f, 120f);

            var descText = AddTextComponent(descRect.gameObject);
            descText.text = "Upgrade description goes here...";
            descText.fontSize = UITheme.FontSize.Body;
            descText.color = UITheme.TextSecondary;
            descText.alignment = TextAlignmentOptions.Center;
            descText.fontStyle = FontStyles.Normal;
            descText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        }

        private void CreateCardTier(RectTransform parent)
        {
            var tierRect = CreateUIObject("TierText", parent);
            tierRect.sizeDelta = new Vector2(300f, 30f);

            var tierText = AddTextComponent(tierRect.gameObject);
            tierText.text = "[COMMON]";
            tierText.fontSize = UITheme.FontSize.Small;
            tierText.color = UITheme.TextMuted;
            tierText.alignment = TextAlignmentOptions.Center;
            tierText.fontStyle = FontStyles.Bold;
        }
    }
}
