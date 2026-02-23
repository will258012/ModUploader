using ModUploader.Resources;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ModUploader
{
    internal static class ResultLocalize
    {
        /// <summary>
        /// <see cref="https://partner.steamgames.com/doc/api/steam_api"/>
        /// </summary>
        /// <param name="result">result.</param>
        /// <returns></returns>
        internal static string ToLocalizedString(this Result result) => Resource_EResult.ResourceManager.GetString("k_EResult" + result.ToString()) ?? result.ToString();
    }
    public static class Utils
    {
        public static bool Ping(string url)
        {
            try
            {
                using var client = new HttpClient();
                var response = client.GetAsync(url).Result;
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsIconic(IntPtr hWnd);
        public static void ActivateExistingWindow()
        {
            var currentProcess = Process.GetCurrentProcess();
            string processName = Process.GetCurrentProcess().ProcessName;
            var process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.Id != currentProcess.Id);
            IntPtr hWnd = process.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                if (IsIconic(hWnd))
                    ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }
        }
    }
}
