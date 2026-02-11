using UnityEngine;
using NeuralBreak.Combat;

namespace NeuralBreak.UI
{
    /// <summary>
    /// NEURAL BREAK - NEON ARCADE CYBER Theme System
    ///
    /// DESIGN DIRECTION: 80s arcade cabinet meets cyberpunk terminal.
    /// Scanlines, glowing edges, CRT warmth, sharp geometric shapes, pulsing neon.
    /// Bold, memorable, unmistakably NEURAL BREAK.
    ///
    /// COLOR PHILOSOPHY:
    /// - Electric Cyan = PRIMARY (UI elements, information, player-friendly)
    /// - Hot Magenta = ACCENT (special events, combos, power-ups)
    /// - Neon Green = GOOD (heals, pickups, positive feedback)
    /// - Plasma Orange = WARNING (achievements, caution)
    /// - Crimson = DANGER (damage, critical warnings, bosses)
    /// - Gold = LEGENDARY (rare items, milestones)
    ///
    /// VISUAL EFFECTS:
    /// - Glow/bloom on all accent colors
    /// - Scanline texture overlay on panels
    /// - CRT curvature on screen edges
    /// - Chromatic aberration on hover states
    /// </summary>
    public static class UITheme
    {
        #region Color Palette - NEON ARCADE CYBER

        // === PRIMARY - Electric Cyan ===
        public static readonly Color Primary = new Color(0.0f, 0.95f, 1f, 1f);              // #00F2FF Electric cyan
        public static readonly Color PrimaryDark = new Color(0.0f, 0.65f, 0.75f, 1f);       // Darker cyan
        public static readonly Color PrimaryGlow = new Color(0.0f, 1f, 1f, 0.6f);           // Cyan glow (stronger)
        public static readonly Color PrimaryDim = new Color(0.0f, 0.4f, 0.5f, 1f);          // Dimmed cyan

        // === ACCENT - Hot Magenta/Pink ===
        public static readonly Color Accent = new Color(1f, 0.1f, 0.6f, 1f);                // #FF1A99 Hot magenta
        public static readonly Color AccentBright = new Color(1f, 0.4f, 0.75f, 1f);         // Bright pink
        public static readonly Color AccentGlow = new Color(1f, 0.2f, 0.65f, 0.6f);         // Magenta glow
        public static readonly Color AccentDim = new Color(0.6f, 0.1f, 0.4f, 1f);           // Dimmed magenta

        // === GOOD - Neon Green ===
        public static readonly Color Good = new Color(0.15f, 1f, 0.35f, 1f);                // #26FF59 Neon green
        public static readonly Color GoodDark = new Color(0.1f, 0.7f, 0.25f, 1f);           // Darker green
        public static readonly Color GoodGlow = new Color(0.2f, 1f, 0.4f, 0.5f);            // Green glow

        // === WARNING - Plasma Orange ===
        public static readonly Color Warning = new Color(1f, 0.6f, 0.1f, 1f);               // #FF9919 Plasma orange
        public static readonly Color WarningDark = new Color(0.85f, 0.45f, 0.05f, 1f);      // Darker orange
        public static readonly Color WarningGlow = new Color(1f, 0.7f, 0.2f, 0.5f);         // Orange glow

        // === DANGER - Crimson Red ===
        public static readonly Color Danger = new Color(1f, 0.1f, 0.15f, 1f);               // #FF1A26 Crimson
        public static readonly Color DangerDark = new Color(0.75f, 0.08f, 0.12f, 1f);       // Darker red
        public static readonly Color DangerGlow = new Color(1f, 0.2f, 0.25f, 0.5f);         // Red glow

        // === LEGENDARY - Gold ===
        public static readonly Color Legendary = new Color(1f, 0.85f, 0.2f, 1f);            // #FFD933 Gold
        public static readonly Color LegendaryGlow = new Color(1f, 0.9f, 0.3f, 0.6f);       // Gold glow

