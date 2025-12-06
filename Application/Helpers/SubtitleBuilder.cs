using Application.Models;

namespace Application.Helpers
{
    public class SubtitleBuilder
    {
        private double _currentOffset = 0;
        private readonly List<SubtitleItem> _finalList = new();

        public void AddScene(List<WordTimestamp> words, int sceneNumber, double audioDurationSec)
        {
            foreach (var word in words)
            {
                _finalList.Add(new SubtitleItem
                {
                    Word = word.Word,
                    // 🔥 SİHİR BURADA: Yerel süreye ofseti ekliyoruz
                    Start = word.Start + _currentOffset,
                    End = word.End + _currentOffset,
                    Confidence = word.Confidence,
                    SceneNumber = sceneNumber
                });
            }

            // Sayacı, bu ses dosyasının süresi kadar ileri sar
            // (Ses dosyasının sonuna, kelimelerin bittiği yerden biraz pay bırakmak gerekebilir)
            _currentOffset += audioDurationSec;
        }

        public List<SubtitleItem> Build() => _finalList;
    }
}
