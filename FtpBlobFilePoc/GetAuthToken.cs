using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Elk.Core.Framework.Authentication;
using Karmak.ELK.Framework.RestCommunication;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FtpBlobFilePoc
{
    public static class GetAuthToken
    {
        [FunctionName("GetAuthToken")]
        public static async Task<JToken> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetAuthToken HTTP trigger function processed a request.");

            var paramDictionary = new Dictionary<string, string>();
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            var array = body.Split('&');
            foreach (var param in array)
            {
                var kvp = param.Split('=');
                paramDictionary.Add(kvp[0], kvp[1]);
            }

            var jsonResult = await MakeHttpRequest(
                HttpMethod.Post,
                Environment.GetEnvironmentVariable("AuthTokenUri"),
                Environment.GetEnvironmentVariable("GatewayUrl"),
                formUrlEncodedContent: new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", paramDictionary["client_id"]),
                    new KeyValuePair<string, string>("client_secret", paramDictionary["client_secret"]),
                    new KeyValuePair<string, string>("scope", "api"),
                    new KeyValuePair<string, string>("grant_type", "karmak_identity"),
                    new KeyValuePair<string, string>("integration_test_authority", paramDictionary["integration_test_authority"])
                });

            return jsonResult["access_token"];
        }

        public static HttpClient HttpClient = new HttpClient();

        private static async Task<JToken> MakeHttpRequest(
            HttpMethod httpMethod,
            string requestUri,
            string baseAddress = null,
            string jsonContent = null,
            IEnumerable<KeyValuePair<string, string>> formUrlEncodedContent = null,
            string bearerToken = null)
        {
            try
            {
                if (bearerToken != null)
                {
                    HttpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                if (baseAddress != null && HttpClient.BaseAddress == null)
                {
                    HttpClient.BaseAddress = new Uri(baseAddress);
                }

                var request = new HttpRequestMessage(httpMethod, requestUri);

                if (jsonContent != null && formUrlEncodedContent != null)
                {
                    throw new ArgumentException("Cannot assign json content and form url encoded content in a single request.");
                }

                if (formUrlEncodedContent != null)
                {
                    request.Content = new FormUrlEncodedContent(formUrlEncodedContent);
                }

                if (jsonContent != null)
                {
                    request.Content =
                        new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json);
                }

                var response = await HttpClient.SendAsync(request);
                var responseBody = response.Content.ReadAsStringAsync();

                var json = responseBody.Result != ""
                    ? JToken.Parse(responseBody.Result)
                    : new JObject();

                return json;
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
