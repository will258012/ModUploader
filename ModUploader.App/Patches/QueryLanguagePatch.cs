using HarmonyLib;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
namespace ModUploader.Patches;

[HarmonyPatch]
//Temporary Fix for https://github.com/Facepunch/Facepunch.Steamworks/issues/525
internal static class QueryLanguagePatch
{
    internal static bool IsDescriptionEditionDisabled;
    [HarmonyPatch(typeof(Query), "ApplyReturns")]
    private static void Postfix(Query __instance, object handle)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var language = traverse.Field<string>("language")?.Value;
            if (traverse.Field<string>("language")?.Value != null)
            {
                var steamUgcType = typeof(SteamUGC);
                var internalProp = steamUgcType.GetProperty("Internal", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (internalProp == null)
                    return;

                var steamUgcInstance = internalProp.GetValue(null);
                if (steamUgcInstance == null)
                    return;

                var setLangMethod = steamUgcInstance.GetType().GetMethod("SetLanguage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (setLangMethod == null)
                    return;

                setLangMethod.Invoke(steamUgcInstance, new object[] { handle, language });
            }
        }
        catch (Exception e)
        {
            App.Logger.Error(e);
            IsDescriptionEditionDisabled = true;
        }
    }
}
