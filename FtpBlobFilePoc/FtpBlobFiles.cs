using Chilkat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
using Task = System.Threading.Tasks.Task;

namespace FtpBlobFilePoc
{
    public class FtpBlobFiles
    {
        [FunctionName("FtpBlobFiles")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var client = new CloudBlobClient(new Uri(Environment.GetEnvironmentVariable("StorageUri")),
                new StorageCredentials(
                    Environment.GetEnvironmentVariable("StorageAccount"),
                    Environment.GetEnvironmentVariable("key")));

            log.LogInformation($"Check for files to ftp: {DateTime.Now}");

            // get list of containers via config or table storage? - stub now for poc
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic jsonContainerList = JsonConvert.DeserializeObject(body);

            // for each container, check for files.  if files exist, get sftp settings for uploading.
            foreach (var container in jsonContainerList.containers)
            {
                FtpBlobStreams(
                    client.GetContainerReference(container.ToString()),
                    RetrieveBlobList(
                        client,
                        container.ToString(),
                        "testFolder",
                        log).Result,
                    log);
            }
        }

        private async Task<JToken> GetAuthToken(string account, string branch, string user)
        {
            var paramDictionary = new Dictionary<string, string>
            {
                { "client_id", Environment.GetEnvironmentVariable("client_id") },
                { "client_secret", Environment.GetEnvironmentVariable("client_secret") },
                { "account", account },
                { "branch", branch },
                { "user", user }
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

        private async Task FtpBlobStreams(CloudBlobContainer container, BlobResultSegment blobs, ILogger log)
        {
            await StubBlobContainerMetadata(container);

            if (!blobs.Results.Any())
            {
                log.LogInformation("No blobs to ftp.");
                return;
            }

            log.LogInformation("Found blobs to ftp.");
            
            Chilkat.Global obj = new Global();

            // this will come from key vault
            obj.UnlockBundle("KARMAK.CBX032021_TDNDgEuT804Y");

            SFtp sftp = new SFtp();

            // resolve to auth token from container identity metadata
            var authToken = await GetAuthToken(
                ExtractMetadataValue(container, "X-Elk-Identity-Account"),
                ExtractMetadataValue(container, "X-Elk-Identity-Branch"),
                ExtractMetadataValue(container, "X-Elk-Identity-User"));

            log.LogInformation($"auth token acquired: {authToken}");

            // these will come from settings via container metadata
            bool success = sftp.Connect("karmakqasftp.westus.cloudapp.azure.com", 22);

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

                await blobFile.FetchAttributesAsync();
                log.LogInformation("Extracting identity metadata from blob file.");

                var blobStream = GetBlobStream(blobFile);

                log.LogInformation($"Uploading {blobFile.Name}");
                var handle = sftp.OpenFile("/upload/testFile.xml", "readWrite", "createNew");
                sftp.WriteFileBytes(handle, blobStream.Result.ToArray());
                sftp.CloseHandle(handle);
                log.LogInformation("File successfully uploaded.");

                await blobFile.DeleteAsync();
                log.LogInformation("Blob removed from container.");
            }
        }

        private async Task StubBlobContainerMetadata(CloudBlobContainer container)
        {
            // stubbing the metadata on this blob for poc purposes
            container.Metadata.Add("X_Elk_Identity_Account", "a14f84af_b3fb_4ac7_b610_1cf192a55ef6");
            container.Metadata.Add("X_Elk_Identity_Branch", "eb6f62d9_0700_4bad_bcc6_595266a957fd");
            container.Metadata.Add("X_Elk_Identity_User", "1223fdd3_e865_4e32_9aae_6e59061c21b3");
            await container.SetMetadataAsync();
        }

        private string ExtractMetadataValue(CloudBlobContainer container, string key)
        {
            var value = container.Metadata[key.Replace('-','_')];

            return value.Replace('_', '-');
        }

        private async Task<MemoryStream> GetBlobStream(
            CloudBlockBlob file)
        {
            var ms = new MemoryStream();
            await file.DownloadToStreamAsync(ms);
            return ms;
        }
    }
}
