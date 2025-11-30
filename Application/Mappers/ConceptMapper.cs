using Application.Contracts.Concept;
using Core.Entity.Pipeline;

namespace Application.Mappers
{
    public static class ConceptMapper
    {
        public static ConceptListDto ToListDto(this Concept e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            CreatedAt = e.CreatedAt
        };

        public static ConceptDetailDto ToDetailDto(this Concept e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            CreatedAt = e.CreatedAt
        };
    }
}
