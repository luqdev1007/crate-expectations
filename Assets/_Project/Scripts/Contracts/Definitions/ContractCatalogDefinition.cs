using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    [CreateAssetMenu(
        fileName = "ContractCatalog",
        menuName = "CrateExpectations/Contracts/Contract Catalog")]
    public sealed class ContractCatalogDefinition : ScriptableObject
    {
        [Tooltip("Заказы, доступные игроку")]
        [SerializeField] private ContractDefinition[] _contracts = Array.Empty<ContractDefinition>();

        public IReadOnlyList<ContractDefinition> Contracts => _contracts;
    }
}
