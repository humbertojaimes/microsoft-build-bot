using System;
namespace CognitiveBot.Helpers
{
    public class ConversationData
    {
        public string Timestamp { get; set; }

        public string ChannelId { get; set; }

        public bool PromptedUserForName { get; set; } = false;
    }
    }

