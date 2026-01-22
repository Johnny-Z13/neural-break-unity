using UnityEngine;

namespace NeuralBreak.UI
{
    /// <summary>
    /// NEURAL BREAK - Unified UI Theme System
    ///
    /// DESIGN DIRECTION: Cyberpunk terminal aesthetic with glitch undertones.
    /// Clean, readable HUD that stays out of the player's way while providing
    /// satisfying feedback through carefully timed animations.
    ///
    /// COLOR PHILOSOPHY:
    /// - Green = GOOD (pickups, heals, positive feedback)
    /// - Cyan = NEUTRAL/INFO (primary UI, score, standard text)
    /// - Magenta/Pink = ACCENT (combo milestones, special events)
    /// - Red = DANGER (damage, warnings, boss encounters)
    /// - Gold/Yellow = ACHIEVEMENT (level-ups, milestones, rewards)
    ///
    /// PLAYER VISIBILITY:
    /// - Player is ALWAYS at screen center
    /// - NO notifications should appear in center third of screen
    /// - Use corners and edges for persistent HUD
    /// - Announcements slide in from top third only
    /// </summary>
    public static class UITheme
    {
        #region Color Palette

        // === PRIMARY COLORS ===
        public static readonly Color Primary = new Color(0.0f, 0.9f, 0.9f, 1f);          // Cyan - main UI
        public static readonly Color PrimaryDark = new Color(0.0f, 0.6f, 0.7f, 1f);      // Darker cyan
        public static readonly Color PrimaryGlow = new Color(0.0f, 1f, 1f, 0.4f);        // Cyan glow

        // === ACCENT COLORS ===
        public static readonly Color Accent = new Color(1f, 0.2f, 0.55f, 1f);            // Hot pink/magenta
        public static readonly Color AccentBright = new Color(1f, 0.4f, 0.7f, 1f);       // Bright pink
        public static readonly Color AccentGlow = new Color(1f, 0.2f, 0.55f, 0.5f);      // Pink glow

        // === STATUS COLORS ===
        public static readonly Color Good = new Color(0.2f, 1f, 0.4f, 1f);               // Neon green - pickups, heals
        public static readonly Color GoodDark = new Color(0.1f, 0.7f, 0.3f, 1f);         // Darker green
        public static readonly Color GoodGlow = new Color(0.2f, 1f, 0.4f, 0.4f);         // Green glow

        public static readonly Color Warning = new Color(1f, 0.75f, 0.1f, 1f);           // Gold/yellow - achievements
        public static readonly Color WarningDark = new Color(0.8f, 0.6f, 0.1f, 1f);      // Darker gold
        public static readonly Color WarningGlow = new Color(1f, 0.8f, 0.2f, 0.5f);      // Gold glow

        public static readonly Color Danger = new Color(1f, 0.15f, 0.2f, 1f);            // Vibrant red
        public static readonly Color DangerDark = new Color(0.7f, 0.1f, 0.15f, 1f);      // Darker red
        public static readonly Color DangerGlow = new Color(1f, 0.2f, 0.2f, 0.5f);       // Red glow

        // === NEUTRAL COLORS ===
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color(0.75f, 0.75f, 0.8f, 1f);  // Light gray-blue
        public static readonly Color TextMuted = new Color(0.5f, 0.5f, 0.55f, 1f);       // Dimmed

        // === BACKGROUND COLORS ===
        public static readonly Color BackgroundDark = new Color(0.03f, 0.03f, 0.08f, 0.95f);    // Near black
        public static readonly Color BackgroundMedium = new Color(0.08f, 0.08f, 0.12f, 0.9f);   // Dark blue-gray
        public static readonly Color BackgroundLight = new Color(0.12f, 0.12f, 0.18f, 0.85f);   // Lighter
        public static readonly Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.7f);           // Screen overlay

        // === UI ELEMENT COLORS ===
        public static readonly Color ButtonNormal = new Color(0.15f, 0.15f, 0.22f, 1f);
        public static readonly Color ButtonHover = new Color(0.0f, 0.5f, 0.6f, 1f);
        public static readonly Color ButtonPressed = Primary;
        public static readonly Color ButtonSelected = new Color(0.0f, 0.4f, 0.5f, 1f);

        public static readonly Color BarBackground = new Color(0.12f, 0.12f, 0.15f, 0.85f);
        public static readonly Color BarBorder = new Color(0.3f, 0.3f, 0.35f, 0.6f);

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
                        new GradientColorKey(Good, 0.7f),
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
                        new GradientColorKey(Primary, 0.5f),
                        new GradientColorKey(Warning, 0.75f),
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

        // === MINIMAP COLORS ===
        public static readonly Color MinimapBackground = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color MinimapBorder = new Color(0.2f, 0.7f, 0.8f, 0.7f);
        public static readonly Color MinimapPlayer = Good;
        public static readonly Color MinimapEnemy = Danger;
        public static readonly Color MinimapElite = Warning;
        public static readonly Color MinimapBoss = Accent;
        public static readonly Color MinimapPickup = Good;

