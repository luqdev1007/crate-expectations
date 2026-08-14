using UnityEngine;
using VContainer;
using VContainer.Unity;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Cargo.UI;
using CrateExpectations.Combat;
using CrateExpectations.Contracts;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Hands;
using CrateExpectations.Core.Input;
using CrateExpectations.Core.Services;
using CrateExpectations.Core.Timing;
using CrateExpectations.Economy;
using CrateExpectations.Guards;
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
using CrateExpectations.Player.View;
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

        [Tooltip("Вьюмодель рук. Ссылкой, а не поиском по иерархии: поиск отдаёт первое " +
                 "совпадение в порядке объектов сцены, то есть привязка зависит от этого " +
                 "порядка и молча ломается, когда его меняют. Ссылка детерминирована, " +
                 "а если её забыть - это видно сразу")]
        [SerializeField] private ViewModelBody _viewModelBody;

        [Tooltip("Стойка вьюмодели по занятости рук. Отдельной ссылкой, а не поиском " +
                 "рядом с вьюмоделью: слой живёт на корне вьюмодели, а рендерер рук - " +
                 "на модели под ним, и связывать их местоположением значит запретить " +
                 "их когда-либо разнести")]
        [SerializeField] private ViewModelStance _viewModelStance;

        [Tooltip("Физическое тело игрока: оно тоже показывает и прячет руки, и по тому же " +
                 "правилу занятости, что и вьюмодель, - только наоборот. Без этой ссылки " +
                 "телесные руки останутся видимыми всегда, то есть вместе с вьюмодельными")]
        [SerializeField] private PlayerBodyView _playerBody;

        protected override void Configure(IContainerBuilder builder)
        {
            // events
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            // Платформа, единственное место, которое придётся тронуть, когда вместо заглушки встанет Steamworks
            builder.Register<IPlatformService, LoggingPlatformService>(Lifetime.Singleton);

            // save service
            builder.Register<ISaveService, StubSaveService>(Lifetime.Singleton);

            // Единственный владелец Time.timeScale. Синглтон здесь не для удобства:
            // два экземпляра сервиса - это два таймера на одну глобальную величину
            builder.Register<IHitStopService, HitStopService>(Lifetime.Singleton);

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

            // Заглушка смерти игрока. В отличие от его же здоровья, регистрируется
            // обычным способом: PlayerDeath в сцене ровно один, стражники такого
            // компонента не носят - им смерть отыгрывает GuardDeath
            builder.RegisterComponentInHierarchy<PlayerDeath>();

            builder.RegisterComponentInHierarchy<PlayerWeaponController>();
            builder.RegisterComponentInHierarchy<MeleeHitStop>();
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

            // Занятость рук. Регистрируется фабрикой, а не типом, намеренно: источники
            // модели - те же компоненты, которые её потом спрашивают, и обычная инъекция
            // замкнула бы граф зависимостей сам на себя. Фабрика для анализатора
            // непрозрачна, а порядок вызова мы задаём руками ниже
            builder.Register(
                resolver => new HandsState(
                    resolver.Resolve<Carrier>(),
                    resolver.Resolve<PlayerWeaponController>(),
                    resolver.Resolve<ContractViewer>()),
                Lifetime.Singleton);

            // Водитель параметров рук. Источником занятости он не является, поэтому берёт
            // модель обычной инъекцией - замкнуть граф ему нечем
            builder.RegisterComponentInHierarchy<HandsAnimatorDriver>();

            builder.RegisterEntryPoint<GameFlow>();

            // Компоненты сцены резолвятся лениво
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<PlayerController>();
                container.Resolve<PlayerDeath>();
                container.Resolve<PlayerWeaponController>();
                container.Resolve<MeleeHitStop>();
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

                // Строго после того, как компоненты выше уже созданы и проинъектированы:
                // фабрика модели резолвит три из них, и на непрогретом контейнере это
                // ушло бы в рекурсию. Раздаём модель отдельным вызовом, а не инъекцией,
                // по той же причине - три из четырёх получателей сами являются её источниками
                HandsState hands = container.Resolve<HandsState>();

                // Переноски в этом списке нет намеренно: гейт «можно ли взять» переехал
                // к тому, кто выбирает цель под прицелом, - в Interactor. Переноска
                // осталась источником занятости, но сама её ни о чём не спрашивает
                container.Resolve<Interactor>().BindHands(hands);
                container.Resolve<PlayerWeaponController>().BindHands(hands);
                container.Resolve<ContractViewer>().BindHands(hands);
                BindViewModel(hands);

                InjectGuards(container);
                InjectPlayerHealth(container);

                // Резолвится именно ЗДЕСЬ - после источников: его инъекция тянет ту же
                // фабрику, и на непрогретом контейнере она ушла бы в рекурсию по тем же
                // трём компонентам
                container.Resolve<HandsAnimatorDriver>();
            });
        }

        /// <summary>
        /// Инъецирует ВСЕХ стражников сцены поимённо.
        /// <para>
        /// Единственное место, где схема регистрации отличается от остальной сцены, и
        /// отличается вынужденно. <c>RegisterComponentInHierarchy</c> находит РОВНО ОДИН
        /// компонент типа - первый в иерархии; вся сцена на этом и стоит, потому что
        /// игрок, инспектор и доска существуют в единственном числе. Стражников будет
        /// несколько, и второй с третьим получили бы пустые поля БЕЗ ошибки в консоли:
        /// контейнер про них просто не узнал бы.
        /// </para>
        /// <para>
        /// Поиск по сцене здесь оправдан ровно потому, что число объектов заранее
        /// неизвестно - ссылкой в инспекторе такое не выражается. Разовый, в момент
        /// сборки контейнера, не в кадре. Выключённые стражники тоже инъецируются:
        /// объект могли погасить до старта и зажечь по ходу смены.
        /// </para>
        /// </summary>
        private static void InjectGuards(IObjectResolver container)
        {
            GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GuardAI guard in guards)
                container.InjectGameObject(guard.gameObject);
        }

        /// <summary>
        /// Инъецирует здоровье игрока - точечно, одним компонентом.
        /// <para>
        /// <c>RegisterComponentInHierarchy</c> тут не годится ровно по той же причине,
        /// что и у стражников: <see cref="HealthComponent"/> в сцене теперь не один -
        /// он у игрока и у каждого стражника, - а регистрация «первый найденный»
        /// молча отдала бы контейнеру случайного из них.
        /// </para>
        /// <para>
        /// Инъецировать объект игрока целиком, как стражника, тоже нельзя, и это
        /// несимметрично намеренно: <c>InjectGameObject</c> идёт по ВСЕМ детям, а среди
        /// детей игрока есть компоненты, чей <c>Construct</c> не только раскладывает
        /// ссылки, но и подписывается на события (<c>HandsAnimatorDriver</c> на бросок
        /// переноски). Повторная инъекция подписала бы их вторым разом, и бросок
        /// отыгрался бы дважды. У стражника таких нет, поэтому там объект целиком безопасен.
        /// </para>
        /// </summary>
        private static void InjectPlayerHealth(IObjectResolver container)
        {
            // От контроллера, а не поиском по сцене: здоровье игрока лежит на том же
            // корне, и связывать их этим фактом честнее, чем брать первое совпадение
            var health = container.Resolve<PlayerController>().GetComponent<HealthComponent>();

            if (health == null)
            {
                Debug.LogError(
                    "На игроке нет HealthComponent - боевые события в шину не пойдут, " +
                    "и убить игрока будет нечем.");

                return;
            }

            container.Inject(health);
        }

        /// <summary>
        /// Отдаёт занятость рукам в кадре. Незаполненная ссылка - это ошибка сборки сцены,
        /// а не «вьюмодели тут нет»: без неё руки просто никогда не появятся, и найти
        /// причину по пустому кадру нельзя.
        /// <para>
        /// Получателей трое, и проверяются они по отдельности: без рендерера рук не будет
        /// видно ничего, без стойки руки появятся, но в переноске уедут под нижнюю кромку,
        /// а без тела в кадре окажутся сразу две пары рук - своя и вьюмодельная.
        /// Все три отказа тише первого, поэтому и сообщения у них свои
        /// </para>
        /// </summary>
        private void BindViewModel(HandsState hands)
        {
            if (_playerBody == null)
            {
                Debug.LogError(
                    $"Сцене '{gameObject.scene.name}' не назначено тело игрока " +
                    "(GameLifetimeScope._playerBody) - телесные руки останутся видимыми " +
                    "и при занятых руках, то есть вместе с руками вьюмодели.", this);
            }
            else
            {
                _playerBody.BindHands(hands);
            }

            if (_viewModelBody == null)
            {
                Debug.LogError(
                    $"Сцене '{gameObject.scene.name}' не назначена вьюмодель рук " +
                    "(GameLifetimeScope._viewModelBody) - руки в кадре не появятся.", this);
            }
            else
            {
                _viewModelBody.BindHands(hands);
            }

            if (_viewModelStance == null)
            {
                Debug.LogError(
                    $"Сцене '{gameObject.scene.name}' не назначена стойка вьюмодели " +
                    "(GameLifetimeScope._viewModelStance) - кадрирование перестанет " +
                    "различать занятость, и в переноске кисти уйдут под кромку кадра.", this);

                return;
            }

            _viewModelStance.BindHands(hands);
        }
    }
}
