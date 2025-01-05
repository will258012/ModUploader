using System;
using System.IO;
using WillCommons;

namespace ModUploader
{
    public class Mod : ModBase
    {
        public override string BaseName => "Mod Uploader";
        public override string Description => "Create and update your mods without launching the game";

        public override void OnEnabled()
        {
            CreateModUploaderShortcut();
        }

        public void OnSettingsUI(UIHelper helper)
        {
            helper.AddButton("Create shout cut", CreateModUploaderShortcut);
        }
        private void CreateModUploaderShortcut()
        {
            try
            {
                Logging.Msg("Creating shout cut...");
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
