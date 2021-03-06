﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SolidLab.DiscordBot.Sound
{
    public class BlobManager : IManageBlob
    {
        private readonly CloudBlobClient _blobClient;

        public BlobManager(CloudStorageAccount storageAccount)
        {
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task StoreBlob(Guid blobId, BlobType type, Stream payload)
        {
            var container = _blobClient.GetContainerReference(type.ToString().ToLower());
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);
            var blob = container.GetBlockBlobReference(blobId.ToString());
            await blob.UploadFromStreamAsync(payload).ConfigureAwait(false);
        }

        public async Task<Stream> GetBlob(Guid blobId, BlobType type)
        {
            var container = _blobClient.GetContainerReference(type.ToString().ToLower());
            var blob = container.GetBlockBlobReference(blobId.ToString());
            var downloadedStream = new MemoryStream();
            await blob.DownloadToStreamAsync(downloadedStream).ConfigureAwait(false);
            return downloadedStream;
        }

        public async Task DeleteBlob(Guid blobId, BlobType type)
        {
            var container = _blobClient.GetContainerReference(type.ToString().ToLower());
            var blob = container.GetBlockBlobReference(blobId.ToString());
            await blob.DeleteAsync().ConfigureAwait(false);
        }
    }

    public enum BlobType
    {
        Mp3 = 0,
        Image = 1
    }

    public interface IManageBlob
    {
        Task StoreBlob(Guid blobId, BlobType type, Stream payload);
        Task<Stream> GetBlob(Guid blobId, BlobType type);
        Task DeleteBlob(Guid blobId, BlobType type);
    }
}
