namespace ChatChan.Service.Identifier
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ChannelId
    {
        public enum ChannelType : uint
        {
            DM = 0, // Direct message, or one-on-one messages.
            GR = 1, // Group messages
        }

        [JsonProperty(PropertyName = "t")]
        public ChannelType Type { get; set; }

        [JsonProperty(PropertyName = "i")]
        public int Id { get; set; }

        public override string ToString()
        {
            return $"{this.Type.ToString()}:{this.Id}";
        }

        public override int GetHashCode()
        {
            var hashCode = -1324594315;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            ChannelId id = obj as ChannelId;
            return id != null &&
                   Type == id.Type &&
                   Id == id.Id;
        }

        public static bool TryParse(string input, out ChannelId channelId)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            channelId = null;
            string[] inputSplits = input.Split(':');
            if (2 != inputSplits.Length)
            {
                return false;
            }

            if (!Enum.TryParse(inputSplits[0], out ChannelType channelType))
            {
                return false;
            }

            if (!int.TryParse(inputSplits[1], out int id))
            {
                return false;
            }

            channelId = new ChannelId { Id = id, Type = channelType };
            return true;
        }
    }

    public class AccountId
    {
        public enum AccountType : uint
        {
            UA = 0, // UserAccount
        }

        [JsonProperty(PropertyName = "t")]
        public AccountType Type { get; set; }

        [JsonProperty(PropertyName = "n")]
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

        public override bool Equals(object obj)
        {
            var id = obj as AccountId;
            return id != null &&
                   this.Type == id.Type &&
                   string.Equals(this.Name, id.Name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var hashCode = -1979447941;
            hashCode = hashCode * -1521134295 + this.Type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            return hashCode;
        }
    }

    public class ImageId
    {
        public enum ImageType : uint
        {
            CI = 0, // CoreImage
        }

        [JsonProperty(PropertyName = "t")]
        public ImageType Type { get; set; }

        [JsonProperty(PropertyName = "g")]
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

        public override bool Equals(object obj)
        {
            var id = obj as ImageId;
            return id != null &&
                   Type == id.Type &&
                   Guid.Equals(id.Guid);
        }

        public override int GetHashCode()
        {
            var hashCode = 652959137;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
            return hashCode;
        }
    }
}
