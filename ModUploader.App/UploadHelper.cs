using Steamworks.Ugc;
using System.Reflection;
using System.Text.RegularExpressions;
namespace ModUploader
{
    public class UploadHelper
    {
        public static UploadHelper Instance { get; } = new UploadHelper();
        public const int CSL_APPID = 255710;
        public string CompatibleTag => lazyCompatibleTag.Value;

        public string currentContentFolderPath { get; set; } = "";
        public string? currentPreviewImageFilePath { get; set; } = null;

        private readonly Lazy<string> lazyCompatibleTag = new(GetCompatibleTag);
        
        public static readonly Regex CompatibleRegex = new(@"\b\d+(?:\.\d+)+-f\d+-compatible\b", RegexOptions.Compiled);

        /// <summary>
        /// Use provided paths to start an upload process.
        /// </summary>
        /// <param name="mod">mod info.</param>
        public async Task<PublishResult> StartUploadAsync(
            ItemInfo mod,
            IProgress<float> progress)
        {
            App.Logger.Info("Item uploading started.");
            App.Logger.Info("Item details: {@editor}", mod);
            currentContentFolderPath = mod.ContentFolderPath;

            if (mod.IsNewItem)
            {
                currentPreviewImageFilePath = mod.PreviewImagePath ?? GetPreviewImageFile();
                return await CreateItemEditor(mod).SubmitAsync(progress);
            }
            else
            {
                currentPreviewImageFilePath ??= mod.PreviewImagePath;
                return await CreateUpdateEditor(mod).SubmitAsync(progress);
            }
        }

        /// <summary>
        /// Create a workshop item.
        /// </summary>
        /// <param name="mod">Mod info.</param>
        private Editor CreateItemEditor(ItemInfo mod)
        {
            if (mod!.IsNewItem || mod.Item.HasValue)
                throw new InvalidOperationException("The item already exists.");

            var editor = Editor.NewCommunityFile
                  .WithTitle(mod.Name)
                  .WithDescription(mod.Description)
                  .WithContent(currentContentFolderPath)
                  .WithPreviewFile(currentPreviewImageFilePath)
                  .WithPrivateVisibility();

                foreach (var tag in mod.Tags)
                    editor = editor.WithTag(tag);

            if (mod.ChangeLog != null)
                editor = editor.WithChangeLog(mod.ChangeLog);

            return editor;
        }
        private Editor CreateUpdateEditor(ItemInfo mod)
        {
            if (mod.IsNewItem || !mod.Item.HasValue)
                throw new InvalidOperationException("New item cannot be updated.");

            var editor = mod.Item.Value.Edit();

            editor = editor.WithTitle(mod.Name)
                  .WithDescription(mod.Description);

            if (mod.UpdatePreviewOnly)
            {
                if(currentPreviewImageFilePath == null)
                    throw new InvalidOperationException("You didn't seem to change the preview image. Upload was canceled.");
                editor = editor.WithPreviewFile(currentPreviewImageFilePath);
            }
            else
            {
                if (currentPreviewImageFilePath != null)
                    editor = editor.WithPreviewFile(currentPreviewImageFilePath);

                if (mod.ChangeLog != null)
                    editor = editor.WithChangeLog(mod.ChangeLog);

                foreach (var tag in mod.Tags)
                    editor = editor.WithTag(tag);
            }
            return editor;
        }

        /// <summary>
        /// Get the default preview image file path.
        /// </summary>
        /// <returns>The image's path</returns>
        private static string GetPreviewImageFile() => Path.Combine(AppContext.BaseDirectory, "Assets", "PreviewImage.png");

        public static string GetCSLPath()
        {
            App.Logger.Info("Loacting CSL path");
            var path = SteamApps.AppInstallDir();
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Failed to locate Cities: Skylines installation path.");
            }
            return path;
        }


        public static string GetCompatibleTag()
        {
            App.Logger.Info("Getting compatible tag");
            try
            {
                var applicationVersion = App.AssemblyCSharp.GetType("BuildConfig")
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
        public static async Task<List<Item>> GetListAsync()
        {
            var query = Query.Items
                .WhereUserPublished(SteamClient.SteamId)
                .InLanguage("english")
                .WithLongDescription(true)
                .SortByTitleAsc();

            int pageNum = 0;
            ResultPage? page = null;
            List<Item> result = [];
            while (true)
            {
                pageNum++;

                page = await query.GetPageAsync(pageNum);

                if (!page.HasValue || page.Value.ResultCount == 0) break;
                result = result.Union(page.Value.Entries).ToList();
            }

            App.Logger.Info($"Received {result.Count} items");
            return result.ToList();
        }
    }

}