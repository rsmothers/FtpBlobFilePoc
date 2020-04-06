using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // can i see a blob file?
            //var result = BlobExistsOnCloud(
            //    client,
            //    "ftp-blob-file-poc",
            //    "testFolder/testfile.xml");

            //if (result.Result)
            //{
            //    log.LogInformation($"Found blob testfile.xml");
            //}
            //else
            //{
            //    log.LogInformation("blob not found.");
            //}

            // this is how i will look for files
            var filesPresent = FilesToFtp(
                client,
                "ftp-blob-file-poc",
                "testFolder",
                log);

            log.LogInformation(
                filesPresent.Result.Results.Any() 
                    ? "Files present to ftp." 
                    : "No files detected.");
        }

        //public static async Task<bool> BlobExistsOnCloud(CloudBlobClient client,
        //    string containerName, string key)
        //{
        //    return await client.GetContainerReference(containerName)
        //        .GetBlockBlobReference(key)
        //        .ExistsAsync();
        //}

        public static async Task<BlobResultSegment> FilesToFtp(
            CloudBlobClient client, 
            string containerName, 
            string directory, 
            ILogger log)
        {
            var fileList = await client
                .GetContainerReference(containerName)
                .GetDirectoryReference(directory)
                .ListBlobsSegmentedAsync(null);

            foreach (var listBlobItem in fileList.Results)
            {
                var blob = (CloudBlockBlob) listBlobItem;
                log.LogInformation($"File found: {blob.Name}");
            }

            return fileList;
        }
    }
}
