using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlobStorage
{
    public class StorageOptions
    {
        static StorageOptions _instance;
        public static StorageOptions Instance => _instance ??= new StorageOptions();

        Dictionary<string, DirProps> dirPropsPerToken = new Dictionary<string, DirProps>(new InvariantStringComparer());
        string host;

        private StorageOptions()
        {
            Directory.CreateDirectory("/storagespace/blobs");
            Directory.CreateDirectory("/storagespace/config");

            var storageProps = JsonSerializer.Deserialize<StorageProps>(File.ReadAllText("/storagespace/config/options.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (Uri.CheckHostName(storageProps?.Host ?? "") == UriHostNameType.Unknown)
                throw new Exception($"host is not valid or it is missing: {storageProps.Host}");

            host = storageProps.Host;

            if(storageProps?.Dirs?.Any() != true)
                throw new Exception("dirs is empty or missing in /storagespace/config/options.json");

            foreach (var dirProps in storageProps.Dirs)
            {
                if (string.IsNullOrWhiteSpace(dirProps.Name))
                    throw new Exception($"Each dir must have a name");
                if (string.IsNullOrWhiteSpace(dirProps.Token))
                    throw new Exception($"Token is missing for {dirProps.Name}");
                if (dirPropsPerToken.ContainsKey(dirProps.Token))
                    throw new Exception($"Token is dublicated among {dirProps.Name} and {dirPropsPerToken[dirProps.Token].Name}");

                dirPropsPerToken[dirProps.Token] = dirProps;
                dirProps.FullPath = Path.Combine("/storagespace/blobs", dirProps.Name);
                Directory.CreateDirectory(dirProps.FullPath);
            }
        }

        public string Host => host;

        public bool TryGetDirPath(string token, out string path)
        {
            if(dirPropsPerToken.TryGetValue(token, out var props))
            {
                path = props.FullPath;
                return true;
            }
            path = null;
            return false;
        }

        public bool TryFindFile(string fileName, out string path)
        {
            var file = new DirectoryInfo("/storagespace/blobs").EnumerateFiles(fileName, SearchOption.AllDirectories).FirstOrDefault()?.FullName;
            if (!string.IsNullOrWhiteSpace(file))
            {
                path = file;
                return true;
            }
            path = null;
            return false;
        }

        public void Init() => Console.WriteLine(JsonSerializer.Serialize(dirPropsPerToken, new JsonSerializerOptions { WriteIndented = true }));

        record DirProps(string Name, string Token)
        {
            public string FullPath { get; set; }
        };
        record StorageProps(DirProps[] Dirs, string Host);


        class InvariantStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => x?.Equals(y, StringComparison.InvariantCultureIgnoreCase) == true;

            public int GetHashCode([DisallowNull] string obj) => obj.GetHashCode();
        }
    }
}
