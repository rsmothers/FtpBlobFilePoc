using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace FtpBlobFilePoc
{
    public static class GetSASUri
    {
        [FunctionName("GetSASUri")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Request new container sas token.");

            // todo: extract ident from auth token being passed in to validate.  how to associate ident to container? - settings?

            return GetSharedAccessSignature(log);
        }

        private static string GetSharedAccessSignature(ILogger log)
        {
            var sasPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
            };

            var container = new CloudBlobContainer(
                new Uri("https://rsmothersstorage.blob.core.windows.net/ftp-blob-file-poc/"),
                new StorageCredentials(
                    "rsmothersstorage",
                    "Z5BlDnPCAvfQEXu9Bi/4Zeu1hqk//CFgdzagOJlghGCLABtCwCIOmlklJBFYdt9kMoCIlCm2XyJw+asNKG1Mqg=="));

            var sasToken = container.GetSharedAccessSignature(sasPolicy, null);

            log.LogInformation($"New token provided for ftp-blob-file-poc container: {container.Uri + sasToken}");

            return container.Uri + sasToken;
        }
    }
}
