using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Yafes.Managers;

namespace Yafes.GameData
{
    /// <summary>
    /// 🎮 Game Silent Installation Manager
    /// Card click'ten tetiklenen silent kurulum sistemi
    /// </summary>
    public static class GameSetup
    {
        #region Events & Visual Feedback
        public static event Action<string, int> InstallationProgress;
        public static event Action<string, InstallationStatus> StatusChanged;
        public static event Action<string> LogMessage;

        // Visual feedback system
        private static Window _currentToastWindow;
        #endregion

        #region Enums
        public enum InstallationStatus
        {
            NotStarted,
            Preparing,
            Installing,
            Completed,
            Failed,
            Cancelled,
            UserCancelled
        }
        #endregion

        #region Silent Installation Parameters
        private static readonly string[] SilentParameters = new[]
        {
            "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-"  // Tek parametre - Inno Setup Full Silent
        };
        #endregion

        #region Main Installation Method
        /// <summary>
        /// 🚀 MAIN METHOD: Game Card'dan tetiklenen silent installation
        /// </summary>
        /// <param name="gameData">Kurulacak oyun verisi</param>
        /// <param name="queueManager">Queue manager referansı (opsiyonel)</param>
        /// <returns>Installation başarılı mı</returns>
        public static async Task<bool> StartSilentInstallation(Yafes.Models.GameData gameData, GameInstallationQueueManager queueManager = null)
        {
            try
            {
                Debug.WriteLine("=== INSTALLATION STARTED ===");

                if (gameData == null)
                {
                    ShowToast("❌ GameData is null - invalid game selection", ToastType.Error);
                    return false;
                }

                ShowToast($"🎮 Starting installation: {gameData.Name}", ToastType.Info);
                StatusChanged?.Invoke(gameData.Id, InstallationStatus.Preparing);

                // 1. Queue'ya ekle (eğer queue manager varsa)
                if (queueManager != null)
                {
                    try
                    {
                        await queueManager.AddGameToInstallationQueue(gameData);
                    }
                    catch
                    {
                        // Continue without queue
                    }
                }

                // 2. Setup.exe dosyasını bul
                string setupPath = await FindSetupFile(gameData);
                if (string.IsNullOrEmpty(setupPath))
                {
                    StatusChanged?.Invoke(gameData.Id, InstallationStatus.Failed);
                    return false;
                }

                // 3. Installation directory'yi belirle
                string installDir = DetermineInstallationDirectory(gameData.Name);

                // 4. Silent installation'ı başlat
                StatusChanged?.Invoke(gameData.Id, InstallationStatus.Installing);

                bool success = await ExecuteSilentInstallation(setupPath, installDir, gameData);

                if (success)
                {
                    StatusChanged?.Invoke(gameData.Id, InstallationStatus.Completed);
                    ShowToast($"🎉 {gameData.Name} installed successfully!", ToastType.Success);

                    // GameData'yı güncelle
                    try
                    {
                        await Yafes.Managers.GameDataManager.UpdateGameInstallStatusAsync(gameData.Id, true);
                    }
                    catch (Exception updateEx)
                    {
                        // Not critical - don't fail installation for this
                    }
                }
                else
                {
                    StatusChanged?.Invoke(gameData.Id, InstallationStatus.Failed);
                }

                Debug.WriteLine("=== INSTALLATION COMPLETED ===");
                return success;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(gameData?.Id ?? "unknown", InstallationStatus.Failed);
                return false;
            }
        }
        #endregion

