using ModUploader.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
