namespace Application.Models
{
    public class SpeechToTextResult
    {
        public string Transcript { get; set; } = default!;
        public List<WordTimestamp> Words { get; set; } = new();
    }

}
