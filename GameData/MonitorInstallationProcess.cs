using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Yafes.GameData
{
    /// <summary>
    /// Installation Process Monitor with FitGirl + Inno Setup Support
    /// Progress Detection + QuickSFV Monitoring + Process Tracking
    /// </summary>
    public class MonitorInstallationProcess
    {
        #region Windows API Declarations
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, System.Text.StringBuilder lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int PBM_GETPOS = 0x0408;
        private const int PBM_GETRANGE = 0x0407;
        private const int WM_GETTEXT = 0x000D;
        private const int WM_GETTEXTLENGTH = 0x000E;
        #endregion

        #region Events
        public event Action<string, int> ProgressChanged;
        public event Action<string, MonitoringStatus> StatusChanged;
        public event Action<string> QuickSFVDetected;
        public event Action<string> SetupTmpDetected;
        public event Action<string, bool> MonitoringCompleted;
        #endregion

        #region Properties
        private readonly string _gameId;
        private readonly Process _installProcess;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isMonitoring;
        private int _lastProgress = -1;
        private MonitoringStatus _currentStatus = MonitoringStatus.NotStarted;
        private DateTime _monitoringStartTime;
        #endregion

        #region Constructor
        public MonitorInstallationProcess(Process installProcess, string gameId)
        {
            _installProcess = installProcess ?? throw new ArgumentNullException(nameof(installProcess));
            _gameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
        }
        #endregion

        #region Main Monitoring Method
        public async Task StartMonitoringAsync()
        {
            try
            {
                if (_isMonitoring) return;

                _isMonitoring = true;
                _monitoringStartTime = DateTime.Now;
                _cancellationTokenSource = new CancellationTokenSource();

                UpdateStatus(MonitoringStatus.Initializing);

                await Task.Delay(3000, _cancellationTokenSource.Token);

                var progressTask = MonitorProgressAsync(_cancellationTokenSource.Token);
                var setupTmpTask = MonitorSetupTmpAsync(_cancellationTokenSource.Token);

                UpdateStatus(MonitoringStatus.Active);

                await WaitForProcessExitAsync(_cancellationTokenSource.Token);

                bool success = _installProcess.ExitCode == 0;
                UpdateStatus(success ? MonitoringStatus.Completed : MonitoringStatus.Failed);
                MonitoringCompleted?.Invoke(_gameId, success);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus(MonitoringStatus.Cancelled);
                MonitoringCompleted?.Invoke(_gameId, false);
            }
            catch (Exception)
            {
                UpdateStatus(MonitoringStatus.Failed);
                MonitoringCompleted?.Invoke(_gameId, false);
            }
            finally
            {
                _isMonitoring = false;
                _cancellationTokenSource?.Dispose();
            }
        }
        #endregion

        #region Progress Monitoring
        private async Task MonitorProgressAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && !_installProcess.HasExited)
                {
                    try
                    {
                        var progressInfo = await DetectInstallationProgressAsync();

                        if (progressInfo.HasValue)
                        {
                            int currentProgress = progressInfo.Value;

                            if (currentProgress != _lastProgress && currentProgress >= 0 && currentProgress <= 100)
                            {
                                _lastProgress = currentProgress;
                                ProgressChanged?.Invoke(_gameId, currentProgress);

                                if (_currentStatus == MonitoringStatus.Active && currentProgress > 0)
                                {
                                    UpdateStatus(MonitoringStatus.Installing);
                                }
                            }
                        }
                    }
                    catch (Exception) { }

                    await Task.Delay(500, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task<int?> DetectInstallationProgressAsync()
        {
            try
            {
                await Task.Yield();

                var setupTmpProgress = await DetectSetupTmpProgressAsync();
                if (setupTmpProgress.HasValue)
                    return setupTmpProgress;

                var innoProgress = await DetectInnoSetupProgressAsync();
                if (innoProgress.HasValue)
                    return innoProgress;

                IntPtr progressBarHandle = FindProgressBarInProcess(_installProcess.Id);
                if (progressBarHandle != IntPtr.Zero)
                {
                    int currentPos = SendMessage(progressBarHandle, PBM_GETPOS, 0, 0);
                    int range = SendMessage(progressBarHandle, PBM_GETRANGE, 0, 0);
                    int maxRange = (range >> 16) & 0xFFFF;
                    int minRange = range & 0xFFFF;

                    if (maxRange > minRange)
                    {
                        int normalizedProgress = ((currentPos - minRange) * 100) / (maxRange - minRange);
                        return Math.Max(0, Math.Min(100, normalizedProgress));
                    }
                    return Math.Max(0, Math.Min(100, currentPos));
                }

                var fitgirlProgress = await DetectFitGirlGenericAsync();
                if (fitgirlProgress.HasValue)
                    return fitgirlProgress;

                var titleProgress = ExtractProgressFromWindowTitle();
                if (titleProgress.HasValue)
                    return titleProgress;

                var timeProgress = GetTimeBasedProgress();
                if (timeProgress.HasValue && timeProgress >= 10)
                    return timeProgress;

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> DetectSetupTmpProgressAsync()
        {
            try
            {
                await Task.Yield();

                Process setupTmpProcess = null;

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        string processName = process.ProcessName.ToLowerInvariant();
                        if (processName == "setup.tmp" || processName.StartsWith("setup.tmp"))
                        {
                            setupTmpProcess = process;
                            break;
                        }
                    }
                    catch { }
                }

                if (setupTmpProcess == null) return null;

                var setupTmpWindows = new List<IntPtr>();

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == setupTmpProcess.Id)
                        {
                            setupTmpWindows.Add(hWnd);
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                foreach (var window in setupTmpWindows)
                {
                    var progressBarClasses = new[]
                    {
                        "msctls_progress32", "TProgressBar", "TNewProgressBar",
                        "Progress", "ProgressBar", "SysProgress32"
                    };

                    foreach (var className in progressBarClasses)
                    {
                        IntPtr progressBar = FindWindowEx(window, IntPtr.Zero, className, null);
                        if (progressBar != IntPtr.Zero)
                        {
                            int pos = SendMessage(progressBar, PBM_GETPOS, 0, 0);
                            int range = SendMessage(progressBar, PBM_GETRANGE, 0, 0);

                            if (pos > 0 && pos <= 100)
                                return pos;

                            if (range > 0)
                            {
                                int maxRange = (range >> 16) & 0xFFFF;
                                int minRange = range & 0xFFFF;

                                if (maxRange > minRange && pos >= minRange && pos <= maxRange)
                                {
                                    int progress = ((pos - minRange) * 100) / (maxRange - minRange);
                                    return progress;
                                }
                            }
                        }
                    }

                    var textBuilder = new System.Text.StringBuilder(1024);
                    GetWindowText(window, textBuilder, textBuilder.Capacity);
                    string windowText = textBuilder.ToString();

                    if (!string.IsNullOrEmpty(windowText))
                    {
                        var progressPatterns = new[]
                        {
                            @"(\d+)%",
                            @"(\d+)\s*of\s*(\d+)",
                            @"Progress.*?(\d+)",
                            @"Installing.*?(\d+)",
                            @"Extracting.*?(\d+)",
                            @"Copying.*?(\d+)",
                            @"\[(\d+)\]"
                        };

                        foreach (var pattern in progressPatterns)
                        {
                            var match = Regex.Match(windowText, pattern, RegexOptions.IgnoreCase);
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                            {
                                if (progress >= 0 && progress <= 100)
                                    return progress;
                            }
                        }
                    }

                    var childProgress = await ScanSetupTmpChildWindows(window);
                    if (childProgress.HasValue)
                        return childProgress;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> ScanSetupTmpChildWindows(IntPtr parentWindow)
        {
            try
            {
                await Task.Yield();

                var childWindows = new List<IntPtr>();

                EnumChildWindows(parentWindow, (hWnd, lParam) =>
                {
                    childWindows.Add(hWnd);
                    return true;
                }, IntPtr.Zero);

                foreach (var child in childWindows)
                {
                    try
                    {
                        var classBuilder = new System.Text.StringBuilder(256);
                        GetClassName(child, classBuilder, classBuilder.Capacity);
                        string className = classBuilder.ToString();

                        var textBuilder = new System.Text.StringBuilder(512);
                        GetWindowText(child, textBuilder, textBuilder.Capacity);
                        string windowText = textBuilder.ToString();

                        if (className.Contains("Progress") || windowText.Contains("%") ||
                            className.Contains("Status") || className.Contains("Label"))
                        {
                            int pos = SendMessage(child, PBM_GETPOS, 0, 0);
                            if (pos > 0 && pos <= 100)
                                return pos;

                            if (!string.IsNullOrEmpty(windowText))
                            {
                                var match = Regex.Match(windowText, @"(\d+)%", RegexOptions.IgnoreCase);
                                if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                                {
                                    if (progress >= 0 && progress <= 100)
                                        return progress;
                                }
                            }
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> DetectFitGirlGenericAsync()
        {
            try
            {
                await Task.Yield();

                var progressFromControls = await ScanAllControlsForProgress();
                if (progressFromControls.HasValue)
                    return progressFromControls;

                var progressFromText = await AnalyzeWindowTextContent();
                if (progressFromText.HasValue)
                    return progressFromText;

                var progressFromMemory = await AnalyzeProcessMemory();
                if (progressFromMemory.HasValue)
                    return progressFromMemory;

                var progressFromFiles = await EstimateProgressFromFileActivity();
                if (progressFromFiles.HasValue)
                    return progressFromFiles;

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> DetectInnoSetupProgressAsync()
        {
            try
            {
                await Task.Yield();

                var innoWindows = new List<IntPtr>();

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == _installProcess.Id)
                        {
                            var titleBuilder = new System.Text.StringBuilder(512);
                            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                            string title = titleBuilder.ToString();

                            var classBuilder = new System.Text.StringBuilder(256);
                            GetClassName(hWnd, classBuilder, classBuilder.Capacity);
                            string className = classBuilder.ToString();

                            if (title.Contains("InnoSetup") || title.Contains("Setup") ||
                                title.Contains("Installation") || title.Contains("Install") ||
                                className.Contains("TSetupForm") || className.Contains("TWizardForm") ||
                                className.Contains("TMainForm") || className.Contains("TForm") ||
                                (!string.IsNullOrEmpty(title) && title.Length > 5))
                            {
                                innoWindows.Add(hWnd);
                            }
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                foreach (var window in innoWindows)
                {
                    var innoControlClasses = new[]
                    {
                        "TNewProgressBar", "TProgressBar", "msctls_progress32",
                        "TPanel", "TNewPanel", "TScrollBox", "TGroupBox",
                        "TNewStaticText", "TLabel", "TNewLabel", "TMemo", "TNewMemo",
                        "TEdit", "TNewEdit", "Static", "Edit",
                        "TWizardForm", "TSetupForm", "TMainForm", "TForm",
                        "TNewButton", "TButton", "TNewCheckBox", "TCheckBox",
                        "TNewRadioButton", "TRadioButton", "Button",
                        "TNewListBox", "TListBox", "TNewCheckListBox",
                        "TBevel", "TImage", "TTimer", "TPageControl", "TTabSheet"
                    };

                    foreach (var className in innoControlClasses)
                    {
                        IntPtr control = FindWindowEx(window, IntPtr.Zero, className, null);
                        if (control != IntPtr.Zero)
                        {
                            var progressValue = await GetProgressFromInnoControl(control, className);
                            if (progressValue.HasValue)
                                return progressValue;
                        }
                    }

                    var childProgress = await ScanInnoChildWindowsEnhanced(window);
                    if (childProgress.HasValue)
                        return childProgress;

                    var textProgress = await AnalyzeInnoWindowTextEnhanced(window);
                    if (textProgress.HasValue)
                        return textProgress;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> GetProgressFromInnoControl(IntPtr control, string className)
        {
            try
            {
                await Task.Yield();

                if (className.Contains("Progress"))
                {
                    int pos = SendMessage(control, PBM_GETPOS, 0, 0);
                    int range = SendMessage(control, PBM_GETRANGE, 0, 0);

                    if (pos > 0 || range > 0)
                    {
                        if (range > 0)
                        {
                            int maxRange = (range >> 16) & 0xFFFF;
                            int minRange = range & 0xFFFF;

                            if (maxRange > minRange && pos >= minRange && pos <= maxRange)
                                return ((pos - minRange) * 100) / (maxRange - minRange);
                        }

                        if (pos >= 0 && pos <= 100)
                            return pos;
                    }
                }

                if (className.Contains("Text") || className.Contains("Label") || className.Contains("Static"))
                {
                    var textBuilder = new System.Text.StringBuilder(512);
                    int textLength = SendMessage(control, WM_GETTEXT, textBuilder.Capacity, textBuilder);

                    if (textLength > 0)
                    {
                        string controlText = textBuilder.ToString();

                        var match = Regex.Match(controlText, @"(\d+)%", RegexOptions.IgnoreCase);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int textProgress))
                            return Math.Max(0, Math.Min(100, textProgress));
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> ScanInnoChildWindowsEnhanced(IntPtr parentWindow)
        {
            try
            {
                await Task.Yield();

                var childWindows = new List<IntPtr>();

                EnumChildWindows(parentWindow, (hWnd, lParam) =>
                {
                    childWindows.Add(hWnd);
                    return true;
                }, IntPtr.Zero);

                foreach (var child in childWindows)
                {
                    try
                    {
                        var classBuilder = new System.Text.StringBuilder(256);
                        GetClassName(child, classBuilder, classBuilder.Capacity);
                        string className = classBuilder.ToString();

                        var textBuilder = new System.Text.StringBuilder(512);
                        GetWindowText(child, textBuilder, textBuilder.Capacity);
                        string windowText = textBuilder.ToString();

                        if (className.Contains("Progress") || windowText.Contains("%") ||
                            className.Contains("Status") || className.Contains("Label") ||
                            className.Contains("Static") || className.Contains("Text"))
                        {
                            var progressMessages = new[] { PBM_GETPOS, 0x400, 0x1000, 0x2000 };

                            foreach (var msg in progressMessages)
                            {
                                int value = SendMessage(child, msg, 0, 0);
                                if (value > 0 && value <= 100)
                                    return value;
                            }

                            if (!string.IsNullOrEmpty(windowText))
                            {
                                var progressPatterns = new[]
                                {
                                    @"(\d+)%",
                                    @"(\d+)\s*of\s*(\d+)",
                                    @"Progress.*?(\d+)",
                                    @"Installing.*?(\d+)",
                                    @"Extracting.*?(\d+)",
                                    @"Copying.*?(\d+)",
                                    @"\[(\d+)\]"
                                };

                                foreach (var pattern in progressPatterns)
                                {
                                    var match = Regex.Match(windowText, pattern, RegexOptions.IgnoreCase);
                                    if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                                    {
                                        if (progress >= 0 && progress <= 100)
                                            return progress;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> AnalyzeInnoWindowTextEnhanced(IntPtr window)
        {
            try
            {
                await Task.Yield();

                var textBuilder = new System.Text.StringBuilder(1024);
                GetWindowText(window, textBuilder, textBuilder.Capacity);
                string windowText = textBuilder.ToString();

                if (!string.IsNullOrEmpty(windowText))
                {
                    var patterns = new[]
                    {
                        @"(\d+)%\s*complete",
                        @"(\d+)%\s*done",
                        @"(\d+)\s*%",
                        @"(\d+)\s*of\s*(\d+)",
                        @"Progress:\s*(\d+)",
                        @"Installing.*?(\d+)%",
                        @"Extracting.*?(\d+)%",
                        @"Copying.*?(\d+)%",
                        @"Decompressing.*?(\d+)%",
                        @"Unpacking.*?(\d+)%",
                        @"Processing.*?(\d+)%",
                        @"\[(\d+)%\]",
                        @"(\d+)\/100",
                        @"Step\s*\d+.*?(\d+)%",
                        @"Phase\s*\d+.*?(\d+)%",
                        @"File\s*(\d+)\s*of\s*(\d+)",
                    };

                    foreach (var pattern in patterns)
                    {
                        var match = Regex.Match(windowText, pattern, RegexOptions.IgnoreCase);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                        {
                            if (progress >= 0 && progress <= 100)
                                return progress;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> ScanAllControlsForProgress()
        {
            try
            {
                await Task.Yield();

                var processWindows = new List<IntPtr>();

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == _installProcess.Id)
                        {
                            processWindows.Add(hWnd);
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                foreach (var window in processWindows)
                {
                    try
                    {
                        var controlClasses = new[]
                        {
                            "msctls_progress32", "ProgressBar", "TProgressBar", "Progress",
                            "SysProgress32", "WindowsControlBar", "Static", "Edit", "Button",
                            "ListBox", "ComboBox", "ScrollBar", "StatusBar", "ToolBar",
                            "RichEdit", "RichEdit20A", "RichEdit20W", "RICHEDIT50W",
                            "SysListView32", "SysTreeView32", "SysTabControl32",
                            "msctls_statusbar32", "msctls_trackbar32", "msctls_updown32",
                            "ATL:", "HwndWrapper", "WindowsForms10", ".NET Control",
                            "TPanel", "TForm", "TLabel", "TMemo", "TEdit", "TButton",
                            "TProgressBar", "TStatusBar", "TStringGrid", "TCheckBox"
                        };

                        foreach (var className in controlClasses)
                        {
                            IntPtr childHandle = FindWindowEx(window, IntPtr.Zero, className, null);

                            if (childHandle != IntPtr.Zero)
                            {
                                int progressValue = SendMessage(childHandle, PBM_GETPOS, 0, 0);

                                if (progressValue > 0 && progressValue <= 100)
                                    return progressValue;

                                var alternativeMessages = new[] { 0x400, 0x401, 0x402, 0x1000, 0x1001, 0x1002 };
                                foreach (var msg in alternativeMessages)
                                {
                                    int altValue = SendMessage(childHandle, msg, 0, 0);
                                    if (altValue > 0 && altValue <= 100)
                                        return altValue;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> AnalyzeWindowTextContent()
        {
            try
            {
                await Task.Yield();

                var processWindows = new List<IntPtr>();

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == _installProcess.Id)
                        {
                            processWindows.Add(hWnd);
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                foreach (var window in processWindows)
                {
                    try
                    {
                        var titleBuilder = new System.Text.StringBuilder(512);
                        GetWindowText(window, titleBuilder, titleBuilder.Capacity);
                        string windowText = titleBuilder.ToString();

                        if (!string.IsNullOrEmpty(windowText))
                        {
                            var patterns = new[]
                            {
                                @"(\d+)%",
                                @"(\d+)\s*percent",
                                @"(\d+)/100",
                                @"Progress.*?(\d+)",
                                @"Installing.*?(\d+)",
                                @"Extracting.*?(\d+)",
                                @"Decompressing.*?(\d+)",
                                @"Unpacking.*?(\d+)",
                                @"Copying.*?(\d+)",
                                @"\[(\d+)\]",
                                @"(\d+)\s*of\s*100",
                                @"Step\s*\d+.*?(\d+)%",
                                @"Phase\s*\d+.*?(\d+)%",
                            };

                            foreach (var pattern in patterns)
                            {
                                var match = Regex.Match(windowText, pattern, RegexOptions.IgnoreCase);

                                if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                                {
                                    if (progress >= 0 && progress <= 100)
                                        return progress;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> AnalyzeProcessMemory()
        {
            try
            {
                await Task.Yield();

                if (!_installProcess.HasExited)
                {
                    try
                    {
                        long workingSet = _installProcess.WorkingSet64;

                        if (workingSet > 50 * 1024 * 1024)
                        {
                            var elapsed = DateTime.Now - _installProcess.StartTime;
                            if (elapsed.TotalMinutes > 1)
                            {
                                int memoryProgress = Math.Min(90, (int)(elapsed.TotalMinutes * 8));
                                return memoryProgress;
                            }
                        }
                    }
                    catch (Exception) { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int?> EstimateProgressFromFileActivity()
        {
            try
            {
                await Task.Yield();

                var tempPaths = new[]
                {
                    Path.GetTempPath(),
                    @"C:\Windows\Temp",
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp",
                    @"C:\Temp",
                    @"C:\tmp"
                };

                foreach (var tempPath in tempPaths)
                {
                    try
                    {
                        if (Directory.Exists(tempPath))
                        {
                            var recentFiles = Directory.GetFiles(tempPath, "*", SearchOption.TopDirectoryOnly)
                                .Where(f => File.GetCreationTime(f) > _installProcess.StartTime.AddMinutes(-2))
                                .Where(f => new FileInfo(f).Length > 1024 * 1024)
                                .ToList();

                            if (recentFiles.Any())
                            {
                                var totalSize = recentFiles.Sum(f => new FileInfo(f).Length);

                                if (totalSize > 100 * 1024 * 1024)
                                {
                                    var elapsed = DateTime.Now - _installProcess.StartTime;
                                    int fileProgress = Math.Min(80, (int)(elapsed.TotalMinutes * 6));
                                    return fileProgress;
                                }
                            }
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IntPtr FindProgressBarInProcess(int processId)
        {
            IntPtr foundProgressBar = IntPtr.Zero;

            try
            {
                var progressBarClasses = new[]
                {
                    "msctls_progress32",
                    "ProgressBar",
                    "TProgressBar",
                    "Progress",
                    "SysProgress32",
                    "WindowsControlBar"
                };

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        if (windowProcessId == processId)
                        {
                            foreach (string className in progressBarClasses)
                            {
                                IntPtr progressBar = FindWindowEx(hWnd, IntPtr.Zero, className, null);
                                if (progressBar != IntPtr.Zero)
                                {
                                    foundProgressBar = progressBar;
                                    return false;
                                }
                            }
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception) { }

            return foundProgressBar;
        }

        private int? ExtractProgressFromWindowTitle()
        {
            try
            {
                if (!_installProcess.HasExited && !string.IsNullOrEmpty(_installProcess.MainWindowTitle))
                {
                    string title = _installProcess.MainWindowTitle;

                    var patterns = new[]
                    {
                        @"(\d+)%",
                        @"(\d+)/100",
                        @"(\d+)\s*of\s*100",
                        @"Progress.*?(\d+)",
                        @"Installing.*?(\d+)",
                        @"\[(\d+)%\]",
                        @"(\d+)\s*percent"
                    };

                    foreach (var pattern in patterns)
                    {
                        var match = Regex.Match(title, pattern, RegexOptions.IgnoreCase);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
                            return Math.Max(0, Math.Min(100, progress));
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        private int? GetTimeBasedProgress()
        {
            try
            {
                if (!_installProcess.HasExited)
                {
                    var elapsed = DateTime.Now - _installProcess.StartTime;
                    int timeProgress = Math.Min(100, (int)(elapsed.TotalMinutes * (100.0 / 15.0)));

                    if (timeProgress >= 5)
                        return timeProgress;
                }
            }
            catch (Exception) { }
            return null;
        }
        #endregion

        #region Setup.tmp & QuickSFV Monitoring
        private async Task MonitorSetupTmpAsync(CancellationToken cancellationToken)
        {
            try
            {
                bool setupTmpDetected = false;
                bool quickSFVMonitoringActive = false;
                int preCheckCount = 0;

                while (!setupTmpDetected && preCheckCount < 120 && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                    preCheckCount++;

                    try
                    {
                        setupTmpDetected = await CheckForSetupTmpAsync();

                        if (setupTmpDetected)
                        {
                            SetupTmpDetected?.Invoke(_gameId);
                            quickSFVMonitoringActive = true;
                            break;
                        }
                    }
                    catch (Exception) { }
                }

                if (!setupTmpDetected) return;

                int quickSFVCheckCount = 0;

                while (quickSFVMonitoringActive && quickSFVCheckCount < 300 && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                    quickSFVCheckCount++;

                    try
                    {
                        bool quickSFVDetected = await CheckForQuickSFVAsync();

                        if (quickSFVDetected)
                        {
                            QuickSFVDetected?.Invoke(_gameId);
                            await ExecuteCleanupSequenceAsync();
                            quickSFVMonitoringActive = false;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task<bool> CheckForSetupTmpAsync()
        {
            try
            {
                await Task.Yield();
                var allProcesses = Process.GetProcesses();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        string processName = process.ProcessName.ToLowerInvariant();
                        if (processName == "setup.tmp" || processName.StartsWith("setup.tmp"))
                            return true;
                    }
                    catch { }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> CheckForQuickSFVAsync()
        {
            try
            {
                await Task.Yield();
                var allProcesses = Process.GetProcesses();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        string processName = process.ProcessName.ToLowerInvariant();
                        if (processName == "quicksfv" || processName == "quicksfv.exe" || processName.StartsWith("quicksfv"))
                            return true;
                    }
                    catch { }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ExecuteCleanupSequenceAsync()
        {
            try
            {
                await TerminateProcessByNameAsync("setup.tmp");
                await Task.Delay(1000);

                await TerminateProcessByNameAsync("quicksfv");
                await Task.Delay(500);
            }
            catch (Exception) { }
        }

        private async Task TerminateProcessByNameAsync(string exactProcessName)
        {
            try
            {
                await Task.Yield();
                var targetProcesses = new List<Process>();

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        string processName = process.ProcessName.ToLowerInvariant();
                        string targetName = exactProcessName.ToLowerInvariant();

                        if (processName == targetName || processName == $"{targetName}.exe" || processName.StartsWith(targetName))
                        {
                            targetProcesses.Add(process);
                        }
                    }
                    catch { }
                }

                foreach (var process in targetProcesses)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.CloseMainWindow();

                                for (int i = 0; i < 10; i++)
                                {
                                    if (process.HasExited) break;
                                    await Task.Delay(100);
                                }
                            }
                            catch { }

                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        try { process.Dispose(); } catch { }
                    }
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region Process Management & Status
        private async Task WaitForProcessExitAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!_installProcess.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        private void UpdateStatus(MonitoringStatus newStatus)
        {
            if (_currentStatus != newStatus)
            {
                _currentStatus = newStatus;
                StatusChanged?.Invoke(_gameId, newStatus);
            }
        }
        #endregion

        #region Public Methods
        public void StopMonitoring()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _isMonitoring = false;
            }
            catch (Exception) { }
        }

        public MonitoringStatus GetCurrentStatus()
        {
            return _currentStatus;
        }

        public bool IsMonitoring()
        {
            return _isMonitoring;
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            try
            {
                StopMonitoring();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception) { }
        }
        #endregion
    }

    public enum MonitoringStatus
    {
        NotStarted,
        Initializing,
        Active,
        Installing,
        Completed,
        Failed,
        Cancelled
    }
}