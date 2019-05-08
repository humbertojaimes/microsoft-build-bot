using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CognitiveBot.Model
{
    public class ResultMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("keyPhrases")]
        public List<string> KeyPhrases { get; set; }

    }
}
