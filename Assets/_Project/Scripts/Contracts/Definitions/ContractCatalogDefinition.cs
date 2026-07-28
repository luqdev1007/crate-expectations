using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Что висит на доске в этой смене. Отдельный ассет от самих заказов: набор предложений
    /// меняется от уровня к уровню, а сами заказы при этом переиспользуются
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContractCatalog",
        menuName = "CrateExpectations/Contracts/Contract Catalog")]
    public sealed class ContractCatalogDefinition : ScriptableObject
    {
        [Tooltip("Заказы, доступные игроку. Порядок - порядок объявлений на доске")]
        [SerializeField] private ContractDefinition[] _contracts = Array.Empty<ContractDefinition>();

        /// <summary>Доступные заказы</summary>
        public IReadOnlyList<ContractDefinition> Contracts => _contracts;
    }
}
