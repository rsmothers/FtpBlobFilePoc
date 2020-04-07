﻿using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Chilkat;

namespace FtpBlobFilePoc
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var client = new CloudBlobClient(new Uri("https://rsmothersstorage.blob.core.windows.net"),
                new StorageCredentials(
                    "rsmothersstorage",
                    "Z5BlDnPCAvfQEXu9Bi/4Zeu1hqk//CFgdzagOJlghGCLABtCwCIOmlklJBFYdt9kMoCIlCm2XyJw+asNKG1Mqg=="));

            log.LogInformation($"Check for files to ftp: {DateTime.Now}");

            FtpBlobStreams(
                RetrieveBlobList(
                    client,
                    "ftp-blob-file-poc",
                    "testFolder",
                    log).Result,
                log);
        }

        private static async Task<BlobResultSegment> RetrieveBlobList(
            CloudBlobClient client, 
            string containerName, 
            string directory, 
            ILogger log)
        {
            log.LogInformation("Retrieving blob list.");

            return await client
                .GetContainerReference(containerName)
                .GetDirectoryReference(directory)
                .ListBlobsSegmentedAsync(null);
        }

        private static void FtpBlobStreams(BlobResultSegment blobs, ILogger log)
        {
            if (!blobs.Results.Any())
            {
                log.LogInformation("No blobs to ftp.");
                return;
            }

            log.LogInformation("Found blobs to ftp.");
            foreach (var blob in blobs.Results)
            {
                var blobFile = (CloudBlockBlob) blob;
                log.LogInformation($"Ftp file {blobFile.Name}");

                Chilkat.Global obj = new Global();
                obj.UnlockBundle("KARMAK.CBX032021_TDNDgEuT804Y");
                SFtp sftp = new SFtp();
                bool success = sftp.Connect("karmakqasftp.westus.cloudapp.azure.com", 22);
                if (success)
                {
                    success = sftp.AuthenticatePw("karmaksftp", "K@rmakQa2019");
                }

                if (success)
                {
                    success = sftp.InitializeSftp();
                }

                if (!success)
                {
                    Console.WriteLine(sftp.LastErrorText);
                    return;
                }

                var blobStream = GetBlob(blobFile);
                StringBuilder sb = new StringBuilder();

                var handle = sftp.OpenFile("/upload/testFile.xml", "readWrite", "createNew");
                sftp.WriteFileBytes(handle, blobStream.Result.ToArray());
                sftp.CloseHandle(handle);
                
                //client.UploadData(
                //    "ftp://karmakqasftp.westus.cloudapp.azure.com/upload/testFile.xml", 
                //    WebRequestMethods.Ftp.UploadFile, 
                //    blobStream.Result.ToArray());
            }
        }

        private static async Task<MemoryStream> GetBlob(
            CloudBlockBlob file)
        {
            var ms = new MemoryStream();

            await file.DownloadToStreamAsync(ms);

            return ms;
        }
    }
}
