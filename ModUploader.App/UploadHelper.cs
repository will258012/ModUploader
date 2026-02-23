using Steamworks.Ugc;
using System.Reflection;
using System.Text.RegularExpressions;
namespace ModUploader
{
    public partial class UploadHelper
    {
        /// <summary>
        /// Singleton instance of <see cref="UploadHelper"/>.
        /// </summary>
        public static UploadHelper Instance { get; } = new UploadHelper();
        /// <summary>
        /// Steam AppID for Cities: Skylines.
        /// </summary>
        public const int CSL_APPID = 255710;
        /// <summary>
        /// Gets the compatibility tag generated from the current game version.
        /// </summary>
        public string CompatibleTag => lazyCompatibleTag.Value;
        private readonly Lazy<string> lazyCompatibleTag = new(GetCompatibleTag);

        private string currentContentFolderPath = "";
        private string? currentPreviewImageFilePath = null;

        /// <summary>
        /// Regular expression used to match legacy compatibility tags.
        /// </summary>
        public static Regex CompatibleRegex => GetCompatibleRegex();

        /// <summary>
        /// Use provided paths to start an upload process.
        /// </summary>
        /// <param name="item">An item info.</param>
        public async Task<PublishResult> StartUploadAsync(ItemInfo item, IProgress<float> progress)
        {
            App.Logger.Info("Item uploading started.");
            App.Logger.Info("Item details: {@editor}", item);
            currentContentFolderPath = item.ContentFolderPath;

            if (item.IsNewItem)
            {
                currentPreviewImageFilePath = item.PreviewImagePath ?? GetPreviewImageFile();
                return await CreateItemEditor(item).SubmitAsync(progress);
            }
            else
            {
                currentPreviewImageFilePath ??= item.PreviewImagePath;
                return await CreateUpdateEditor(item).SubmitAsync(progress);
            }
        }

        /// <summary>
        /// Create an editor for updating an existing workshop item.
        /// </summary>
        /// <param name="item">An item info.</param>
        /// <returns>An <see cref="Editor"/> instance configured for update.</returns>
        private Editor CreateItemEditor(ItemInfo item)
        {
            if (!item.IsNewItem || item.Item.HasValue)
                throw new InvalidOperationException("The item already exists.");

            var editor = Editor.NewCommunityFile
                  .WithTitle(item.Name)
                  .WithDescription(item.Description)
                  .WithContent(currentContentFolderPath)
                  .WithPreviewFile(currentPreviewImageFilePath)
                  .WithPrivateVisibility();

            foreach (var tag in item.Tags)
                editor = editor.WithTag(tag);

            if (item.ChangeLog != null)
                editor = editor.WithChangeLog(item.ChangeLog);

            return editor;
        }
        /// <summary>
        /// Create an editor for updating an existing workshop item.
        /// </summary>
        /// <param name="item">An item info.</param>
        /// <returns>An <see cref="Editor"/> instance configured for update.</returns>
        private Editor CreateUpdateEditor(ItemInfo item)
        {
            if (item.IsNewItem || !item.Item.HasValue)
                throw new InvalidOperationException("New item cannot be updated.");

            var editor = item.Item.Value.Edit();

            editor = editor.WithTitle(item.Name)
                  .WithDescription(item.Description);

            if (item.UpdatePreviewOnly)
            {
                if (currentPreviewImageFilePath == null)
                    throw new InvalidOperationException("You didn't seem to change the preview image. Upload was canceled.");
                editor = editor.WithPreviewFile(currentPreviewImageFilePath);
            }
            else
            {
                editor = editor.WithContent(currentContentFolderPath);

                if (currentPreviewImageFilePath != null)
                    editor = editor.WithPreviewFile(currentPreviewImageFilePath);

                if (item.ChangeLog != null)
                    editor = editor.WithChangeLog(item.ChangeLog);

                foreach (var tag in item.Tags)
                    editor = editor.WithTag(tag);
            }
            return editor;
        }

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

        /// <summary>
        /// Retrieves all workshop items published by the current user.
        /// </summary>
        /// <returns>A list of published <see cref="Item"/>.</returns>
        public static async Task<List<Item>> GetItemListAsync()
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

        [GeneratedRegex(@"\b\d+(?:\.\d+)+-f\d+-compatible\b", RegexOptions.Compiled)]
        private static partial Regex GetCompatibleRegex();
    }

}