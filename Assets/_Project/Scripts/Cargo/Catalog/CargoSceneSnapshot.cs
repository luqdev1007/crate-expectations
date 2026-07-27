using System;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    [Serializable]
    public struct CargoCrateSnapshot
    {
        /// <summary>Ключ контента истинного типа груза</summary>
        public string TypeKey;

        /// <summary>Ключ контента заявленного типа, если отличается, то ящик переливали</summary>
        public string DeclaredTypeKey;

        /// <summary>Имя ассета окраски, если пусто ящик не крашен</summary>
        public string PaintId;

        /// <summary>Имя ассета пломбы, если пусто, то пломбы нет</summary>
        public string StampId;

        public Vector3 Position;

        public Quaternion Rotation;
    }

    [Serializable]
    public struct CargoSceneSnapshot
    {
        [SerializeField] private CargoCrateSnapshot[] _crates;

        public CargoSceneSnapshot(CargoCrateSnapshot[] crates) => _crates = crates;

        public CargoCrateSnapshot[] Crates => _crates ?? Array.Empty<CargoCrateSnapshot>();
    }
}
