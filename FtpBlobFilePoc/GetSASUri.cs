using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace FtpBlobFilePoc
{
    public class GetSASUri
    {
        [FunctionName("GetSASUri")]
        public string Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Request new container sas token.");

            // assume we are taking the auth token from the http request and pulling account, branch, user from it here
            // may not be able to hook in middleware for explicitelkcontext

            // here we query settings to obtain the container name - stubbed for poc purposes
            var containerName = "ftp-blob-file-poc";

            return GetSharedAccessSignature(containerName, log);
        }

        private string GetSharedAccessSignature(string containerName, ILogger log)
        {
            var sasPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24),
                Permissions = 
                    SharedAccessBlobPermissions.Write 
                    | SharedAccessBlobPermissions.List
                    | SharedAccessBlobPermissions.Read
                    | SharedAccessBlobPermissions.Delete
            };

            var container = new CloudBlobContainer(
                new Uri($"{Environment.GetEnvironmentVariable("StorageUri")}/{containerName}/"),
                new StorageCredentials(
                    Environment.GetEnvironmentVariable("StorageAccount"),
                    Environment.GetEnvironmentVariable("key")));

            var sasToken = container.GetSharedAccessSignature(sasPolicy, null);

            log.LogInformation($"New token provided for container - {containerName}.");

            return container.Uri + sasToken;
        }
    }
}
