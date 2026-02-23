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
