using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Yafes.GameData;

namespace Yafes.Managers
{
    public class GameCardManager
    {
        private GameInstallationQueueManager _queueManager;

        public void SetQueueManager(GameInstallationQueueManager queueManager)
        {
            _queueManager = queueManager;
        }

        public async Task<Border> CreateGameCard(Yafes.Models.GameData game)
        {
            try
            {
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 160,
                    Width = 120,
                    Cursor = Cursors.Hand,
                    Tag = game,
                    CornerRadius = new CornerRadius(8),
                    ClipToBounds = true,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    }
                };

                var mainGrid = new Grid();

                if (!string.IsNullOrEmpty(game.ImageName))
                {
                    try
                    {
                        BitmapImage bitmapImage = await Task.Run(() => ImageManager.GetGameImage(game.ImageName));

                        if (bitmapImage != null && bitmapImage != ImageManager.GetDefaultImage())
                        {
                            var foregroundImage = new Image
                            {
                                Source = bitmapImage,
                                Stretch = Stretch.UniformToFill,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Width = Double.NaN,
                                Height = Double.NaN,
                                Opacity = 1.0
                            };

                            RenderOptions.SetBitmapScalingMode(foregroundImage, BitmapScalingMode.HighQuality);
                            mainGrid.Children.Add(foregroundImage);
                        }
                        else
                        {
                            var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                            mainGrid.Children.Add(iconGrid);
                        }
                    }
                    catch (Exception)
                    {
                        var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                        mainGrid.Children.Add(iconGrid);
                    }
                }
                else
                {
                    var iconGrid = CreateFullFrameCategoryIcon(game.Category);
                    mainGrid.Children.Add(iconGrid);
                }

                var repackerInfo = RepackerBadgeManager.ExtractRepackerFromFileName(game.ImageName);
                if (!string.IsNullOrEmpty(repackerInfo.repacker))
                {
                    var repackerBadge = RepackerBadgeManager.CreateRepackerBadge(repackerInfo);
                    mainGrid.Children.Add(repackerBadge);
                }

                var textOverlay = new Border
                {
                    Background = new LinearGradientBrush(
                        Color.FromArgb(0, 0, 0, 0),
                        Color.FromArgb(220, 0, 0, 0),
                        new Point(0, 0.7), new Point(0, 1)),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 55,
                    Opacity = 0,
                    Name = "TextOverlay"
                };

                var textStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(8, 0, 8, 10)
                };

