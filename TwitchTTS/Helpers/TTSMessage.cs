using ElevenLabs;

namespace TwitchTTS.Helpers
{
    public class TTSMessage
    {
        public required int QueueNumber { get; set; }
        public required Chatter Chatter { get; set; }
        public required string HexColour { get; set; }
        public required string Message { get; set; }
        public VoiceClip? VoiceClip { get; set; } = null;
        public bool TTSProcessing { get; set; } = false;
    }
}