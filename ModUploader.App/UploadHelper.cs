using Microsoft.WindowsAPICodePack.Dialogs;
using ShellProgressBar;
using System.Diagnostics;
namespace ModUploader
{
    public class UploadHelper
    {
        public static UploadHelper Instance { get; } = new UploadHelper();
        public const uint CSL_APPID = 255710U;
        const int MAX_RETRIES = 3;
        public static AppId_t CSL_APPLD_T => new AppId_t(255710U);
        public string CompatibleTag => _lazyCompatibleTag.Value;

        private AutoResetEvent isReady = new AutoResetEvent(false);
        private CreateItemResult_t createItemResult;
        private SubmitItemUpdateResult_t submitItemUpdateResult;
        private string currentContentFolderPath = "";
        private string? currentPreviewImageFilePath;
        private readonly Lazy<string> _lazyCompatibleTag = new Lazy<string>(GetCompatibleTag);
        private FileStream[] stream = new FileStream[2];
        /// <summary>
        /// Use provided paths to start an upload process.
        /// </summary>
        /// <param name="mod">mod info.</param>
        /// <param name="contentPath">Mod content path.</param>
        /// <param name="previewImagePath">Mod preview image path. If null, will not change the image.</param>
        public void StartUpload(ModInfo mod, string contentPath, string? previewImagePath)
        {
            if (mod.IsNewMod)
            {
                currentContentFolderPath = contentPath;
                currentPreviewImageFilePath = previewImagePath ?? CreatePreviewImageFile(Path.GetTempPath());
            }
            else
            {
                currentContentFolderPath = contentPath;
                currentPreviewImageFilePath = previewImagePath;
            }
            UploadImpl(mod);
        }

        /// <summary>
        /// Manual choose the paths to start an upload process.
        /// </summary>
        /// <param name="mod">mod info.</param>
        public void StartUpload(ModInfo mod)
        {
            ChooseFolder(mod);
            UploadImpl(mod);
        }

        private void UploadImpl(ModInfo mod)
        {
            if (mod.IsNewMod)// Create
            {
                int createMaxRetries = MAX_RETRIES;

                while (createMaxRetries > 0)
                {
                    try
                    {
                        CreateItem(ref mod);
                        createMaxRetries = 0; // Success  
                    }
                    catch (Exception e)
                    {
                        createMaxRetries--;
                        Console.WriteLine($"{Upload_Retry} ({MAX_RETRIES - createMaxRetries} / {MAX_RETRIES})");
                        if (createMaxRetries <= 0)
                            throw;
                        Program.Logger.Error(e);
                        Program.Logger.Info($"Start retry operation: ({MAX_RETRIES - createMaxRetries} / {MAX_RETRIES})");
                    }
                }
            }

            int UploadMaxRetries = MAX_RETRIES;
            VerifyAndOccupy();
            while (UploadMaxRetries > 0)// Upload
            {
                try
                {
                    UploadItem(mod);
                    UploadMaxRetries = 0; // Success
                    ReleaseOccupy();
                }
                catch (Exception e)
                {
                    UploadMaxRetries--;
                    Console.WriteLine($"{Upload_Retry} ({MAX_RETRIES - UploadMaxRetries} / {MAX_RETRIES})");
                    if (UploadMaxRetries <= 0)
                    {
                        ReleaseOccupy();
                        throw;
                    }
                    Program.Logger.Error(e);
                    Program.Logger.Info($"Start retry operation: ({MAX_RETRIES - UploadMaxRetries} / {MAX_RETRIES})");
                }
            }
        }

        /// <summary>
        /// Upload a workshop item.
        /// </summary>
        /// <param name="mod">Mod info.</param>
        /// <exception cref="Exception"></exception>
        private void UploadItem(ModInfo mod)
        {
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} upload started.");
            var handle = SteamUGC.StartItemUpdate(CSL_APPLD_T, mod.PublishedFileId_t);

