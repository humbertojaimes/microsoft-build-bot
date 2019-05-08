using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CognitiveBot.Model
{
    public class ResultDocument
    {
        [JsonProperty("documents")]
        public IEnumerable<ResultMessage> ResultMessages { get; set; }
    }
}
