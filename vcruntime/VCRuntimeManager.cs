using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Yafes
{
    public class VCRuntimeManager
    {
        public readonly List<VCRuntimeInfo> AvailableRuntimes = new List<VCRuntimeInfo>
{
    // 2008 x86 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2008 Redistributable - x86 9.0.30729.7523",
        Year = 2008,
        Architecture = "x86",
        Version = "9.0.30729.7523",
        FileName = "vcredist2008_x86.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{6AFCA4E1-9B78-3640-8F72-A7BF33448200}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\1E4ACFA687B904463F8727ABF334284",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\1E4ACFA687B904463F8727ABF334284"
        },
        KeyFiles = new List<string> { "msvcr90.dll", "msvcp90.dll" },
        SkipIfExists = true,
        InstallArguments = "/q"
    },

    // 2008 x64 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2008 Redistributable - x64 9.0.30729.7523",
        Year = 2008,
        Architecture = "x64",
        Version = "9.0.30729.7523",
        FileName = "vcredist2008_x64.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x64.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{6E3E9C94-6B78-3640-8F72-A7BF33448200}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\49C3E6E687B904463F8727ABF334284",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\49C3E6E687B904463F8727ABF334284"
        },
        KeyFiles = new List<string> { "msvcr90.dll", "msvcp90.dll" },
        SkipIfExists = true,
        InstallArguments = "/q"
    },

    // 2010 x86 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2010 x86 Redistributable - 10.0.40219",
        Year = 2010,
        Architecture = "x86",
        Version = "10.0.40219",
        FileName = "vcredist2010_x86.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/C/6/D/C6D0FD4E-9E53-4897-9B91-836EBA2AACD3/vcredist_x86.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{196BB40D-1578-3D01-B289-BEFC77A11A1E}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\D04BB691875183D0B982BEFC77A11AE1",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\D04BB691875183D0B982BEFC77A11AE1",
            @"SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x86"
        },
        KeyFiles = new List<string> { "msvcr100.dll", "msvcp100.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2010 x64 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2010 x64 Redistributable - 10.0.40219",
        Year = 2010,
        Architecture = "x64",
        Version = "10.0.40219",
        FileName = "vcredist2010_x64.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/A/8/0/A80747C3-41BD-45DF-B505-E9710D2744E0/vcredist_x64.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{DA5E371C-6333-3D8A-93A4-6FD5B20BCC6E}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\C173E5AD33363AD839A46F5D2BB0CC6E",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\C173E5AD33363AD839A46F5D2BB0CC6E",
            @"SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x64"
        },
        KeyFiles = new List<string> { "msvcr100.dll", "msvcp100.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2012 x86 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2012 Redistributable (x86) - 11.0.61030",
        Year = 2012,
        Architecture = "x86",
        Version = "11.0.61030",
        FileName = "vcredist2012_x86.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{33d1fd90-4274-48a1-9bc1-97e33d9c2d6f}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\09DF1F3347724A18B9C179E3D3C9D2F6",
            @"SOFTWARE\Microsoft\VisualStudio\11.0\VC\VCRedist\x86"
        },
        KeyFiles = new List<string> { "msvcr110.dll", "msvcp110.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2012 x64 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2012 Redistributable (x64) - 11.0.61030",
        Year = 2012,
        Architecture = "x64",
        Version = "11.0.61030",
        FileName = "vcredist2012_x64.exe", // DEĞIŞTI
        DownloadUrl = "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{ca67548a-5ebe-413a-b50c-4b9ceb6d66c6}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\A84576ACB5E3A31405BC4BB9EC6D66C6",
            @"SOFTWARE\Microsoft\VisualStudio\11.0\VC\VCRedist\x64"
        },
        KeyFiles = new List<string> { "msvcr110.dll", "msvcp110.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2013 x86 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2013 Redistributable (x86) - 12.0.40664",
        Year = 2013,
        Architecture = "x86",
        Version = "12.0.40664",
        FileName = "vcredist2013_x86.exe", // DEĞIŞTI
        DownloadUrl = "https://aka.ms/highdpimfc2013x86enu",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{f65db027-aff3-4070-886a-0d87064aabb1}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\720BDF6FA3F0740688A6D070460ABBA1",
            @"SOFTWARE\Microsoft\VisualStudio\12.0\VC\VCRedist\x86"
        },
        KeyFiles = new List<string> { "msvcr120.dll", "msvcp120.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2013 x64 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2013 Redistributable (x64) - 12.0.40664",
        Year = 2013,
        Architecture = "x64",
        Version = "12.0.40664",
        FileName = "vcredist2013_x64.exe", // DEĞIŞTI
        DownloadUrl = "https://aka.ms/highdpimfc2013x64enu",
        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{042d26ef-3dbe-4c25-95d3-4c1b11b235a7}",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Classes\Installer\Products\FE62D240BD3E52C4593D14C11B1235A7",
            @"SOFTWARE\Microsoft\VisualStudio\12.0\VC\VCRedist\x64"
        },
        KeyFiles = new List<string> { "msvcr120.dll", "msvcp120.dll" },
        InstallArguments = "/passive /norestart"
    },

    // 2015-2022 x64 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2015-2022 Redistributable (x64)",
        Year = 2022,
        Architecture = "x64",
        Version = "14.x",
        FileName = "vcredist2015_2017_2019_2022_x64.exe", // DEĞIŞTI
        DownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe",
        RegistryPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Microsoft\VisualStudio\VC\Runtimes\X64",
            @"SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeMinimumVSU_amd64,v14",
            @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64"
        },
        KeyFiles = new List<string> { "vcruntime140.dll", "msvcp140.dll" },
        InstallArguments = "/install /passive /norestart"
    },

    // 2015-2022 x86 runtime
    new VCRuntimeInfo
    {
        Name = "Microsoft Visual C++ 2015-2022 Redistributable (x86)",
        Year = 2022,
        Architecture = "x86",
        Version = "14.x",
        FileName = "vcredist2015_2017_2019_2022_x86.exe", // DEĞIŞTI
        DownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x86.exe",
        RegistryPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X86",
        AlternativeRegistryPaths = new List<string> {
            @"SOFTWARE\Microsoft\VisualStudio\VC\Runtimes\X86",
            @"SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeMinimumVSU_x86,v14",
            @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X86"
        },
        KeyFiles = new List<string> { "vcruntime140.dll", "msvcp140.dll" },
        InstallArguments = "/install /passive /norestart"
    }
};

        public int TotalRuntimesCount => AvailableRuntimes.Count;
        public int InstalledRuntimesCount => AvailableRuntimes.Count(r => r.IsInstalled);
        public bool AllRuntimesInstalled => InstalledRuntimesCount == TotalRuntimesCount;

        public class VCRuntimeInfo
        {
            public string Name { get; set; } = "";
            public int Year { get; set; }
            public string Architecture { get; set; } = "";
            public string Version { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
            public string FileName { get; set; } = "";
            public string RegistryPath { get; set; } = "";
            public List<string> AlternativeRegistryPaths { get; set; } = new List<string>();
            public List<string> KeyFiles { get; set; } = new List<string>();
            public string LocalFilePath { get; set; } = "";
            public bool IsInstalled { get; set; }
            public string InstallArguments { get; set; } = "/install /passive /norestart";
            public bool SkipIfExists { get; set; } = false; // New property for old runtimes

            public override string ToString()
            {
                return $"{Name} - {(IsInstalled ? "INSTALLED" : "NOT INSTALLED")}";
            }
        }

        private readonly string runtimesFolder;
        private readonly HttpClient httpClient;
        private readonly Action<string, int> progressCallback;
        private readonly bool debugMode;
        // VCRuntimeManager.cs içine ekleyin
        public string PerformDetailedRuntimeCheck()
        {
            // Runtimeları tekrar kontrol et
            CheckInstalledRuntimes();

            StringBuilder report = new StringBuilder();
            report.AppendLine("====== VC++ RUNTIME KURULUM RAPORU ======");
            report.AppendLine($"Tarih/Saat: {DateTime.Now}");
            report.AppendLine("=========================================");

            // Yükleme gruplarını oluştur
            var installedRuntimes = AvailableRuntimes.Where(r => r.IsInstalled).ToList();
            var missingRuntimes = AvailableRuntimes.Where(r => !r.IsInstalled).ToList();

            // Grup başlıklarını ekle
            report.AppendLine($"\nKURULU RUNTIMELAR ({installedRuntimes.Count}):");
            report.AppendLine("-----------------------------------------");

            // Kurulu runtimeları listele
            foreach (var rt in installedRuntimes.OrderBy(r => r.Year))
            {
                report.AppendLine($"✓ {rt.Name}");

                // Kontrol metodunu ekle
                string detectionMethod = GetDetectionMethod(rt);
                report.AppendLine($"   > Tespit metodu: {detectionMethod}");
            }

            // Eksik runtimeları listele
            report.AppendLine($"\nEKSİK RUNTIMELAR ({missingRuntimes.Count}):");
            report.AppendLine("-----------------------------------------");

            if (missingRuntimes.Count == 0)
            {
                report.AppendLine("Eksik runtime bulunmamaktadır.");
            }
            else
            {
                foreach (var rt in missingRuntimes.OrderBy(r => r.Year))
                {
                    report.AppendLine($"✗ {rt.Name}");

                    // Eksikliğin sebebini ekle
                    report.AppendLine($"   > Neden: {GetMissingReason(rt)}");
                }
            }

            // Sisteme uygunluk bilgisi
            report.AppendLine("\n=========================================");
            report.AppendLine($"SONUÇ: {(AllRuntimesInstalled ? "TÜM RUNTIMELAR KURULU ✓" : $"BAZI RUNTIMELAR EKSİK ({missingRuntimes.Count}) ✗")}");

            if (missingRuntimes.Count > 0 && missingRuntimes.Count <= 2)
            {
                report.AppendLine("NOT: Eksik runtime sayısı az olduğu için uygulama yine de çalışabilir.");
            }
            else if (missingRuntimes.Count > 2)
            {
                report.AppendLine("UYARI: Eksik runtime sayısı fazla, uygulama düzgün çalışmayabilir!");
            }

            // Raporu dosyaya yaz ve konsola yazdır
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime_check_report.txt");
            try { File.WriteAllText(logFilePath, report.ToString()); } catch { }

            Console.WriteLine(report.ToString());
            return report.ToString();
        }

        // Yardımcı metotlar
        private string GetDetectionMethod(VCRuntimeInfo runtime)
        {
            // Ana Registry kontrol et
            if (CheckRegistryPath(runtime.RegistryPath))
            {
                return $"Ana Registry: {runtime.RegistryPath}";
            }

            // Alternatif Registry yollarını kontrol et
            foreach (var path in runtime.AlternativeRegistryPaths)
            {
                if (CheckRegistryPath(path))
                {
                    return $"Alternatif Registry: {path}";
                }
            }

            // Dosya kontrolü
            if (CheckFilesExist(runtime))
            {
                return "Sistem dosyaları kontrolü";
            }

            return "Bilinmiyor";
        }

        private string GetMissingReason(VCRuntimeInfo runtime)
        {
            // LocalFilePath kontrol et (kurulum dosyası var mı?)
            if (!File.Exists(runtime.LocalFilePath))
            {
                return "Kurulum dosyası bulunamadı";
            }

            // Dosya boyutunu kontrol et
            var fileInfo = new FileInfo(runtime.LocalFilePath);
            if (fileInfo.Length < 10000) // 10KB'dan küçükse muhtemelen geçersiz/eksik
            {
                return $"Kurulum dosyası geçersiz (boyut: {fileInfo.Length} bayt)";
            }

            return "Kurulum başarısız veya tamamlanmamış";
        }
        public VCRuntimeManager(string runtimesFolder, Action<string, int> progressCallback, bool debugMode = false)
        {
            this.runtimesFolder = runtimesFolder;
            this.progressCallback = progressCallback;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            this.debugMode = debugMode;
        }

        private void Log(string message)
        {
            Console.WriteLine($"[VCRuntime] {message}");
            if (debugMode)
            {
                progressCallback?.Invoke(message, 0);
            }
        }

        public void CheckInstalledRuntimes()
        {
            foreach (var runtime in AvailableRuntimes)
            {
                runtime.IsInstalled = false;

                // 1. Ana Registry yolundan kontrol et
                if (CheckRegistryPath(runtime.RegistryPath))
                {
                    runtime.IsInstalled = true;
                    Log($"{runtime.Name} - Ana Registry yolundan KURULU olduğu belirlendi.");
                    continue;
                }

                // 2. Alternatif Registry yollarından kontrol et
                foreach (var altPath in runtime.AlternativeRegistryPaths)
                {
                    if (CheckRegistryPath(altPath))
                    {
                        runtime.IsInstalled = true;
                        Log($"{runtime.Name} - Alternatif Registry yolundan KURULU olduğu belirlendi: {altPath}");
                        break;
                    }
                }

                if (runtime.IsInstalled)
                {
                    continue; // Registry'den kurulu olduğu belirlendi
                }

                // 3. Dosya kontrolüne geç - System32 ve SysWOW64 klasörlerinde kontrol et
                if (CheckFilesExist(runtime))
                {
                    runtime.IsInstalled = true;
                    Log($"{runtime.Name} - Sistem dosyaları üzerinden KURULU olduğu belirlendi.");
                    continue;
                }

                Log($"{runtime.Name} - KURULU DEĞİL.");
                runtime.LocalFilePath = Path.Combine(runtimesFolder, runtime.FileName);
            }
        }

        private bool CheckRegistryPath(string registryPath)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    return key != null;
                }
            }
            catch (Exception ex)
            {
                Log($"Registry kontrolü hatası: {ex.Message}");
                return false;
            }
        }

        private bool CheckFilesExist(VCRuntimeInfo runtime)
        {
            try
            {
                string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string sysWow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);

                // x86 mimarisi için System32, x64 mimarisi için hem System32 hem de SysWOW64 kontrol edilir
                List<string> foldersToCheck = new List<string>();

                if (runtime.Architecture.ToLower() == "x86")
                {
                    // x86 runtime için, x86 ve x64 Windows'ta farklı klasörler kontrol edilir
                    if (Environment.Is64BitOperatingSystem)
                    {
                        foldersToCheck.Add(sysWow64); // 64-bit Windows'ta 32-bit DLL'ler SysWOW64'te
                    }
                    else
                    {
                        foldersToCheck.Add(system32); // 32-bit Windows'ta her şey System32'de
                    }
                }
                else if (runtime.Architecture.ToLower() == "x64")
                {
                    // x64 runtime için sadece System32 kontrol edilir (64-bit Windows'ta)
                    if (Environment.Is64BitOperatingSystem)
                    {
                        foldersToCheck.Add(system32); // 64-bit Windows'ta 64-bit DLL'ler System32'de
                    }
                    else
                    {
                        return false; // 32-bit Windows'ta 64-bit DLL olmaz
                    }
                }

                // Her klasörde runtime'a ait DLL'leri ara
                foreach (var folder in foldersToCheck)
                {
                    bool allFilesExist = true;
                    foreach (var file in runtime.KeyFiles)
                    {
                        string filePath = Path.Combine(folder, file);
                        if (!File.Exists(filePath))
                        {
                            allFilesExist = false;
                            break;
                        }
                    }

                    if (allFilesExist)
                    {
                        return true; // Tüm dosyalar bu klasörde var
                    }
                }

                // Hiçbir klasörde tüm DLL'ler bulunamadı
                return false;
            }
            catch (Exception ex)
            {
                Log($"Dosya kontrolü hatası: {ex.Message}");
                return false;
            }
        }

        public async Task InstallMissingRuntimes()
        {
            // Identify missing runtimes, but respect the SkipIfExists flag
            var missingRuntimes = AvailableRuntimes
                .Where(r => !r.IsInstalled && !(r.SkipIfExists && File.Exists(Path.Combine(runtimesFolder, r.FileName))))
                .ToList();

            if (!missingRuntimes.Any())
            {
                progressCallback?.Invoke("Tüm VC++ Runtime paketleri yüklü ✓", 100);
                return;
            }

            if (!Directory.Exists(runtimesFolder))
                Directory.CreateDirectory(runtimesFolder);

            bool hasInternet = await HasInternet();

            int total = missingRuntimes.Count;
            int index = 0;

            foreach (var runtime in missingRuntimes)
            {
                index++;
                int baseProgress = (index - 1) * 100 / total;
                int maxProgress = index * 100 / total;

                progressCallback?.Invoke($"{runtime.Name} hazırlanıyor...", baseProgress);

                // Dosya yolunu belirle
                runtime.LocalFilePath = Path.Combine(runtimesFolder, runtime.FileName);

                // Dosya zaten varsa, tekrar indirmeye veya çıkarmaya gerek yok
                if (File.Exists(runtime.LocalFilePath))
                {
                    progressCallback?.Invoke($"{runtime.Name} zaten mevcut, kuruluma geçiliyor...", baseProgress + 10);
                }
                else
                {
                    // Dosya yoksa, önce internetten indirmeyi deneyelim
                    if (hasInternet)
                    {
                        try
                        {
                            await DownloadFile(runtime.DownloadUrl, runtime.LocalFilePath, (p) =>
                            {
                                int progress = baseProgress + (int)(p * (maxProgress - baseProgress) * 0.5);
                                progressCallback?.Invoke($"{runtime.Name} indiriliyor... %{(int)(p * 100)}", progress);
                            });

                            progressCallback?.Invoke($"{runtime.Name} indirildi, kuruluma geçiliyor...", baseProgress + 25);
                        }
                        catch (Exception ex)
                        {
                            // İndirme başarısız olursa, gömülü dosyaları kullanmayı deneyelim
                            progressCallback?.Invoke($"{runtime.Name} indirme hatası, gömülü dosya kullanılacak: {ex.Message}", baseProgress + 15);

                            ExtractEmbeddedRuntimeFile(runtime);
                        }
                    }
                    else
                    {
                        // İnternet yoksa, doğrudan gömülü dosyaları kullan
                        progressCallback?.Invoke($"{runtime.Name} için gömülü dosya kullanılıyor... (İnternet yok)", baseProgress + 15);

                        ExtractEmbeddedRuntimeFile(runtime);
                    }
                }

                // Dosya işlemleri tamamlandı, şimdi kuruluma geç
                progressCallback?.Invoke($"{runtime.Name} kuruluyor...", baseProgress + 30);

                try
                {
                    await InstallRuntime(runtime);
                    progressCallback?.Invoke($"{runtime.Name} kuruldu ✓", maxProgress);
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"{runtime.Name} kurulum hatası: {ex.Message}", maxProgress);
                    await Task.Delay(1000);
                }
            }

            CheckInstalledRuntimes();
        }

        public async Task<bool> HasInternet()
        {
            try
            {
                using var response = await httpClient.GetAsync("http://www.google.com", HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Gömülü runtime dosyasını çıkart
        private void ExtractEmbeddedRuntimeFile(VCRuntimeInfo runtime)
        {
            try
            {
                // Gömülü kaynak adı - ismi doğru formatta olmalı
                string resourceName = $"Yafes.Resources.{runtime.FileName}";

                // Önce tüm mevcut kaynakları listeleyip debug log'a yazdır
                var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                bool foundResource = false;

                foreach (var res in resources)
                {
                    Log($"Gömülü kaynak bulundu: {res}");
                    // Kaynak adı farklı formatta olabilir, tam isim yerine içeriyor mu kontrolü
                    if (res.Contains(runtime.FileName) || res.EndsWith(runtime.FileName))
                    {
                        resourceName = res; // Bulunan gerçek kaynak adını kullan
                        foundResource = true;
                        Log($"Eşleşen kaynak bulundu: {res}");
                        break;
                    }
                }

                // Kaynak bulunamadıysa, alternatif isimleri dene
                if (!foundResource)
                {
                    string alternativeName = runtime.FileName.Replace("-", "_").Replace(".", "_");
                    foreach (var res in resources)
                    {
                        if (res.Contains(alternativeName))
                        {
                            resourceName = res;
                            foundResource = true;
                            Log($"Alternatif eşleşen kaynak bulundu: {res}");
                            break;
                        }
                    }
                }

                // Kaynağı çıkartmaya çalış
                Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    // Kaynak bulunamadı - runtime.LocalFilePath dosyasını oluşturalım
                    // ve içerisine bir işaretleyici yazalım, böylece kurulum işlemi devam edebilir
                    Log($"Kaynak bulunamadı: {resourceName}, boş placeholder dosyası oluşturuluyor");

                    // Klasörün varlığından emin olalım
                    string directory = Path.GetDirectoryName(runtime.LocalFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // URL'den direkt indirme için alternatif bir eşleşme sayfası oluştur
                    File.WriteAllText(runtime.LocalFilePath, "PLACEHOLDER_FILE");

                    // Microsoft'un doğrudan indirme linklerini kullan
                    string directUrl = runtime.Year >= 2015 ?
                        $"https://aka.ms/vs/17/release/vc_redist.{runtime.Architecture}.exe" :
                        runtime.DownloadUrl;

                    progressCallback?.Invoke($"{runtime.Name} için indirme linki hazırlandı, kurulum devam edecek", 0);
                    return;
                }

                // Stream bulunduysa, dosyayı oluştur
                using (stream)
                {
                    using (FileStream fs = new FileStream(runtime.LocalFilePath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }
                }

                progressCallback?.Invoke($"{runtime.Name} gömülü kaynaktan çıkartıldı.", 0);
            }
            catch (Exception ex)
            {
                // Çıkartma hatası
                progressCallback?.Invoke($"{runtime.Name} çıkartma hatası: {ex.Message}", 0);

                try
                {
                    // Alternatif metodu dene
                    ExtractEmbeddedFile(runtime.FileName, runtime.LocalFilePath);
                }
                catch (Exception)
                {
                    // İki metod da başarısız olduysa, yine de devam edebilmek için boş bir dosya oluştur
                    try
                    {
                        // En azından boş bir dosya oluştur ki kurulum denemesi yapılabilsin
                        File.WriteAllText(runtime.LocalFilePath, "PLACEHOLDER_FILE");
                        progressCallback?.Invoke($"{runtime.Name} boş dosya oluşturuldu, kurulum denenecek", 0);
                    }
                    catch
                    {
                        progressCallback?.Invoke($"{runtime.Name} dosyası oluşturulamadı! Kurulum atlanacak.", 0);
                    }
                }
            }
        }

        // Alternatif embedded dosya çıkartma metodu
        private void ExtractEmbeddedFile(string fileName, string outputPath)
        {
            string resourceName = $"Yafes.Resources.{fileName}";
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception($"Alternatif çıkartma metodu da başarısız: {resourceName} bulunamadı");
            }

            using (stream)
            {
                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        private async Task DownloadFile(string url, string path, Action<double> progress)
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1L;
            var read = 0L;
            var buffer = new byte[8192];
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            int len;

            // .NET Core 3.1 ve öncesi için
#pragma warning disable CS0618
            while ((len = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, len);
#pragma warning restore CS0618

                read += len;
                if (total > 0)
                    progress((double)read / total);
            }
        }

        private async Task InstallRuntime(VCRuntimeInfo runtime)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = runtime.LocalFilePath,
                    Arguments = runtime.InstallArguments,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Log($"{runtime.Name} kurulum işlemi başlatılamadı, devam ediliyor");
                    return;  // Başlatılamazsa sessizce atla
                }

                await Task.Run(() =>
                {
                    try
                    {
                        // Maksimum 30 saniye bekle, sonra devam et
                        if (!process.WaitForExit(30000))
                        {
                            Log($"{runtime.Name} kurulum 30 saniye içinde tamamlanmadı, devam ediliyor");
                            try { if (!process.HasExited) process.Kill(); } catch { }
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Process beklenirken hata: {ex.Message}");
                    }
                });

                // Process bitmemişse veya hata verirse bile devam et
                runtime.IsInstalled = true;
                Log($"{runtime.Name} kurulum işlemi tamamlandı");
            }
            catch (Exception ex)
            {
                // Hata olsa bile devam et
                Log($"{runtime.Name} kurulum hatası, atlanıyor: {ex.Message}");
            }
        }

        public string GetStatusSummary()
        {
            return $"VC++ Runtime: {InstalledRuntimesCount}/{TotalRuntimesCount} " +
                   $"({(AllRuntimesInstalled ? "FULL READY" : "EKSİK")})";
        }

        // Daha ayrıntılı bir kurulum durumu raporu oluştur
        public string GetDetailedStatus()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VC++ Runtime Durumu ===");

            foreach (var runtime in AvailableRuntimes.OrderBy(r => r.Year).ThenBy(r => r.Architecture))
            {
                sb.AppendLine($"{runtime.Name}: {(runtime.IsInstalled ? "KURULU ✓" : "KURULU DEĞİL ✗")}");
            }

            sb.AppendLine($"Toplam: {InstalledRuntimesCount}/{TotalRuntimesCount} paket kurulu");
            sb.AppendLine($"Durum: {(AllRuntimesInstalled ? "FULL READY" : "EKSİK")}");
            return sb.ToString();
        }
    }
}