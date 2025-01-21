using Microsoft.WindowsAPICodePack.Dialogs;

namespace ModUploader
{
    public class Program
    {
        public static Logger Logger { get; } = LogManager.GetLogger("ModUploader");
        [STAThread]
        public static void Main(string[] args)
        {
            Console.Title = "Mod Uploader";
            _ = new Mutex(true, "Global\\ModUploader", out var createdNew);
            if (!createdNew)
            {
                TaskDialog.Show(null, Main_MultiInstances, Console.Title, TaskDialogStandardIcon.Error);
                return;
            }

            Logger.Info($"Mod Uploader started. v{Assembly.GetExecutingAssembly().GetName().Version}");
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Logger.Fatal($"Unhandled exception: {e.ExceptionObject}");
            Environment.SetEnvironmentVariable("SteamAppId", UploadHelper.CSL_APPID.ToString());
            try
            {
                if (!SteamAPI.Init())
                {
                    Logger.Fatal("Failed to initialize Steam API.");
                    Console.WriteLine("Steam must be running!");
                    return;
                }
                #region Command
                if (args.Length > 0)
                {
                    Logger.Info($"Given args: {string.Join(", ", args.Select(arg => $"\"{arg}\""))}");
                    if (args.Contains("-help"))
                    {
                        Console.WriteLine(Command_Help);
                        return;
                    }
                    else if (args.Contains("-update"))
                    {
                        int index = Array.IndexOf(args, "-update");
                        if (index + 2 < args.Length && uint.TryParse(args[index + 1], out var workshopId))
                        {
                            string contentPath = args[index + 2];
                            string? previewImagePath = index + 3 < args.Length ? args[index + 3] : null;
                            UpdateMod(workshopId, contentPath, previewImagePath);
                            return;
                        }
                    }
                    else if (args.Contains("-newmod"))
                    {
                        int index = Array.IndexOf(args, "-newmod");
                        if (index + 2 < args.Length)
                        {
                            string name = args[index + 1];
                            string contentPath = args[index + 2];
                            string? previewImagePath = index + 3 < args.Length ? args[index + 3] : null;
                            CreateMod(name, contentPath, previewImagePath);
                            return;
                        }
                    }
                    Console.Error.WriteLine(Command_Error);
                    Console.Error.WriteLine(Command_Help);
                    return;
                }
                #endregion
                #region Console Interface
                Console.Clear();
                Console.Write($@" __    __     ______     _____     __  __     ______   __         ______     ______     _____     ______     ______    
/\ ""-./  \   /\  __ \   /\  __-.  /\ \/\ \   /\  == \ /\ \       /\  __ \   /\  __ \   /\  __-.  /\  ___\   /\  == \   
\ \ \-./\ \  \ \ \/\ \  \ \ \/\ \ \ \ \_\ \  \ \  _-/ \ \ \____  \ \ \/\ \  \ \  __ \  \ \ \/\ \ \ \  __\   \ \  __<   
 \ \_\ \ \_\  \ \_____\  \ \____-  \ \_____\  \ \_\    \ \_____\  \ \_____\  \ \_\ \_\  \ \____-  \ \_____\  \ \_\ \_\ 
  \/_/  \/_/   \/_____/   \/____/   \/_____/   \/_/     \/_____/   \/_____/   \/_/\/_/   \/____/   \/_____/   \/_/ /_/ 
Version {Assembly.GetExecutingAssembly().GetName().Version}
=======================================================================================================================");
                while (true)
                {
                    Console.WriteLine("\n" + Main_Message);
                    var input = Console.ReadLine() ?? "";

                    switch (input.ToLower())
                    {
                        case "":
                            Console.Error.WriteLine(Main_InvalidInput);
                            break;
                        case "newmod":
                            CreateMod();
                            break;
                        case "mymod":
                            UploadHelper.Instance.GetModList();
                            break;
                        case "exit":
                            return;
                        default:
                            var newInput = input.ToLower().Split(' ');
                            if (uint.TryParse(newInput[0], out var workshopId))
                            {
                                UpdateMod(workshopId, input.Contains("-UpdatePreviewOnly", StringComparison.OrdinalIgnoreCase));
                            }
                            else if (TryExtractWorkshopIdFromURL(newInput[0], out var workshopId1))
                            {
                                UpdateMod(workshopId1, input.Contains("-UpdatePreviewOnly", StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                Console.Error.WriteLine(Main_InvalidInput);
                            }
                            break;
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                Logger.Fatal($"Unhandled exception: {e}");
            }
            finally
            {
                SteamAPI.Shutdown();
            }
        }

        /// <summary>
        /// Create a mod.
        /// </summary>
        private static void CreateMod()
        {
            try
            {
                Logger.Info($"Start mod creation");
                string? name;
                do
                {
                    Console.WriteLine(Main_NewMod_EnterTitle);
                    name = Console.ReadLine();
                } while (string.IsNullOrWhiteSpace(name));

                Console.WriteLine(Main_NewMod_EnterDescription);
                var description = Console.ReadLine() ?? string.Empty;
                var mod = new ModInfo(name, description);

                UploadHelper.Instance.StartUpload(mod);
            }
            catch (Exception e)
            {
                Logger.Error(Main_CreateFail + e);
            }
        }
        /// <summary>
        /// Create a mod, use provided paths.
        /// </summary>
        /// <param name="title">Mod title.</param>
        /// <param name="contentPath">Mod content path.</param>
        /// <param name="previewImagePath">Mod preview image path. If null, will use the default image.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static void CreateMod(string title, string contentPath, string? previewImagePath)
        {
            try
            {
                Logger.Info($"Start mod creation: title:\"{title}\" contentPath:\"{contentPath}\" previewImagePath:\"{previewImagePath}\"");
                if (!Directory.Exists(contentPath) || (previewImagePath != null && !File.Exists(previewImagePath)))
                {
                    throw new DirectoryNotFoundException(Main_DirectoryNotFound);
                }
                var mod = new ModInfo(title, "");
                UploadHelper.Instance.StartUpload(mod, contentPath, previewImagePath);
            }
            catch (Exception e)
            {
                Logger.Error(Main_CreateFail + e);
            }
        }
        /// <summary>
        /// Update a mod.
        /// </summary>
        /// <param name="workshopId">Mod's workshop id.</param>
        private static void UpdateMod(uint workshopId, bool updatePreviewOnly)
        {
            try
            {
                Logger.Info($"Start mod update: workshopId:\"{workshopId}\" updatePreviewOnly:\"{updatePreviewOnly}\"");
                Console.WriteLine(Main_Update_Getinfo);
                var mod = new ModInfo(workshopId,updatePreviewOnly);
                Console.WriteLine($"{Main_Update_WillUpdate} {mod.Name} ({mod.PublishedFileId})");
                UploadHelper.Instance.StartUpload(mod);
            }
            catch (Exception e)
            {
                Logger.Error(Main_UpdateFail + e);
            }
        }
        /// <summary>
        /// Update a mod, use provided paths.
        /// </summary>
        /// <param name="workshopId">Mod's workshop id.</param>
        /// <param name="contentPath">Mod content path.</param>
        /// <param name="previewImagePath">Mod preview image path. If null, will not change the image.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static void UpdateMod(uint workshopId, string contentPath, string? previewImagePath)
        {
            try
            {
                Logger.Info($"Start mod update: workshopId:\"{workshopId}\" contentPath:\"{contentPath}\" previewImagePath:\"{previewImagePath}\"");
                if (!Directory.Exists(contentPath) || (previewImagePath != null && !File.Exists(previewImagePath)))
                {
                    throw new DirectoryNotFoundException(Main_DirectoryNotFound);
                }
                Console.WriteLine(Main_Update_Getinfo);
                var mod = new ModInfo(workshopId,false);
                Console.WriteLine($"{Main_Update_WillUpdate} {mod.Name} ({mod.PublishedFileId})");
                UploadHelper.Instance.StartUpload(mod, contentPath, previewImagePath);
            }
            catch (Exception e)
            {
                Logger.Error(Main_UpdateFail + e);
            }
        }
        private static bool TryExtractWorkshopIdFromURL(string URL, out uint workshopId)
        {
            workshopId = 0;
            try
            {
                Logger.Info($"Parsing URL: {URL}");
                var parsedUri = new Uri(URL);

                if (!parsedUri.Host.Contains("steamcommunity.com"))
                {
                    Console.Error.WriteLine("Given URL is not supported.");
                    return false;
                }
                var query = parsedUri.Query;
                var queryParams = System.Web.HttpUtility.ParseQueryString(query);

                var idString = queryParams["id"];
                if (string.IsNullOrWhiteSpace(idString) || !uint.TryParse(idString, out workshopId))
                {
                    Console.Error.WriteLine("Given URL has missing or invalid 'id' parameter.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to parse URI: {e}");
                return false;
            }
        }

    }
}