            if (mod.IsNewMod)
            {
                SteamUGC.SetItemTitle(handle, mod.Name);
                SteamUGC.SetItemDescription(handle, mod.Description);
            }

            if (currentPreviewImageFilePath != null)
                SteamUGC.SetItemPreview(handle, currentPreviewImageFilePath);

            if (!mod.UpdatePreviewOnly)
                SteamUGC.SetItemContent(handle, currentContentFolderPath);

            SteamUGC.SetItemTags(handle, ["Mod", CompatibleTag]);

            var callback = SteamUGC.SubmitItemUpdate(handle, "");
            CallResult<SubmitItemUpdateResult_t>.Create(OnItemSubmitted).Set(callback);

            var progressOptions = new ProgressBarOptions
            {
                ProgressBarOnBottom = true,
                EnableTaskBarProgress = true,
                ForegroundColor = ConsoleColor.Blue,
                BackgroundColor = ConsoleColor.Gray,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                ForegroundColorError = ConsoleColor.DarkRed,
            };

            using var progressBar = new ProgressBar(100, Upload_Uploading, progressOptions);
            while (!isReady.WaitOne(50))
            {
                SteamAPI.RunCallbacks();
                var itemUpdateProgress = SteamUGC.GetItemUpdateProgress(handle, out var passed, out var total);

                if (total != 0 && itemUpdateProgress != EItemUpdateStatus.k_EItemUpdateStatusInvalid)
                {
                    progressBar.MaxTicks = (int)total;
                    progressBar.Tick((int)passed);
                }
            }

            progressBar.AsProgress<float>().Report(1);
            if (submitItemUpdateResult.m_eResult != EResult.k_EResultOK)
            {
                progressBar.ObservedError = true;
                throw new Exception($"{submitItemUpdateResult.m_eResult.ToLocalizedString()} ({submitItemUpdateResult.m_eResult})");
            }

