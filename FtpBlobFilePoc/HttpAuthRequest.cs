using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace FtpBlobFilePoc
{
    public class HttpAuthRequest
    {
        public static HttpClient HttpClient = new HttpClient();

        public async Task<Dictionary<string, string>> BuildParamsDictionary(Stream requestBody)
        {
            var paramDictionary = new Dictionary<string, string>();
            string body = await new StreamReader(requestBody).ReadToEndAsync();
            var array = body.Split('&');
            foreach (var param in array)
            {
                var kvp = param.Split('=');
                paramDictionary.Add(kvp[0], kvp[1]);
            }

            return paramDictionary;
        }

        public List<KeyValuePair<string, string>> BuildFormUrlEncodedAuthContent(Dictionary<string, string> paramDictionary)
        {
            var formUrlEncodedContent = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", paramDictionary["client_id"]),
                new KeyValuePair<string, string>("client_secret", paramDictionary["client_secret"]),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("grant_type", "karmak_identity")
            };

            if (paramDictionary.ContainsKey("integration_test_authority"))
            {
                formUrlEncodedContent.Add(new KeyValuePair<string, string>("integration_test_authority", paramDictionary["integration_test_authority"]));
            }

            if (paramDictionary.ContainsKey("account"))
            {
                formUrlEncodedContent.Add(new KeyValuePair<string, string>("account", paramDictionary["account"]));
            }

            if (paramDictionary.ContainsKey("branch"))
            {
                formUrlEncodedContent.Add(new KeyValuePair<string, string>("branch", paramDictionary["branch"]));
            }

            if (paramDictionary.ContainsKey("user"))
            {
                formUrlEncodedContent.Add(new KeyValuePair<string, string>("user", paramDictionary["user"]));
            }

            if (paramDictionary.ContainsKey(("ContainerName")))
            {
                formUrlEncodedContent.Add(new KeyValuePair<string, string>("ContainerName", paramDictionary["ContainerName"]));
            }

            return formUrlEncodedContent;
        }

        public async Task<JToken> MakeHttpRequest(
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
