using UnityEngine;
using VContainer;
using VContainer.Unity;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Cargo.UI;
using CrateExpectations.Contracts;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Input;
using CrateExpectations.Core.Services;
using CrateExpectations.Economy;
using CrateExpectations.Inspection;
using CrateExpectations.Inspection.AI;
using CrateExpectations.Inspection.UI;
using CrateExpectations.Inspection.View;
using CrateExpectations.Interaction;
using CrateExpectations.Interaction.UI;
using CrateExpectations.Inventory;
using CrateExpectations.Persistence;
using CrateExpectations.Platform;
using CrateExpectations.Player;
using CrateExpectations.Player.Combat;
using CrateExpectations.UI;

namespace CrateExpectations.Bootstrap
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Tooltip("Стартовые деньги и предел долга")]
        [SerializeField] private EconomyDefinition _economy;

        [Tooltip("Заказы, которые висят на доске в этой смене")]
        [SerializeField] private ContractCatalogDefinition _contracts;

        [Tooltip("Куда игра сохраняется: ключ слота и его имя для игрока")]
        [SerializeField] private SaveSlotDefinition _saveSlot;

        [Tooltip("Стабильные идентификаторы контента груза")]
        [SerializeField] private CargoRegistryDefinition _cargoRegistry;

        [SerializeField] private InspectionDefinition _inspection;

        protected override void Configure(IContainerBuilder builder)
        {
            // events
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            // Платформа, единственное место, которое придётся тронуть, когда вместо заглушки встанет Steamworks
            builder.Register<IPlatformService, LoggingPlatformService>(Lifetime.Singleton);

            // save service
            builder.Register<ISaveService, StubSaveService>(Lifetime.Singleton);

            // addressables
            builder.Register<ICargoCatalog, AddressableCargoCatalog>(Lifetime.Singleton);

            // input
            builder.RegisterEntryPoint<InputReader>(Lifetime.Singleton).As<IInputReader>();

            // Экономика и контракты
            builder.RegisterInstance(_contracts);
            builder.RegisterInstance(_inspection);
            builder.Register<PayoutCalculator>(Lifetime.Singleton);

            builder.Register<IEconomyService>(
                resolver => new EconomyService(_economy.Rules, resolver.Resolve<IEventBus>()),
                Lifetime.Singleton);

            // Реестр груза
            builder.Register<ICargoInventory, CargoInventory>(Lifetime.Singleton);
            builder.Register<CargoRegistrar>(Lifetime.Singleton);

            // Менеджер контрактов и вывоз груза
            builder.Register<IContractManager, ContractManager>(Lifetime.Singleton);
            builder.Register<CargoHandoff>(Lifetime.Singleton);

            // Сохранения. Координатор собирает снимки у систем и отдаёт их ISaveService
            // про диск он не знает, поэтому облако Steam встанет строкой выше
            builder.RegisterInstance(_saveSlot);
            builder.RegisterInstance(_cargoRegistry);
            builder.Register<CargoSceneKeeper>(Lifetime.Singleton);
            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);
            builder.Register<SaveHotkeys>(Lifetime.Singleton);

            // Точки входа сцены
            builder.RegisterComponentInHierarchy<PlayerController>();
            builder.RegisterComponentInHierarchy<PlayerWeaponController>();
            builder.RegisterComponentInHierarchy<Interactor>();
            builder.RegisterComponentInHierarchy<Carrier>();
            builder.RegisterComponentInHierarchy<InteractionPromptView>();
            builder.RegisterComponentInHierarchy<CargoSpawner>();

            // Карточка груза: цель ей даёт Interactor, поэтому своего луча она не пускает
            builder.RegisterComponentInHierarchy<CargoInfoCard>();

            // Голос инспектора: реплики идут в пузырь над головой, а исход досмотра -
            // в удар печатью на весь экран
            builder.RegisterComponentInHierarchy<VerdictStampView>();
            builder.RegisterComponentInHierarchy<InspectorSpeechBubble>();
            builder.Register<IInspectorVoice>(
                resolver => new CompositeInspectorVoice(
                    resolver.Resolve<InspectorSpeechBubble>(),
                    resolver.Resolve<VerdictStampView>()),
                Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<InspectorAI>();

            // Доска и HUD
            builder.RegisterComponentInHierarchy<ContractBoard>();
            builder.RegisterComponentInHierarchy<BalanceView>();
            builder.RegisterComponentInHierarchy<ContractViewer>();
            builder.RegisterComponentInHierarchy<SaveStatusView>();

            builder.RegisterEntryPoint<GameFlow>();

            // Компоненты сцены резолвятся лениво
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<PlayerController>();
                container.Resolve<PlayerWeaponController>();
                container.Resolve<Interactor>();
                container.Resolve<Carrier>();
                container.Resolve<InteractionPromptView>();
                container.Resolve<CargoSpawner>();
                container.Resolve<CargoInfoCard>();
                container.Resolve<InspectorAI>();
                container.Resolve<ContractBoard>();
                container.Resolve<BalanceView>();
                container.Resolve<ContractViewer>();
                container.Resolve<SaveStatusView>();

                // Слушатели шины создаются сразу, а не при первом обращении: они должны
                // успеть подписаться до того, как появится первый ящик
                container.Resolve<CargoRegistrar>();
                container.Resolve<IContractManager>();
                container.Resolve<CargoHandoff>();

                // Учётчик груза - тоже слушатель CargoSpawned: опоздай он с подпиской,
                // стартовая партия прошла бы мимо сохранения. Горячие клавиши создаются
                // сразу по той же причине - подписаться на ввод до первого нажатия
                container.Resolve<CargoSceneKeeper>();
                container.Resolve<SaveHotkeys>();
            });
        }
    }
}
