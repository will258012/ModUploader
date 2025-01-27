using ModUploader.Resources;

namespace ModUploader
{
    internal static class EResultLocalize
    {
        /// <summary>
        /// <see cref="https://partner.steamgames.com/doc/api/steam_api"/>
        /// </summary>
        /// <param name="eResult">result.</param>
        /// <returns></returns>
        internal static string ToLocalizedString(this EResult eResult) => Resource_EResult.ResourceManager.GetString(eResult.ToString()) ?? eResult.ToString();
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
    }
}
