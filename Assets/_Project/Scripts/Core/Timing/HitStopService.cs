using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Core.Timing
{
    /// <summary>
    /// Единственный владелец <see cref="Time.timeScale"/> в игре.
    /// <para>
    /// Отсчёт идёт в нескалированном времени - иначе таймер заморозки замерял бы сам себя
    /// и при <c>scale = 0</c> не истёк бы никогда.
    /// </para>
    /// <para>
    /// Реализует <see cref="IDisposable"/> не для порядка: если сцена выгрузится посреди
    /// заморозки, <c>timeScale</c> останется нулевым и переживёт даже выход из play mode -
    /// Unity его не сбрасывает. Симптом такой утечки выглядит как поломка управления,
    /// и искать её будут в контроллере игрока. Поэтому масштаб восстанавливается
    /// и по концу заморозки, и при разрушении контейнера.
    /// </para>
    /// </summary>
    public sealed class HitStopService : IHitStopService, IDisposable
    {
        /// <summary>
        /// Масштаб, в который время возвращается после заморозки. Константа, а не
        /// запомненное перед заморозкой значение: запоминание сложилось бы само с собой,
        /// вложись одна заморозка в другую, и вернуло бы игру в нулевой масштаб
        /// </summary>
        private const float NormalTimeScale = 1f;

        private readonly CancellationTokenSource _lifetime = new();

        private float _frozenUntil;
        private bool _running;

        /// <inheritdoc />
        public bool IsFrozen => _running;

        /// <inheritdoc />
        public void Freeze(float seconds, float scale)
        {
            if (seconds <= 0f)
                return;

            float until = UnityEngine.Time.unscaledTime + seconds;

            // Уже морозим - только двигаем конец вперёд. Заново запускать цикл нельзя:
            // два цикла на один timeScale - это ровно та драка, ради которой сервис и заведён
            if (_running)
            {
                _frozenUntil = Mathf.Max(_frozenUntil, until);
                return;
            }

            _frozenUntil = until;
            _running = true;
            UnityEngine.Time.timeScale = Mathf.Clamp01(scale);

            Run(_lifetime.Token).Forget();
        }

        /// <summary>
        /// Ждём покадрово, а не одним <c>Delay</c>: конец заморозки могли отодвинуть,
        /// пока мы ждали, и одно ожидание фиксированной длины этого бы не заметило
        /// </summary>
        private async UniTaskVoid Run(CancellationToken token)
        {
            try
            {
                while (UnityEngine.Time.unscaledTime < _frozenUntil)
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            catch (OperationCanceledException)
            {
                // Контейнер разрушен посреди заморозки - выходим в finally и чиним масштаб
            }
            finally
            {
                _running = false;
                UnityEngine.Time.timeScale = NormalTimeScale;
            }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();

            // Отмена дойдёт до цикла только на следующем кадре, а его может уже не быть.
            // Чиним масштаб здесь же, синхронно
            if (_running)
            {
                _running = false;
                UnityEngine.Time.timeScale = NormalTimeScale;
            }
        }
    }
}
