using UnityEngine;

namespace CrateExpectations.Core.Services
{
    /// <summary>
    /// Реализация <see cref="IPlayerTarget"/> поверх одного трансформа.
    /// <para>
    /// Принимает <see cref="Transform"/>, а не компонент игрока, намеренно: так адаптер
    /// остаётся в <c>Core</c> и ничего не знает про модуль <c>Player</c>. Кто именно
    /// окажется целью, решает тот, кто её собирает, - сейчас это <c>GameLifetimeScope</c>,
    /// который берёт трансформ у уже зарегистрированного <c>PlayerController</c>.
    /// </para>
    /// <para>
    /// Ссылка берётся один раз и живёт со scope. Игрока в этой игре не пересоздают:
    /// он существует всю смену, и следить за его заменой означало бы обслуживать
    /// случай, которого нет
    /// </para>
    /// </summary>
    public sealed class PlayerTarget : IPlayerTarget
    {
        private readonly Transform _transform;

        public PlayerTarget(Transform transform) => _transform = transform;

        /// <inheritdoc />
        public bool Exists => _transform != null;

        /// <inheritdoc />
        public Vector3 Position => _transform == null ? Vector3.zero : _transform.position;

        /// <inheritdoc />
        public Transform Transform => _transform;
    }
}