        // === SHIELD COLORS ===
        public static readonly Color ShieldActive = new Color(0.2f, 0.85f, 1f, 1f);
        public static readonly Color ShieldInactive = new Color(0.2f, 0.2f, 0.25f, 0.4f);

        #endregion

        #region Typography

        // === FONT SIZES (relative to 1080p reference) ===
        public static class FontSize
        {
            public const float Tiny = 11f;
            public const float Small = 14f;
            public const float Body = 18f;
            public const float Medium = 22f;
            public const float Large = 28f;
            public const float Title = 36f;
            public const float Headline = 48f;
            public const float Display = 64f;
            public const float Giant = 80f;
        }

        // === FONT WEIGHTS ===
        // Using TMPro FontStyles
        // Normal, Bold, Italic, BoldItalic

        #endregion

        #region Animation Timing

        // === DURATIONS (in seconds) ===
        public static class Duration
        {
            public const float Instant = 0.05f;
            public const float Fast = 0.1f;
            public const float Quick = 0.15f;
            public const float Normal = 0.25f;
            public const float Smooth = 0.35f;
            public const float Slow = 0.5f;
            public const float Dramatic = 0.8f;

            // Notification hold times
            public const float NotificationBrief = 1.5f;
            public const float NotificationNormal = 2.5f;
            public const float NotificationLong = 4f;
        }

        // === ANIMATION SCALES ===
        public static class Scale
        {
            public const float PunchSmall = 1.1f;
            public const float PunchMedium = 1.2f;
            public const float PunchLarge = 1.35f;
            public const float PunchDramatic = 1.5f;
        }

        #endregion

        #region Layout Constants

        // === SCREEN REGIONS (normalized 0-1) ===
        // Player is ALWAYS center - never put notifications here
        public static class SafeZone
        {
            // Center exclusion zone (player area)
            public const float CenterExcludeTop = 0.65f;     // Don't go below this in top notifications
            public const float CenterExcludeBottom = 0.35f;  // Don't go above this in bottom notifications

            // HUD margins (from screen edge)
            public const float MarginSmall = 12f;
            public const float MarginNormal = 20f;
            public const float MarginLarge = 30f;
        }

        // === CANVAS SORTING ORDERS ===
        public static class SortOrder
        {
            public const int Background = 0;
            public const int Minimap = 80;
            public const int HUD = 90;
            public const int XPBar = 92;
            public const int BossHealth = 95;
            public const int DamageNumbers = 120;
            public const int Announcements = 150;
            public const int LevelUp = 160;
            public const int Screens = 200;
            public const int Achievements = 220;
            public const int Debug = 999;
        }

        // === NOTIFICATION POSITIONS (Y offset from anchor) ===
        public static class NotificationY
        {
            // Top notifications - slide down from top
            public const float TopAnnouncement = -100f;      // Wave announcements
            public const float TopAlert = -180f;             // Boss warnings, etc.
            public const float TopInfo = -60f;               // XP bar, level display

            // Bottom notifications - stay near bottom
            public const float BottomHUD = 30f;              // Heat bar
            public const float BottomInfo = 100f;            // Additional info
        }

        #endregion

        #region Combo Milestones

        public static readonly (int threshold, string message, Color color)[] ComboMilestones = new[]
        {
            (5, "NICE!", Warning),
            (10, "GREAT!", new Color(1f, 0.65f, 0.1f)),
            (15, "AWESOME!", new Color(1f, 0.5f, 0.2f)),
            (20, "INCREDIBLE!", new Color(1f, 0.35f, 0.3f)),
            (30, "UNSTOPPABLE!", Accent),
            (50, "GODLIKE!", new Color(1f, 0.2f, 1f)),
            (75, "LEGENDARY!", new Color(0.8f, 0.4f, 1f)),
            (100, "TRANSCENDENT!", new Color(1f, 1f, 1f)),
        };

        #endregion

        #region Damage Number Styles

        public static class DamageStyle
        {
            // Normal hit
            public const float NormalSize = 16f;
            public static readonly Color NormalColor = TextPrimary;

            // Big hit (20+ damage)
            public const float BigHitSize = 24f;
            public static readonly Color BigHitColor = Warning;
            public const float BigHitScale = 1.15f;

            // Critical (if implemented)
            public const float CriticalSize = 28f;
            public static readonly Color CriticalColor = new Color(1f, 0.5f, 0.1f);
            public const float CriticalScale = 1.3f;

            // Kill/XP
            public const float XPSize = 14f;
            public static readonly Color XPColor = Primary;

            // Heal
            public const float HealSize = 20f;
            public static readonly Color HealColor = Good;

            // Level Up (keep out of DamageNumbers - use LevelUpAnnouncement instead)
            // REMOVED - duplicate notification
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
            if (preserveAlpha)
            {
                result.a = a.a;
            }
            return result;
        }

        /// <summary>
        /// Get pulse color for warning effects
        /// </summary>
        public static Color GetPulseColor(Color baseColor, float time, float frequency = 4f, float intensity = 0.3f)
        {
            float pulse = Mathf.Sin(time * frequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            return Color.Lerp(baseColor, Color.white, pulse * intensity);
        }

        #endregion
    }
}
