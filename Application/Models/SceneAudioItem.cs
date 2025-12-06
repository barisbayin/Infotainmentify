namespace Application.Models
{

    public class SceneAudioItem
    {
        public int SceneNumber { get; set; }
        public string AudioFilePath { get; set; } = default!; // "/users/1/runs/10/audio/scene_1.mp3"
        public string TextSpoken { get; set; } = default!;    // Ne söylendi?
        // public double DurationSec { get; set; } // İleride FFmpeg ile hesaplayıp buraya yazarız
    }
}
