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
            foreach (string baseUrl in zabranjeniUrlovi)
            {
                try
                {
                    string rssUrl = baseUrl.TrimEnd('/') + "/rss";
                    HttpResponseMessage response = await httpClient.GetAsync(rssUrl);
                    string contentType = response.Content.Headers.ContentType.MediaType;

                    string content = await response.Content.ReadAsStringAsync();

                    if (contentType.Contains("xml") || content.TrimStart().StartsWith("<rss") || content.Contains("<item>"))
                    {
                        await ParsirajRSS(content);
                    }
                    else
                    {
                        await PronadjiRSSLinkoveIZHtmla(httpClient, baseUrl, content);
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"Greška kod {baseUrl}: {ex.Message}");
                }
            }
        }
    }

    private static async Task PronadjiRSSLinkoveIZHtmla(HttpClient httpClient, string baseUrl, string html)
    {
        var rssLinks = new HashSet<string>();
        MatchCollection matches = Regex.Matches(html, "<a[^>]+href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            string href = match.Groups[1].Value;

            if (href.Contains("rss") || href.EndsWith(".xml") || href.Contains("feed"))
            {
                string fullUrl = href.StartsWith("http") ? href : baseUrl.TrimEnd('/') + "/" + href.TrimStart('/');

                try
                {
                    string rssContent = await httpClient.GetStringAsync(fullUrl);
                    if (rssContent.Contains("<item>"))
                    {
                        await ParsirajRSS(rssContent);
                    }
                }
                catch
                {
                    // skip ako ne može da se skine/parsira
                }
            }
        }
    }

    private static async Task ParsirajRSS(string rssContent)
    {
        var xmlDoc = new System.Xml.XmlDocument();
        try
        {
            xmlDoc.LoadXml(rssContent);
            var itemNodes = xmlDoc.SelectNodes("//item");

            if (itemNodes == null) return;

            foreach (System.Xml.XmlNode item in itemNodes)
            {
                string title = item["title"]?.InnerText?.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(title) && !zabranjeniNaslovi.Contains(title))
                {
                    zabranjeniNaslovi.Add(title);
                }
            }
        }
        catch
        {
            // nije validan XML
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
