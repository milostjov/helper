using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly string triggerPath = "C:\\ProgramData\\SystemInFor\\trigger.txt";
    private static readonly List<string> zabranjeniNaslovi = new List<string>();

    private static readonly string[] zabranjeniUrlovi = {
        "https://informer.rs",
        "https://www.kurir.rs",
        "https://www.alo.rs",
        "https://www.novosti.rs",
        "https://www.politika.rs",
        "https://www.pink.rs",
        "https://www.b92.net",
        "https://www.telegraf.rs",
        "https://www.sd.rs",
        "https://www.espreso.co.rs/",
        "https://www.pravda.rs",
        "https://studiob.rs/",
        "https://happytv.rs/",
        "https://objektiv.rs/",
        "https://www.republika.rs/",
        "https://www.tanjug.rs"
    };

    static void Main()
    {
        try
        {
            if (File.Exists(triggerPath))
                File.Delete(triggerPath);
        }
        catch { }

        Directory.CreateDirectory("C:\\ProgramData\\SystemInFor");

        ProveriNasloveSaSajtaAsync().GetAwaiter().GetResult();

        var thread = new Thread(new ThreadStart(Run));
        thread.IsBackground = true;
        thread.Start();

        Thread.Sleep(Timeout.Infinite);
    }

    private static async Task ProveriNasloveSaSajtaAsync()
    {
        using (var httpClient = new HttpClient())
        {
            foreach (string url in zabranjeniUrlovi)
            {
                try
                {
                    string html = await httpClient.GetStringAsync(url);
                    Match match = Regex.Match(html, "<title>\\s*(.+?)\\s*</title>", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string title = match.Groups[1].Value.ToLower();
                        zabranjeniNaslovi.Add(title);
                    }
                }
                catch { }
            }
        }
    }

    private static void Run()
    {
        Thread.Sleep(TimeSpan.FromMinutes(15));
        while (true)
        {
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    StringBuilder sb = new StringBuilder(256);
                    if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
                    {
                        string title = sb.ToString().ToLower();
                        foreach (string zabranjenNaslov in zabranjeniNaslovi)
                        {
                            if (title.Contains(zabranjenNaslov))
                            {
                                File.WriteAllText(triggerPath, DateTime.Now + " - Detektovan zabranjeni prozor: " + title);
                                return false;
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            Thread.Sleep(5000);
        }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
}
