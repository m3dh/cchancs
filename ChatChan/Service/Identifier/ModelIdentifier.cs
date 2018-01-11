namespace ChatChan.Service.Identifier
{
    using System;

    public class AccountId
    {
        public enum AccountType : uint
        {
            UA = 0, // UserAccount
        }

        public AccountType Type { get; set; }

        public string Name { get; set; }

        public static bool TryParse(string input, out AccountId accountId)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            accountId = null;
            string[] inputSplits = input.Split(':');
            if (2 != inputSplits.Length)
            {
                return false;
            }

            if (!Enum.TryParse(inputSplits[0], out AccountType type))
            {
                return false;
            }

            if (string.IsNullOrEmpty(inputSplits[1]))
            {
                return false;
            }

            accountId = new AccountId
            {
                Type = type,
                Name = inputSplits[1],
            };

            return true;
        }

        public override string ToString()
        {
            return $"{this.Type.ToString()}:{this.Name}";
        }
    }

    public class ImageId
    {
        public enum ImageType : uint
        {
            CI = 0, // CoreImage
        }

        public ImageType Type { get; set; }

        public Guid Guid { get; set; }

        public static bool TryParse(string input, out ImageId imageId)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            imageId = null;
            string[] inputSplits = input.Split(':');
            if (2 != inputSplits.Length)
            {
                return false;
            }

            if (!Enum.TryParse(inputSplits[0], out ImageType type))
            {
                return false;
            }

            if (!Guid.TryParse(inputSplits[1], out Guid guid))
            {
                return false;
            }

            imageId = new ImageId
            {
                Type = type,
                Guid = guid,
            };

            return true;
        }

        public override string ToString()
        {
            return $"{this.Type.ToString()}:{this.Guid:N}";
        }
    }
}
