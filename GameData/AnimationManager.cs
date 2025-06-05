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
        private readonly double _originalWindowHeight;
        private readonly double _progressBarHeight;
        private bool _isWindowCompact;

        private const double SIDEBAR_SLIDE_DISTANCE = -280;
        private const double ANIMATION_DURATION = 600;
        private const double PROGRESSBAR_SLIDE_DISTANCE = 800;
        private const double PROGRESSBAR_ANIMATION_DURATION = 800;
        private const double WINDOW_HEIGHT_DURATION = 700;

        public AnimationManager(Window parentWindow, Border leftSidebar, TranslateTransform leftSidebarTransform,
            Border progressBarContainer, TranslateTransform progressBarTransform,
            double originalWindowHeight, double progressBarHeight)
        {
            _parentWindow = parentWindow;
            _leftSidebar = leftSidebar;
            _leftSidebarTransform = leftSidebarTransform;
            _progressBarContainer = progressBarContainer;
            _progressBarTransform = progressBarTransform;
            _originalWindowHeight = originalWindowHeight;
            _progressBarHeight = progressBarHeight;
            _isWindowCompact = false;
        }

        public bool IsWindowCompact => _isWindowCompact;

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