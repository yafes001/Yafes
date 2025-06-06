using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Yafes.GameData; // GameSetup için

namespace Yafes.Managers
{
    public class GameCardManager
    {
        // GameInstallationQueueManager referansı
        private GameInstallationQueueManager _queueManager;

        /// <summary>
        /// Queue manager'ı set etmek için (GamesPanelManager'dan çağrılacak)
        /// </summary>
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
                    Height = 160, // ✅ Biraz daha yüksek - daha iyi oran
                    Width = 120,  // ✅ Sabit genişlik - 3:4 ratio (4:5 benzeri)
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
                            // ✅ TEK IMAGE - Çerçeveye TAM OTURAN görüntü
                            var foregroundImage = new Image
                            {
                                Source = bitmapImage,
                                Stretch = Stretch.UniformToFill, // ✅ UniformToFill - Çerçeveyi tamamen doldurur
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Width = Double.NaN,
                                Height = Double.NaN,
                                Opacity = 1.0 // Tam opak
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
                    catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
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

        /// <summary>
        /// 🎯 UPDATED: Card click - Silent installation başlatır
        /// </summary>
        private async void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border card)
                {
                    string gameName = "Unknown Game";
                    Yafes.Models.GameData gameData = null;

                    // GameData'yı al
                    if (card.Tag is Yafes.Models.GameData data)
                    {
                        gameData = data;
                        gameName = gameData.Name;
                    }
                    else
                    {
                        // Fallback: StackPanel'den oyun adını al
                        var stackPanel = card.Child as StackPanel;
                        if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock gameNameTextBlock)
                        {
                            gameName = gameNameTextBlock.Text;
                        }
                    }

                    // 1. Immediate visual feedback
                    CreateShakeEffect(card);

                    // 2. Eğer gerçek GameData varsa silent installation başlat
                    if (gameData != null)
                    {
                        // Card'ın görsel durumunu "installing" moduna al
                        SetCardInstalling(card, true);

                        // Silent installation başlat
                        bool installationSuccess = await GameSetup.StartSilentInstallation(gameData, _queueManager);

                        if (installationSuccess)
                        {
                            // Installation başarılı - card'ı "installed" moduna al
                            SetCardInstalled(card, true);

                            // Success glow effect
                            CreateSuccessGlowEffect(card);
                        }
                        else
                        {
                            // Installation başarısız - card'ı normal moduna döndür
                            SetCardInstalling(card, false);

                            // Error shake effect
                            CreateErrorShakeEffect(card);
                        }
                    }
                    else
                    {
                        // Default games (Steam, Epic, etc.) için - platform açma uyarısı
                        ShowPlatformLaunchMessage(gameName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GameCard_Click error: {ex.Message}");

                // Error durumunda card'ı normale döndür
                if (sender is Border errorCard)
                {
                    SetCardInstalling(errorCard, false);
                    CreateErrorShakeEffect(errorCard);
                }
            }
        }

        /// <summary>
        /// 🔄 Card'ın installing durumunu görsel olarak gösterir
        /// </summary>
        private void SetCardInstalling(Border card, bool isInstalling)
        {
            try
            {
                if (isInstalling)
                {
                    // Installing state: Orange glow + rotating border
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };

                    // Rotating glow animation
                    var pulseAnimation = new DoubleAnimation
                    {
                        From = 0.4,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(1000),
                        RepeatBehavior = RepeatBehavior.Forever,
                        AutoReverse = true
                    };

                    card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, pulseAnimation);
                }
                else
                {
                    // Normal state: Reset to default
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    };

                    // Stop animations
                    card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetCardInstalling error: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Card'ı installed durumunda gösterir
        /// </summary>
        private void SetCardInstalled(Border card, bool isInstalled)
        {
            try
            {
                if (isInstalled)
                {
                    // Installed state: Green glow
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(0, 255, 0),
                        BlurRadius = 10,
                        ShadowDepth = 0,
                        Opacity = 0.6
                    };

                    // Add "INSTALLED" overlay
                    AddInstalledOverlay(card);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetCardInstalled error: {ex.Message}");
            }
        }