        #region Setup File Detection
        /// <summary>
        /// 🔍 Setup.exe dosyasını bulur - TAMAMEN OTOMATİK
        /// </summary>
        private static async Task<string> FindSetupFile(Yafes.Models.GameData gameData)
        {
            try
            {
                // Arama yolları listesi (öncelik sırasına göre)
                var searchPaths = new[]
                {
                    @"D:\GameSetups",     // Ana konum
                    @"E:\GameSetups",     // İkincil konum  
                    @"F:\GameSetups",     // Üçüncül konum
                    @"G:\GameSetups",     // Dördüncül konum
                    @"C:\GameSetups",     // Sistem diski
                    @"D:\Games",          // Alternatif Games klasörü
                    @"E:\Games",          // Alternatif Games klasörü
                    @"F:\Games"           // Alternatif Games klasörü
                };

                // Setup dosya adları (priority order)
                var setupFileNames = new[]
                {
                    "setup.exe",
                    "Setup.exe",
                    "SETUP.EXE",
                    "install.exe",
                    "Install.exe",
                    "installer.exe",
                    "Installer.exe"
                };

                // Her path'i sistematik olarak kontrol et
                foreach (string basePath in searchPaths)
                {
                    if (!Directory.Exists(basePath))
                        continue;

                    // Oyun klasörünü bul
                    string gameFolder = await FindGameFolderAdvanced(basePath, gameData.Name);
                    if (string.IsNullOrEmpty(gameFolder))
                        continue;

                    // Setup dosyasını bul
                    foreach (string setupFileName in setupFileNames)
                    {
                        string setupPath = Path.Combine(gameFolder, setupFileName);
                        if (File.Exists(setupPath))
                        {
                            return setupPath;
                        }
                    }

                    // Alt klasörlerde de ara (max 2 level deep)
                    string subSetupPath = await SearchInSubfolders(gameFolder, setupFileNames, 2);
                    if (!string.IsNullOrEmpty(subSetupPath))
                    {
                        return subSetupPath;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 📁 Oyun klasörünü bulur - GELİŞMİŞ FUZZY MATCHING
        /// </summary>
        private static async Task<string> FindGameFolderAdvanced(string basePath, string gameName)
        {
            try
            {
                await Task.Yield();

                var directories = Directory.GetDirectories(basePath);
                string cleanGameName = CleanGameName(gameName);

                // 1. TAM EŞLEŞMe - En yüksek öncelik
                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir);
                    string cleanFolderName = CleanGameName(folderName);

                    if (cleanFolderName.Equals(cleanGameName, StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }

                // 2. BAŞLANGIÇ EŞLEŞMESİ - Yüksek öncelik
                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir);
                    string cleanFolderName = CleanGameName(folderName);

                    if (cleanFolderName.StartsWith(cleanGameName, StringComparison.OrdinalIgnoreCase) ||
                        cleanGameName.StartsWith(cleanFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }

                // 3. KELİME BAZLI EŞLEŞMe - Orta öncelik
                var gameWords = SplitIntoWords(cleanGameName);
                var bestMatch = "";
                int bestScore = 0;

                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir);
                    string cleanFolderName = CleanGameName(folderName);
                    var folderWords = SplitIntoWords(cleanFolderName);

                    int matchScore = CalculateWordMatchScore(gameWords, folderWords);
                    if (matchScore > bestScore && matchScore >= gameWords.Length / 2) // En az yarısı eşleşmeli
                    {
                        bestScore = matchScore;
                        bestMatch = dir;
                    }
                }

                if (!string.IsNullOrEmpty(bestMatch))
                {
                    return bestMatch;
                }

                // 4. İÇERİK BAZLI EŞLEŞMe - Düşük öncelik
                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir);
                    string cleanFolderName = CleanGameName(folderName);

                    if (cleanFolderName.Contains(cleanGameName, StringComparison.OrdinalIgnoreCase) ||
                        cleanGameName.Contains(cleanFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 🔍 Alt klasörlerde setup.exe arar (recursive - max depth)
        /// </summary>
        private static async Task<string> SearchInSubfolders(string parentFolder, string[] setupFileNames, int maxDepth)
        {
            try
            {
                if (maxDepth <= 0) return null;

                await Task.Yield();

                var subDirectories = Directory.GetDirectories(parentFolder);

                foreach (string subDir in subDirectories)
                {
                    // Bu klasörde setup var mı?
                    foreach (string setupFileName in setupFileNames)
                    {
                        string setupPath = Path.Combine(subDir, setupFileName);
                        if (File.Exists(setupPath))
                        {
                            return setupPath;
                        }
                    }

                    // Daha derine in (recursive)
                    string deeperSetup = await SearchInSubfolders(subDir, setupFileNames, maxDepth - 1);
                    if (!string.IsNullOrEmpty(deeperSetup))
                    {
                        return deeperSetup;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 📝 String'i kelimelere böler
        /// </summary>
        private static string[] SplitIntoWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];

            return text.Split(new char[] { ' ', '-', '_', '.', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 🎯 Kelime eşleşme skorunu hesaplar
        /// </summary>
        private static int CalculateWordMatchScore(string[] gameWords, string[] folderWords)
        {
            int score = 0;

            foreach (string gameWord in gameWords)
            {
                if (gameWord.Length < 3) continue; // Çok kısa kelimeler skip

                foreach (string folderWord in folderWords)
                {
                    if (folderWord.Equals(gameWord, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 2; // Tam eşleşme +2
                    }
                    else if (folderWord.Contains(gameWord, StringComparison.OrdinalIgnoreCase) ||
                             gameWord.Contains(folderWord, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 1; // Kısmi eşleşme +1
                    }
                }
            }

            return score;
        }

        /// <summary>
        /// 🧹 Oyun adını temizler - SADECE BOYUT BİLGİSİNİ KALDIR, REPACKER'I KORU
        /// </summary>
        private static string CleanGameName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string cleaned = name;

            // 1. BOYUT BİLGİLERİNİ KALDIR (GB, MB, TB ile biten sayılar)
            var sizePatterns = new[]
            {
                @"\s*\d+[\.,]?\d*\s*GB\s*",     // "5.2 GB", "15GB", "2.5 GB"
                @"\s*\d+[\.,]?\d*\s*MB\s*",     // "150 MB", "500MB", "1.2 MB"  
                @"\s*\d+[\.,]?\d*\s*TB\s*",     // "1 TB", "2TB", "1.5 TB"
                @"\s*\d+[\.,]?\d*\s*gb\s*",     // Küçük harf versiyonları
                @"\s*\d+[\.,]?\d*\s*mb\s*",
                @"\s*\d+[\.,]?\d*\s*tb\s*",
                @"\s*\d+[\.,]?\d*\s*Gb\s*",     // Karışık harf versiyonları
                @"\s*\d+[\.,]?\d*\s*Mb\s*",
                @"\s*\d+[\.,]?\d*\s*Tb\s*"
            };

            foreach (string pattern in sizePatterns)
            {
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, pattern, " ",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // 2. SADECE FAZLA BOŞLUKLARI TEMİZLE
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            // 3. KARŞILAŞTIRMA İÇİN NORMALIZE ET (REPACKER TAG'LERİ KORUYARAK)
            cleaned = cleaned
                .Replace("'", "")           // Apostrof'ları kaldır
                .Replace(":", "")           // İki nokta'ları kaldır  
                .Replace(".", "")           // Nokta'ları kaldır
                .Replace("™", "")           // Trademark'ları kaldır
                .Replace("®", "")           // Registered'ları kaldır
                .Replace("©", "")           // Copyright'ları kaldır
                .Replace("!", "")           // Ünlem'leri kaldır
                .Replace("?", "")           // Soru işareti'lerini kaldır
                .Replace("&", "and")        // &'i and'e çevir
                .Replace("+", "plus")       // +'yı plus'a çevir
                .Trim()                     // Başta/sonda boşluk varsa kaldır
                .ToLowerInvariant();        // Küçük harfe çevir

            return cleaned;
        }

        #endregion

        #region Installation Directory
        /// <summary>
        /// 📂 Installation directory'yi belirler
        /// </summary>
        private static string DetermineInstallationDirectory(string gameName)
        {
            try
            {
                // Varsayılan: C:\Games\[GameName]
                string baseDir = @"C:\Games";

                // Registry'den özel yol varsa al
                try
                {
                    var regValue = Microsoft.Win32.Registry.GetValue(
                        @"HKEY_CURRENT_USER\SOFTWARE\Yafes",
                        "GamesInstallPath",
                        baseDir) as string;

                    if (!string.IsNullOrEmpty(regValue) && Directory.Exists(Path.GetDirectoryName(regValue)))
                    {
                        baseDir = regValue;
                    }
                }
                catch
                {
                    // Registry error - use default
                }

                string installDir = Path.Combine(baseDir, SanitizeDirectoryName(gameName));

                // Directory'yi oluştur
                Directory.CreateDirectory(installDir);

                return installDir;
            }
            catch (Exception ex)
            {
                return @"C:\Games\" + SanitizeDirectoryName(gameName);
            }
        }

        /// <summary>
        /// 🧹 Directory adını temizler
        /// </summary>
        private static string SanitizeDirectoryName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "UnknownGame";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            return name.Trim();
        }
        #endregion

        #region Silent Installation Execution
        /// <summary>
        /// ⚙️ Silent installation'ı execute eder
        /// </summary>
        private static async Task<bool> ExecuteSilentInstallation(string setupPath, string installDir, Yafes.Models.GameData gameData)
        {
            try
            {
                // Her parametre için deneme yap
                for (int i = 0; i < SilentParameters.Length; i++)
                {
                    string parameter = SilentParameters[i].Replace("{INSTALLDIR}", installDir);

                    InstallationProgress?.Invoke(gameData.Id, (i * 100) / SilentParameters.Length);

                    bool success = await TryInstallWithParameter(setupPath, parameter, gameData.Id);

                    if (success)
                    {
                        // Installation başarılı - verify et
                        if (await VerifyInstallation(installDir, gameData.Name))
                        {
                            InstallationProgress?.Invoke(gameData.Id, 100);
                            return true;
                        }
                    }

                    // Küçük delay ekle
                    await Task.Delay(1000);
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 🧪 Tek parametre ile installation dener
        /// </summary>
        private static async Task<bool> TryInstallWithParameter(string setupPath, string parameter, string gameId)
        {
            try
            {
                // Checkbox bypass için INI dosyası oluştur
                try
                {
                    await CreateCheckboxBypassConfig(setupPath);
                }
                catch
                {
                    // Continue without config
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = setupPath,
                    Arguments = parameter,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    Verb = "runas"
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return false;
                }

                // 3 dakika timeout (checkbox'lar için yeterli süre)
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

                try
                {
                    // Process'i izle ve checkbox killer başlat
                    var monitoringTask = MonitorInstallationProcess(process, gameId);

                    await process.WaitForExitAsync(cts.Token);

                    int exitCode = process.ExitCode;
                    bool success = exitCode == 0;

                    return success;
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                        await Task.Delay(2000);
                    }
                    catch
                    {
                        // Ignore kill errors
                    }

                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                StatusChanged?.Invoke(gameId, InstallationStatus.UserCancelled);
                return false;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 📝 Checkbox bypass için config dosyası oluşturur
        /// </summary>
        private static async Task CreateCheckboxBypassConfig(string setupPath)
        {
            try
            {
                string setupDir = Path.GetDirectoryName(setupPath);
                if (string.IsNullOrEmpty(setupDir)) return;

                // Inno Setup için setup.iss dosyası oluştur
                string issPath = Path.Combine(setupDir, "setup.iss");
                var issContent = @"[Setup]
DisableReadyPage=yes
DisableFinishedPage=yes
DisableWelcomePage=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyMemo=yes
DisableStartupPrompt=yes
CreateAppDir=yes
UsePreviousAppDir=no
CreateUninstallRegKey=yes
CreateDesktopIcon=yes
CreateQuickLaunchIcon=no
CreateStartMenuIcon=yes
RunAfterInstall=no

[Tasks]
Name: desktopicon; Description: Create desktop icon; Flags: checked
Name: quicklaunchicon; Description: Create quick launch icon; Flags: unchecked  
Name: startmenu; Description: Create start menu icon; Flags: checked
Name: startup; Description: Run at startup; Flags: unchecked
Name: associate; Description: Associate file types; Flags: unchecked
Name: launch; Description: Launch game after installation; Flags: unchecked
Name: runafter; Description: Run after installation; Flags: unchecked

[Code]
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := True;
end;
";

                await File.WriteAllTextAsync(issPath, issContent);

                // NSIS için config
                string nsiConfigPath = Path.Combine(setupDir, "installer.cfg");
                var nsiContent = @"[Options]
NoDesktopShortcut=0
NoQuickLaunch=1  
NoStartMenu=0
NoStartup=1
NoFileAssoc=1
SilentMode=1
NoRestart=1
NoLaunchAfter=1
NoRunAfter=1
";

                await File.WriteAllTextAsync(nsiConfigPath, nsiContent);
            }
            catch
            {
                // Config oluşturulamasa da devam et
            }
        }

        /// <summary>
        /// 👁️ Installation process'ini izler - SETUP.TMP BASED MONITORING
        /// </summary>
        private static async Task MonitorInstallationProcess(Process installProcess, string gameId)
        {
            try
            {
                // 5 saniye bekle (kurulumun başlaması için)
                await Task.Delay(5000);

                // setup.tmp detection loop başlat
                _ = Task.Run(async () =>
                {
                    bool setupTmpDetected = false;
                    bool isMonitoringActive = false;
                    int preCheckCount = 0;

                    // PHASE 1: setup.tmp'yi aramaya başla
                    while (!setupTmpDetected && preCheckCount < 120) // 4 dakika max setup.tmp arama
                    {
                        await Task.Delay(2000); // 2 saniyede bir kontrol
                        preCheckCount++;

                        try
                        {
                            setupTmpDetected = await CheckForSetupTmp();

                            if (setupTmpDetected)
                            {
                                isMonitoringActive = true;
                                break;
                            }
                        }
                        catch
                        {
                            // Silent continue
                        }
                    }

                    if (!setupTmpDetected)
                        return;

                    // PHASE 2: setup.tmp bulundu - QuickSFV monitoring başlat
                    int monitoringCount = 0;
                    while (isMonitoringActive && monitoringCount < 300) // 10 dakika max QuickSFV arama
                    {
                        await Task.Delay(2000); // 2 saniyede bir kontrol
                        monitoringCount++;

                        try
                        {
                            bool quickSFVDetected = await CheckForQuickSFV();

                            if (quickSFVDetected)
                            {
                                // CLEANUP SEQUENCE
                                await ExecuteCleanupSequence();

                                isMonitoringActive = false;
                                break;
                            }
                        }
                        catch
                        {
                            // Silent continue
                        }
                    }
                });

                await Task.CompletedTask;
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 🔍 setup.tmp process'ini kontrol eder
        /// </summary>
        private static async Task<bool> CheckForSetupTmp()
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
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Process access error - skip
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 🔍 QuickSFV.EXE process'ini kontrol eder
        /// </summary>
        private static async Task<bool> CheckForQuickSFV()
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

                        if (processName == "quicksfv" ||
                            processName == "quicksfv.exe" ||
                            processName.StartsWith("quicksfv"))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Process access error - skip
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 🔪 Cleanup sequence executor - ÖNCE setup.tmp SONRA QuickSFV
        /// </summary>
        private static async Task ExecuteCleanupSequence()
        {
            try
            {
                // STEP 1: setup.tmp'yi sonlandır (ÖNCELİK)
                await TerminateProcessByExactName("setup.tmp", "Setup temporary process");
                await Task.Delay(1000); // 1 saniye bekle

                // STEP 2: QuickSFV.EXE'yi sonlandır (SONRA)
                await TerminateProcessByExactName("quicksfv", "QuickSFV unwanted process");
                await Task.Delay(500); // 0.5 saniye bekle
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 🔪 Exact process name ile process sonlandırır
        /// </summary>
        private static async Task TerminateProcessByExactName(string exactProcessName, string description)
        {
            try
            {
                await Task.Yield();

                var allProcesses = Process.GetProcesses();
                var targetProcesses = new List<Process>();

                // Exact name matching
                foreach (var process in allProcesses)
                {
                    try
                    {
                        string processName = process.ProcessName.ToLowerInvariant();
                        string targetName = exactProcessName.ToLowerInvariant();

                        if (processName == targetName ||
                            processName == $"{targetName}.exe" ||
                            processName.StartsWith(targetName))
                        {
                            targetProcesses.Add(process);
                        }
                    }
                    catch
                    {
                        // Process enumeration error - skip
                    }
                }

                if (targetProcesses.Count == 0)
                    return;

                // Her process'i sonlandır
                foreach (var process in targetProcesses)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            // Graceful close attempt
                            try
                            {
                                process.CloseMainWindow();

                                // 1 saniye bekle graceful close için
                                for (int i = 0; i < 10; i++)
                                {
                                    if (process.HasExited)
                                        break;
                                    await Task.Delay(100);
                                }
                            }
                            catch
                            {
                                // Graceful close failed
                            }

                            // Force kill if still running
                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                    }
                    catch
                    {
                        // Termination error - continue
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch
                        {
                            // Disposal error - ignore
                        }
                    }
                }
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 🔪 Checkbox window'larını kapatır ve işaretsiz hale getirir
        /// </summary>
        private static async Task KillCheckboxWindows()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Yaygın installer window title'ları
                    var installerTitles = new[]
                    {
                        "Setup", "Installer", "Installation", "Install", "Kurulum",
                        "Select Additional Tasks", "Additional Tasks", "Choose Components",
                        "Select Components", "Installation Options", "Setup Options"
                    };

                    foreach (string title in installerTitles)
                    {
                        try
                        {
                            // Windows API kullanarak pencereyi bul
                            IntPtr hWnd = FindWindow(null, title);
                            if (hWnd != IntPtr.Zero)
                            {
                                // Checkbox'ları işaretsiz hale getir
                                UncheckAllCheckboxes(hWnd);

                                // "Next" veya "Install" butonuna bas
                                ClickInstallButton(hWnd);
                            }
                        }
                        catch
                        {
                            // Window handling error - continue
                        }
                    }
                });
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// ☑️ Checkbox'ları istediğimiz şekilde ayarlar
        /// </summary>
        private static void UncheckAllCheckboxes(IntPtr parentWindow)
        {
            try
            {
                // Target checkbox'lar ve durumları
                var checkboxSettings = new Dictionary<string, bool>
                {
                    // ✅ İSTENEN CHECKBOX'LAR
                    {"desktop", true},          // Desktop kısayol OLUŞSUN
                    {"shortcut", true},         // Desktop shortcut OLUŞSUN  
                    {"startmenu", true},        // Start menu OLUŞSUN
                    {"start menu", true},       // Start menu OLUŞSUN
                    
                    // ❌ İSTENMEYEN CHECKBOX'LAR
                    {"launch", false},          // Oyuna GİRMESİN
                    {"run", false},             // Çalıştırmasın
                    {"start", false},           // Başlatmasın (eğer launch değilse)
                    {"quicklaunch", false},     // Quick launch YOK
                    {"quick launch", false},    // Quick launch YOK
                    {"startup", false},         // Windows startup'ta çalışmasın
                    {"associate", false},       // Dosya ilişkilendirmesi YOK
                    {"file association", false}, // Dosya ilişkilendirmesi YOK
                    {"readme", false},          // Readme açmasın
                    {"help", false},            // Yardım açmasın
                    {"browser", false},         // Browser açmasın
                    {"website", false},         // Website açmasın
                    {"update", false},          // Otomatik update YOK
                    {"check update", false},    // Update kontrolü YOK
                };

                // Checkbox control'larını bul ve ayarla
                EnumChildWindows(parentWindow, (hWnd, lParam) =>
                {
                    try
                    {
                        const int BM_SETCHECK = 0x00F1;
                        const int BST_CHECKED = 0x0001;
                        const int BST_UNCHECKED = 0x0000;

                        // Window class name'ini al
                        var className = new System.Text.StringBuilder(256);
                        GetClassName(hWnd, className, className.Capacity);

                        // Window text'ini al
                        var windowText = new System.Text.StringBuilder(256);
                        GetWindowText(hWnd, windowText, windowText.Capacity);

                        string text = windowText.ToString().ToLower();

                        // Checkbox ise kontrol et
                        if (className.ToString().ToLower().Contains("button"))
                        {
                            // Text'e göre checkbox durumunu belirle
                            foreach (var setting in checkboxSettings)
                            {
                                if (text.Contains(setting.Key))
                                {
                                    int checkState = setting.Value ? BST_CHECKED : BST_UNCHECKED;
                                    SendMessage(hWnd, BM_SETCHECK, checkState, 0);
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Individual control error - continue
                    }

                    return true; // Continue enumeration
                }, IntPtr.Zero);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 🖱️ Install/Next butonuna tıklar
        /// </summary>
        private static void ClickInstallButton(IntPtr parentWindow)
        {
            try
            {
                var buttonTexts = new[] { "Install", "Next", "Kurulum", "İleri", "Devam", "Continue", "OK" };

                EnumChildWindows(parentWindow, (hWnd, lParam) =>
                {
                    try
                    {
                        var buttonText = new System.Text.StringBuilder(256);
                        GetWindowText(hWnd, buttonText, buttonText.Capacity);

                        string text = buttonText.ToString();
                        if (buttonTexts.Any(bt => text.Contains(bt, StringComparison.OrdinalIgnoreCase)))
                        {
                            const int BM_CLICK = 0x00F5;
                            SendMessage(hWnd, BM_CLICK, 0, 0);
                            return false; // Stop enumeration - button found
                        }
                    }
                    catch
                    {
                        // Individual control error - continue
                    }

                    return true; // Continue enumeration
                }, IntPtr.Zero);
            }
            catch
            {
                // Silent fail
            }
        }

        #region Windows API Declarations
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);
        #endregion
        #endregion

        #region Installation Verification
        /// <summary>
        /// ✅ Installation'ı verify eder
        /// </summary>
        private static async Task<bool> VerifyInstallation(string installDir, string gameName)
        {
            try
            {
                await Task.Delay(2000); // Installation'ın tamamen bitmesi için bekle

                if (!Directory.Exists(installDir))
                {
                    return false;
                }

                // Directory'de dosya var mı kontrol et
                var files = Directory.GetFiles(installDir, "*.*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    return false;
                }

                // Executable dosya var mı kontrol et
                var exeFiles = Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories);
                if (exeFiles.Length > 0)
                {
                    return true;
                }

                // En azından bazı dosyalar varsa OK say
                bool hasEnoughFiles = files.Length > 5;
                return hasEnoughFiles;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region Public Utility Methods
        /// <summary>
        /// 🛑 Installation'ı iptal eder
        /// </summary>
        public static void CancelInstallation(string gameId)
        {
            try
            {
                StatusChanged?.Invoke(gameId, InstallationStatus.Cancelled);
            }
            catch (Exception ex)
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 📊 Installation status'unu döndürür
        /// </summary>
        public static bool IsGameInstalled(Yafes.Models.GameData gameData)
        {
            try
            {
                if (gameData == null)
                    return false;

                string installDir = DetermineInstallationDirectory(gameData.Name);
                return Directory.Exists(installDir) && Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 🎮 Oyunu başlatır
        /// </summary>
        public static async Task<bool> LaunchGame(Yafes.Models.GameData gameData)
        {
            try
            {
                if (gameData == null || !IsGameInstalled(gameData))
                {
                    return false;
                }

                string installDir = DetermineInstallationDirectory(gameData.Name);
                var exeFiles = Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories);

                if (exeFiles.Length == 0)
                {
                    return false;
                }

                // İlk exe dosyasını çalıştır (daha gelişmiş logic eklenebilir)
                Process.Start(new ProcessStartInfo
                {
                    FileName = exeFiles[0],
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exeFiles[0])
                });

                // Last played güncelle
                gameData.LastPlayed = DateTime.Now;
                await Yafes.Managers.GameDataManager.UpdateGameInstallStatusAsync(gameData.Id, true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region Visual Feedback System
        public enum ToastType
        {
            Info,
            Success,
            Warning,
            Error
        }

        /// <summary>
        /// 🎨 Toast notification gösterir
        /// </summary>
        private static void ShowToast(string message, ToastType type)
        {
            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // Eski toast'ı kapat
                    _currentToastWindow?.Close();

                    // Yeni toast oluştur
                    _currentToastWindow = CreateToastWindow(message, type);
                    _currentToastWindow.Show();

                    // Error mesajları için uzun süre (10 saniye), diğerleri için 5 saniye
                    int displaySeconds = type == ToastType.Error ? 10 : 5;

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(displaySeconds)
                    };

                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        _currentToastWindow?.Close();
                        _currentToastWindow = null;
                    };

                    timer.Start();

                    // Error mesajlarını ayrıca debug'a da yaz
                    if (type == ToastType.Error)
                    {
                        // Critical error'ları MessageBox ile de göster
                        if (message.Contains("error") || message.Contains("failed") || message.Contains("exception"))
                        {
                            try
                            {
                                MessageBox.Show(
                                    $"⚠️ INSTALLATION ERROR\n\n{message}\n\nCheck debug output for details.",
                                    "GameSetup Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                            catch
                            {
                                // MessageBox fail durumunda silent continue
                            }
                        }
                    }
                });
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// 🪟 Toast window oluşturur
        /// </summary>
        private static Window CreateToastWindow(string message, ToastType type)
        {
            var window = new Window
            {
                Width = 350,
                Height = 80,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };

            // Ekranın sağ alt köşesine konumlandır
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width - 20;
            window.Top = SystemParameters.PrimaryScreenHeight - window.Height - 20;

            // İçerik oluştur
            var border = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(10, 10, 10, 10)
            };

            // Tip'e göre renk seç
            var (backgroundColor, textColor) = type switch
            {
                ToastType.Success => ("#4CAF50", "#FFFFFF"),
                ToastType.Warning => ("#FF9800", "#FFFFFF"),
                ToastType.Error => ("#F44336", "#FFFFFF"),
                _ => ("#2196F3", "#FFFFFF")
            };

            border.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(backgroundColor));

            // Shadow effect
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Direction = 315,
                ShadowDepth = 5,
                BlurRadius = 10,
                Opacity = 0.3
            };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = message,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Medium,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            border.Child = textBlock;
            window.Content = border;

            // Fade in animation
            window.Opacity = 0;
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            window.BeginAnimation(Window.OpacityProperty, fadeIn);

            return window;
        }
        #endregion
    }
}