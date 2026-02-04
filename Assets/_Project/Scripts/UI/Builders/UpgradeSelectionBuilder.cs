using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builder for upgrade selection screen - HOLOGRAPHIC ARCADE TERMINAL style.
    /// Transparent floating panels, glowing selection rings, pulsing neon borders.
    /// Compact, non-intrusive, feels part of the game world.
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
            var screenRoot = CreateUIObject("UpgradeSelectionScreen", parent);
            StretchToFill(screenRoot);

            var canvasGroup = screenRoot.gameObject.AddComponent<CanvasGroup>();

            // Subtle darkened vignette overlay (much more transparent)
            CreateBackgroundOverlay(screenRoot);

            // Subtle scanline overlay for CRT feel
            CreateScanlineOverlay(screenRoot);

            // Floating holographic content panel
            var contentPanel = CreateContentPanel(screenRoot);

            // Compact title with glow
            CreateTitle(contentPanel);

            // Card container (no subtitle - let cards speak)
            var cardContainer = CreateCardContainer(contentPanel);

            // Minimal instructions at bottom
            CreateInstructions(contentPanel);

            // Add screen component
            var screen = screenRoot.gameObject.AddComponent<UpgradeSelectionScreen>();
            SetPrivateField(screen, "m_screenRoot", screenRoot.gameObject);
            SetPrivateField(screen, "m_cardContainer", cardContainer);

            var titleText = contentPanel.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            SetPrivateField(screen, "m_titleText", titleText);

            // Find subtitle if it exists (for backwards compat)
            var subtitleText = contentPanel.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
            SetPrivateField(screen, "m_subtitleText", subtitleText);

            screenRoot.gameObject.SetActive(false);

            return screen;
        }

        private void CreateBackgroundOverlay(RectTransform parent)
        {
            var overlayRect = CreateUIObject("Overlay", parent);
            StretchToFill(overlayRect);

            var overlayImg = overlayRect.gameObject.AddComponent<Image>();
            // Much more transparent - let the game show through
            overlayImg.color = new Color(0.01f, 0.01f, 0.03f, 0.6f);
        }

        private void CreateScanlineOverlay(RectTransform parent)
        {
            var scanlineRect = CreateUIObject("Scanlines", parent);
            StretchToFill(scanlineRect);

            var scanlineImg = scanlineRect.gameObject.AddComponent<RawImage>();
            scanlineImg.color = new Color(0f, 0f, 0f, 0.04f); // Very subtle
            scanlineImg.raycastTarget = false;

            var tex = CreateScanlineTexture(4, 64);
            scanlineImg.texture = tex;
            scanlineImg.uvRect = new Rect(0, 0, 1, Screen.height / 4f);
        }

        private Texture2D CreateScanlineTexture(int height, int width)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = (y % 2 == 0) ? 0.12f : 0f;
                    tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private RectTransform CreateContentPanel(RectTransform parent)
        {
            var panelRect = CreateUIObject("ContentPanel", parent);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            // Smaller, more compact
            panelRect.sizeDelta = new Vector2(1050f, 520f);

            // No solid background - fully transparent, just floating elements

            // Layout
            var vlg = panelRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(30, 30, 25, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            return panelRect;
        }

        private void CreateHolographicBorder(RectTransform parent, Color color)
        {
            // Create multiple glow layers for holographic effect
            for (int i = 3; i >= 1; i--)
            {
                var borderRect = CreateUIObject($"GlowBorder_{i}", parent);
                StretchToFill(borderRect);
                borderRect.SetAsFirstSibling();

                float expand = i * 3f;
                borderRect.offsetMin = new Vector2(-expand, -expand);
                borderRect.offsetMax = new Vector2(expand, expand);

                var borderImg = borderRect.gameObject.AddComponent<Image>();
                borderImg.color = Color.clear;
                borderImg.raycastTarget = false;

                var outline = borderRect.gameObject.AddComponent<Outline>();
                float alpha = 0.08f + (0.08f / i);
                outline.effectColor = color.WithAlpha(alpha);
                outline.effectDistance = new Vector2(i * 1.5f, -i * 1.5f);
            }

            // Main crisp border - thinner, sharper
            var mainOutline = parent.gameObject.AddComponent<Outline>();
            mainOutline.effectColor = color.WithAlpha(0.7f);
            mainOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void CreateTitle(RectTransform parent)
        {
            var titleRect = CreateUIObject("TitleText", parent);
            titleRect.sizeDelta = new Vector2(990f, 50f);

            var titleText = AddTextComponent(titleRect.gameObject);
            if (titleText != null)
            {
                titleText.text = "◈ UPGRADE AVAILABLE ◈";
                titleText.fontSize = UITheme.FontSize.Title;
                titleText.color = UITheme.Primary;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontStyle = FontStyles.Bold;
                titleText.characterSpacing = UITheme.LetterSpacing.ExtraWide;

                // Add glow shadow
                var shadow = titleRect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = UITheme.PrimaryGlow;
                shadow.effectDistance = new Vector2(0, -2);

                // Second shadow for stronger glow
                var shadow2 = titleRect.gameObject.AddComponent<Shadow>();
                shadow2.effectColor = UITheme.Primary.WithAlpha(0.3f);
                shadow2.effectDistance = new Vector2(0, -4);
            }
        }

        private Transform CreateCardContainer(RectTransform parent)
        {
            var containerRect = CreateUIObject("CardContainer", parent);
            containerRect.sizeDelta = new Vector2(990f, 400f);

            var hlg = containerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30f; // Tighter spacing
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            return containerRect;
        }

        private void CreateInstructions(RectTransform parent)
        {
            var instrRect = CreateUIObject("Instructions", parent);
            instrRect.sizeDelta = new Vector2(990f, 25f);

            var instrText = AddTextComponent(instrRect.gameObject);
            if (instrText != null)
            {
                instrText.text = "[ ◄ ► ]  SELECT   •   [ FIRE ]  CONFIRM";
                instrText.fontSize = UITheme.FontSize.Small;
                instrText.color = UITheme.TextMuted;
                instrText.alignment = TextAlignmentOptions.Center;
                instrText.characterSpacing = UITheme.LetterSpacing.Arcade;
            }
        }

        /// <summary>
        /// Build a single upgrade card with HOLOGRAPHIC NEON style.
        /// Compact, transparent, with dramatic selection glow.
        /// </summary>
        public UpgradeCard BuildCard(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogError("[UpgradeSelectionBuilder] BuildCard called with null parent!");
                return null;
            }

            var cardRect = CreateUIObject("UpgradeCard", parent);
            if (cardRect == null)
            {
                Debug.LogError("[UpgradeSelectionBuilder] Failed to create card RectTransform!");
                return null;
            }
            // Smaller, more compact cards
            cardRect.sizeDelta = new Vector2(280f, 380f);

            // Selection glow container (behind everything)
            CreateSelectionGlow(cardRect);

            // Transparent card background
            var bgImg = cardRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.03f, 0.08f, 0.7f);
            bgImg.raycastTarget = true;

            // Subtle border
            var borderOutline = cardRect.gameObject.AddComponent<Outline>();
            borderOutline.effectColor = UITheme.Primary.WithAlpha(0.4f);
            borderOutline.effectDistance = new Vector2(1f, -1f);

            // Button
            var button = cardRect.gameObject.AddComponent<Button>();
            button.targetGraphic = bgImg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.1f);
            colors.pressedColor = UITheme.Primary.WithAlpha(0.3f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1.05f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var nav = button.navigation;
            nav.mode = Navigation.Mode.Horizontal;
            button.navigation = nav;

            // Vertical layout
            var vlg = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(18, 18, 20, 15);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Tier badge (compact)
            CreateCardTierBadge(cardRect);

            // Icon (smaller)
            CreateCardIcon(cardRect);

            // Name
            CreateCardName(cardRect);

            // Divider
            CreateDividerLine(cardRect);

            // Description
            CreateCardDescription(cardRect);

            // Spacer
            var spacerRect = CreateUIObject("Spacer", cardRect);
            spacerRect.sizeDelta = new Vector2(0f, 5f);
            var layoutElement = spacerRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1f;

            // Stats
            CreateCardStats(cardRect);

            // Add card component
            var card = cardRect.gameObject.AddComponent<UpgradeCard>();
            SetPrivateField(card, "m_background", bgImg);
            SetPrivateField(card, "m_button", button);

            var iconImg = cardRect.Find("Icon/IconImage")?.GetComponent<Image>();
            var nameText = cardRect.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = cardRect.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var tierText = cardRect.Find("TierBadge/TierText")?.GetComponent<TextMeshProUGUI>();

            SetPrivateField(card, "m_icon", iconImg);
            SetPrivateField(card, "m_nameText", nameText);
            SetPrivateField(card, "m_descriptionText", descText);
            SetPrivateField(card, "m_tierText", tierText);

            var canvasGroup = cardRect.gameObject.AddComponent<CanvasGroup>();

            var animator = cardRect.gameObject.AddComponent<UpgradeCardAnimator>();
            SetPrivateField(animator, "m_background", bgImg);
            SetPrivateField(animator, "m_canvasGroup", canvasGroup);

            return card;
        }

        private void CreateSelectionGlow(RectTransform cardRect)
        {
            // This creates the glowing selection ring effect
            var glowContainer = CreateUIObject("SelectionGlow", cardRect);
            StretchToFill(glowContainer);
            glowContainer.SetAsFirstSibling();

            // Expand beyond card bounds for glow effect
            glowContainer.offsetMin = new Vector2(-12f, -12f);
            glowContainer.offsetMax = new Vector2(12f, 12f);

            var glowImg = glowContainer.gameObject.AddComponent<Image>();
            glowImg.color = Color.clear; // Starts invisible
            glowImg.raycastTarget = false;

            // Multiple glow outlines
            var glow1 = glowContainer.gameObject.AddComponent<Outline>();
            glow1.effectColor = Color.clear;
            glow1.effectDistance = new Vector2(4f, -4f);

            var glow2 = glowContainer.gameObject.AddComponent<Outline>();
            glow2.effectColor = Color.clear;
            glow2.effectDistance = new Vector2(2f, -2f);
        }

        private void CreateCardTierBadge(RectTransform parent)
        {
            var badgeRect = CreateUIObject("TierBadge", parent);
            badgeRect.sizeDelta = new Vector2(244f, 22f);

            // Background
            var bgRect = CreateUIObject("Background", badgeRect);
            StretchToFill(bgRect);
            var badgeBg = bgRect.gameObject.AddComponent<Image>();
            badgeBg.color = new Color(0.08f, 0.06f, 0.15f, 0.8f);
            badgeBg.raycastTarget = false;

            // Text
            var textRect = CreateUIObject("TierText", badgeRect);
            StretchToFill(textRect);

            var tierText = AddTextComponent(textRect.gameObject);
            if (tierText != null)
            {
                tierText.text = "COMMON";
                tierText.fontSize = UITheme.FontSize.Tiny;
                tierText.color = UITheme.TextMuted;
                tierText.alignment = TextAlignmentOptions.Center;
                tierText.fontStyle = FontStyles.Bold;
                tierText.characterSpacing = UITheme.LetterSpacing.ExtraWide;
            }
        }

        private void CreateCardIcon(RectTransform parent)
        {
            var iconContainer = CreateUIObject("Icon", parent);
            iconContainer.sizeDelta = new Vector2(90f, 90f);

            // Hexagonal-feel background
            var iconBg = iconContainer.gameObject.AddComponent<Image>();
            iconBg.color = new Color(0.06f, 0.05f, 0.12f, 0.9f);

            // Glow outline
            var iconOutline = iconContainer.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = UITheme.Primary.WithAlpha(0.5f);
            iconOutline.effectDistance = new Vector2(2f, -2f);

            // Actual icon
            var iconRect = CreateUIObject("IconImage", iconContainer);
            iconRect.sizeDelta = new Vector2(70f, 70f);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;

            var iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.color = UITheme.Primary;
            iconImg.preserveAspect = true;
        }

        private void CreateCardName(RectTransform parent)
        {
            var nameRect = CreateUIObject("NameText", parent);
            nameRect.sizeDelta = new Vector2(244f, 40f);

            var nameText = AddTextComponent(nameRect.gameObject);
            if (nameText != null)
            {
                nameText.text = "UPGRADE";
                nameText.fontSize = UITheme.FontSize.Medium;
                nameText.color = UITheme.TextPrimary;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.fontStyle = FontStyles.Bold;
                nameText.characterSpacing = UITheme.LetterSpacing.Wide;
                nameText.textWrappingMode = TextWrappingModes.Normal;

                // Subtle glow
                var shadow = nameRect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = UITheme.PrimaryGlow.WithAlpha(0.4f);
                shadow.effectDistance = new Vector2(0, -1);
            }
        }

        private void CreateDividerLine(RectTransform parent)
        {
            var dividerRect = CreateUIObject("Divider", parent);
            dividerRect.sizeDelta = new Vector2(200f, 1f);

            var dividerImg = dividerRect.gameObject.AddComponent<Image>();
            dividerImg.color = UITheme.Primary.WithAlpha(0.25f);
        }

        private void CreateCardDescription(RectTransform parent)
        {
            var descRect = CreateUIObject("DescriptionText", parent);
            descRect.sizeDelta = new Vector2(244f, 80f);

            var descText = AddTextComponent(descRect.gameObject);
            if (descText != null)
            {
                descText.text = "Upgrade description here.";
                descText.fontSize = UITheme.FontSize.Small;
                descText.color = UITheme.TextSecondary;
                descText.alignment = TextAlignmentOptions.Center;
                descText.fontStyle = FontStyles.Normal;
                descText.textWrappingMode = TextWrappingModes.Normal;
                descText.lineSpacing = 3f;
            }
        }

        private void CreateCardStats(RectTransform parent)
        {
            var statsRect = CreateUIObject("StatsText", parent);
            statsRect.sizeDelta = new Vector2(244f, 25f);

            var statsText = AddTextComponent(statsRect.gameObject);
            if (statsText != null)
            {
                statsText.text = "+25% DAMAGE";
                statsText.fontSize = UITheme.FontSize.Body;
                statsText.color = UITheme.Good;
                statsText.alignment = TextAlignmentOptions.Center;
                statsText.fontStyle = FontStyles.Bold;
                statsText.characterSpacing = UITheme.LetterSpacing.Wide;
            }
        }
    }
}
