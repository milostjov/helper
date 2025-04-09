using System;
using System.Collections.Generic;
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
    private static readonly string logPath = "C:\\ProgramData\\SystemInFor\\Helper_log.txt";
    private static readonly string blackList = "C:\\ProgramData\\SystemInFor\\blacklist.txt";

    private static readonly List<string> zabranjeniNaslovi = new List<string>();

    private static readonly string[] zabranjeniUrlovi = {
        "https://informer.rs",
        //"https://www.kurir.rs",
        //"https://www.alo.rs",
        //"https://www.novosti.rs",
        //"https://www.politika.rs",
        //"https://www.pink.rs",
        //"https://www.b92.net",
        //"https://www.telegraf.rs",
        //"https://www.sd.rs",
        //"https://www.espreso.co.rs/",
        //"https://www.pravda.rs",
        //"https://studiob.rs/",
        //"https://happytv.rs/",
        //"https://objektiv.rs/",
        //"https://www.republika.rs/",
        //"https://www.tanjug.rs"
    };

    static void Main()
    {
        try
        {
            foreach (var path in new[] { logPath, triggerPath, blackList })
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        catch
        {
            
        }



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
                    Log($"⏳ Proveravam RSS URL: {rssUrl}");

                    HttpResponseMessage response = await httpClient.GetAsync(rssUrl);
                    string contentType = response.Content.Headers.ContentType.MediaType;
                    string content = await response.Content.ReadAsStringAsync();

                    Log($"📥 Dobijen content-type: {contentType}");

                    if (contentType.Contains("xml") || content.TrimStart().StartsWith("<rss") || content.Contains("<item>"))
                    {
                        Log($"✅ Detektovan validan RSS XML na: {rssUrl}");
                        await ParsirajRSS(content, httpClient);

                    }
                    else
                    {
                        Log($"❗ Nije validan XML – pokušavam da pronađem RSS linkove u HTML sadržaju sajta: {baseUrl}");
                        await PronadjiRSSLinkoveIZHtmla(httpClient, baseUrl, content);
                    }
                }
                catch (Exception ex)
                {
                    Log($"💥 Greška kod '{baseUrl}': {ex.Message}");
                }
            }

        }
        try
        {
            File.WriteAllLines(blackList, zabranjeniNaslovi);
            Log($"📝 Sačuvano {zabranjeniNaslovi.Count} naslova u blacklist.txt");
        }
        catch (Exception ex)
        {
            Log($"⚠️ Greška prilikom pisanja blacklist.txt: {ex.Message}");
        }
    }


    private static async Task PronadjiRSSLinkoveIZHtmla(HttpClient httpClient, string baseUrl, string html)
    {
        var rssLinks = new HashSet<string>();
        Log($"🔍 Tražim RSS linkove u HTML stranici: {baseUrl}");

        MatchCollection matches = Regex.Matches(html, "<a[^>]+href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);

        Log($"🔗 Pronađeno {matches.Count} <a> linkova u HTML-u.");

        foreach (Match match in matches)
        {
            string href = match.Groups[1].Value;

            if (href.Contains("rss") || href.EndsWith(".xml") || href.Contains("feed"))
            {
                string fullUrl = href.StartsWith("http") ? href : baseUrl.TrimEnd('/') + "/" + href.TrimStart('/');

                if (!rssLinks.Add(fullUrl))
                    continue; // već obrađeno

                Log($"➡️ Pokušavam da učitam RSS link: {fullUrl}");

                try
                {
                    string rssContent = await httpClient.GetStringAsync(fullUrl);

                    if (rssContent.Contains("<item>"))
                    {
                        Log($"✅ RSS validan: {fullUrl}");
                        await ParsirajRSS(rssContent, httpClient);

                    }
                    else
                    {
                        Log($"⚠️ Učitano ali ne sadrži <item>: {fullUrl}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Ne mogu da učitam RSS sa: {fullUrl} | Greška: {ex.Message}");
                }
            }
        }
    }

    private static async Task<string> PreuzmiHtmlTitleAsync(string url, HttpClient httpClient)
    {
        try
        {
            string html = await httpClient.GetStringAsync(url);
            Match match = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (match.Success)
            {
                string title = match.Groups[1].Value.Trim();
                Log($"🌐 Preuzet <title> sa stranice: {title}");
                return title;
            }
            else
            {
                Log($"⚠️ Nema <title> na stranici: {url}");
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Greška prilikom preuzimanja HTML title sa {url}: {ex.Message}");
        }

        return null;
    }


    private static async Task ParsirajRSS(string rssContent, HttpClient httpClient)
    {
        var xmlDoc = new System.Xml.XmlDocument();
        try
        {
            xmlDoc.LoadXml(rssContent);
            var itemNodes = xmlDoc.SelectNodes("//item");

            if (itemNodes == null || itemNodes.Count == 0)
            {
                Log("⚠️ RSS sadržaj ne sadrži nijedan <item>.");
                return;
            }

            Log($"📄 RSS sadrži {itemNodes.Count} <item> elemenata.");

            bool koristiRSS = true;
            int dodato = 0;

            // ✨ Provera na prvom članku
            try
            {
                var prviItem = itemNodes[0];
                string rssTitle = prviItem["title"]?.InnerText?.Trim();
                string link = prviItem["link"]?.InnerText?.Trim();
                string htmlTitle = await PreuzmiHtmlTitleAsync(link, httpClient);

                if (!string.IsNullOrWhiteSpace(rssTitle) && !string.IsNullOrWhiteSpace(htmlTitle))
                {
                    string a = rssTitle.ToLower();
                    string b = htmlTitle.ToLower();

                    koristiRSS = (a == b);

                    Log(koristiRSS
                        ? "🔁 RSS i HTML title su identični — koristićemo RSS naslove."
                        : "🔁 RSS i HTML title se razlikuju — koristićemo naslove sa stranice.");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Provera identičnosti naslovâ nije uspela: {ex.Message}");
            }

            // ⏬ Glavna petlja
            foreach (System.Xml.XmlNode item in itemNodes)
            {
                string naslovZaDodavanje = null;

                if (koristiRSS)
                {
                    naslovZaDodavanje = item["title"]?.InnerText?.Trim().ToLower();
                }
                else
                {
                    string link = item["link"]?.InnerText?.Trim();
                    string htmlTitle = await PreuzmiHtmlTitleAsync(link, httpClient);
                    naslovZaDodavanje = htmlTitle?.Trim().ToLower();
                }

                if (!string.IsNullOrWhiteSpace(naslovZaDodavanje) && !zabranjeniNaslovi.Contains(naslovZaDodavanje))
                {
                    zabranjeniNaslovi.Add(naslovZaDodavanje);
                    dodato++;
                    //Log($"➕ Dodajem naslov: {naslovZaDodavanje}");
                }
            }

            Log($"✅ Ukupno dodato naslova iz ovog RSS-a: {dodato}");
        }
        catch (Exception ex)
        {
            Log($"❌ Greška prilikom parsiranja RSS-a: {ex.Message}");
        }
    }


    private static bool NaslovJeSlican(string a, string b)
    {
        string Normalize(string s) =>
            Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9čćžšđа-яё\s]", "").Replace("  ", " ");

        var setA = new HashSet<string>(Normalize(a).Split(' '));
        var setB = new HashSet<string>(Normalize(b).Split(' '));

        int zajednicke = 0;
        foreach (var rec in setA)
            if (setB.Contains(rec))
                zajednicke++;

        return zajednicke >= 5; // prag - možeš menjati
    }




    private static void Run()
    {
        Thread.Sleep(TimeSpan.FromMinutes(2));

        HashSet<string> blacklist = new HashSet<string>();
        try
        {
            if (File.Exists(blackList))
            {
                string[] lines = File.ReadAllLines(blackList);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        blacklist.Add(line.Trim().ToLower());
                }
                Log($"🧠 Učitano {blacklist.Count} naslova iz blacklist fajla.");
            }
            else
            {
                Log("⚠️ blacklist.txt nije pronađen.");
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Greška pri učitavanju blacklist.txt: {ex.Message}");
        }

        while (true)
        {
            try
            {
                bool found = false;
                string foundTitle = "";

                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;

                    StringBuilder sb = new StringBuilder(2048); // veći buffer
                    if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
                    {
                        string title = sb.ToString().ToLower();

                        if (string.IsNullOrWhiteSpace(title)) return true;

                        foreach (string zabranjen in blacklist)
                        {
                            if (title.Contains(zabranjen) || zabranjen.Contains(title) || NaslovJeSlican(title, zabranjen))
                            {
                                found = true;
                                foundTitle = title;
                                Log($"🔎 Prozor: {title}");

                                return false; // prekini pretragu
                            }
                        }
                    }

                    return true;
                }, IntPtr.Zero);

                if (found)
                {
                    string poruka = $"{DateTime.Now} - Detektovan zabranjeni prozor: {foundTitle}";
                    File.WriteAllText(triggerPath, poruka);
                    Log($"🚫 {poruka}");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Greška u Run: {ex.Message}");
            }

            Thread.Sleep(10000);
        }
    }


    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message + Environment.NewLine);
        }
        catch { }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
}
