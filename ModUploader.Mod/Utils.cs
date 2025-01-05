using ColossalFramework.Plugins;
using IWshRuntimeLibrary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static ColossalFramework.Plugins.PluginManager;

namespace ModUploader
{
    internal static class Utils
    {
        /// <summary>
        /// Creates a windows Shortcut (.lnk)
        /// </summary>
        /// <param name="shortcutPath">Path of the Shortcut to create</param>
        /// <param name="targetPath">Reference Path of the Shortcut</param>
        public static void CreateShortcut(string shortcutPath, string targetPath, string arguments = "", string description = "")
        {
            var shell = new WshShell();

            try
            {
                var lnk = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                try
                {
                    lnk.WorkingDirectory = Directory.GetParent(targetPath).FullName;
                    lnk.TargetPath = targetPath;
                    lnk.Arguments = arguments;
                    lnk.Description = description;
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        /// <summary>
        /// Gets the mod directory file path of the currently executing mod assembly.
        /// </summary>
        public static string GetAssemblyPath()
        {
            // No path cached - get list of currently active plugins.
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            IEnumerable<PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

            // Iterate through list.
            foreach (PluginInfo plugin in plugins)
            {

                // Iterate through each assembly in plugins
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    if (assembly == thisAssembly)
                    {
                        return plugin.modPath;
                    }
                }

            }
            return null;
        }
    }
}