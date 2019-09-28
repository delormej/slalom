using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;

namespace SlalomTracker.Cloud
{ 
    public class BlobRestApi
    {
        public static List<String> GetBlobs(string storageAccountName, string storageAccountKey, string container) 
        {
            CancellationToken cancel = new CancellationToken();
            Task<List<string>> listTask = ListBlobsAsync(storageAccountName, storageAccountKey, container, cancel);
            listTask.Wait(cancel);
            return listTask.Result;
        }

        /// <summary>
        /// This is the method to call the REST API to retrieve a list of
        /// containers in the specific storage account.
        /// This will call CreateRESTRequest to create the request, 
        /// then check the returned status code. If it's OK (200), it will 
        /// parse the response and show the list of containers found.
        /// </summary>
        private static async Task<List<string>> ListBlobsAsync(string storageAccountName, 
            string storageAccountKey, string container, CancellationToken cancellationToken)
        {
            // Construct the URI. This will look like this:
            //   https://myaccount.blob.core.windows.net/resource
            String uri = string.Format("http://{0}.blob.core.windows.net/{1}?restype=container&comp=list", 
                storageAccountName, container);

            // Set this to whatever payload you desire. Ours is null because 
            //   we're not passing anything in.
            Byte[] requestPayload = null;

            //Instantiate the request message with a null payload.
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {

                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2009-09-19");
                // If you need any additional headers, add them here before creating
                //   the authorization header. 

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);

                return await GetResults(httpRequestMessage, cancellationToken);
            }
        }

        private static async Task<List<string>> GetResults(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken) 
        {
            List<string> results = new List<string>();

            // Send the request.
            using (HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
            {
                // If successful (status code = 200), 
                //   parse the XML response for the blob .
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                    XElement x = XElement.Parse(xmlString);
                    var elements = x.XPathSelectElements("/Blobs/Blob/Url");
                    foreach (var element in elements) 
                    {
                        results.Add(element.Value);
                    }
                }
            }
            return results;
        }
    }
}