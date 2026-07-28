using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace CrateExpectations.Core.Diagnostics
{
    /// <summary>
    /// Замер кадра без окна профайлера: среднее и пиковое время кадра, средний и пиковый
    /// GC Alloc за кадр. Считает окнами и печатает одну строку итога.
    ///
    /// <para>Нужен, потому что окно профайлера показывает цифры <b>редактора</b> вместе
    /// с цифрами игры, а в билде окна нет вообще. Этот же компонент работает и там,
    /// и там, и мерит одно и то же - иначе "до" и "после" не сравнить.</para>
    ///
    /// <para>Инструмент разработки: выключается снятием галки, из релизной сборки уходит
    /// вместе с объектом. Сам он в установившемся режиме не аллоцирует - строка итога
    /// собирается раз в окно, а не каждый кадр.</para>
    /// </summary>
    public sealed class FrameStatsProbe : MonoBehaviour
    {
        [Tooltip("Длина окна замера в секундах. По его концу печатается строка итога")]
        [Min(0.5f)]
        [SerializeField] private float _windowSeconds = 5f;

        [Tooltip("Пропустить первые кадры: на старте сцены грузится контент, и эти кадры " +
                 "к установившемуся режиму отношения не имеют.")]
        [Min(0)]
        [SerializeField] private int _warmupFrames = 120;

        private readonly StringBuilder _report = new(160);

        private ProfilerRecorder _gcAlloc;

        private int _frames;
        private float _elapsed;
        private float _peakFrameMs;
        private long _gcTotal;
        private long _gcPeak;
        private int _warmupLeft;

        /// <summary>Итог последнего закрытого окна. Пусто, пока первое окно не закрылось</summary>
        public string LastReport { get; private set; } = string.Empty;

        private void OnEnable()
        {
            // Счётчик существует только когда профайлер собирает данные: в редакторе он
            // включён, в Development Build - тоже, в релизной сборке счётчика нет,
            // и это не ошибка, а отсутствие данных
            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            _warmupLeft = _warmupFrames;
            ResetWindow();
        }

        private void OnDisable()
        {
            if (_gcAlloc.Valid) _gcAlloc.Dispose();
        }

        private void Update()
        {
            if (_warmupLeft > 0)
            {
                _warmupLeft--;
                return;
            }

            float frameMs = Time.unscaledDeltaTime * 1000f;
            _elapsed += Time.unscaledDeltaTime;
            _frames++;

            if (frameMs > _peakFrameMs) _peakFrameMs = frameMs;

            if (_gcAlloc.Valid)
            {
                long allocated = _gcAlloc.LastValue;
                _gcTotal += allocated;
                if (allocated > _gcPeak) _gcPeak = allocated;
            }

            if (_elapsed < _windowSeconds) return;

            Report();
            ResetWindow();
        }

        private void Report()
        {
            if (_frames == 0) return;

            float averageMs = _elapsed * 1000f / _frames;

            _report.Clear();
            _report.Append("[Кадр] среднее ").Append(averageMs.ToString("F2"))
                .Append(" мс (пик ").Append(_peakFrameMs.ToString("F2"))
                .Append("), ").Append((1f / (averageMs / 1000f)).ToString("F0")).Append(" fps");

            if (_gcAlloc.Valid)
            {
                _report.Append(" · GC ").Append(_gcTotal / _frames)
                    .Append(" Б/кадр (пик ").Append(_gcPeak).Append(" Б)");
            }
            else
            {
                _report.Append(" · GC не измеряется: профайлер выключен");
            }

            _report.Append(" · кадров ").Append(_frames);

            LastReport = _report.ToString();
            Debug.Log(LastReport, this);
        }

        private void ResetWindow()
        {
            _frames = 0;
            _elapsed = 0f;
            _peakFrameMs = 0f;
            _gcTotal = 0;
            _gcPeak = 0;
        }
    }
}
