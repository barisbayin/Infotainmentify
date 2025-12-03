namespace Application.Models
{

    public class SceneImageItem
    {
        public int SceneNumber { get; set; }
        public string ImagePath { get; set; } = default!; // "/users/1/runs/10/scene_1.png"
        public string PromptUsed { get; set; } = default!; // Debug için
    }
}
