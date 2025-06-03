using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Yafes.GameData
{
    /// <summary>
    /// Oyun paneli yönetim sınıfı - Games butonuna tıklandığında animasyonlu panel geçişlerini sağlar
    /// </summary>
    public class GamesManager
    {
        private readonly Main _mainWindow;
        private readonly Border _terminalPanel;
        private readonly Border _gamesPanel;
        private readonly TranslateTransform _terminalTransform;
        private readonly TranslateTransform _gamesPanelTransform;

        /// <summary>
        /// GamesManager constructor - Main window ve XAML elementlerine referans alır
        /// </summary>
        /// <param name="mainWindow">Ana pencere referansı</param>
        public GamesManager(Main mainWindow)
        {
            _mainWindow = mainWindow;
            // _mainWindow.AddLog("🎮 GamesManager başlatılıyor..."); ← KALDIR

            // XAML elementlerini Tag ile bul - SESSIZCE
            _terminalPanel = FindElementByTag<Border>(_mainWindow, "TerminalPanel");
            _gamesPanel = FindElementByTag<Border>(_mainWindow, "GamesPanel");

            // Sadece kritik hatayı logla
            if (_terminalPanel == null || _gamesPanel == null)
            {
                _mainWindow.AddLog("❌ KRITIK: Oyun panelleri bulunamadı!");
                return;
            }

            // Transform'ları al
            _terminalTransform = _terminalPanel.RenderTransform as TranslateTransform;
            _gamesPanelTransform = _gamesPanel.RenderTransform as TranslateTransform;

            if (_terminalTransform == null || _gamesPanelTransform == null)
            {
                _mainWindow.AddLog("❌ KRITIK: Panel animasyonları desteklenmiyor!");
                return;
            }

            InitializeGameCards();
            // _mainWindow.AddLog("✅ GamesManager başarıyla başlatıldı"); ← KALDIR
        }

        /// <summary>
        /// Oyun kartlarına hover efektleri ve tıklama olayları ekler
        /// </summary>
        private void InitializeGameCards()
        {
            try
            {
                var gamesGrid = FindChild<UniformGrid>(_gamesPanel, "gamesGrid");
                if (gamesGrid != null)
                {
                    foreach (Border gameCard in gamesGrid.Children)
                    {
                        gameCard.MouseEnter += GameCard_MouseEnter;
                        gameCard.MouseLeave += GameCard_MouseLeave;
                        gameCard.MouseLeftButtonDown += GameCard_Click;
                        gameCard.Cursor = Cursors.Hand;
                    }
                    // _mainWindow.AddLog($"🎮 {gamesGrid.Children.Count} oyun kartı etkinleştirildi"); ← KALDIR
                }
                // else { _mainWindow.AddLog("⚠️ Games grid bulunamadı..."); } ← KALDIR
            }
            catch (Exception ex)
            {
                // Sadece kritik hataları logla
                _mainWindow.AddLog($"❌ Oyun sistemi hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Games Panel gösterme animasyonu - Log'u aşağıya kaydırır ve games panelini gösterir
        /// </summary>
        /// <summary>
        /// Games Panel gösterme animasyonu - Log'u aşağıya kaydırır ve games panelini gösterir
        /// </summary>
        public void ShowGamesPanel()
        {
            try
            {
                // NULL KONTROLÜ
                if (_terminalPanel == null || _gamesPanel == null)
                {
                    _mainWindow.AddLog("❌ Panel elementleri bulunamadı! XAML Tag'lerini kontrol edin.");
                    return;
                }

                if (_terminalTransform == null || _gamesPanelTransform == null)
                {
                    //_mainWindow.AddLog("❌ Panel transform'ları null! RenderTransform tanımlı mı?");
                    return;
                }

                _mainWindow.AddLog("🎮 Games paneli açılıyor...");

                // 1. Games Panel'i görünür yap (animasyon öncesi)
                _gamesPanel.Visibility = Visibility.Visible;
                _gamesPanel.Opacity = 0; // Başlangıçta şeffaf

                // 2. TERMINAL PANELİ AŞAĞI KAYDIRMA ANİMASYONU
                var terminalMoveAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 306, // Games panel yüksekliği + margin (290 + 16)
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // 3. TERMINAL PANELİ YÜKSEKLİK KÜÇÜLTME ANİMASYONU
                var terminalResizeAnimation = new DoubleAnimation
                {
                    From = 596,
                    To = 290, // Terminal panel'in yeni yüksekliği (games için yer aç)
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // 4. GAMES PANELİ YUKARI ÇIKMA ANİMASYONU
                var gamesPanelShowAnimation = new DoubleAnimation
                {
                    From = -50,
                    To = 0, // Normal pozisyona getir
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // 5. GAMES PANELİ OPACITY ANİMASYONU
                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1, // Tam görünür yap
                    Duration = TimeSpan.FromMilliseconds(400),
                    BeginTime = TimeSpan.FromMilliseconds(200) // 200ms gecikme ile başla
                };

                // 6. ANİMASYONLARI BAŞLAT
                _terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                _terminalPanel.BeginAnimation(FrameworkElement.HeightProperty, terminalResizeAnimation);
                _gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                _gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // 7. LOG MESAJI
               // _mainWindow.AddLog("✅ Games kataloğu açıldı - Oyunları inceleyebilirsiniz!");
              //  _mainWindow.AddLog("💡 Başka bir kategori seçerek normal görünüme dönebilirsiniz");
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Games paneli açma hatası: {ex.Message}");
                _mainWindow.AddLog($"   Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Games Panel gizleme animasyonu - Normal log görünümüne döndürür
        /// </summary>
        public void HideGamesPanel()
        {
            try
            {
                // Eğer Games panel zaten gizliyse hiçbir şey yapma
                if (_gamesPanel.Visibility == Visibility.Collapsed)
                    return;

                // 1. Animasyonları oluştur
                var terminalMoveAnimation = new DoubleAnimation
                {
                    To = 0, // Terminal panel'i normal pozisyona getir
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                var terminalResizeAnimation = new DoubleAnimation
                {
                    To = 596, // Terminal panel'in yüksekliğini normale döndür
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                var gamesPanelHideAnimation = new DoubleAnimation
                {
                    To = -50, // Games panel'i yukarıya taşı
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    To = 0, // Games panel'i şeffaf yap
                    Duration = TimeSpan.FromMilliseconds(500)
                };

                // 2. Animasyon tamamlandığında games panel'i gizle
                gamesPanelOpacityAnimation.Completed += (s, e) =>
                {
                    _gamesPanel.Visibility = Visibility.Collapsed;
                };

                // 3. Animasyonları başlat
                _terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                _terminalPanel.BeginAnimation(FrameworkElement.HeightProperty, terminalResizeAnimation);
                _gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelHideAnimation);
                _gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // 4. Log mesajı ekle
                _mainWindow.AddLog("📦 Normal görünüm geri yüklendi");
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Games paneli kapatma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Game Card üzerine mouse geldiğinde hover efekti
        /// </summary>
        private void GameCard_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Border card = sender as Border;
                if (card != null)
                {
                    // Hover animasyonu - kartı büyüt
                    var scaleTransform = new ScaleTransform(1, 1);
                    card.RenderTransform = scaleTransform;
                    card.RenderTransformOrigin = new Point(0.5, 0.5);

                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.05,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

                    // Border renk efekti - altın renge çevir
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // #FFD700
                    card.Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 215, 0),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Hover efekti hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Game Card'dan mouse ayrıldığında normal duruma döndür
        /// </summary>
        private void GameCard_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                Border card = sender as Border;
                if (card != null)
                {
                    // Normal duruma dön animasyonu
                    var scaleTransform = card.RenderTransform as ScaleTransform;
                    if (scaleTransform != null)
                    {
                        var scaleAnimation = new DoubleAnimation
                        {
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(200),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };

                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                    }

                    // Border'ı normale döndür - turuncu renge çevir
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // #FFA500
                    card.Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 165, 0),
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.4
                    };
                }
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Hover çıkış efekti hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Game Card tıklama olayı - Oyunu kurulum kuyruğuna ekler
        /// </summary>
        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Border card = sender as Border;
                if (card != null)
                {
                    // Oyun adını bul
                    var stackPanel = card.Child as StackPanel;
                    if (stackPanel != null && stackPanel.Children.Count >= 2)
                    {
                        var gameNameTextBlock = stackPanel.Children[1] as TextBlock;
                        if (gameNameTextBlock != null)
                        {
                            string gameName = gameNameTextBlock.Text;

                            // Oyunu kurulum kuyruğuna ekle
                            AddGameToQueue(gameName);

                            // Log'a ekle
                            _mainWindow.AddLog($"🎯 {gameName} kurulum kuyruğuna eklendi!");
                            _mainWindow.AddLog($"💾 Boyut: {GetGameSize(gameName)} - İndirilmeye hazır");

                            // Kısa titreşim efekti
                            CreateShakeEffect(card);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Oyun tıklama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kart tıklandığında titreşim efekti oluşturur
        /// </summary>
        private void CreateShakeEffect(Border card)
        {
            try
            {
                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                var translateTransform = new TranslateTransform();
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(translateTransform);

                // Eğer mevcut transform varsa onu da ekle
                if (card.RenderTransform != null && card.RenderTransform != Transform.Identity)
                {
                    transformGroup.Children.Add(card.RenderTransform);
                }

                card.RenderTransform = transformGroup;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Titreşim efekti hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Oyunu kurulum kuyruğuna ekler
        /// </summary>
        private void AddGameToQueue(string gameName)
        {
            try
            {
                // ListBox'ı Tag ile bul
                var lstDrivers = FindElementByTag<ListBox>(_mainWindow, "MainDriversList");

                if (lstDrivers == null)
                {
                    _mainWindow.AddLog("❌ Kurulum kuyruğu listesi bulunamadı!");
                    return;
                }

                // Oyunun zaten kuyrukta olup olmadığını kontrol et
                bool alreadyInQueue = false;
                foreach (var item in lstDrivers.Items)
                {
                    if (item.ToString().Contains(gameName))
                    {
                        alreadyInQueue = true;
                        break;
                    }
                }

                if (alreadyInQueue)
                {
                    _mainWindow.AddLog($"⚠️ {gameName} zaten kurulum kuyruğunda!");
                    return;
                }

                // lstDrivers listesine ekle (icon + isim formatında)
                string queueItem = $"{GetGameIcon(gameName)} {gameName}";
                lstDrivers.Items.Add(queueItem);

                _mainWindow.AddLog($"✅ Kuyruk güncellendi - Toplam {lstDrivers.Items.Count} öğe");
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Kuyruğa ekleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tag'e göre element bulur
        /// </summary>
        private T FindElementByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            if (parent == null) return null;
            return FindElementByTagRecursive<T>(parent, tag);
        }
        private T FindElementByTagRecursive<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Tag?.ToString() == tag)
                {
                    // _mainWindow.AddLog($"✅ '{tag}' Tag'i bulundu!"); ← KALDIR
                    return element;
                }

                var result = FindElementByTagRecursive<T>(child, tag);
                if (result != null) return result;
            }

            return null;
        }
        /// <summary>
        /// Oyun adına göre ikonu döndürür
        /// </summary>
        private string GetGameIcon(string gameName)
        {
            var icons = new Dictionary<string, string>
            {
                { "Steam", "🎯" },
                { "Epic Games", "🎮" },
                { "GOG Galaxy", "🎲" },
                { "Origin", "⚡" },
                { "Battle.net", "🚀" },
                { "Ubisoft Connect", "🎪" },
                { "Rockstar", "🎭" },
                { "Xbox App", "⭐" }
            };

            return icons.ContainsKey(gameName) ? icons[gameName] : "🎮";
        }

        /// <summary>
        /// Oyun adına göre dosya boyutunu döndürür
        /// </summary>
        private string GetGameSize(string gameName)
        {
            var sizes = new Dictionary<string, string>
            {
                { "Steam", "150 MB" },
                { "Epic Games", "200 MB" },
                { "GOG Galaxy", "80 MB" },
                { "Origin", "120 MB" },
                { "Battle.net", "90 MB" },
                { "Ubisoft Connect", "110 MB" },
                { "Rockstar", "85 MB" },
                { "Xbox App", "95 MB" }
            };

            return sizes.ContainsKey(gameName) ? sizes[gameName] : "100 MB";
        }

        /// <summary>
        /// XAML elementini ismiyle arar ve döndürür
        /// </summary>
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;

                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// Tüm oyun kartlarını yeniden başlatır (gerekirse)
        /// </summary>
        public void RefreshGameCards()
        {
            try
            {
                InitializeGameCards();
                _mainWindow.AddLog("🔄 Oyun kartları yenilendi");
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog($"❌ Oyun kartları yenileme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Games Manager durumunu döndürür
        /// </summary>
        public bool IsGamesPanelVisible => _gamesPanel.Visibility == Visibility.Visible;
    }
}