            Console.WriteLine(Upload_Success);
            Process.Start("explorer.exe", $"steam://url/CommunityFilePage/{mod.PublishedFileId}");
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} is successfully uploaded.");
        }
        /// <summary>
        /// Create a workshop item.
        /// </summary>
        /// <param name="mod">Mod info.</param>
        /// <exception cref="Exception"></exception>
        private void CreateItem(ref ModInfo mod)
        {
            Program.Logger.Info("Workshop item creation started.");
            var steamAPICall_t = SteamUGC.CreateItem(CSL_APPLD_T, EWorkshopFileType.k_EWorkshopFileTypeFirst);
            CallResult<CreateItemResult_t>.Create(OnItemCreated).Set(steamAPICall_t);
            Console.WriteLine(Upload_Creating);
            while (!isReady.WaitOne(50))
            {
                SteamAPI.RunCallbacks();
            }
            if (createItemResult.m_eResult != EResult.k_EResultOK)
            {
                throw new Exception($"{createItemResult.m_eResult.ToLocalizedString()} ({createItemResult.m_eResult})");
            }
            mod.PublishedFileId = createItemResult.m_nPublishedFileId.m_PublishedFileId;
            Console.WriteLine(string.Format(Upload_CreateSuccess, mod.PublishedFileId));
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} is successfully created.");
        }
        private void ChooseFolder(ModInfo mod)
        {
            Program.Logger.Info("Started folder choosing.");
            if (!mod.UpdatePreviewOnly)
                currentContentFolderPath = ChooseContentFolder();
            currentPreviewImageFilePath = ChoosePreviewImageFile(mod);
        }
        private static string ChooseContentFolder()
        {
            Console.WriteLine(Upload_ChooseContentFolder_Title);
            using CommonOpenFileDialog dialog = new()
            {
                Title = Upload_ChooseContentFolder_Title,
                IsFolderPicker = true,
                EnsurePathExists = true,
                Multiselect = false,
                ShowPlacesList = true,
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName ?? "";
            }

            throw new InvalidOperationException(Upload_ChooseContentFolder_Cancel);
        }
        private static string? ChoosePreviewImageFile(ModInfo mod)
        {
            string? file = null;
            if (!mod.UpdatePreviewOnly)
                using (TaskDialog taskDialog = new()
                {
                    Caption = Console.Title,
                    InstructionText = mod.IsNewMod ? Upload_ChoosePreviewImageFile_TaskDialog_InstructionText_Newmod : Upload_ChoosePreviewImageFile_TaskDialog_InstructionText_Update,
                    Text = mod.IsNewMod ? Upload_ChoosePreviewImageFile_TaskDialog_Text_NewMod : Upload_ChoosePreviewImageFile_TaskDialog_Text_Update,
                    Icon = TaskDialogStandardIcon.Information,
                    StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No,
                })
                {
                    TaskDialogResult result = taskDialog.Show();
                    if (result == TaskDialogResult.No)
                    {
                        return mod.IsNewMod ? CreatePreviewImageFile(Path.GetTempPath()) : null;
                    }
                }
            Console.WriteLine(Upload_ChoosePreviewImageFile_OpenFileDialog_Title);
            using (CommonOpenFileDialog dialog = new()
            {
                Title = Upload_ChoosePreviewImageFile_OpenFileDialog_Title,
                EnsureFileExists = true,
                Multiselect = false,
                ShowPlacesList = true,
                Filters = { new CommonFileDialogFilter("PNG", "*.png") }
            })
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    file = dialog.FileName;
                }
                else if (!mod.UpdatePreviewOnly)
                {
                    file = mod.IsNewMod ? CreatePreviewImageFile(Path.GetTempPath()) : null;
                    TaskDialog.Show(null, mod.IsNewMod ? Upload_ChoosePreviewImageFile_OpenFileDialog_Cancel_Newmod :
                        Upload_ChoosePreviewImageFile_OpenFileDialog_Cancel_Update,
                        Console.Title, TaskDialogStandardIcon.Information);
                }
                else throw new InvalidOperationException(Upload_ChooseContentFolder_Cancel);
            }

            return file;
        }
        /// <summary>
        /// Create the default preview image file from embedded resources.
        /// </summary>
        /// <param name="path">the directory path where the image being created</param>
        /// <returns>The image's path</returns>
        private static string CreatePreviewImageFile(string path)
        {
            string folder = Path.Combine(path, "PreviewImage.png");
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string imagePath = "ModUploader.Resources.PreviewImage.png";
            using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(imagePath))
            {
                using FileStream fileStream = new(folder, FileMode.Create, FileAccess.Write);
                manifestResourceStream?.CopyTo(fileStream);
            }
            return folder;
        }
        /// <summary>
        /// Check and occupy the necessary files.
        /// Prevent invalid files from being uploaded (a mod obviously has at least one assembly!),
        /// and files from being deleted or modified during the upload process.
        /// </summary>
        /// <exception cref="DllNotFoundException"></exception>
        private void VerifyAndOccupy()
        {
            if (!string.IsNullOrEmpty(currentPreviewImageFilePath))
                stream[0] = new FileStream(currentPreviewImageFilePath, FileMode.Open);//Occupy the preview image file.
            if (!string.IsNullOrEmpty(currentContentFolderPath))
            {
                var dllFiles = Directory.GetFiles(currentContentFolderPath, "*.dll");
                if (dllFiles.Length == 0)
                {
                    throw new DllNotFoundException(Upload_NoDll);
                }
                stream[1] = new FileStream(dllFiles.First(), FileMode.Open);//Occupy the assembly file (also means the whole content folder)
            }
        }
        /// <summary>
        /// Release the occupied files.
        /// </summary>
        private void ReleaseOccupy()
        {
            stream[0]?.Close();
            stream[1]?.Close();
        }
        public static string GetCSLPath()
        {
            SteamApps.GetAppInstallDir(CSL_APPLD_T, out string path, 256U);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Failed to locate Cities: Skylines installation path.");
            }
            return path;
        }


        public static string GetCompatibleTag()
        {
            Program.Logger.Info("Getting compatible tag");
            try
            {
                var cslPath = GetCSLPath();
                var assemblyCSharpPath = Path.Combine(cslPath, "Cities_Data", "Managed", "Assembly-CSharp.dll");
                var unityEnginePath = Path.Combine(cslPath, "Cities_Data", "Managed", "UnityEngine.dll");

                if (!File.Exists(assemblyCSharpPath) || !File.Exists(unityEnginePath))
                {
                    throw new FileNotFoundException($"Necessary assemblies not found. \nMissing: {assemblyCSharpPath}\n or/and {unityEnginePath}");
                }

                var assembly = Assembly.LoadFile(assemblyCSharpPath);
                _ = Assembly.LoadFrom(unityEnginePath);

                var applicationVersion = assembly?.GetType("BuildConfig")
                                                         ?.GetProperty("applicationVersion", BindingFlags.Public | BindingFlags.Static)
                                                         ?.GetValue(null)?.ToString();

                if (string.IsNullOrEmpty(applicationVersion))
                {
                    throw new InvalidOperationException("Failed to get applicationVersion.");
                }

                return $"{applicationVersion}-compatible";
            }
            catch (Exception e)
            {
                throw new Exception("Failed to receive compatible tag.", e);
            }
        }
        public void GetModList()
        {
            try
            {
                Console.WriteLine(Upload_GetModList_Fetching);
                Program.Logger.Info("Mod list fetching started.");
                UGCQueryHandle_t handle = SteamUGC.CreateQueryUserUGCRequest(
                    new AccountID_t((uint)SteamUser.GetSteamID().m_SteamID),
                    EUserUGCList.k_EUserUGCList_Published,
                    EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                    EUserUGCListSortOrder.k_EUserUGCListSortOrder_TitleAsc,
                    CSL_APPLD_T,
                    CSL_APPLD_T,
                    1
                );
                SteamUGC.AddRequiredTag(handle, "Mod");

                var callBack = SteamUGC.SendQueryUGCRequest(handle);

                CallResult<SteamUGCQueryCompleted_t>.Create((result, _) =>
                {
                    isReady.Set();

                    if (result.m_eResult != EResult.k_EResultOK)
                    {
                        Program.Logger.Error($"Failed to fetch mod list: {result.m_eResult.ToLocalizedString()} ({result.m_eResult})");
                        return;
                    }

                    var modCount = 0;
                    for (uint i = 0; i < result.m_unNumResultsReturned; i++)
                    {
                        SteamUGC.GetQueryUGCResult(handle, i, out var detail);
                        if (detail.m_rgchTags.Contains("Mod"))
                        {
                            Console.WriteLine($"({detail.m_nPublishedFileId}) {detail.m_rgchTitle}");
                            modCount++;
                        }
                    }
                    if (modCount == 0)
                    {
                        Console.WriteLine(Upload_GetModList_NoMod);
                    }

                    SteamUGC.ReleaseQueryUGCRequest(handle);
                }).Set(callBack);

                while (!isReady.WaitOne(50))
                {
                    SteamAPI.RunCallbacks();
                }

                Program.Logger.Info("Mod list fetching completed.");
            }
            catch (Exception e)
            {
                Program.Logger.Error($"Failed to fetch mod list: {e}");
            }
        }
        private void OnItemCreated(CreateItemResult_t res, bool bIOFailure)
        {
            createItemResult = res;
            isReady.Set();
        }

        private void OnItemSubmitted(SubmitItemUpdateResult_t res, bool bIOFailure)
        {
            submitItemUpdateResult = res;
            isReady.Set();
        }
    }

}