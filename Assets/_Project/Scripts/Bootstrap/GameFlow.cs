using UnityEngine;
using VContainer.Unity;
using CrateExpectations.Core.Services;

namespace CrateExpectations.Bootstrap
{
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
