namespace Application.Contracts.Pipeline
{
    // Application/Contracts/Pipeline/PipelineDtos.cs

    public class PipelineRunListDto
    {
        // --- ✅ MEVCUT ALANLAR (Bunlara dokunmuyoruz, koruyoruz) ---
        public int Id { get; set; }

        // Bu alanı AI'dan gelen başlığı basmak için kullanacağız
        public string? RunContextTitle { get; set; }

        public string TemplateName { get; set; } = default!;

        // Senin kodunda string tutuluyormuş, string devam edelim (Enum.ToString() yaparız)
        public string Status { get; set; } = default!;

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // --- 🔥 YENİ EKLENEN ALANLAR (Frontend History Sayfası İçin) ---
        public string ConceptName { get; set; } = string.Empty; // Filtreleme ve gösterim için

        // İkonları çizmek için bu listeye mecburuz
        public List<StageExecutionSummaryDto> StageExecutions { get; set; } = new();
    }
}
