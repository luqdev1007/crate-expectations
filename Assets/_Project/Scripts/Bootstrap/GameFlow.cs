using UnityEngine;
using VContainer.Unity;
using CrateExpectations.Core.Services;

namespace CrateExpectations.Bootstrap
{
    /// <summary>Точка входа. Пока просто подтверждает, что DI собрался. Расширится в след. фазах</summary>
    public sealed class GameFlow : IStartable
    {
        private readonly IPlatformService _platform;
        private readonly ISaveService _save;

        public GameFlow(IPlatformService platform, ISaveService save)
        {
            _platform = platform;
            _save = save;
        }

        public void Start()
        {
            Debug.Log($"[GameFlow] Boot OK. Platform.IsAvailable = {_platform.IsAvailable}");
        }
    }
}
