using Steamworks.Ugc;

namespace ModUploader
{
    public class ItemInfo
    {

        public string Name { get; set; }
        public string? Description { get; set; }
        public string[] Tags { get; set; }
        public ulong PublishedFileId { get; set; }
        public bool UpdatePreviewOnly { get; set; }
        public string ContentFolderPath { get; set; }
        public string? PreviewImagePath { get; set; }
        public string? ChangeLog { get; set; }
        public string PreviewImageUrl =>
                    IsNewItem || !Item.HasValue || string.IsNullOrEmpty(Item.Value.PreviewImageUrl)
                    ? "ms-appx:///Assets/PreviewImage.png"
                    : Item.Value.PreviewImageUrl;
        public Item? Item { get; private set; }
        public bool IsNewItem { get; }

        public ItemInfo(uint publishedFileId, bool updatePreviewOnly)
        {
            App.Logger.Info($"Querying {publishedFileId}...");
            PublishedFileId = publishedFileId;
            UpdatePreviewOnly = updatePreviewOnly;

            var queryResult = Steamworks.Ugc.Item.GetAsync(publishedFileId).GetAwaiter().GetResult();

            string? error = string.Empty;

            if (!queryResult.HasValue)
            {
                error += ModInfo_QueryFail;
            }
            else
            {
                if (queryResult.Value.Result != Result.OK)
                {
                    error += ($"{ModInfo_QueryFail} {queryResult.Value.Result.ToLocalizedString()} ({queryResult.Value.Result})\n");
                }

                if (queryResult.Value.CreatorApp != UploadHelper.CSL_APPID)
                {
                    error += $"{ModInfo_NotCSL}\n";
                }

                if (queryResult.Value.HasTag("Mod"))
                {
                    error += $"{ModInfo_NotMod}\n";
                }

                if (!queryResult.Value.Owner.IsMe)
                {
                    error += $"{ModInfo_NotAuthor}\n";
                }
            }
            if (error != string.Empty)
            {
                throw new Exception($"\n{error}{Name} ({PublishedFileId})");
            }

            Item = queryResult.Value;
            Name = queryResult.Value.Title;
            Description = queryResult.Value.Description;
        }

        public ItemInfo(Item item)
        {
            Name = item.Title;
            Description = item.Description;
            PublishedFileId = item.Id;
            Item = item;
            Tags = item.Tags;
        }
        public ItemInfo(string name, string? description, string[] tags)
        {
            Name = name;
            Description = description;
            Tags = tags;
            IsNewItem = true;
        }

    }
}
