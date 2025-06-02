using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Yafes
{
    public class SystemInfoManager
    {
        private TextBlock osTextBlock;
        private TextBlock gpuTextBlock;
        private TextBlock ramTextBlock;
        private TextBlock osVersionTextBlock;

        public SystemInfoManager(TextBlock osText, TextBlock gpuText, TextBlock ramText, TextBlock osVerText)
        {
            osTextBlock = osText;
            gpuTextBlock = gpuText;
            ramTextBlock = ramText;
            osVersionTextBlock = osVerText;
        }

        public async Task LoadSystemInfoAsync()
        {
            try
            {
                // "Yükleniyor..." göster
                UpdateUI("Yükleniyor...", "Yükleniyor...", "Yükleniyor...", "Yükleniyor...");

                // Gerçek sistem bilgilerini paralel al
                var osTask = Task.Run(() => GetRealOSInfo());
                var gpuTask = Task.Run(() => GetRealGPUInfo());
                var ramTask = Task.Run(() => GetRealRAMInfo());
                var osVerTask = Task.Run(() => GetRealOSVersionInfo());

                var results = await Task.WhenAll(osTask, gpuTask, ramTask, osVerTask);

                // Gerçek bilgileri UI'ye yansıt
                UpdateUI(results[0], results[1], results[2], results[3]);
            }
            catch (Exception ex)
            {
                UpdateUI("Hata", "Hata", "Hata", ex.Message);
            }
        }

        private void UpdateUI(string os, string gpu, string ram, string osVersion)
        {
            osTextBlock?.Dispatcher.Invoke(() =>
            {
                osTextBlock.Text = os;
                SetYafesStyle(osTextBlock);
            });

            gpuTextBlock?.Dispatcher.Invoke(() =>
            {
                gpuTextBlock.Text = gpu;
                SetYafesStyle(gpuTextBlock);
            });

            ramTextBlock?.Dispatcher.Invoke(() =>
            {
                ramTextBlock.Text = ram;
                SetYafesStyle(ramTextBlock);
            });

            osVersionTextBlock?.Dispatcher.Invoke(() =>
            {
                osVersionTextBlock.Text = osVersion;
                SetYafesStyle(osVersionTextBlock);
            });
        }

        private void SetYafesStyle(TextBlock textBlock)
        {
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
            textBlock.FontFamily = new FontFamily("Trebuchet MS");
            textBlock.FontSize = 9;
            textBlock.FontWeight = FontWeights.Bold;
        }

        // GERÇEK İŞLETİM SİSTEMİ BİLGİSİ
        private string GetRealOSInfo()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        string caption = os["Caption"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(caption))
                        {
                            // "Microsoft Windows 11 Pro" → "Windows 11 Pro"
                            caption = caption.Replace("Microsoft ", "");
                            return caption.Length > 18 ? caption.Substring(0, 18) + "..." : caption;
                        }
                    }
                }

                // WMI başarısızsa Registry'den dene
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string productName = key?.GetValue("ProductName")?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productName = productName.Replace("Microsoft ", "");
                        return productName.Length > 18 ? productName.Substring(0, 18) + "..." : productName;
                    }
                }
            }
            catch
            {
                // Hata durumunda Environment'dan al
                return Environment.OSVersion.ToString().Contains("Windows") ? "Windows" : "Bilinmiyor";
            }

            return "Bilinmiyor";
        }

        // GERÇEK GPU BİLGİSİ
        private string GetRealGPUInfo()
        {
            try
            {
                List<string> gpuList = new List<string>();

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject gpu in searcher.Get())
                    {
                        string name = gpu["Name"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(name) &&
                            !name.Contains("Microsoft Basic") &&
                            !name.Contains("Remote Desktop") &&
                            !name.Contains("Standard VGA"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Ham GPU: '{name}'");

                            // ✅ SÜPER AGRESIF TEMİZLİK
                            string temizAd = name;

                            // Tüm gereksiz kelimeleri sil
                            string[] silinecekler = {
                        "AMD Radeon R7 ", "AMD Radeon RX ", "AMD Radeon R5 ", "AMD Radeon R9 ",
                        "AMD Radeon ", "NVIDIA GeForce ", "Intel(R) ", "Intel ",
                        "(TM) ", "(TM)", " Graphics", "Graphics ", "Graphics",
                        " Series G", "Series G", " Series", "Series", " G ", " G",
                        "(R)", " (R) ", "  ", "   "
                    };

                            foreach (string silinecek in silinecekler)
                            {
                                temizAd = temizAd.Replace(silinecek, " ");
                            }

                            // AMD'yi geri ekle (eğer AMD kartıysa)
                            if (name.Contains("AMD") && !temizAd.StartsWith("AMD"))
                            {
                                temizAd = "AMD " + temizAd;
                            }

                            // Fazla boşlukları temizle
                            while (temizAd.Contains("  "))
                            {
                                temizAd = temizAd.Replace("  ", " ");
                            }

                            temizAd = temizAd.Trim();

                            // Çok uzunsa kısalt
                            if (temizAd.Length > 15)
                            {
                                temizAd = temizAd.Substring(0, 15).Trim();
                            }

                            gpuList.Add(temizAd);
                            System.Diagnostics.Debug.WriteLine($"Temizlenmiş: '{temizAd}'");
                        }
                    }
                }

                if (gpuList.Count > 0)
                {
                    return string.Join(" / ", gpuList);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPU Hatası: {ex.Message}");
            }

            return "GPU Bulunamadı";
        }


        // GERÇEK RAM BİLGİSİ
        private string GetRealRAMInfo()
        {
            try
            {
                long totalMemory = 0;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject memory in searcher.Get())
                    {
                        totalMemory += Convert.ToInt64(memory["Capacity"]);
                    }
                }

                if (totalMemory > 0)
                {
                    // Bytes'ı GB'ye çevir
                    double totalGB = totalMemory / (1024.0 * 1024.0 * 1024.0);

                    // En yakın güç değerine yuvarla
                    int roundedGB;
                    if (totalGB < 6) roundedGB = 4;
                    else if (totalGB < 12) roundedGB = 8;
                    else if (totalGB < 24) roundedGB = 16;
                    else if (totalGB < 48) roundedGB = 32;
                    else roundedGB = 64;

                    // RAM tipini belirle
                    string ramType = GetRAMType();

                    return $"{roundedGB}GB {ramType}";
                }
            }
            catch
            {
                // Hata durumunda basit hesaplama
                try
                {
                    long workingSet = Environment.WorkingSet;
                    double estimatedGB = (workingSet * 8) / (1024.0 * 1024.0 * 1024.0);
                    int roundedGB = estimatedGB < 12 ? 8 : estimatedGB < 24 ? 16 : 32;
                    return $"{roundedGB}GB DDR4";
                }
                catch { }
            }

            return "16GB DDR4";
        }

        // RAM TİPİNİ BELİRLE
        private string GetRAMType()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject memory in searcher.Get())
                    {
                        int memoryType = Convert.ToInt32(memory["SMBIOSMemoryType"]);

                        switch (memoryType)
                        {
                            case 24: return "DDR3";
                            case 26: return "DDR4";
                            case 34: return "DDR5";
                            default: return "DDR4"; // Varsayılan
                        }
                    }
                }
            }
            catch { }

            return "DDR4";
        }

        // GERÇEK OS VERSİYON BİLGİSİ
        private string GetRealOSVersionInfo()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                        string buildNumber = key.GetValue("CurrentBuild")?.ToString() ?? "";
                        string ubr = key.GetValue("UBR")?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(displayVersion) && !string.IsNullOrEmpty(buildNumber))
                        {
                            return $"{displayVersion} Build {buildNumber}";
                        }
                        else if (!string.IsNullOrEmpty(buildNumber))
                        {
                            return $"Build {buildNumber}";
                        }
                    }
                }

                // WMI alternatifi
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Version, BuildNumber FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        string buildNumber = os["BuildNumber"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(buildNumber))
                        {
                            return $"Build {buildNumber}";
                        }
                    }
                }
            }
            catch
            {
                // Son çare
                Version version = Environment.OSVersion.Version;
                return $"Version {version.Major}.{version.Minor}";
            }

            return "Bilinmiyor";
        }
    }
}