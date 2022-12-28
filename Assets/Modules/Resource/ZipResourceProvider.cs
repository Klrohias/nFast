using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Klrohias.NFast.Resource
{
    public class ZipResourceProvider : IResourceProvider
    {
        public string CachePath { get; set; }

        private readonly ZipArchive _zipFile;
        public ZipResourceProvider(string cachePath, ZipArchive zipFile)
        {
            CachePath = cachePath;
            _zipFile = zipFile;
        }

        private ZipArchiveEntry GetZipEntry(string path)
            => _zipFile.GetEntry(path) ?? 
               throw new FileNotFoundException($"File '{path}' not found", path);

        public Task<Stream> GetStreamResource(string id)
        {
            return Task.FromResult(GetZipEntry(id).Open());
        }

        public async Task<string> GetResourcePath(string id)
        {
            var path = Path.Combine(CachePath, id);
            if (File.Exists(path)) return path;

            var stream = await GetStreamResource(id);
            var outStream = File.OpenWrite(path);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            stream.Close();
            outStream.Close();

            return path;
        }
    }
}