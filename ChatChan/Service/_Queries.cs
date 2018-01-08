namespace ChatChan.Service
{
    internal static class Queries
    {
        /* Image database (which is to be replaced in the future) */
        // Create images
        public const string ImageCreation = "INSERT INTO _images(Uuid, Data) VALUES(?,?)";

        // Query images by image UUID
        public const string ImageQueryByUUID = "SELECT Data FROM _images WHERE Uuid = ?";
    }
}
