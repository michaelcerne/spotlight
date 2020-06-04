using System;
using System.Timers;

using CitizenFX.Core;
using API = CitizenFX.Core.Native.API;
using System.Net.Http;
using System.Reflection;

namespace UpdateAnnouncer
{
    public class UpdateAnnouncer : BaseScript
    {
        private const double INTERVAL_MS = 6 * 60 * 60 * 1000; // 6 hours
        private const string RELEASES_URI = "http://www.michaelcerne.com/dc/tls-not-enforced-do-not-trust/versions/fivem_spotlight.php";

        private static readonly HttpClientHandler handler = new HttpClientHandler();
        private static readonly HttpClient client = new HttpClient();
        private static readonly Version current = Assembly.GetExecutingAssembly().GetName().Version;

        public UpdateAnnouncer()
        {
            EventHandlers["onResourceStart"] += new Action<string>(OnResourceStart);
        }

        private void OnResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;

            handler.AllowAutoRedirect = false;
            client.DefaultRequestHeaders.Add("User-Agent", "FivemSpotlight/" + current.ToString());

            CheckForUpdatesAndReport(null, null); // call immediately
            ConfigureTimer(); // and then every INTERVAL_MS
        }

        private void ConfigureTimer()
        {
            Timer timer = new Timer(INTERVAL_MS);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(CheckForUpdatesAndReport);
            timer.Enabled = true;
        }

        public async void CheckForUpdatesAndReport(object source, ElapsedEventArgs e)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(RELEASES_URI);
                response.EnsureSuccessStatusCode();
                Version latest = new Version((await response.Content.ReadAsStringAsync() + ".0").TrimStart('v'));

                int test = current.CompareTo(latest);

                if (test < 0)
                    VersionNotify(false);
            }
            catch (Exception)
            {
                Debug.WriteLine("Could not get new Spotlight releases.");
            }
        }

        public void VersionNotify(bool prerelease)
        {
            Debug.Write("\n^1---\n-\n- ");
            Debug.Write(prerelease ? "^1This is a prerelease Spotlight build. Expect bugs." : "^1An update is available for Spotlight!\n- Get it at: https://github.com/michaelcerne/spotlight/releases\n- or: https://gtapolicemods.com/index.php?/files/file/1150-spotlight-enhanced-sync");
            Debug.Write("^1\n-\n---\n");
        }
    }
}
