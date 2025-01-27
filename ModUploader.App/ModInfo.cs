namespace ModUploader
{
    public class ModInfo
    {
        public string Name { get; }
        public string Description { get; }
        public ulong PublishedFileId { get; set; }
        public bool IsNewMod { get; }
        public bool UpdatePreviewOnly { get; }
        public PublishedFileId_t PublishedFileId_t => new(PublishedFileId);
        public ModInfo(uint publishedFileId, bool updatePreviewOnly)
        {
            Program.Logger.Info($"Querying {publishedFileId}...");
            PublishedFileId = publishedFileId;
            UpdatePreviewOnly = updatePreviewOnly;
            var handle = SteamUGC.CreateQueryUGCDetailsRequest([PublishedFileId_t], 1);
            SteamUGC.AddRequiredTag(handle, "Mod");

            var callBack = SteamUGC.SendQueryUGCRequest(handle);
            CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted).Set(callBack);

            while (!isReady.WaitOne(50))
            {
                SteamAPI.RunCallbacks();
            }
            SteamUGC.GetQueryUGCResult(handle, 0, out var queryResult);
            SteamUGC.ReleaseQueryUGCRequest(handle);

            Name = queryResult.m_rgchTitle;
            Description = queryResult.m_rgchDescription;

            string? error = string.Empty;
            if (queryResult.m_eResult != EResult.k_EResultOK)
            {
                error += ($"{ModInfo_QueryFail} {queryResult.m_eResult.ToLocalizedString()} ({queryResult.m_eResult})\n");
            }

            if (queryResult.m_nConsumerAppID.m_AppId != UploadHelper.CSL_APPID)
            {
                error += $"{ModInfo_NotCSL}\n";
            }

            if (queryResult.m_eFileType != EWorkshopFileType.k_EWorkshopFileTypeFirst || !queryResult.m_rgchTags.Contains("Mod"))
            {
                error += $"{ModInfo_NotMod}\n";
            }

            if (queryResult.m_ulSteamIDOwner != SteamUser.GetSteamID().m_SteamID)
            {
                error += $"{ModInfo_NotAuthor}\n";
            }

            if (error != string.Empty)
            {
                throw new Exception($"\n{error}{Name} ({PublishedFileId})");
            }
        }

        public ModInfo(string name, string description)
        {
            Name = name;
            Description = description;
            IsNewMod = true;
        }

        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t result, bool bIOFailure) => isReady.Set();

        private AutoResetEvent isReady = new AutoResetEvent(false);
    }
}
