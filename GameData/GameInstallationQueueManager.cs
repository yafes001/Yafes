// GameInstallationQueueManager.cs - LAMBDA TASARIMI İLE UYUMLU TAM VERSİYON
// Oyun yükleme kuyruğunu yönetir

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Yafes.Managers
{
    /// <summary>
    /// Oyun yükleme ve kurulum kuyruğunu yöneten sınıf - Lambda teması ile uyumlu
    /// </summary>
    public class GameInstallationQueueManager
    {
        private readonly StackPanel _gameInstallationsPanel;
        private readonly TextBlock _noGameInstallationsText;
        private readonly TextBox _logTextBox;
        private readonly Window _parentWindow;

        // Aktif oyun yüklemeleri
        private readonly List<GameInstallationItem> _activeGameInstallations;

        // UI elemanları için referanslar
        private Border _gameInstallationQueuePanel;
        private ListBox _gameListBox;
        private ScrollViewer _scrollViewer;
        private TextBlock _titleText;

        // Lambda efektleri için animasyonlar
        private Storyboard _pulseAnimation;
        private Storyboard _glowAnimation;

        // Events
        public event Action<string> LogMessage;

        public GameInstallationQueueManager(StackPanel gameInstallationsPanel, TextBlock noGameInstallationsText, TextBox logTextBox, Window parentWindow = null)
        {
            _gameInstallationsPanel = gameInstallationsPanel ?? throw new ArgumentNullException(nameof(gameInstallationsPanel));
            _noGameInstallationsText = noGameInstallationsText ?? throw new ArgumentNullException(nameof(noGameInstallationsText));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));
            _parentWindow = parentWindow;

            _activeGameInstallations = new List<GameInstallationItem>();

            LogMessage += (message) =>
            {
                try
                {
                    _logTextBox?.Dispatcher.Invoke(() =>
                    {
                        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                        _logTextBox.ScrollToEnd();
                    });
                }
                catch (Exception)
                {
                    // Silent fail
                }
            };

            InitializeLambdaAnimations();
        }

        /// <summary>
        /// Lambda temalı animasyonları başlatır
        /// </summary>
        private void InitializeLambdaAnimations()
        {
            try
            {
                // Lambda pulse animasyonu
                _pulseAnimation = new Storyboard
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };

                var pulseOpacity = new DoubleAnimation
                {
                    From = 0.6,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(1200),
                    AutoReverse = true
                };

                _pulseAnimation.Children.Add(pulseOpacity);

                // Lambda glow animasyonu
                _glowAnimation = new Storyboard
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };

                var glowRadius = new DoubleAnimation
                {
                    From = 6,
                    To = 12,
                    Duration = TimeSpan.FromMilliseconds(2000),
                    AutoReverse = true
                };

                _glowAnimation.Children.Add(glowRadius);
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Oyun yükleme kuyruğu panelini oluşturur - Lambda teması ile
        /// </summary>
        public Border CreateGameInstallationQueuePanel()
        {
            try
            {
                if (_parentWindow == null) return null;

                var mainCanvas = UIHelperManager.FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                if (mainCanvas == null) return null;

                Border rightSidebar = null;
                foreach (var child in mainCanvas.Children)
                {
                    if (child is Border border)
                    {
                        double leftValue = Canvas.GetLeft(border);
                        if (Math.Abs(leftValue - 890) < 1)
                        {
                            rightSidebar = border;
                            break;
                        }
                    }
                }

                if (rightSidebar == null) return null;

                var sidebarCanvas = rightSidebar.Child as Canvas;
                if (sidebarCanvas == null) return null;

                // Lambda temalı ana panel
                _gameInstallationQueuePanel = new Border
                {
                    Width = 170,
                    Height = 404,
                    Visibility = Visibility.Collapsed,
                    Name = "GameInstallationQueuePanel"
                };

                // Lambda gradient background
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(26, 255, 165, 0), 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(149, 34, 34, 34), 0.2));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(250, 17, 17, 17), 0.8));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(26, 255, 165, 0), 1));

                _gameInstallationQueuePanel.Background = gradientBrush;
                _gameInstallationQueuePanel.BorderBrush = new SolidColorBrush(Color.FromArgb(153, 255, 165, 0));
                _gameInstallationQueuePanel.BorderThickness = new Thickness(0);

                // Lambda glow effect
                _gameInstallationQueuePanel.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(255, 165, 0),
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.3
                };

                Canvas.SetTop(_gameInstallationQueuePanel, 168);

                var transform = new TranslateTransform();
                transform.X = 220;
                _gameInstallationQueuePanel.RenderTransform = transform;

                var contentCanvas = new Canvas
                {
                    Margin = new Thickness(8)
                };

                // Lambda temalı başlık
                _titleText = new TextBlock
                {
                    Text = "OYUN KURULUM KUYRUĞU (0)",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.6
                    }
                };
                Canvas.SetLeft(_titleText, 0);
                Canvas.SetTop(_titleText, 0);

                // Lambda ScrollViewer ile ListBox
                CreateLambdaScrollableList();

                contentCanvas.Children.Add(_titleText);
                contentCanvas.Children.Add(_scrollViewer);
                _gameInstallationQueuePanel.Child = contentCanvas;

                Panel.SetZIndex(_gameInstallationQueuePanel, 1000);
                sidebarCanvas.Children.Add(_gameInstallationQueuePanel);

                // Lambda animasyonlarını başlat
                StartLambdaAnimations();

                LogMessage?.Invoke("🎮 Lambda temalı oyun kurulum kuyruğu oluşturuldu");

                return _gameInstallationQueuePanel;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateGameInstallationQueuePanel hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lambda temalı scrollable liste oluşturur
        /// </summary>
        private void CreateLambdaScrollableList()
        {
            try
            {
                // Lambda ScrollViewer oluştur
                _scrollViewer = new ScrollViewer
                {
                    Width = 154,
                    Height = 376,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Background = Brushes.Transparent
                };

                // Lambda ScrollViewer stilini uygula
                try
                {
                    var scrollViewerStyle = _parentWindow?.FindResource("LambdaScrollViewerStyle") as Style;
                    if (scrollViewerStyle != null)
                    {
                        _scrollViewer.Style = scrollViewerStyle;
                    }
                }
                catch (Exception)
                {
                    // Stil bulunamazsa manuel Lambda scrollbar oluştur
                    CreateManualLambdaScrollBar();
                }

                // Lambda ListBox oluştur
                _gameListBox = new ListBox
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 8,
                    FontWeight = FontWeights.Normal,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Name = "GameInstallationListBox",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0)
                };

                // ListBox'ın kendi scrollbar'ını devre dışı bırak
                ScrollViewer.SetVerticalScrollBarVisibility(_gameListBox, ScrollBarVisibility.Disabled);
                ScrollViewer.SetHorizontalScrollBarVisibility(_gameListBox, ScrollBarVisibility.Disabled);

                // Lambda List stillerini uygula
                try
                {
                    var listStyle = _parentWindow?.FindResource("LambdaListStyle") as Style;
                    if (listStyle != null)
                    {
                        _gameListBox.Style = listStyle;
                    }

                    var listItemStyle = _parentWindow?.FindResource("LambdaListItemStyle") as Style;
                    if (listItemStyle != null)
                    {
                        _gameListBox.ItemContainerStyle = listItemStyle;
                    }
                }
                catch (Exception)
                {
                    // Stil bulunamazsa varsayılan kullan
                    ApplyManualLambdaListStyle();
                }

                // ListBox'ı ScrollViewer'a ekle
                _scrollViewer.Content = _gameListBox;

                // ScrollViewer'ı Canvas'a yerleştir
                Canvas.SetLeft(_scrollViewer, 0);
                Canvas.SetTop(_scrollViewer, 20);
            }
            catch (Exception)
            {
                // Fallback: basit liste oluştur
                CreateFallbackList();
            }
        }

        /// <summary>
        /// Lambda scrollbar bulunamazsa manuel oluşturur
        /// </summary>
        private void CreateManualLambdaScrollBar()
        {
            try
            {
                // Manuel Lambda scrollbar stili oluştur
                var scrollBarStyle = new Style(typeof(ScrollBar));

                // Background gradient
                var backgroundBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromArgb(64, 255, 165, 0), 0));
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromArgb(128, 0, 0, 0), 0.5));
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromArgb(64, 255, 165, 0), 1));

                scrollBarStyle.Setters.Add(new Setter(ScrollBar.BackgroundProperty, backgroundBrush));
                scrollBarStyle.Setters.Add(new Setter(ScrollBar.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(153, 255, 165, 0))));
                scrollBarStyle.Setters.Add(new Setter(ScrollBar.BorderThicknessProperty, new Thickness(0)));
                scrollBarStyle.Setters.Add(new Setter(ScrollBar.WidthProperty, 12.0));

                _scrollViewer.Resources.Add(typeof(ScrollBar), scrollBarStyle);
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Lambda liste stili bulunamazsa manuel uygular
        /// </summary>
        private void ApplyManualLambdaListStyle()
        {
            try
            {
                // Manuel Lambda liste stili
                var listStyle = new Style(typeof(ListBox));
                listStyle.Setters.Add(new Setter(ListBox.BackgroundProperty, Brushes.Transparent));
                listStyle.Setters.Add(new Setter(ListBox.BorderThicknessProperty, new Thickness(0)));
                listStyle.Setters.Add(new Setter(ListBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(204, 204, 204))));

                _gameListBox.Style = listStyle;

                // Manuel Lambda liste item stili
                var itemStyle = new Style(typeof(ListBoxItem));
                itemStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, Brushes.Transparent));
                itemStyle.Setters.Add(new Setter(ListBoxItem.BorderThicknessProperty, new Thickness(0)));
                itemStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(5, 2, 5, 2)));
                itemStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(0, 1, 0, 1)));

                _gameListBox.ItemContainerStyle = itemStyle;
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Fallback basit liste oluşturur
        /// </summary>
        private void CreateFallbackList()
        {
            try
            {
                _gameListBox = new ListBox
                {
                    Width = 154,
                    Height = 376,
                    Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(77, 255, 165, 0)),
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 8,
                    Name = "GameInstallationListBox"
                };

                Canvas.SetLeft(_gameListBox, 0);
                Canvas.SetTop(_gameListBox, 20);

                _scrollViewer = null; // ScrollViewer kullanmayacağız
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Lambda animasyonlarını başlatır
        /// </summary>
        private void StartLambdaAnimations()
        {
            try
            {
                if (_titleText != null && _pulseAnimation != null)
                {
                    Storyboard.SetTarget(_pulseAnimation.Children[0], _titleText.Effect);
                    Storyboard.SetTargetProperty(_pulseAnimation.Children[0], new PropertyPath(DropShadowEffect.OpacityProperty));
                    _pulseAnimation.Begin();
                }

                if (_gameInstallationQueuePanel?.Effect != null && _glowAnimation != null)
                {
                    Storyboard.SetTarget(_glowAnimation.Children[0], _gameInstallationQueuePanel.Effect);
                    Storyboard.SetTargetProperty(_glowAnimation.Children[0], new PropertyPath(DropShadowEffect.BlurRadiusProperty));
                    _glowAnimation.Begin();
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Lambda temalı oyun kartı oluşturur
        /// </summary>
        private ListBoxItem CreateLambdaGameListItem(Yafes.Models.GameData gameData, int gameNumber)
        {
            try
            {
                string color = "#00F5FF"; // Lambda cyan rengi

                var itemContainer = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(60, 0, 245, 255)),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(2, 1, 2, 2),
                    Height = 35,
                    Width = 146,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Tag = $"LAMBDA_GAME_ITEM_{gameData.Id}",
                    Effect = new DropShadowEffect
                    {
                        Color = (Color)ColorConverter.ConvertFromString(color),
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.7
                    }
                };

                var contentGrid = new Grid();
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });

                // Lambda sayı container'ı
                var numberContainer = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Effect = new DropShadowEffect
                    {
                        Color = (Color)ColorConverter.ConvertFromString(color),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 1.0
                    }
                };

                var numberText = new TextBlock
                {
                    Text = gameNumber.ToString(),
                    Foreground = Brushes.Black,
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                numberContainer.Child = numberText;
                Grid.SetColumn(numberContainer, 0);
                Grid.SetRow(numberContainer, 0);
                Grid.SetRowSpan(numberContainer, 2);

                // Oyun adı
                var nameText = new TextBlock
                {
                    Text = gameData.Name,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(4, 1, 2, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameText, 1);
                Grid.SetRow(nameText, 0);

                // Boyut ve durum
                var sizeText = new TextBlock
                {
                    Text = $"{gameData.Size ?? "Bilinmeyen"} • ⏳ Sırada",
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 7,
                    Margin = new Thickness(4, 0, 2, 1),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(sizeText, 1);
                Grid.SetRow(sizeText, 1);

                contentGrid.Children.Add(numberContainer);
                contentGrid.Children.Add(nameText);
                contentGrid.Children.Add(sizeText);

                itemContainer.Child = contentGrid;

                // Lambda pulse animasyonu
                var pulseAnimation = new DoubleAnimation
                {
                    From = 0.6,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(1200),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };
                numberContainer.Effect.BeginAnimation(DropShadowEffect.OpacityProperty, pulseAnimation);

                var listItem = new ListBoxItem
                {
                    Content = itemContainer,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 1, 0, 1),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Tag = $"LAMBDA_GAME_ITEM_{gameData.Id}"
                };

                return listItem;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Kuyruk sayacını günceller - Lambda efektli
        /// </summary>
        private void UpdateQueueCounter()
        {
            try
            {
                if (_titleText != null)
                {
                    int realItemCount = _activeGameInstallations.Count;
                    _titleText.Text = $"OYUN KURULUM KUYRUĞU ({realItemCount})";

                    // Sayaç değiştiğinde Lambda glow efekti
                    if (realItemCount > 0)
                    {
                        var glowPulse = new DoubleAnimation
                        {
                            From = 0.6,
                            To = 1.2,
                            Duration = TimeSpan.FromMilliseconds(300),
                            AutoReverse = true
                        };
                        _titleText.Effect?.BeginAnimation(DropShadowEffect.OpacityProperty, glowPulse);
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Oyun yükleme kuyruğuna yeni oyun ekler - Lambda temalı
        /// </summary>
        public async Task AddGameToInstallationQueue(Yafes.Models.GameData game)
        {
            try
            {
                if (game == null) return;

                // Aynı oyun zaten kuyrukta mı kontrol et
                if (_activeGameInstallations.Any(x => x.GameData.Id == game.Id))
                {
                    LogMessage?.Invoke($"⚠️ {game.Name} zaten yükleme kuyruğunda!");
                    return;
                }

                // Yeni yükleme item'ı oluştur
                var installationItem = new GameInstallationItem
                {
                    GameData = game,
                    Status = GameInstallationStatus.Queued,
                    Progress = 0,
                    Id = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now
                };

                _activeGameInstallations.Add(installationItem);

                // UI'yi güncelle
                await _parentWindow.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (_gameListBox != null)
                        {
                            // Lambda temalı oyun kartı oluştur
                            int gameNumber = _activeGameInstallations.Count;
                            var lambdaGameItem = CreateLambdaGameListItem(game, gameNumber);

                            if (lambdaGameItem != null)
                            {
                                _gameListBox.Items.Add(lambdaGameItem);
                                UpdateQueueCounter();
                                LogMessage?.Invoke($"🎮 {game.Name} Lambda kurulum kuyruğuna eklendi");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"❌ UI güncelleme hatası: {ex.Message}");
                    }
                });

                // Otomatik yüklemeyi başlat
                await StartNextInstallationIfPossible();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ AddGameToInstallationQueue hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Bir sonraki yüklemeyi başlatır - Lambda efektlerle
        /// </summary>
        private async Task StartNextInstallationIfPossible()
        {
            try
            {
                var currentInstalling = _activeGameInstallations.FirstOrDefault(x => x.Status == GameInstallationStatus.Installing);
                if (currentInstalling != null) return;

                var nextInQueue = _activeGameInstallations.FirstOrDefault(x => x.Status == GameInstallationStatus.Queued);
                if (nextInQueue == null) return;

                nextInQueue.Status = GameInstallationStatus.Installing;
                UpdateGameInstallationUI(nextInQueue);

                LogMessage?.Invoke($"🚀 Lambda yükleyici: {nextInQueue.GameData.Name} başlatılıyor...");

                await SimulateGameInstallation(nextInQueue);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartNextInstallationIfPossible hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyun yükleme simülasyonu - Lambda progress efektleri
        /// </summary>
        private async Task SimulateGameInstallation(GameInstallationItem item)
        {
            try
            {
                for (int progress = 0; progress <= 100; progress += 5)
                {
                    item.Progress = progress;
                    UpdateGameInstallationUI(item);

                    if (progress == 100)
                    {
                        item.Status = GameInstallationStatus.Completed;
                        item.EndTime = DateTime.Now;
                        LogMessage?.Invoke($"✅ Lambda kurulum tamamlandı: {item.GameData.Name}");

                        // Başarılı kurulum için Lambda glow efekti
                        await TriggerSuccessAnimation(item);
                    }

                    await Task.Delay(500);
                }

                await Task.Delay(2000);
                await RemoveGameFromQueue(item.Id);
                await StartNextInstallationIfPossible();
            }
            catch (Exception ex)
            {
                item.Status = GameInstallationStatus.Failed;
                UpdateGameInstallationUI(item);
                LogMessage?.Invoke($"❌ Lambda kurulum başarısız: {item.GameData.Name} - {ex.Message}");

                // Hata için Lambda red glow efekti
                await TriggerErrorAnimation(item);
            }
        }

        /// <summary>
        /// Başarılı kurulum animasyonu
        /// </summary>
        private async Task TriggerSuccessAnimation(GameInstallationItem item)
        {
            try
            {
                await _parentWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (_gameListBox != null)
                    {
                        foreach (ListBoxItem listItem in _gameListBox.Items)
                        {
                            if (listItem.Tag?.ToString() == $"LAMBDA_GAME_ITEM_{item.GameData.Id}")
                            {
                                if (listItem.Content is Border border)
                                {
                                    // Yeşil glow efekti
                                    var successGlow = new DropShadowEffect
                                    {
                                        Color = Colors.LimeGreen,
                                        BlurRadius = 15,
                                        ShadowDepth = 0,
                                        Opacity = 1.0
                                    };
                                    border.Effect = successGlow;

                                    // Fade out animasyonu
                                    var fadeOut = new DoubleAnimation
                                    {
                                        From = 1.0,
                                        To = 0.3,
                                        Duration = TimeSpan.FromMilliseconds(1000)
                                    };
                                    border.BeginAnimation(Border.OpacityProperty, fadeOut);
                                }
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Hata animasyonu
        /// </summary>
        private async Task TriggerErrorAnimation(GameInstallationItem item)
        {
            try
            {
                await _parentWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (_gameListBox != null)
                    {
                        foreach (ListBoxItem listItem in _gameListBox.Items)
                        {
                            if (listItem.Tag?.ToString() == $"LAMBDA_GAME_ITEM_{item.GameData.Id}")
                            {
                                if (listItem.Content is Border border)
                                {
                                    // Kırmızı glow efekti
                                    var errorGlow = new DropShadowEffect
                                    {
                                        Color = Colors.Red,
                                        BlurRadius = 12,
                                        ShadowDepth = 0,
                                        Opacity = 0.8
                                    };
                                    border.Effect = errorGlow;

                                    // Shake animasyonu
                                    var shakeTransform = new TranslateTransform();
                                    border.RenderTransform = shakeTransform;

                                    var shakeAnimation = new DoubleAnimationUsingKeyFrames();
                                    shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                                    shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
                                    shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
                                    shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
                                    shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));

                                    shakeTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
                                }
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// UI'daki oyun yükleme kontrolünü günceller - Lambda efektlerle
        /// </summary>
        private void UpdateGameInstallationUI(GameInstallationItem item)
        {
            try
            {
                _parentWindow?.Dispatcher.Invoke(() =>
                {
                    if (_gameListBox != null)
                    {
                        foreach (ListBoxItem listItem in _gameListBox.Items)
                        {
                            if (listItem.Tag?.ToString() == $"LAMBDA_GAME_ITEM_{item.GameData.Id}")
                            {
                                if (listItem.Content is Border border && border.Child is Grid grid)
                                {
                                    foreach (var child in grid.Children)
                                    {
                                        if (child is TextBlock textBlock && textBlock.Text.Contains("•"))
                                        {
                                            string statusText = item.Status switch
                                            {
                                                GameInstallationStatus.Queued => "⏳ Sırada",
                                                GameInstallationStatus.Installing => $"🔄 Yükleniyor ({item.Progress}%)",
                                                GameInstallationStatus.Completed => "✅ Tamamlandı",
                                                GameInstallationStatus.Failed => "❌ Başarısız",
                                                _ => "❓ Bilinmiyor"
                                            };

                                            textBlock.Text = $"{item.GameData.Size ?? "Bilinmeyen"} • {statusText}";

                                            // Durum değişikliğinde renk güncellemesi
                                            if (item.Status == GameInstallationStatus.Installing)
                                            {
                                                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold
                                            }
                                            else if (item.Status == GameInstallationStatus.Completed)
                                            {
                                                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // LimeGreen
                                            }
                                            else if (item.Status == GameInstallationStatus.Failed)
                                            {
                                                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 69, 0)); // Red
                                            }

                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Oyunu kuyruktdan kaldırır - Lambda fade out efekti ile
        /// </summary>
        private async Task RemoveGameFromQueue(string itemId)
        {
            try
            {
                await _parentWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (_gameListBox != null)
                    {
                        ListBoxItem itemToRemove = null;
                        foreach (ListBoxItem listItem in _gameListBox.Items)
                        {
                            if (listItem.Tag?.ToString().Contains(itemId) == true)
                            {
                                itemToRemove = listItem;
                                break;
                            }
                        }

                        if (itemToRemove != null)
                        {
                            // Lambda fade out animasyonu
                            var fadeOut = new DoubleAnimation
                            {
                                From = 1.0,
                                To = 0.0,
                                Duration = TimeSpan.FromMilliseconds(500)
                            };

                            fadeOut.Completed += (s, e) =>
                            {
                                _gameListBox.Items.Remove(itemToRemove);
                            };

                            itemToRemove.BeginAnimation(ListBoxItem.OpacityProperty, fadeOut);
                        }
                    }

                    var item = _activeGameInstallations.FirstOrDefault(x => x.Id == itemId);
                    if (item != null)
                    {
                        _activeGameInstallations.Remove(item);
                    }

                    UpdateQueueCounter();
                });
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ RemoveGameFromQueue hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tüm yüklemeleri iptal eder - Lambda efekti ile
        /// </summary>
        public async Task CancelAllInstallations()
        {
            try
            {
                LogMessage?.Invoke("🛑 Lambda kurulum sistemi: Tüm işlemler iptal ediliyor...");

                foreach (var item in _activeGameInstallations.ToList())
                {
                    item.Status = GameInstallationStatus.Cancelled;
                    await RemoveGameFromQueue(item.Id);
                }

                // Genel temizlik animasyonu
                await _parentWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (_gameListBox != null)
                    {
                        var clearAnimation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.0,
                            Duration = TimeSpan.FromMilliseconds(800)
                        };

                        clearAnimation.Completed += (s, e) =>
                        {
                            _gameListBox.Items.Clear();
                            _gameListBox.Opacity = 1.0;
                            UpdateQueueCounter();
                        };

                        _gameListBox.BeginAnimation(ListBox.OpacityProperty, clearAnimation);
                    }
                });

                LogMessage?.Invoke("✅ Lambda kurulum sistemi temizlendi");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CancelAllInstallations hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktif yükleme sayısını döndürür
        /// </summary>
        public int GetActiveInstallationCount()
        {
            return _activeGameInstallations.Count;
        }

        /// <summary>
        /// Lambda panelinin görünürlüğünü değiştirir
        /// </summary>
        public void SetPanelVisibility(bool isVisible)
        {
            try
            {
                if (_gameInstallationQueuePanel != null)
                {
                    _gameInstallationQueuePanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                    if (isVisible)
                    {
                        StartLambdaAnimations();
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Oyun yükleme item'ı
        /// </summary>
        public class GameInstallationItem
        {
            public string Id { get; set; } = string.Empty;
            public Yafes.Models.GameData GameData { get; set; }
            public GameInstallationStatus Status { get; set; }
            public int Progress { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
        }

        /// <summary>
        /// Oyun yükleme durumları
        /// </summary>
        public enum GameInstallationStatus
        {
            Queued,
            Installing,
            Completed,
            Failed,
            Cancelled
        }
    }
}