        // === TEXT COLORS ===
        public static readonly Color TextPrimary = new Color(0.95f, 0.98f, 1f, 1f);         // Slightly blue-white
        public static readonly Color TextSecondary = new Color(0.6f, 0.65f, 0.75f, 1f);     // Cool gray
        public static readonly Color TextMuted = new Color(0.4f, 0.42f, 0.5f, 1f);          // Dimmed
        public static readonly Color TextGlow = new Color(0.7f, 0.9f, 1f, 0.3f);            // Text shadow glow

        // === BACKGROUND COLORS - Deep Purple-Black ===
        public static readonly Color BackgroundDark = new Color(0.02f, 0.02f, 0.06f, 0.98f);     // Near black with purple tint
        public static readonly Color BackgroundMedium = new Color(0.06f, 0.05f, 0.12f, 0.95f);   // Dark purple
        public static readonly Color BackgroundLight = new Color(0.1f, 0.08f, 0.18f, 0.9f);      // Lighter purple
        public static readonly Color BackgroundOverlay = new Color(0.02f, 0.01f, 0.05f, 0.85f);  // Screen overlay
        public static readonly Color BackgroundPanel = new Color(0.04f, 0.03f, 0.1f, 0.92f);     // Panel background

        // === UI ELEMENT COLORS ===
        public static readonly Color ButtonNormal = new Color(0.08f, 0.06f, 0.15f, 1f);
        public static readonly Color ButtonHover = new Color(0.0f, 0.4f, 0.5f, 1f);
        public static readonly Color ButtonPressed = Primary;
        public static readonly Color ButtonSelected = new Color(0.0f, 0.3f, 0.4f, 1f);

        // === BAR COLORS ===
        public static readonly Color BarBackground = new Color(0.08f, 0.06f, 0.12f, 0.9f);
        public static readonly Color BarBorder = new Color(0.2f, 0.25f, 0.35f, 0.7f);
        public static readonly Color BarGlow = new Color(0.0f, 0.8f, 1f, 0.3f);

        // === CARD COLORS ===
        public static readonly Color CardBackground = new Color(0.05f, 0.04f, 0.1f, 0.95f);
        public static readonly Color CardBorder = new Color(0.0f, 0.7f, 0.8f, 0.6f);
        public static readonly Color CardHover = new Color(0.08f, 0.06f, 0.15f, 1f);
        public static readonly Color CardSelected = new Color(0.0f, 0.25f, 0.35f, 1f);

        // === SCANLINE EFFECT ===
        public static readonly Color ScanlineColor = new Color(0f, 0f, 0f, 0.08f);
        public const float ScanlineSpacing = 3f;

        // === GLOW INTENSITIES ===
        public const float GlowIntensityLow = 0.3f;
        public const float GlowIntensityMedium = 0.5f;
        public const float GlowIntensityHigh = 0.8f;
        public const float GlowIntensityMax = 1.2f;

        #endregion

        #region Gradients

        // === HEALTH BAR GRADIENT ===
        public static Gradient HealthGradient
        {
            get
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(Danger, 0f),
                        new GradientColorKey(Warning, 0.35f),
                        new GradientColorKey(Good, 0.65f),
                        new GradientColorKey(Good, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
                return gradient;
            }
        }

        // === HEAT BAR GRADIENT ===
        public static Gradient HeatGradient
        {
            get
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(Primary, 0f),
                        new GradientColorKey(Primary, 0.45f),
                        new GradientColorKey(Warning, 0.7f),
                        new GradientColorKey(Danger, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
                return gradient;
            }
        }

        // === NEON RAINBOW GRADIENT (for special effects) ===
        public static Gradient NeonRainbow
        {
            get
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(Primary, 0f),
                        new GradientColorKey(Accent, 0.33f),
                        new GradientColorKey(Warning, 0.66f),
                        new GradientColorKey(Primary, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
                return gradient;
            }
        }

        #endregion

        #region Minimap Colors

