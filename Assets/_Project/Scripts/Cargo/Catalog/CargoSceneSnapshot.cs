using System;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    /// <summary>
    /// Один ящик на диске: что в нём на самом деле, чем он притворяется и где стоит.
    /// Истина и заявленное сохраняются раздельно - иначе после загрузки покрашенный ром
    /// стал бы либо честным ромом, либо настоящими специями, и вся игра развалилась бы
    /// на ровном месте
    /// </summary>
    [Serializable]
    public struct CargoCrateSnapshot
    {
        /// <summary>Ключ контента истинного типа груза</summary>
        public string TypeKey;

        /// <summary>Ключ контента заявленного типа. Отличается от <see cref="TypeKey"/> - ящик переливали</summary>
        public string DeclaredTypeKey;

        /// <summary>Имя ассета окраски. Пусто - ящик не крашен</summary>
        public string PaintId;

        /// <summary>Имя ассета пломбы. Пусто - пломбы нет</summary>
        public string StampId;

        public Vector3 Position;

        public Quaternion Rotation;
    }

    /// <summary>Весь груз на локации в момент сохранения</summary>
    [Serializable]
    public struct CargoSceneSnapshot
    {
        /// <summary>Ящики. Никогда не <c>null</c> после загрузки - см. <see cref="Crates"/></summary>
        [SerializeField] private CargoCrateSnapshot[] _crates;

        public CargoSceneSnapshot(CargoCrateSnapshot[] crates) => _crates = crates;

        /// <summary>Ящики. Пустой сейв и сейв без груза здесь неразличимы - и не должны различаться</summary>
        public CargoCrateSnapshot[] Crates => _crates ?? Array.Empty<CargoCrateSnapshot>();
    }
}
