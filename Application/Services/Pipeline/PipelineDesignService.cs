using Core.Contracts;
using Core.Entity.Pipeline;

namespace Application.Services.Pipeline
{
    public class PipelineDesignService
    {
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IRepository<StageConfig> _stageRepo;
        private readonly IUnitOfWork _uow;

        public PipelineDesignService(IRepository<ContentPipelineTemplate> repo, IRepository<StageConfig> stageRepo, IUnitOfWork uow)
        {
            _templateRepo = repo;
            _stageRepo = stageRepo;
            _uow = uow;
        }

        public async Task<int> CreateTemplateAsync(ContentPipelineTemplate template, List<StageConfig> stages, int userId)
        {
            // 1. Template'i kaydet
            template.AppUserId = userId;
            await _templateRepo.AddAsync(template);
            await _uow.SaveChangesAsync(); // ID oluşsun

            // 2. Stage'leri sırala ve bağla
            int order = 1;
            foreach (var stage in stages)
            {
                stage.ContentPipelineTemplateId = template.Id;
                stage.Order = order++;
                await _stageRepo.AddAsync(stage);
            }

            await _uow.SaveChangesAsync();
            return template.Id;
        }
    }
}
