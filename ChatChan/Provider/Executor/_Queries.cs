namespace ChatChan.Provider.Executor
{
    internal static class Queries
    {
        /* Image database (which is to be replaced in the future) */
        // Create images
        public const string ImageCreation = "INSERT INTO _images(Uuid, Type, Data) VALUES(@uuid,@type,@data)";

        // Query images by image UUID
        public const string ImageQueryByUuid = "SELECT Type, Data, CreatedAt FROM _images WHERE Uuid = @uuid";
    }
}
