using Core.Entity.Pipeline;
using Core.Enums;

namespace Application.Pipeline
{
    public class PipelineContext
    {
        // Hafızadaki veri havuzu: Hangi stage ne üretti?
        private readonly Dictionary<StageType, object> _stageOutputs = new();

        // Çalışmakta olan Run ve Template bilgisine her yerden erişebilmek için
        public ContentPipelineRun Run { get; }

        public PipelineContext(ContentPipelineRun run)
        {
            Run = run;

            // Eğer "Resume" (kaldığı yerden devam) senaryosu varsa,
            // daha önce tamamlanmış stage'lerin outputlarını DB'den yükleyip
            // buraya dolduran bir kod bloğu buraya eklenebilir.
            // HydrateFromHistory(run.StageExecutions);
        }

        /// <summary>
        /// Bir stage işini bitirince veriyi buraya atar.
        /// </summary>
        public void SetOutput(StageType stage, object data)
        {
            _stageOutputs[stage] = data;
        }

        /// <summary>
        /// Diğer stage'ler veriyi buradan okur.
        /// </summary>
        public T GetOutput<T>(StageType stage)
        {
            if (_stageOutputs.TryGetValue(stage, out var data))
            {
                if (data is T typedData)
                    return typedData;

                throw new InvalidCastException($"Kanka, '{stage}' verisi var ama istediğin tipte ({typeof(T).Name}) değil!");
            }

            throw new KeyNotFoundException($"Kanka, '{stage}' verisine erişmeye çalışıyorsun ama o stage henüz çalışmamış veya veri üretmemiş.");
        }

        /// <summary>
        /// Veri var mı yok mu kontrolü (Optional durumlar için)
        /// </summary>
        public bool HasOutput(StageType stage) => _stageOutputs.ContainsKey(stage);
    }
}
