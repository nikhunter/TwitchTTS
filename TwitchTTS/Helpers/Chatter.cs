using ElevenLabs.Voices;

namespace TwitchTTS.Helpers
{
    public class Chatter
    {
        public required string Username { get; set; }
        public Voice Voice { get; set; } = new Voice("Random", "Random");
        public bool TTSActive { get; set; } = false;
    }
}