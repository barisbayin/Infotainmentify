using Application.Contracts.Prompts;
using Core.Entity;

namespace Application.Contracts.Mappers
{
    public static class PromptMapper
    {
        public static PromptListDto ToListDto(this Prompt e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Category = e.Category,
            Language = e.Language,
            // Description gridde uzun kaçabilir, istersen kırpabilirsin:
            Description = e.Description?.Length > 50 ? e.Description[..47] + "..." : e.Description,
            IsActive = e.IsActive,
            UpdatedAt = e.UpdatedAt
        };

        public static PromptDetailDto ToDetailDto(this Prompt e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Category = e.Category,
            Language = e.Language,
            Description = e.Description,
            IsActive = e.IsActive,
            Body = e.Body,
            SystemPrompt = e.SystemPrompt,
            CreatedAt = e.CreatedAt // <--- BUNU EKLEMELİSİN
        };
    }
}