        public static readonly Color MinimapBackground = new Color(0.02f, 0.02f, 0.05f, 0.7f);
        public static readonly Color MinimapBorder = Primary.WithAlpha(0.6f);
        public static readonly Color MinimapPlayer = Good;
        public static readonly Color MinimapEnemy = Danger;
        public static readonly Color MinimapElite = Warning;
        public static readonly Color MinimapBoss = Accent;
        public static readonly Color MinimapPickup = Good;

        #endregion

        #region Shield Colors

        public static readonly Color ShieldActive = new Color(0.2f, 0.9f, 1f, 1f);
        public static readonly Color ShieldInactive = new Color(0.15f, 0.15f, 0.2f, 0.4f);
        public static readonly Color ShieldGlow = new Color(0.3f, 0.95f, 1f, 0.5f);

        #endregion

        #region Typography

        // === FONT SIZES (scaled for impact) ===
        public static class FontSize
        {
            public const float Tiny = 12f;
            public const float Small = 14f;
            public const float Body = 18f;
            public const float Medium = 24f;
            public const float Large = 32f;
            public const float Title = 42f;
            public const float Headline = 56f;
            public const float Display = 72f;
            public const float Giant = 96f;
            public const float Massive = 128f;
        }

        // === LETTER SPACING ===
        public static class LetterSpacing
        {
            public const float Tight = -2f;
            public const float Normal = 0f;
            public const float Wide = 5f;
            public const float ExtraWide = 12f;
            public const float Arcade = 8f;  // Classic arcade feel
        }

        #endregion

        #region Animation Timing

        public static class Duration
        {
            public const float Instant = 0.05f;
            public const float Fast = 0.1f;
            public const float Quick = 0.15f;
            public const float Normal = 0.25f;
            public const float Smooth = 0.35f;
            public const float Slow = 0.5f;
            public const float Dramatic = 0.8f;
            public const float Epic = 1.2f;

            // Notification hold times
            public const float NotificationBrief = 1.5f;
            public const float NotificationNormal = 2.5f;
            public const float NotificationLong = 4f;

            // Pulse/glow cycles
            public const float PulseFast = 0.3f;
            public const float PulseNormal = 0.6f;
            public const float PulseSlow = 1.2f;
        }

        public static class Scale
        {
            public const float PunchSmall = 1.08f;
            public const float PunchMedium = 1.15f;
            public const float PunchLarge = 1.25f;
            public const float PunchDramatic = 1.4f;
            public const float PunchExplosive = 1.6f;
        }

        #endregion

        #region Layout Constants

        public static class SafeZone
        {
            public const float CenterExcludeTop = 0.65f;
            public const float CenterExcludeBottom = 0.35f;
            public const float MarginSmall = 12f;
            public const float MarginNormal = 20f;
            public const float MarginLarge = 32f;
        }

        public static class SortOrder
        {
            public const int Background = 0;
            public const int Scanlines = 50;
            public const int Minimap = 80;
            public const int HUD = 90;
            public const int XPBar = 92;
            public const int BossHealth = 95;
            public const int DamageNumbers = 120;
            public const int Announcements = 150;
            public const int LevelUp = 160;
            public const int Screens = 200;
            public const int Achievements = 220;
            public const int Overlay = 250;
            public const int Debug = 999;
        }

        public static class NotificationY
        {
            public const float TopAnnouncement = -100f;
            public const float TopAlert = -180f;
            public const float TopInfo = -60f;
            public const float BottomHUD = 30f;
            public const float BottomInfo = 100f;
        }

        #endregion

        #region Combo Milestones - ARCADE STYLE

        public static readonly (int threshold, string message, Color color)[] ComboMilestones = new[]
        {
            (5, "NICE!", Warning),
            (10, "GREAT!", new Color(1f, 0.5f, 0.15f)),        // Orange
            (15, "AWESOME!", new Color(1f, 0.35f, 0.25f)),     // Red-orange
            (20, "INCREDIBLE!", Accent),                        // Magenta
            (30, "UNSTOPPABLE!", new Color(0.9f, 0.2f, 1f)),   // Purple
            (50, "GODLIKE!", new Color(0.6f, 0.3f, 1f)),       // Violet
            (75, "LEGENDARY!", Legendary),                      // Gold
            (100, ">>> TRANSCENDENT <<<", TextPrimary),         // White flash
        };