        /// <summary>
        /// 📍 "INSTALLED" overlay ekler
        /// </summary>
        private void AddInstalledOverlay(Border card)
        {
            try
            {
                var mainGrid = card.Child as Grid;
                if (mainGrid != null)
                {
                    var installedBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(200, 0, 255, 0)),
                        CornerRadius = new CornerRadius(3),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(8, 2, 8, 2)
                    };

                    var installedText = new TextBlock
                    {
                        Text = "KURULU",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    installedBadge.Child = installedText;
                    Panel.SetZIndex(installedBadge, 200);

                    mainGrid.Children.Add(installedBadge);

                    // Fade in animation
                    installedBadge.Opacity = 0;
                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(500)
                    };
                    installedBadge.BeginAnimation(Border.OpacityProperty, fadeIn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddInstalledOverlay error: {ex.Message}");
            }
        }

        /// <summary>
        /// ✨ Success glow effect
        /// </summary>
        private void CreateSuccessGlowEffect(Border card)
        {
            try
            {
                var glowAnimation = new DoubleAnimation
                {
                    From = 0.6,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };

                card.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, glowAnimation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateSuccessGlowEffect error: {ex.Message}");
            }
        }

        /// <summary>
        /// ❌ Error shake effect
        /// </summary>
        private void CreateErrorShakeEffect(Border card)
        {
            try
            {
                // Red flash + shake
                var originalBrush = card.BorderBrush;
                card.BorderBrush = new SolidColorBrush(Colors.Red);

                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(4)
                };

                var translateTransform = new TranslateTransform();
                card.RenderTransform = translateTransform;

                shakeAnimation.Completed += (s, e) =>
                {
                    card.BorderBrush = originalBrush; // Restore original color
                    card.RenderTransform = null;
                };

                translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateErrorShakeEffect error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🎮 Platform games için launch message
        /// </summary>
        private void ShowPlatformLaunchMessage(string gameName)
        {
            try
            {
                MessageBox.Show(
                    $"🎮 {gameName} platformunu açmak istiyor musunuz?",
                    "Platform Launcher",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowPlatformLaunchMessage error: {ex.Message}");
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
            catch (Exception ex)
            {
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
            catch (Exception ex)
            {
            }
        }

        // XAML'daki spesifik logo için metotlar - Main.xaml analizi
        public void HideMainBackgroundLogo(Window mainWindow)
        {
            try
            {
                // Main.xaml'daki Canvas.Left="392" Canvas.Top="230" konumundaki logo
                var mainCanvas = mainWindow.FindName("MainCanvas") as Canvas;
                if (mainCanvas != null)
                {
                    foreach (var child in mainCanvas.Children)
                    {
                        if (child is Image logoImage &&
                            logoImage.Source?.ToString().Contains("logo.png") == true)
                        {
                            // Canvas pozisyonunu kontrol et
                            double left = Canvas.GetLeft(logoImage);
                            double top = Canvas.GetTop(logoImage);

                            // Spesifik logo pozisyonu: Canvas.Left="392" Canvas.Top="230"
                            if (Math.Abs(left - 392) < 5 && Math.Abs(top - 230) < 5)
                            {
                                logoImage.Visibility = Visibility.Collapsed;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void ShowMainBackgroundLogo(Window mainWindow)
        {
            try
            {
                // Main.xaml'daki Canvas.Left="392" Canvas.Top="230" konumundaki logo
                var mainCanvas = mainWindow.FindName("MainCanvas") as Canvas;
                if (mainCanvas != null)
                {
                    foreach (var child in mainCanvas.Children)
                    {
                        if (child is Image logoImage &&
                            logoImage.Source?.ToString().Contains("logo.png") == true)
                        {
                            // Canvas pozisyonunu kontrol et
                            double left = Canvas.GetLeft(logoImage);
                            double top = Canvas.GetTop(logoImage);

                            // Spesifik logo pozisyonu: Canvas.Left="392" Canvas.Top="230"
                            if (Math.Abs(left - 392) < 5 && Math.Abs(top - 230) < 5)
                            {
                                logoImage.Visibility = Visibility.Visible;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
            catch (Exception ex)
            {
            }
        }
    }
}