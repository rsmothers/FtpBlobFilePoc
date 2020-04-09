using Chilkat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FtpBlobFilePoc
{
    public static class FtpBlobFiles
    {
        [FunctionName("FtpBlobFiles")]
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

            Chilkat.Global obj = new Global();
            obj.UnlockBundle("KARMAK.CBX032021_TDNDgEuT804Y");
            SFtp sftp = new SFtp();
            //bool success = sftp.Connect("karmakqasftp.westus.cloudapp.azure.com", 22);
            bool success = true;
            if (success)
            {
                log.LogInformation("Connected to sftp server.");
                //success = sftp.AuthenticatePw("karmaksftp", "K@rmakQa2019");
            }

            if (success)
            {
                log.LogInformation("Successfully authenticated.");
                //success = sftp.InitializeSftp();
            }

            if (!success)
            {
                log.LogError("Error initializing sftp.");
                Console.WriteLine(sftp.LastErrorText);
                return;
            }

            foreach (var blob in blobs.Results)
            {
                var blobFile = (CloudBlockBlob) blob;
                log.LogInformation($"Next file to upload: {blobFile.Name}");

                //var blobStream = GetBlob(blobFile);
                //var handle = sftp.OpenFile("/upload/testFile.xml", "readWrite", "createNew");
                //sftp.WriteFileBytes(handle, blobStream.Result.ToArray());
                //sftp.CloseHandle(handle);
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
