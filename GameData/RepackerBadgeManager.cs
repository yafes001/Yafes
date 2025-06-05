using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                { "FG", (Color.FromRgb(46, 204, 113), "FG", new[] { "FG", "FITGIRL","[FitGirl Repack]" }) },
                { "DD", (Color.FromRgb(231, 76, 60), "DD", new[] { "DODI", "DD" }) },
                { "EAS", (Color.FromRgb(230, 126, 34), "EAS", new[] { "ELAMIGOS", "AMIGOS", "EAS" }) },
                { "CDX", (Color.FromRgb(52, 152, 219), "CDX", new[] { "CODEX", "CDX" }) },
                { "SDRW", (Color.FromRgb(155, 89, 182), "SDRW", new[] { "SKIDROW", "SKR", "SDRW" }) },
                { "PLZ", (Color.FromRgb(241, 196, 15), "PLZ", new[] { "PLAZA", "PLZ" }) },
                { "CPY", (Color.FromRgb(244, 143, 177), "CPY", new[] { "CPY" }) },
                { "EMP", (Color.FromRgb(212, 175, 55), "EMP", new[] { "EMPRESS", "EMP" }) },
                { "HDL", (Color.FromRgb(149, 165, 166), "HDL", new[] { "HOODLUM", "HDL" }) },
                { "TNY", (Color.FromRgb(26, 188, 156), "TNY", new[] { "TINY", "TINYREPACKS", "TNY" }) },
                { "RLD", (Color.FromRgb(192, 57, 43), "RLD", new[] { "RELOADED", "RLD" }) }
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
            var ribbonContainer = new Canvas
            {
                Width = 50,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0),
                ClipToBounds = false
            };

            Panel.SetZIndex(ribbonContainer, 100);

            var mainRibbon = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(repackerInfo.badgeColor, 0.0),
                        new GradientStop(Color.FromArgb(255,
                            (byte)(repackerInfo.badgeColor.R * 0.8),
                            (byte)(repackerInfo.badgeColor.G * 0.8),
                            (byte)(repackerInfo.badgeColor.B * 0.8)), 0.6),
                        new GradientStop(Color.FromArgb(255,
                            (byte)(repackerInfo.badgeColor.R * 0.7),
                            (byte)(repackerInfo.badgeColor.G * 0.7),
                            (byte)(repackerInfo.badgeColor.B * 0.7)), 1.0)
                    }
                },
                Width = 45,
                Height = 18,
                CornerRadius = new CornerRadius(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromArgb(120, 0, 0, 0),
                    BlurRadius = 6,
                    ShadowDepth = 3,
                    Opacity = 0.8,
                    Direction = 315
                }
            };

            var ribbonText = new TextBlock
            {
                Text = repackerInfo.displayName,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Arial"),
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 1,
                    ShadowDepth = 1,
                    Opacity = 0.8
                }
            };

            mainRibbon.Child = ribbonText;

            var foldTriangle = new Polygon
            {
                Points = new PointCollection
                {
                    new System.Windows.Point(45, 0),
                    new System.Windows.Point(50, 9),
                    new System.Windows.Point(45, 18)
                },
                Fill = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(255,
                            (byte)(repackerInfo.badgeColor.R * 0.6),
                            (byte)(repackerInfo.badgeColor.G * 0.6),
                            (byte)(repackerInfo.badgeColor.B * 0.6)), 0.0),
                        new GradientStop(Color.FromArgb(255,
                            (byte)(repackerInfo.badgeColor.R * 0.4),
                            (byte)(repackerInfo.badgeColor.G * 0.4),
                            (byte)(repackerInfo.badgeColor.B * 0.4)), 1.0)
                    }
                },
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromArgb(80, 0, 0, 0),
                    BlurRadius = 3,
                    ShadowDepth = 2,
                    Opacity = 0.6
                }
            };

            Canvas.SetLeft(mainRibbon, 0);
            Canvas.SetTop(mainRibbon, 1);
            Canvas.SetLeft(foldTriangle, 0);
            Canvas.SetTop(foldTriangle, 1);

            ribbonContainer.Children.Add(foldTriangle);
            ribbonContainer.Children.Add(mainRibbon);

            var finalContainer = new Border
            {
                Child = ribbonContainer,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, -5, 0)
            };

            return finalContainer;
        }
    }
}