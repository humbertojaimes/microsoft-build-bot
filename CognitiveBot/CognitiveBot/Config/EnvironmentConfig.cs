using System;
namespace CognitiveBot.Config
{
    public class EnvironmentConfig
    {

        public String DataSource { get; set; }

        public String DbUser { get; set; }

        public String Password { get; set; }

        public String LanguageEndPoint { get; set; }

        public String TextEndPoint { get; set; }

        public String LuisEndPoint { get; set; }

        public String LuisAppId { get; set; }

        public String DirectLine { get; set; }

    }
}
