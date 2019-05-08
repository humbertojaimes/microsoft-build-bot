using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CognitiveBot.Model;
using Newtonsoft.Json;

namespace CognitiveBot.Helpers
{
    public class TextAnalysisHelper
    {
        public static async Task<ResultDocument> MakeKeywordAnalysisAsync(IEnumerable<RequestMessage> messages, string endpoint)
        {
            ResultDocument messageAnalysis = null;
            HttpClient httpClient = new HttpClient();

            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(new RequestDocument() { Messages = messages }));
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");


            var url = $"http://{endpoint}/text/analytics/v2.0/keyPhrases";
            Console.WriteLine(url);
            Console.WriteLine(JsonConvert.SerializeObject(new RequestDocument() { Messages = messages }));
            using (HttpResponseMessage responseMessage = await httpClient.PostAsync(url, httpContent))
            {
                responseMessage.EnsureSuccessStatusCode();

                if (responseMessage.IsSuccessStatusCode)
                {
                    string stringResponse = await responseMessage.Content.ReadAsStringAsync();
                    messageAnalysis =
                        JsonConvert.DeserializeObject<ResultDocument>(
                            stringResponse);
                }
            }
            return messageAnalysis;
        }


        public static async Task<LuisResult> MakeLUISAnalysisAsync(string message, string endpoint, string appId)
        {
            LuisResult messageAnalysis = null;
            HttpClient httpClient = new HttpClient();
            
            string url = $"http://{endpoint}/luis/v2.0/apps/{appId}?q={message}&staging=false&timezoneOffset=0&verbose=false&log=true";

            Console.WriteLine(url);

            using (HttpResponseMessage responseMessage = await httpClient.GetAsync(url))
            {
                responseMessage.EnsureSuccessStatusCode();

                if (responseMessage.IsSuccessStatusCode)
                {
                    string stringResponse = await responseMessage.Content.ReadAsStringAsync();
                    messageAnalysis =
                        JsonConvert.DeserializeObject<LuisResult>(
                            stringResponse);
                }
            }
            return messageAnalysis;
        }
    }
}
