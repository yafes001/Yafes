using System;
using System.Collections.Generic;
using System.IO;
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
    /// ENHANCED Games Panel yönetimi - Slide Animation eklendi
    /// Mevcut tüm özellikler korunmuş + Sol sidebar slide animasyonu
    /// </summary>
    public class GamesPanelManager
    {
        private readonly Window _parentWindow;
        private readonly TextBox _logTextBox;
        private bool _isGamesVisible = false;

        // ✅ YENİ: Slide Animation için gerekli referanslar
        private Border _leftSidebar;
        private TranslateTransform _leftSidebarTransform;
        private const double SIDEBAR_SLIDE_DISTANCE = -280; // Sol sidebar'ın kayacağı mesafe
        private const double ANIMATION_DURATION = 600; // Animasyon süresi (millisecond)

        // Events - MEVCUT
        public event Action<string> LogMessage;

        public GamesPanelManager(Window parentWindow, TextBox logTextBox)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));

            // MEVCUT Event subscription
            LogMessage += (message) => {
                _logTextBox.Dispatcher.Invoke(() => {
                    _logTextBox.AppendText(message + "\n");
                    _logTextBox.ScrollToEnd();
                });
            };

            // ✅ YENİ: Sidebar referanslarını başlat
            InitializeSidebarElements();
        }

        public bool IsGamesVisible => _isGamesVisible;

        /// <summary>
        /// ✅ YENİ: Sol sidebar için element referanslarını başlatır
        /// 