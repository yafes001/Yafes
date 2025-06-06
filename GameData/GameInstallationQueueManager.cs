// GameInstallationQueueManager.cs - YENİ SINIF
// Oyun yükleme kuyruğunu yönetir

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Yafes.Managers
{
    /// <summary>
    /// Oyun yükleme ve kurulum kuyruğunu yöneten sınıf
    /// </summary>
    public class GameInstallationQueueManager
    {
        private readonly StackPanel _gameInstallationsPanel;
        private readonly TextBlock _noGameInstallationsText;
        private readonly TextBox _logTextBox;
        private readonly Window _parentWindow;

        // Aktif oyun yüklemeleri
        private readonly List<GameInstallationItem> _activeGameInstallations;

        // Events
        public event Action<string> LogMessage;

        public GameInstallationQueueManager(StackPanel gameInstallationsPanel, TextBlock noGameInstallationsText, TextBox logTextBox, Window parentWindow = null)
        {
            _gameInstallationsPanel = gameInstallationsPanel ?? throw new ArgumentNullException(nameof(gameInstallationsPanel));
            _noGameInstallationsText = noGameInstallationsText ?? throw new ArgumentNullException(nameof(noGameInstallationsText));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));
            _parentWindow = parentWindow;

            _activeGameInstallations = new List<GameInstallationItem>();

            LogMessage += (message) => {
                _logTextBox?.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };
        }

        /// <summary>
        /// Oyun yükleme kuyruğu panelini oluşturur
        /// </summary>
        public Border CreateGameInstallationQueuePanel()
        {
            try
            {
                if (_parentWindow == null) return null;

                var mainCanvas = UIHelperManager.FindElementByName<Canvas>(_parentWindow, "MainCanvas");
                if (mainCanvas == null)
                {
                    return null;
                }

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

                if (rightSidebar == null)
                {
                    return null;
                }

                var sidebarCanvas = rightSidebar.Child as Canvas;
                if (sidebarCanvas == null)
                {
                    return null;
                }

                var gameQueuePanel = new Border
                {
                    Width = 170,
                    Height = 404,
                    Background = new SolidColorBrush(Color.FromArgb(26, 255, 165, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(77, 255, 165, 0)),
                    BorderThickness = new Thickness(1),
                    Visibility = Visibility.Collapsed,
                    Name = "GameInstallationQueuePanel"
                };

                Canvas.SetTop(gameQueuePanel, 168);

                var transform = new TranslateTransform();
                transform.X = 220;
                gameQueuePanel.RenderTransform = transform;

                var contentCanvas = new Canvas
                {
                    Margin = new Thickness(8)
                };

                var titleText = new TextBlock
                {
                    Text = "🎮 OYUN KURULUM KUYRUĞU",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.6
                    }
                };
                Canvas.SetLeft(titleText, 0);
                Canvas.SetTop(titleText, 0);

                var gameListBox = new ListBox
                {
                    Width = 154,
                    Height = 376,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                    FontFamily = new FontFamily("Trebuchet MS"),
                    FontSize = 8,
                    FontWeight = FontWeights.Normal,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Name = "GameInstallationListBox"
                };

                Canvas.SetLeft(gameListBox, 0);
                Canvas.SetTop(gameListBox, 20);

                // Başlangıçta örnek oyun kurulum öğeleri oluştur
                CreateSampleGameInstallationItems(gameListBox);

                contentCanvas.Children.Add(titleText);
                contentCanvas.Children.Add(gameListBox);
                gameQueuePanel.Child = contentCanvas;

                Panel.SetZIndex(gameQueuePanel, 1000);
                sidebarCanvas.Children.Add(gameQueuePanel);

                return gameQueuePanel;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateGameInstallationQueuePanel hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Örnek oyun kurulum öğelerini oluşturur
        /// </summary>
        private void CreateSampleGameInstallationItems(ListBox gameListBox)
        {
            try
            {
                // 🎮 NUMARALANDIRILMIŞ OYUN KURULUM ÖĞELERİ - TEMİZ TASARIM
                var gameInstallItems = new[]
                {
                    new { Name = "Cyberpunk 2077", Status = "Installing", Progress = 45, Color = "#00F5FF" },
                    new { Name = "The Witcher 3", Status = "Queued", Progress = 0, Color = "#FF6B35" },
                    new { Name = "Red Dead Redemption 2", Status = "Downloading", Progress = 78, Color = "#9D4EDD" },
                    new { Name = "Grand Theft Auto V", Status = "Queued", Progress = 0, Color = "#FFD60A" }
                };

                for (int index = 0; index < gameInstallItems.Length; index++)
                {
                    var game = gameInstallItems[index];
                    int gameNumber = index + 1;

                    var itemContainer = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(game.Color)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(2, 1, 2, 2),
                        Height = 32,
                        Width = 146,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = (Color)ColorConverter.ConvertFromString(game.Color),
                            BlurRadius = 6,
                            ShadowDepth = 0,
                            Opacity = 0.4
                        }
                    };

                    var contentGrid = new Grid();
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });

                    var numberContainer = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(game.Color)),
                        Width = 16,
                        Height = 16,
                        CornerRadius = new CornerRadius(8),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = (Color)ColorConverter.ConvertFromString(game.Color),
                            BlurRadius = 4,
                            ShadowDepth = 0,
                            Opacity = 0.8
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

                    var nameText = new TextBlock
                    {
                        Text = game.Name,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(game.Color)),
                        FontFamily = new FontFamily("Trebuchet MS"),
                        FontSize = 7,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(4, 1, 2, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(nameText, 1);
                    Grid.SetRow(nameText, 0);

                    var progressContainer = new Grid();
                    progressContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    progressContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                    Grid.SetColumn(progressContainer, 1);
                    Grid.SetRow(progressContainer, 1);

                    var progressBackground = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100)),
                        BorderThickness = new Thickness(0.5),
                        Height = 6,
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(4, 1, 2, 1)
                    };

                    var progressFill = new Border
                    {
                        Background = new LinearGradientBrush
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(1, 0),
                            GradientStops = new GradientStopCollection
                            {
                                new GradientStop((Color)ColorConverter.ConvertFromString(game.Color), 0),
                                new GradientStop(Color.FromArgb(200, ((Color)ColorConverter.ConvertFromString(game.Color)).R,
                                                               ((Color)ColorConverter.ConvertFromString(game.Color)).G,
                                                               ((Color)ColorConverter.ConvertFromString(game.Color)).B), 1)
                            }
                        },
                        Height = 6,
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(4, 1, 2, 1),
                        Width = (game.Progress / 100.0) * 105,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = (Color)ColorConverter.ConvertFromString(game.Color),
                            BlurRadius = 4,
                            ShadowDepth = 0,
                            Opacity = 0.6
                        }
                    };

                    var progressText = new TextBlock
                    {
                        Text = game.Status == "Queued" ? "⏳" : $"{game.Progress}%",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(game.Color)),
                        FontFamily = new FontFamily("Trebuchet MS"),
                        FontSize = 6,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0)
                    };
                    Grid.SetColumn(progressText, 1);

                    var progressGrid = new Grid();
                    progressGrid.Children.Add(progressBackground);
                    progressGrid.Children.Add(progressFill);
                    Grid.SetColumn(progressGrid, 0);
                    progressContainer.Children.Add(progressGrid);
                    progressContainer.Children.Add(progressText);

                    if (game.Status == "Installing")
                    {
                        var glowAnimation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.4,
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(1000),
                            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                            AutoReverse = true
                        };
                        progressFill.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, glowAnimation);

                        var numberGlowAnimation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.6,
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(800),
                            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                            AutoReverse = true
                        };
                        numberContainer.Effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, numberGlowAnimation);
                    }

                    contentGrid.Children.Add(numberContainer);
                    contentGrid.Children.Add(nameText);
                    contentGrid.Children.Add(progressContainer);

                    itemContainer.Child = contentGrid;

                    var listItem = new ListBoxItem
                    {
                        Content = itemContainer,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0, 1, 0, 1),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };

                    gameListBox.Items.Add(listItem);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ CreateSampleGameInstallationItems hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyun yükleme kuyruğuna yeni oyun ekler
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
                    Id = Guid.NewGuid().ToString()
                };

                // Kuyruga ekle
                _activeGameInstallations.Add(installationItem);

                // UI'ya ekle
                var installationControl = CreateGameInstallationControl(installationItem);

                await _gameInstallationsPanel.Dispatcher.InvokeAsync(() =>
                {
                    _gameInstallationsPanel.Children.Add(installationControl);
                    UpdateVisibility();
                });

                LogMessage?.Invoke($"🎮 {game.Name} oyun yükleme kuyruğuna eklendi");

                // Otomatik yüklemeyi başlat
                await StartNextInstallationIfPossible();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ AddGameToInstallationQueue hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyun yükleme kontrolü oluşturur
        /// </summary>
        private Border CreateGameInstallationControl(GameInstallationItem item)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 165, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 165, 0)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(3),
                Tag = item.Id,
                Height = 45
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Oyun adı ve durum
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 2)
            };

            var gameNameText = new TextBlock
            {
                Text = item.GameData.Name,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                MaxWidth = 100,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var statusText = new TextBlock
            {
                Text = GetStatusText(item.Status),
                FontSize = 8,
                Foreground = GetStatusColor(item.Status),
                Margin = new Thickness(5, 0, 0, 0),
                Name = "StatusText"
            };

            headerPanel.Children.Add(gameNameText);
            headerPanel.Children.Add(statusText);

            // Progress bar
            var progressBar = new ProgressBar
            {
                Height = 8,
                Value = item.Progress,
                Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                BorderThickness = new Thickness(0),
                Name = "ProgressBar"
            };

            Grid.SetRow(headerPanel, 0);
            Grid.SetRow(progressBar, 1);

            mainGrid.Children.Add(headerPanel);
            mainGrid.Children.Add(progressBar);
            container.Child = mainGrid;

            return container;
        }

        /// <summary>
        /// Bir sonraki yüklemeyi başlatır
        /// </summary>
        private async Task StartNextInstallationIfPossible()
        {
            try
            {
                // Şu anda yüklenen oyun var mı?
                var currentInstalling = _activeGameInstallations.FirstOrDefault(x => x.Status == GameInstallationStatus.Installing);
                if (currentInstalling != null) return; // Zaten bir yükleme var

                // Kuyrukta bekleyen oyun var mı?
                var nextInQueue = _activeGameInstallations.FirstOrDefault(x => x.Status == GameInstallationStatus.Queued);
                if (nextInQueue == null) return; // Kuyrukta bekleyen yok

                // Yüklemeyi başlat
                nextInQueue.Status = GameInstallationStatus.Installing;
                UpdateGameInstallationUI(nextInQueue);

                LogMessage?.Invoke($"🚀 {nextInQueue.GameData.Name} yüklemesi başlatılıyor...");

                // Simüle edilmiş yükleme işlemi
                await SimulateGameInstallation(nextInQueue);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartNextInstallationIfPossible hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyun yükleme simülasyonu
        /// </summary>
        private async Task SimulateGameInstallation(GameInstallationItem item)
        {
            try
            {
                // 10 saniyede %100'e çıkar
                for (int progress = 0; progress <= 100; progress += 5)
                {
                    item.Progress = progress;
                    UpdateGameInstallationUI(item);

                    if (progress == 100)
                    {
                        item.Status = GameInstallationStatus.Completed;
                        LogMessage?.Invoke($"✅ {item.GameData.Name} yüklemesi tamamlandı!");
                    }

                    await Task.Delay(500); // 500ms bekle
                }

                // Tamamlanan oyunu kuyruktdan kaldır (2 saniye sonra)
                await Task.Delay(2000);
                await RemoveGameFromQueue(item.Id);

                // Bir sonraki yüklemeyi başlat
                await StartNextInstallationIfPossible();
            }
            catch (Exception ex)
            {
                item.Status = GameInstallationStatus.Failed;
                UpdateGameInstallationUI(item);
                LogMessage?.Invoke($"❌ {item.GameData.Name} yüklemesi başarısız: {ex.Message}");
            }
        }

        /// <summary>
        /// UI'daki oyun yükleme kontrolünü günceller
        /// </summary>
        private void UpdateGameInstallationUI(GameInstallationItem item)
        {
            _gameInstallationsPanel.Dispatcher.Invoke(() =>
            {
                var control = _gameInstallationsPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(x => x.Tag?.ToString() == item.Id);

                if (control?.Child is Grid grid)
                {
                    // Status text güncelle
                    var statusText = grid.Children
                        .OfType<StackPanel>()
                        .FirstOrDefault()?.Children
                        .OfType<TextBlock>()
                        .FirstOrDefault(x => x.Name == "StatusText");

                    if (statusText != null)
                    {
                        statusText.Text = GetStatusText(item.Status);
                        statusText.Foreground = GetStatusColor(item.Status);
                    }

                    // Progress bar güncelle
                    var progressBar = grid.Children
                        .OfType<ProgressBar>()
                        .FirstOrDefault(x => x.Name == "ProgressBar");

                    if (progressBar != null)
                    {
                        progressBar.Value = item.Progress;
                    }
                }
            });
        }

        /// <summary>
        /// Oyunu kuyruktdan kaldırır
        /// </summary>
        private async Task RemoveGameFromQueue(string itemId)
        {
            try
            {
                await _gameInstallationsPanel.Dispatcher.InvokeAsync(() =>
                {
                    // UI'dan kaldır
                    var control = _gameInstallationsPanel.Children
                        .OfType<Border>()
                        .FirstOrDefault(x => x.Tag?.ToString() == itemId);

                    if (control != null)
                    {
                        _gameInstallationsPanel.Children.Remove(control);
                    }

                    // Listeden kaldır
                    var item = _activeGameInstallations.FirstOrDefault(x => x.Id == itemId);
                    if (item != null)
                    {
                        _activeGameInstallations.Remove(item);
                    }

                    UpdateVisibility();
                });
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ RemoveGameFromQueue hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Görünürlük durumunu günceller
        /// </summary>
        private void UpdateVisibility()
        {
            bool hasActiveInstallations = _gameInstallationsPanel.Children.Count > 1; // 1 = default text

            _noGameInstallationsText.Visibility = hasActiveInstallations ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Durum metnini döndürür
        /// </summary>
        private string GetStatusText(GameInstallationStatus status)
        {
            return status switch
            {
                GameInstallationStatus.Queued => "Sırada",
                GameInstallationStatus.Installing => "Yükleniyor",
                GameInstallationStatus.Completed => "Tamamlandı",
                GameInstallationStatus.Failed => "Başarısız",
                GameInstallationStatus.Cancelled => "İptal",
                _ => "Bilinmiyor"
            };
        }

        /// <summary>
        /// Durum rengini döndürür
        /// </summary>
        private SolidColorBrush GetStatusColor(GameInstallationStatus status)
        {
            return status switch
            {
                GameInstallationStatus.Queued => new SolidColorBrush(Color.FromRgb(255, 165, 0)),     // Turuncu
                GameInstallationStatus.Installing => new SolidColorBrush(Color.FromRgb(0, 245, 255)), // Cyan
                GameInstallationStatus.Completed => new SolidColorBrush(Color.FromRgb(0, 255, 0)),    // Yeşil
                GameInstallationStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 0, 0)),       // Kırmızı
                GameInstallationStatus.Cancelled => new SolidColorBrush(Color.FromRgb(128, 128, 128)), // Gri
                _ => new SolidColorBrush(Color.FromRgb(255, 255, 255))
            };
        }

        /// <summary>
        /// Tüm yüklemeleri iptal eder
        /// </summary>
        public async Task CancelAllInstallations()
        {
            try
            {
                LogMessage?.Invoke("🛑 Tüm oyun yüklemeleri iptal ediliyor...");

                foreach (var item in _activeGameInstallations.ToList())
                {
                    item.Status = GameInstallationStatus.Cancelled;
                    await RemoveGameFromQueue(item.Id);
                }

                LogMessage?.Invoke("✅ Tüm oyun yüklemeleri iptal edildi");
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
    }

    /// <summary>
    /// Oyun yükleme öğesi
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
        Queued,      // Sırada
        Installing,  // Yükleniyor
        Completed,   // Tamamlandı
        Failed,      // Başarısız
        Cancelled    // İptal edildi
    }
}