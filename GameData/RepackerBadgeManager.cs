using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Yafes.Managers
{
    public static class RepackerBadgeManager
    {
        public static (string repacker, Color badgeColor, string displayName) ExtractRepackerFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return ("", Colors.Gray, "");

            var upperFileName = fileName.ToUpper();

            var repackers = new Dictionary<string, (Color color, string display, string[] patterns)>
            {
                { "FG", (Color.FromRgb(46, 204, 113), "FitGirl", new[] { "FG", "FITGIRL","[FitGirl Repack]" }) },
                { "DD", (Color.FromRgb(255, 20, 60), "DODI", new[] { "DODI", "DD" }) },
                { "EAS", (Color.FromRgb(230, 126, 34), "ElAmigos", new[] { "ELAMIGOS", "AMIGOS", "EAS" }) },
                { "CDX", (Color.FromRgb(52, 152, 219), "CODEX", new[] { "CODEX", "CDX" }) },
                { "SDRW", (Color.FromRgb(155, 89, 182), "SKIDROW", new[] { "SKIDROW", "SKR", "SDRW" }) },
                { "PLZ", (Color.FromRgb(241, 196, 15), "PLAZA", new[] { "PLAZA", "PLZ" }) },
                { "CPY", (Color.FromRgb(244, 143, 177), "CPY", new[] { "CPY" }) },
                { "EMP", (Color.FromRgb(212, 175, 55), "EMPRESS", new[] { "EMPRESS", "EMP" }) },
                { "HDL", (Color.FromRgb(149, 165, 166), "HOODLUM", new[] { "HOODLUM", "HDL" }) },
                { "TNY", (Color.FromRgb(26, 188, 156), "TINY", new[] { "TINY", "TINYREPACKS", "TNY" }) },
                { "RLD", (Color.FromRgb(192, 57, 43), "RELOADED", new[] { "RELOADED", "RLD" }) },
                { "RUNE", (Color.FromRgb(255, 69, 0), "RUNE", new[] { "RUNE", "rune" }) }
            };

            foreach (var repackerEntry in repackers)
            {
                var repackerKey = repackerEntry.Key;
                var repackerData = repackerEntry.Value;

                foreach (var pattern in repackerData.patterns)
                {
                    var searchPatterns = new[]
                    {
                        $"_{pattern}_", $"-{pattern}-", $"_{pattern}.", $"-{pattern}.",
                        $".{pattern}.", $"[{pattern}]", $"({pattern})", $"{pattern}_",
                        $"{pattern}-", $"{pattern}.", $"_{pattern}_[0-9]", $"-{pattern}_[0-9]",
                        $"{pattern}[0-9]", $" {pattern} ", $" {pattern}_", $"_{pattern} ",
                        $"^{pattern}_", $"_{pattern}$"
                    };

                    foreach (var searchPattern in searchPatterns)
                    {
                        var simplePattern = searchPattern
                            .Replace("^", "")
                            .Replace("$", "")
                            .Replace("[0-9]", "");

                        if (upperFileName.Contains(simplePattern.ToUpper()))
                        {
                            return (repackerKey, repackerData.color, repackerData.display);
                        }
                    }

                    if (upperFileName.Contains(pattern.ToUpper()))
                    {
                        return (repackerKey, repackerData.color, repackerData.display);
                    }
                }
            }

            return ("", Colors.Gray, "Unknown");
        }

        public static Border CreateRepackerBadge((string repacker, Color badgeColor, string displayName) repackerInfo)
        {
            // Basit, temiz container
            var container = new Border
            {
                Background = new SolidColorBrush(repackerInfo.badgeColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.3
                }
            };

            Panel.SetZIndex(container, 100);

            // Sadece text
            var text = new TextBlock
            {
                Text = repackerInfo.displayName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI")
            };

            container.Child = text;

            // Basit hover efekti
            container.MouseEnter += (s, e) =>
            {
                container.Opacity = 0.8;
            };

            container.MouseLeave += (s, e) =>
            {
                container.Opacity = 1.0;
            };

            return container;
        }
    }
}