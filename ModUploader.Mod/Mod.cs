using System;
using System.IO;
using WillCommons;

namespace ModUploader
{
    public class Mod : ModBase
    {
        public override string BaseName => "Mod Uploader";
        public override string Description => "Upload your mods/assets to workshop easily and quickly without launching the game";

        public override void OnEnabled()
        {
            CreateModUploaderShortcut();
        }

        public void OnSettingsUI(UIHelper helper)
        {
            helper.AddButton("Create shortcut", CreateModUploaderShortcut);
        }
        private void CreateModUploaderShortcut()
        {
            try
            {
                Logging.Msg("Creating shortcut...");
                string exePath = Path.Combine(Utils.GetAssemblyPath(), "ModUploader.exe");
                string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ModUploader.lnk");
                Utils.CreateShortcut(shortcutPath, exePath, "", "ModUploader");
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }
    }
}
