using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder;
    
namespace CognitiveBot.Helpers
{
    public static class LuisParser
    {
        public static string GetEntityValue(RecognizerResult result, string intent = "")
        {
            foreach (var entity in result.Entities)
            {
                var product = JObject.Parse(entity.Value.ToString())[Constants.ProductLabel];
                var productName = JObject.Parse(entity.Value.ToString())[Constants.ProductNameLabel];
                var email = JObject.Parse(entity.Value.ToString())[Constants.EmailLabel];
                var number = JObject.Parse(entity.Value.ToString())[Constants.NumberLabel];

                if (number != null && intent == Constants.NumberLabel)
                {
                    dynamic value = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    return $"{Constants.NumberLabel}_{value.number[0].text}";
                }

                if (product != null)
                    return $"{Constants.ProductLabel}_";

                if (productName != null)
                {
                    dynamic value = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    return $"{Constants.ProductNameLabel}_{value.ProductName[0].text}";
                }

                if (email != null)
                {
                    dynamic value = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    return $"{Constants.EmailLabel}_{value.email[0].text}";
                }
            }

            return "_";
        }
    }
}
