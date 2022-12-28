using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Klrohias.NFast.Resource
{
    public class ZipResourceProvider : IResourceProvider
    {
        public string CachePath { get; set; }

        private readonly ZipFile _zipFile;
        public ZipResourceProvider(string cachePath, ZipFile zipFile)
        {
            CachePath = cachePath;
            _zipFile = zipFile;
        }

        private ZipEntry GetZipEntry(string path)
            => _zipFile.GetEntry(path) ?? 
               throw new FileNotFoundException($"File '{path}' not found", path);

        public Task<Stream> GetStreamResource(string id)
        {
            return Task.FromResult(_zipFile.GetInputStream(GetZipEntry(id)));
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