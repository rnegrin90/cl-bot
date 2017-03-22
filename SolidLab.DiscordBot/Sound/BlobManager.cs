using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SolidLab.DiscordBot.Sound
{
    public class BlobManager : IManageBlob
    {
        private readonly CloudBlobClient _blobClient;

        public BlobManager(string connString)
        {
            var account = CloudStorageAccount.Parse(connString);
            _blobClient = account.CreateCloudBlobClient();
        }

        public async Task StoreBlob(Guid blobId, BlobType type, Stream payload)
        {
            var container = _blobClient.GetContainerReference(type.ToString());
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(blobId.ToString());
            await blob.UploadFromStreamAsync(payload);
        }

        public async Task<Stream> GetBlob(Guid blobId, BlobType type)
        {
            var container = _blobClient.GetContainerReference(type.ToString());
            var blob = container.GetBlockBlobReference(blobId.ToString());
            var downloadedStream = new MemoryStream();
            await blob.DownloadToStreamAsync(downloadedStream);
            return downloadedStream;
        }
    }

    public enum BlobType
    {
        Mp3 = 0,
        Image = 1
    }

    public interface IManageBlob
    {
        
    }
}
