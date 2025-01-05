using Microsoft.WindowsAPICodePack.Dialogs;
using ShellProgressBar;
using System.Diagnostics;
namespace ModUploader
{
    public class UploadHelper
    {
        public static UploadHelper Instance { get; } = new UploadHelper();
        public const uint CSL_APPID = 255710U;
        public static AppId_t CSL_APPLD_T => new AppId_t(255710U);
        public string CompatibleTag => _lazyCompatibleTag.Value;

        private AutoResetEvent isReady = new AutoResetEvent(false);
        private CreateItemResult_t createItemResult;
        private SubmitItemUpdateResult_t submitItemUpdateResult;
        private string currentContentFolderPath = "";
        private string? currentPreviewImageFilePath;
        private readonly Lazy<string> _lazyCompatibleTag = new Lazy<string>(GetCompatibleTag);

        /// <summary>
        /// Update a mod, use provided paths.
        /// </summary>
        /// <param name="mod">mod info.</param>
        /// <param name="contentPath">Mod content path.</param>
        /// <param name="previewImagePath">Mod preview image path. If null, will not change the image.</param>
        public void UpdateMod(ModInfo mod, string contentPath, string? previewImagePath)
        {
            currentContentFolderPath = contentPath;
            currentPreviewImageFilePath = previewImagePath;
            UploadItem(mod);
        }

        /// <summary>
        /// Update a mod, manual choose the paths.
        /// </summary>
        /// <param name="mod">mod info.</param>
        public void UpdateMod(ModInfo mod)
        {
            ChooseFolder(mod.IsNewMod);
            UploadItem(mod);
        }
        /// <summary>
        /// Upload a workshop item.
        /// </summary>
        /// <param name="mod">Mod info.</param>
        /// <exception cref="Exception"></exception>
        private void UploadItem(ModInfo mod)
        {
            var handle = SteamUGC.StartItemUpdate(CSL_APPLD_T, mod.PublishedFileId_t);

            if (mod.IsNewMod)
            {
                SteamUGC.SetItemTitle(handle, mod.Name);
                SteamUGC.SetItemDescription(handle, mod.Description);
            }

            if (currentPreviewImageFilePath != null)
                SteamUGC.SetItemPreview(handle, currentPreviewImageFilePath);

            SteamUGC.SetItemContent(handle, currentContentFolderPath);

            SteamUGC.SetItemTags(handle, ["Mod", CompatibleTag]);

            var callback = SteamUGC.SubmitItemUpdate(handle, "");
            CallResult<SubmitItemUpdateResult_t>.Create(OnItemSubmitted).Set(callback);
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} upload started.");

            var progressOptions = new ProgressBarOptions
            {
                ProgressBarOnBottom = true,
                EnableTaskBarProgress = true,
                ForegroundColor = ConsoleColor.Blue,
                BackgroundColor = ConsoleColor.Gray,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                ForegroundColorError = ConsoleColor.DarkRed,
            };

            using var progressBar = new ShellProgressBar.ProgressBar(100, Upload_Uploading, progressOptions);
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
                throw new Exception(submitItemUpdateResult.m_eResult.ToLocalizedString());
            }

            Console.WriteLine(Upload_Success);
            Process.Start("explorer.exe", $"steam://url/CommunityFilePage/{mod.PublishedFileId}");
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} is successfully uploaded.");
        }
        /// <summary>
        /// Update a mod, use provided paths.
        /// </summary>
        /// <param name="mod">mod info.</param>
        /// <param name="contentPath">Mod content path.</param>
        /// <param name="previewImagePath">Mod preview image path. If null, will use the default image.</param>
        public void CreateMod(ModInfo mod, string contentPath, string? previewImagePath)
        {
            CreateItem(ref mod);
            currentContentFolderPath = contentPath;
            currentPreviewImageFilePath = previewImagePath ?? CreatePreviewImageFile(Path.GetTempPath());
            UploadItem(mod);
        }
        /// <summary>
        /// Create a mod, manual choose the paths.
        /// </summary>
        /// <param name="mod">mod info.</param>
        public void CreateMod(ModInfo mod)
        {
            ChooseFolder(mod.IsNewMod);
            CreateItem(ref mod);
            UploadItem(mod);
        }
        /// <summary>
        /// Create a workshop item.
        /// </summary>
        /// <param name="mod">Mod info.</param>
        /// <exception cref="Exception"></exception>
        private void CreateItem(ref ModInfo mod)
        {
            SteamAPICall_t steamAPICall_t = SteamUGC.CreateItem(CSL_APPLD_T, EWorkshopFileType.k_EWorkshopFileTypeFirst);
            CallResult<CreateItemResult_t>.Create(OnItemCreated).Set(steamAPICall_t);
            Console.WriteLine(Upload_Creating);
            Program.Logger.Info("Workshop item creation started.");
            while (!isReady.WaitOne(50))
            {
                SteamAPI.RunCallbacks();
            }
            if (createItemResult.m_eResult != EResult.k_EResultOK)
            {
                throw new Exception(createItemResult.m_eResult.ToLocalizedString());
            }
            mod.PublishedFileId = createItemResult.m_nPublishedFileId.m_PublishedFileId;
            Console.WriteLine(string.Format(Upload_CreateSuccess, mod.PublishedFileId));
            Program.Logger.Info($"Workshop item {mod.PublishedFileId} is successfully created.");
        }

        private void ChooseFolder(bool isNewMod)
        {
            currentContentFolderPath = ChooseContentFolder();
            currentPreviewImageFilePath = ChoosePreviewImageFile(isNewMod);
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
                return dialog.FileName;
            }

            throw new InvalidOperationException(Upload_ChooseContentFolder_Cancel);
        }


        private static string? ChoosePreviewImageFile(bool isNewMod)
        {
            string? file = null;
            using (TaskDialog taskDialog = new()
            {
                Caption = Console.Title,
                InstructionText = isNewMod ? Upload_ChoosePreviewImageFile_TaskDialog_InstructionText_Newmod : Upload_ChoosePreviewImageFile_TaskDialog_InstructionText_Update,
                Text = isNewMod ? Upload_ChoosePreviewImageFile_TaskDialog_Text_NewMod : Upload_ChoosePreviewImageFile_TaskDialog_Text_Update,
                Icon = TaskDialogStandardIcon.Information,
                StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No,
            })
            {
                TaskDialogResult result = taskDialog.Show();
                if (result == TaskDialogResult.No)
                {
                    return isNewMod ? CreatePreviewImageFile(Path.GetTempPath()) : null;
                }
            }

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
                else
                {
                    file = isNewMod ? CreatePreviewImageFile(Path.GetTempPath()) : null;
                    TaskDialog.Show(null, isNewMod ? Upload_ChoosePreviewImageFile_OpenFileDialog_Cancel_Newmod :
                        Upload_ChoosePreviewImageFile_OpenFileDialog_Cancel_Update,
                        Console.Title, TaskDialogStandardIcon.Information);
                }
            }

            return file;
        }

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
                throw new Exception("Failed to retrieve compatible tag.", e);
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
                        Program.Logger.Error($"Failed to fetch mod list: {result.m_eResult.ToLocalizedString()}");
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