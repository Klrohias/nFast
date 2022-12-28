using System.IO;
using System.Threading.Tasks;

namespace Klrohias.NFast.Resource
{
    public class DirectoryResourceProvider : IResourceProvider
    {
        private readonly string _path;
        public DirectoryResourceProvider(string path)
        {
            _path = path;
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();
        }

        public async Task<Stream> GetStreamResource(string id)
        {
            var path = await GetResourcePath(id);
            if (!File.Exists(path))
                throw new FileNotFoundException($"File '{id}' not found", id);
            var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return stream;
        }

        public Task<string> GetResourcePath(string id)
        {
            return Task.FromResult(Path.Combine(_path, id));
        }
    }
}