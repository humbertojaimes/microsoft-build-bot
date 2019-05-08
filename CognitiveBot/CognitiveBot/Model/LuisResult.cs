using System;
using System.Collections.Generic;

namespace CognitiveBot.Model
{
    public class TopScoringIntent
    {
        public string Intent { get; set; }
        public double Score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string Type { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public double Score { get; set; }
    }

    public class LuisResult
    {
        public string Query { get; set; }
        public TopScoringIntent TopScoringIntent { get; set; }
        public List<Entity> Entities { get; set; }
    }
}
