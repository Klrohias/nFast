using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Klrohias.NFast.Resource
{
    public interface IResourceProvider
    {
        public Task<Stream> GetStreamResource(string id);
        public Task<string> GetResourcePath(string id);
    }
}