using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Yafes.Managers
{
    /// <summary>
    /// Games Panel yönetimi için özel class
    /// Main.xaml.cs'den games logic'ini ayırır
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // Events
        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            // Event subscription
            LogMessage += (message) => {
                _logTextBox.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// Games panel toggle işlemi
        /// </summary>
        public async Task<bool> ToggleGamesPanel()
        {
            try
            {
                LogMessage?.Invoke($"🎮 Games butonu tıklandı - Mevcut durum: {(_isGamesVisible ? "AÇIK" : "KAPALI")}");

                if (!_isGamesVisible)
                {
                    LogMessage?.Invoke("🔛 Games panel açılıyor...");
                    bool success = await ShowGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = true;
                        LogMessage?.Invoke("✅ Games panel başarıyla açıldı");
                    }
                    return success;
                }
                else
                {
                    LogMessage?.Invoke("🔴 Games panel kapatılıyor...");
                    bool success = HideGamesPanel();
                    if (success)
                    {
                        _isGamesVisible = false;
                        LogMessage?.Invoke("✅ Games panel başarıyla kapatıldı");
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ToggleGamesPanel hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Games panelini gösterir
        /// </summary>
        private async Task<bool> ShowGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🎮 BAŞLAMA: ShowGamesPanel çalışıyor...");

                // 1. Panel'leri bul
                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null)
                {
                    LogMessage?.Invoke("❌ HATA: GamesPanel bulunamadı! Tag='GamesPanel' kontrolü");
                    return false;
                }

                if (terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ HATA: TerminalPanel bulunamadı! Tag='TerminalPanel' kontrolü");
                    return false;
                }

                LogMessage?.Invoke("✅ Panel'ler bulundu");

                // 2. Games Panel'i görünür yap
                gamesPanel.Visibility = Visibility.Visible;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Visible");

                // 3. Animasyonları başlat
                await StartShowAnimations(gamesPanel, terminalPanel);

                // 4. Kategori listesini gizle
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Collapsed;
                    LogMessage?.Invoke("✅ Kategori listesi gizlendi");
                }

                // 5. Oyun verilerini yükle
                LogMessage?.Invoke("📊 Oyun verileri yükleniyor...");
                await LoadGamesIntoPanel(gamesPanel);

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen açıldı ve yüklendi!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ShowGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Games panelini gizler
        /// </summary>
        private bool HideGamesPanel()
        {
            try
            {
                LogMessage?.Invoke("🔴 BAŞLAMA: HideGamesPanel çalışıyor...");

                var gamesPanel = FindElementByTag<Border>(_parentWindow, "GamesPanel");
                var terminalPanel = FindElementByTag<Border>(_parentWindow, "TerminalPanel");

                if (gamesPanel == null || terminalPanel == null)
                {
                    LogMessage?.Invoke("❌ Panel'ler bulunamadı, gizleme iptal");
                    return false;
                }

                // Games panel'i gizle
                gamesPanel.Visibility = Visibility.Collapsed;
                LogMessage?.Invoke("✅ GamesPanel.Visibility = Collapsed");

                // Terminal'i normale döndür
                StartHideAnimations(terminalPanel);

                // Kategori listesini geri göster
                var lstDrivers = FindElementByName<ListBox>(_parentWindow, "lstDrivers");
                if (lstDrivers != null)
                {
                    lstDrivers.Visibility = Visibility.Visible;
                    LogMessage?.Invoke("✅ Kategori listesi geri gösterildi");
                }

                LogMessage?.Invoke("✅ BİTİŞ: Games panel tamamen gizlendi");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ HideGamesPanel HATA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Panel gösterme animasyonları
        /// </summary>
        private async Task StartShowAnimations(Border gamesPanel, Border terminalPanel)
        {
            try
            {
                // Terminal animasyonu
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform == null)
                {
                    terminalTransform = new TranslateTransform();
                    terminalPanel.RenderTransform = terminalTransform;
                }

                var terminalMoveAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 306,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Games Panel animasyonu
                var gamesPanelTransform = gamesPanel.RenderTransform as TranslateTransform;
                if (gamesPanelTransform == null)
                {
                    gamesPanelTransform = new TranslateTransform();
                    gamesPanel.RenderTransform = gamesPanelTransform;
                }

                var gamesPanelShowAnimation = new DoubleAnimation
                {
                    From = -50,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Opacity animasyonu
                var gamesPanelOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };

                LogMessage?.Invoke("🎬 Animasyonlar başlatılıyor...");

                // Animasyonları başlat
                terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                gamesPanelTransform.BeginAnimation(TranslateTransform.YProperty, gamesPanelShowAnimation);
                gamesPanel.BeginAnimation(UIElement.OpacityProperty, gamesPanelOpacityAnimation);

                // Animasyon tamamlanana kadar bekle
                await Task.Delay(700);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ StartShowAnimations hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Panel gizleme animasyonları
        /// </summary>
        private void StartHideAnimations(Border terminalPanel)
        {
            try
            {
                var terminalTransform = terminalPanel.RenderTransform as TranslateTransform;
                if (terminalTransform != null)
                {
                    var terminalMoveAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    terminalTransform.BeginAnimation(TranslateTransform.YProperty, terminalMoveAnimation);
                    Log