                var gameNameText = new TextBlock
                {
                    Text = game.Name,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 110,
                    LineHeight = 12,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 5,
                        ShadowDepth = 2,
                        Opacity = 0.9
                    }
                };

                var gameSizeText = new TextBlock
                {
                    Text = game.Size,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 0),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 4,
                        ShadowDepth = 1,
                        Opacity = 0.9
                    }
                };

                textStack.Children.Add(gameNameText);
                textStack.Children.Add(gameSizeText);
                textOverlay.Child = textStack;

                var overlayTransform = new TranslateTransform { Y = 55 };
                textOverlay.RenderTransform = overlayTransform;

                mainGrid.Children.Add(textOverlay);
                gameCard.Child = mainGrid;

                gameCard.MouseEnter += (s, e) => GameCard_MouseEnter_WithOverlay(s, e);
                gameCard.MouseLeave += (s, e) => GameCard_MouseLeave_WithOverlay(s, e);
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Border CreateDefaultGameCard(string name, string icon, string size, string category)
        {
            try
            {
                var gameCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Height = 80,
                    Cursor = Cursors.Hand,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    }
                };

                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.6
                    }
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = size,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                gameCard.Child = stackPanel;

                gameCard.MouseEnter += GameCard_MouseEnter_WithOverlay;
                gameCard.MouseLeave += GameCard_MouseLeave_WithOverlay;
                gameCard.MouseLeftButtonDown += GameCard_Click;

                return gameCard;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void CreateDefaultGameCards(UniformGrid gamesGrid)
        {
            try
            {
                var defaultGames = new[]
                {
                    new { Name = "Steam", Icon = "🎯", Size = "150 MB", Category = "Platform" },
                    new { Name = "Epic Games", Icon = "🎮", Size = "200 MB", Category = "Platform" },
                    new { Name = "GOG Galaxy", Icon = "🎲", Size = "80 MB", Category = "Platform" },
                    new { Name = "Origin", Icon = "⚡", Size = "120 MB", Category = "Platform" },
                    new { Name = "Battle.net", Icon = "🚀", Size = "90 MB", Category = "Platform" },
                    new { Name = "Ubisoft Connect", Icon = "🎪", Size = "110 MB", Category = "Platform" },
                    new { Name = "Rockstar", Icon = "🎭", Size = "85 MB", Category = "Platform" },
                    new { Name = "Xbox App", Icon = "⭐", Size = "95 MB", Category = "Platform" }
                };

                foreach (var game in defaultGames)
                {
                    var gameCard = CreateDefaultGameCard(game.Name, game.Icon, game.Size, game.Category);
                    if (gameCard != null)
                    {
                        gamesGrid.Children.Add(gameCard);
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private Grid CreateFullFrameCategoryIcon(string category)
        {
            var iconGrid = new Grid
            {
                Background = new LinearGradientBrush(
                    Color.FromArgb(100, 0, 0, 0),
                    Color.FromArgb(140, 0, 0, 0),
                    new Point(0, 0),
                    new Point(1, 1)
                ),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var categoryIcon = GetCategoryIcon(category);
            var iconText = new TextBlock
            {
                Text = categoryIcon,
                FontSize = 52,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 0, 0),
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.9
                }
            };

            iconGrid.Children.Add(iconText);
            return iconGrid;
        }

        private string GetCategoryIcon(string category)
        {
            return category?.ToLower() switch
            {
                "fps" => "🔫",
                "rpg" => "🗡️",
                "racing" => "🏎️",
                "action" => "⚔️",
                "strategy" => "♟️",
                "sports" => "⚽",
                "horror" => "👻",
                "simulation" => "🎛️",
                "puzzle" => "🧩",
                "adventure" => "🗺️",
                "platform" => "🎮",
                _ => "🎮"
            };
        }

        private async void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    string gameName = "Unknown Game";
                    Yafes.Models.GameData gameData = null;

                    CreateShakeEffect(card);

                    if (card.Tag is Yafes.Models.GameData data)
                    {
                        gameData = data;
                        gameName = gameData.Name;
                    }
                    else
                    {
                        gameName = ExtractGameNameFromCard(card);

                        if (!string.IsNullOrEmpty(gameName) && gameName != "Unknown Game")
                        {
                            gameData = CreateMockGameData(gameName);
                        }
                    }

                    if (gameData != null && !string.IsNullOrEmpty(gameName) && gameName != "Unknown Game")
                    {
                        if (_queueManager != null)
                        {
                            try
                            {
                                await CreateSuccessEffect(card);
                                await _queueManager.AddGameToInstallationQueue(gameData);
                            }
                            catch (Exception)
                            {
                                await CreateErrorEffect(card);
                            }
                        }
                        else
                        {
                            await CreateErrorEffect(card);
                        }
                    }
                    else
                    {
                        await CreatePlatformEffect(card);
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private async Task CreateSuccessEffect(Border card)
        {
            try
            {
                var successAnimation = new DoubleAnimation
                {
                    From = 0.4,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                var originalEffect = card.Effect;
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Green,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };

                card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, successAnimation);

                await Task.Delay(800);
                card.Effect = originalEffect;
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private async Task CreateErrorEffect(Border card)
        {
            try
            {
                var errorAnimation = new DoubleAnimation
                {
                    From = 0.4,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };

                var originalEffect = card.Effect;
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Red,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };

                card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, errorAnimation);

                await Task.Delay(900);
                card.Effect = originalEffect;
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private async Task CreatePlatformEffect(Border card)
        {
            try
            {
                var platformAnimation = new DoubleAnimation
                {
                    From = 0.4,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                var originalEffect = card.Effect;
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Orange,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };

                card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, platformAnimation);

                await Task.Delay(800);
                card.Effect = originalEffect;
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private string ExtractGameNameFromCard(Border card)
        {
            try
            {
                if (card.Child is Grid mainGrid)
                {
                    foreach (var child in mainGrid.Children)
                    {
                        if (child is Border overlay && overlay.Name == "TextOverlay")
                        {
                            if (overlay.Child is StackPanel textStack)
                            {
                                if (textStack.Children.Count > 0 && textStack.Children[0] is TextBlock gameNameBlock)
                                {
                                    var gameName = gameNameBlock.Text?.Trim();
                                    if (!string.IsNullOrEmpty(gameName))
                                    {
                                        return gameName;
                                    }
                                }

                                foreach (var stackChild in textStack.Children)
                                {
                                    if (stackChild is TextBlock textBlock)
                                    {
                                        var text = textBlock.Text?.Trim();

                                        if (!string.IsNullOrEmpty(text) &&
                                            textBlock.FontWeight == FontWeights.Bold &&
                                            !text.Contains("MB") && !text.Contains("GB"))
                                        {
                                            return text;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (card.Child is StackPanel stackPanel)
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is TextBlock textBlock && textBlock.FontWeight == FontWeights.Bold)
                            {
                                var text = textBlock.Text?.Trim();

                                if (!string.IsNullOrEmpty(text) && !text.Contains("MB") && !text.Contains("GB"))
                                {
                                    return text;
                                }
                            }
                        }
                    }
                }

                return "Unknown Game";
            }
            catch (Exception)
            {
                return "Unknown Game";
            }
        }

        private Yafes.Models.GameData CreateMockGameData(string gameName)
        {
            try
            {
                return new Yafes.Models.GameData
                {
                    Id = $"mock_{gameName.Replace(" ", "_").ToLower()}",
                    Name = gameName,
                    ImageName = "",
                    SetupPath = "",
                    Category = "General",
                    Size = "Unknown",
                    IsInstalled = false,
                    LastPlayed = DateTime.MinValue,
                    Description = $"Mock game data for {gameName}"
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void GameCard_MouseEnter_WithOverlay(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    var scaleTransform = new ScaleTransform(1.05, 1.05);
                    card.RenderTransform = scaleTransform;
                    card.RenderTransformOrigin = new Point(0.5, 0.5);

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 215, 0),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };

                    var mainGrid = card.Child as Grid;
                    if (mainGrid != null)
                    {
                        foreach (var child in mainGrid.Children)
                        {
                            if (child is Border overlay && overlay.Name == "TextOverlay")
                            {
                                var transform = overlay.RenderTransform as TranslateTransform;
                                if (transform != null)
                                {
                                    var opacityAnimation = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromMilliseconds(300),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };

                                    var slideAnimation = new DoubleAnimation
                                    {
                                        From = 50,
                                        To = 0,
                                        Duration = TimeSpan.FromMilliseconds(300),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };

                                    overlay.BeginAnimation(Border.OpacityProperty, opacityAnimation);
                                    transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private void GameCard_MouseLeave_WithOverlay(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    card.RenderTransform = scaleTransform;

                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    };

                    var mainGrid = card.Child as Grid;
                    if (mainGrid != null)
                    {
                        foreach (var child in mainGrid.Children)
                        {
                            if (child is Border overlay && overlay.Name == "TextOverlay")
                            {
                                var transform = overlay.RenderTransform as TranslateTransform;
                                if (transform != null)
                                {
                                    var opacityAnimation = new DoubleAnimation
                                    {
                                        From = 1,
                                        To = 0,
                                        Duration = TimeSpan.FromMilliseconds(200),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                                    };

                                    var slideAnimation = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 50,
                                        Duration = TimeSpan.FromMilliseconds(200),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                                    };

                                    overlay.BeginAnimation(Border.OpacityProperty, opacityAnimation);
                                    transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        public void HideMainBackgroundLogo(Window mainWindow)
        {
            try
            {
                var mainCanvas = mainWindow.FindName("MainCanvas") as Canvas;
                if (mainCanvas != null)
                {
                    foreach (var child in mainCanvas.Children)
                    {
                        if (child is Image logoImage &&
                            logoImage.Source?.ToString().Contains("logo.png") == true)
                        {
                            double left = Canvas.GetLeft(logoImage);
                            double top = Canvas.GetTop(logoImage);

                            if (Math.Abs(left - 392) < 5 && Math.Abs(top - 230) < 5)
                            {
                                logoImage.Visibility = Visibility.Collapsed;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        public void ShowMainBackgroundLogo(Window mainWindow)
        {
            try
            {
                var mainCanvas = mainWindow.FindName("MainCanvas") as Canvas;
                if (mainCanvas != null)
                {
                    foreach (var child in mainCanvas.Children)
                    {
                        if (child is Image logoImage &&
                            logoImage.Source?.ToString().Contains("logo.png") == true)
                        {
                            double left = Canvas.GetLeft(logoImage);
                            double top = Canvas.GetTop(logoImage);

                            if (Math.Abs(left - 392) < 5 && Math.Abs(top - 230) < 5)
                            {
                                logoImage.Visibility = Visibility.Visible;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private void CreateShakeEffect(Border card)
        {
            try
            {
                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 3,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                var translateTransform = new TranslateTransform();
                card.RenderTransform = translateTransform;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch (Exception)
            {
                // Silent fail
            }
        }
    }
}