// AnimationManager.cs - TAM SINIF (Oyun Kuyruğu ile Güncellenmiş)

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Yafes.Managers
{
    public class AnimationManager
    {
        private readonly Window _parentWindow;
        private readonly Border _leftSidebar;
        private readonly TranslateTransform _leftSidebarTransform;
        private readonly Border _progressBarContainer;
        private readonly TranslateTransform _progressBarTransform;

        // Kurulum kuyruğu
        private readonly Border _installationQueue;
        private readonly TranslateTransform _installationQueueTransform;

        // YENİ: Oyun yükleme kuyruğu
        private readonly Border _gameInstallationQueue;
        private readonly TranslateTransform _gameInstallationQueueTransform;

        private readonly double _originalWindowHeight;
        private readonly double _progressBarHeight;
        private bool _isWindowCompact;

        // Mevcut sabitler
        private const double SIDEBAR_SLIDE_DISTANCE = -280;
        private const double ANIMATION_DURATION = 600;
        private const double PROGRESSBAR_SLIDE_DISTANCE = 800;
        private const double PROGRESSBAR_ANIMATION_DURATION = 800;
        private const double WINDOW_HEIGHT_DURATION = 700;
        private const double INSTALLATION_QUEUE_SLIDE_DISTANCE = 450;
        private const double INSTALLATION_QUEUE_ANIMATION_DURATION = 600;

        // YENİ: Oyun kuyruğu sabitleri - DÜZELTME
        private const double GAME_QUEUE_SLIDE_DISTANCE = 220; // Daha az mesafe
        private const double GAME_QUEUE_ANIMATION_DURATION = 600; // Daha hızlı

        // GÜNCELLENMİŞ CONSTRUCTOR
        public AnimationManager(Window parentWindow, Border leftSidebar, TranslateTransform leftSidebarTransform,
            Border progressBarContainer, TranslateTransform progressBarTransform,
            Border installationQueue, TranslateTransform installationQueueTransform,
            Border gameInstallationQueue, TranslateTransform gameInstallationQueueTransform, // YENİ PARAMETRELER
            double originalWindowHeight, double progressBarHeight)
        {
            _parentWindow = parentWindow;
            _leftSidebar = leftSidebar;
            _leftSidebarTransform = leftSidebarTransform;
            _progressBarContainer = progressBarContainer;
            _progressBarTransform = progressBarTransform;

            // Kurulum kuyruğu
            _installationQueue = installationQueue;
            _installationQueueTransform = installationQueueTransform;

            // YENİ: Oyun kuyruğu
            _gameInstallationQueue = gameInstallationQueue;
            _gameInstallationQueueTransform = gameInstallationQueueTransform;

            _originalWindowHeight = originalWindowHeight;
            _progressBarHeight = progressBarHeight;
            _isWindowCompact = false;
        }

        public bool IsWindowCompact => _isWindowCompact;

        // MEVCUT METODLAR (değişmez)
        public async Task SlideSidebarOut()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = SIDEBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        public async Task SlideSidebarIn()
        {
            try
            {
                if (_leftSidebar == null || _leftSidebarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = SIDEBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _leftSidebarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        public async Task SlideProgressBarOut()
        {
            try
            {
                if (_progressBarContainer == null || _progressBarTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = PROGRESSBAR_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(PROGRESSBAR_ANIMATION_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += async (s, e) => {
                    _progressBarContainer.Visibility = Visibility.Collapsed;
                    await CompactWindowHeight();
                    tcs.SetResult(true);
                };

                _progressBarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        public async Task SlideProgressBarIn()
        {
            try
            {
                if (_progressBarContainer == null || _progressBarTransform == null) return;

                await ExpandWindowHeight();

                _progressBarContainer.Visibility = Visibility.Visible;
                _progressBarTransform.X = PROGRESSBAR_SLIDE_DISTANCE;

                var slideAnimation = new DoubleAnimation
                {
                    From = PROGRESSBAR_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(PROGRESSBAR_ANIMATION_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _progressBarTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        // KURULUM KUYRUĞU METODLARI (mevcut)
        public async Task SlideInstallationQueueDown()
        {
            try
            {
                if (_installationQueue == null || _installationQueueTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = INSTALLATION_QUEUE_SLIDE_DISTANCE,
                    Duration = TimeSpan.FromMilliseconds(INSTALLATION_QUEUE_ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    _installationQueue.Visibility = Visibility.Collapsed;
                    tcs.SetResult(true);
                };

                _installationQueueTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SlideInstallationQueueDown hatası: {ex.Message}");
            }
        }

        public async Task SlideInstallationQueueUp()
        {
            try
            {
                if (_installationQueue == null || _installationQueueTransform == null) return;

                _installationQueue.Visibility = Visibility.Visible;
                _installationQueueTransform.Y = INSTALLATION_QUEUE_SLIDE_DISTANCE;

                var slideAnimation = new DoubleAnimation
                {
                    From = INSTALLATION_QUEUE_SLIDE_DISTANCE,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(INSTALLATION_QUEUE_ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _installationQueueTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SlideInstallationQueueUp hatası: {ex.Message}");
            }
        }

        public bool IsInstallationQueueHidden()
        {
            try
            {
                if (_installationQueue == null) return false;
                return _installationQueue.Visibility == Visibility.Collapsed ||
                       (_installationQueueTransform != null && _installationQueueTransform.Y > 300);
            }
            catch
            {
                return false;
            }
        }

        // YENİ METODLAR - OYUN YÜKLEME KUYRUĞU

        /// <summary>
        /// 🎮 Oyun yükleme kuyruğunu sağdan içeri kaydırır (kurulum kuyruğunun yerine)
        /// </summary>
        public async Task SlideGameInstallationQueueIn()
        {
            try
            {
                if (_gameInstallationQueue == null || _gameInstallationQueueTransform == null) return;

                // Önce görünür yap ve sağda konumlandır
                _gameInstallationQueue.Visibility = Visibility.Visible;
                _gameInstallationQueueTransform.X = GAME_QUEUE_SLIDE_DISTANCE; // Sağdan başla

                var slideAnimation = new DoubleAnimation
                {
                    From = GAME_QUEUE_SLIDE_DISTANCE, // Sağdan
                    To = 0, // Kurulum kuyruğunun tam yerine
                    Duration = TimeSpan.FromMilliseconds(GAME_QUEUE_ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    tcs.SetResult(true);
                };

                _gameInstallationQueueTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SlideGameInstallationQueueIn hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// 🎮 Oyun yükleme kuyruğunu sağa kaydırarak gizler
        /// </summary>
        public async Task SlideGameInstallationQueueOut()
        {
            try
            {
                if (_gameInstallationQueue == null || _gameInstallationQueueTransform == null) return;

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = GAME_QUEUE_SLIDE_DISTANCE, // Sağa gizle
                    Duration = TimeSpan.FromMilliseconds(GAME_QUEUE_ANIMATION_DURATION),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource<bool>();
                slideAnimation.Completed += (s, e) => {
                    _gameInstallationQueue.Visibility = Visibility.Collapsed;
                    tcs.SetResult(true);
                };

                _gameInstallationQueueTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SlideGameInstallationQueueOut hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔍 Oyun kuyruğunun gizli olup olmadığını kontrol eder
        /// </summary>
        public bool IsGameInstallationQueueHidden()
        {
            try
            {
                if (_gameInstallationQueue == null) return true;
                return _gameInstallationQueue.Visibility == Visibility.Collapsed ||
                       (_gameInstallationQueueTransform != null && _gameInstallationQueueTransform.X > 150);
            }
            catch
            {
                return true;
            }
        }

        // MEVCUT PENCERE METODLARI (değişmez)
        public async Task CompactWindowHeight()
        {
            try
            {
                if (_isWindowCompact) return;

                var heightAnimation = new DoubleAnimation
                {
                    From = _originalWindowHeight,
                    To = _originalWindowHeight - _progressBarHeight,
                    Duration = TimeSpan.FromMilliseconds(WINDOW_HEIGHT_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                heightAnimation.Completed += (s, e) => {
                    _isWindowCompact = true;
                    tcs.SetResult(true);
                };

                _parentWindow.BeginAnimation(Window.HeightProperty, heightAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }

        public async Task ExpandWindowHeight()
        {
            try
            {
                if (!_isWindowCompact) return;

                var heightAnimation = new DoubleAnimation
                {
                    From = _originalWindowHeight - _progressBarHeight,
                    To = _originalWindowHeight,
                    Duration = TimeSpan.FromMilliseconds(WINDOW_HEIGHT_DURATION),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource<bool>();
                heightAnimation.Completed += (s, e) => {
                    _isWindowCompact = false;
                    tcs.SetResult(true);
                };

                _parentWindow.BeginAnimation(Window.HeightProperty, heightAnimation);
                await tcs.Task;
            }
            catch (Exception ex)
            {
            }
        }
    }
}