        #endregion

        #region Damage Number Styles

        public static class DamageStyle
        {
            public const float NormalSize = 18f;
            public static readonly Color NormalColor = TextPrimary;

            public const float BigHitSize = 26f;
            public static readonly Color BigHitColor = Warning;
            public const float BigHitScale = 1.2f;

            public const float CriticalSize = 32f;
            public static readonly Color CriticalColor = Accent;
            public const float CriticalScale = 1.4f;

            public const float XPSize = 16f;
            public static readonly Color XPColor = Primary;

            public const float HealSize = 22f;
            public static readonly Color HealColor = Good;
        }

        #endregion

        #region Tier Colors

        public static Color GetTierColor(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Common => TextSecondary,
                UpgradeTier.Rare => Primary,
                UpgradeTier.Epic => Accent,
                UpgradeTier.Legendary => Legendary,
                _ => TextSecondary
            };
        }

        public static Color GetTierGlow(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Common => TextSecondary.WithAlpha(0.2f),
                UpgradeTier.Rare => PrimaryGlow,
                UpgradeTier.Epic => AccentGlow,
                UpgradeTier.Legendary => LegendaryGlow,
                _ => TextSecondary.WithAlpha(0.2f)
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get color with modified alpha
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Lerp between colors with optional alpha preservation
        /// </summary>
        public static Color LerpColor(Color a, Color b, float t, bool preserveAlpha = false)
        {
            Color result = Color.Lerp(a, b, t);
            if (preserveAlpha) result.a = a.a;
            return result;
        }

        /// <summary>
        /// Get pulse color for glow effects (arcade style)
        /// </summary>
        public static Color GetPulseColor(Color baseColor, float time, float frequency = 3f, float intensity = 0.4f)
        {
            float pulse = Mathf.Sin(time * frequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            return Color.Lerp(baseColor, Color.white, pulse * intensity);
        }

        /// <summary>
        /// Get glow color (brighter, with bloom feel)
        /// </summary>
        public static Color GetGlowColor(Color baseColor, float intensity = 0.5f)
        {
            return new Color(
                Mathf.Min(baseColor.r + intensity, 1f),
                Mathf.Min(baseColor.g + intensity, 1f),
                Mathf.Min(baseColor.b + intensity, 1f),
                baseColor.a * 0.6f
            );
        }

        /// <summary>
        /// Get chromatic aberration offset colors
        /// </summary>
        public static (Color red, Color cyan) GetChromaticColors(Color baseColor, float intensity = 0.3f)
        {
            Color red = new Color(
                Mathf.Min(baseColor.r + intensity, 1f),
                baseColor.g * (1f - intensity),
                baseColor.b * (1f - intensity),
                baseColor.a
            );
            Color cyan = new Color(
                baseColor.r * (1f - intensity),
                Mathf.Min(baseColor.g + intensity * 0.5f, 1f),
                Mathf.Min(baseColor.b + intensity, 1f),
                baseColor.a
            );
            return (red, cyan);
        }

        /// <summary>
        /// Get glitch offset for text/UI elements
        /// </summary>
        public static Vector2 GetGlitchOffset(float time, float intensity = 2f)
        {
            float noise = Mathf.PerlinNoise(time * 10f, 0f);
            if (noise > 0.9f) // Occasional glitch
            {
                return new Vector2(
                    (Mathf.PerlinNoise(time * 50f, 0f) - 0.5f) * intensity * 4f,
                    (Mathf.PerlinNoise(0f, time * 50f) - 0.5f) * intensity
                );
            }
            return Vector2.zero;
        }

        #endregion
    }
}
