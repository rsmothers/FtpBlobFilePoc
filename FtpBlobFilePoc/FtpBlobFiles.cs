using Chilkat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FtpBlobFilePoc
{
    public class FtpBlobFiles
    {
        [FunctionName("FtpBlobFiles")]
        public async System.Threading.Tasks.Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var client = new CloudBlobClient(new Uri(Environment.GetEnvironmentVariable("StorageUri")),
                new StorageCredentials(
                    Environment.GetEnvironmentVariable("StorageAccount"),
                    Environment.GetEnvironmentVariable("key")));

            log.LogInformation($"Check for files to ftp: {DateTime.Now}");

            // get list of containers via config or table storage? - stub now for poc
            var containerList = new List<string>
            {
                "ftp-blob-file-poc"
            };

            // for each container, check for files.  if files exist, get sftp settings for uploading.
            foreach (var containerName in containerList)
            {
                var authToken = await GetAuthToken(containerName);
                log.LogInformation($"auth token acquired: {authToken}");

                FtpBlobStreams(
                    RetrieveBlobList(
                        client,
                        containerName,
                        "testFolder",
                        log).Result,
                    log);
            }
        }

        private async Task<JToken> GetAuthToken(string containerName)
        {
            // credentials would come from an alias lookup against the container name and pipeline client id/secret.
            // need auth token to access secrets and settings.
            var paramDictionary = new Dictionary<string, string>
            {
                { "client_id", "ryan_client" },
                { "client_secret", "ryanclientsecret" },
                { "ContainerName", containerName }
            };

            var httpAuthRequest = new HttpAuthRequest();
            var jsonResult = await httpAuthRequest.MakeHttpRequest(
                HttpMethod.Post,
                Environment.GetEnvironmentVariable("AuthTokenUri"),
                Environment.GetEnvironmentVariable("GatewayUrl"),
                formUrlEncodedContent: httpAuthRequest.BuildFormUrlEncodedAuthContent(paramDictionary));

            return jsonResult["access_token"];
        }

        private async Task<BlobResultSegment> RetrieveBlobList(
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

        private void FtpBlobStreams(BlobResultSegment blobs, ILogger log)
        {
            if (!blobs.Results.Any())
            {
                log.LogInformation("No blobs to ftp.");
                return;
            }

            log.LogInformation("Found blobs to ftp.");

            // get settings.  can we do this without implicit elk context in settings client since unsure if can hook up middleware?
            // is table storage a better option if the above proves to be a challenge?

            Chilkat.Global obj = new Global();
            obj.UnlockBundle("KARMAK.CBX032021_TDNDgEuT804Y");
            SFtp sftp = new SFtp();
            bool success = sftp.Connect("karmakqasftp.westus.cloudapp.azure.com", 22);
            //bool success = true;
            if (success)
            {
                log.LogInformation("Connected to sftp server.");
                success = sftp.AuthenticatePw("karmaksftp", "K@rmakQa2019");
            }

            if (success)
            {
                log.LogInformation("Successfully authenticated.");
                success = sftp.InitializeSftp();
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

                var blobStream = GetBlob(blobFile);
                var handle = sftp.OpenFile("/upload/testFile.xml", "readWrite", "createNew");
                sftp.WriteFileBytes(handle, blobStream.Result.ToArray());
                sftp.CloseHandle(handle);

                log.LogInformation("File successfully uploaded.");
                blobFile.DeleteAsync();
                log.LogInformation("Blob removed from container.");
            }
        }

        private async Task<MemoryStream> GetBlob(
            CloudBlockBlob file)
        {
            var ms = new MemoryStream();

            await file.DownloadToStreamAsync(ms);

            return ms;
        }
    }
}
