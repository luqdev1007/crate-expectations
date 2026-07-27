# Готовность к Steamworks

Короткая заметка о том, что́ придётся сделать, когда в проект придёт настоящий Steamworks SDK,
и почему это будет правкой в одном файле, а не рефакторингом.

## Что уже сделано

Игровой код **нигде** не знает, кто стоит за платформой и за сохранением. Он видит два
интерфейса из `Core/Services`:

```csharp
public interface IPlatformService
{
    bool IsAvailable { get; }
    UniTask UnlockAchievementAsync(string achievementId);
}

public interface ISaveService
{
    UniTask SaveAsync<T>(string key, T data);
    UniTask<T> LoadAsync<T>(string key);
    UniTask<bool> ExistsAsync(string key);
}
```

Оба асинхронны с первого дня - не потому, что запись в файл этого требует, а потому что
её потребует облако. Появись `async` только вместе со Steam, менять пришлось бы все вызовы
разом; сейчас на месте останется даже сигнатура.

Кто эти интерфейсы вообще упоминает - весь список:

| Файл | Зачем |
| --- | --- |
| `Core/Services/IPlatformService.cs`, `ISaveService.cs` | сами интерфейсы |
| `Platform/*` | реализации |
| `Bootstrap/GameLifetimeScope.cs` | composition root - единственное место, где выбирается реализация |
| `Bootstrap/GameFlow.cs` | получает их инъекцией |
| `Persistence/GameStateService.cs` | пользуется `ISaveService` через конструктор |

## Проверка подменой, а не обещанием

Абстракция чего-то стоит, только если по ней хоть раз проехали. Поэтому в проекте **две**
реализации `IPlatformService`: `StubPlatformService` (ничего не умеет, `IsAvailable == false`)
и `LoggingPlatformService` (считает себя доступной, помнит выданные достижения и не выдаёт
одно и то же дважды - ровно так ведёт себя Steam).

Переход с одной на другую - это диф:

```
Assets/_Project/Scripts/Platform/LoggingPlatformService.cs   (новый файл)
Assets/_Project/Scripts/Bootstrap/GameLifetimeScope.cs       (одна строка регистрации)
```

Ни один файл игрового кода не изменён. Проверяется по логу на старте - `GameFlow` печатает
`Platform.IsAvailable`, и значение меняется с `False` на `True` без единой правки в самом
`GameFlow`.

То же самое для сохранений сделано тестом: `FakeSaveService` из `Persistence.Tests` встаёт
на место `StubSaveService`, и `GameStateService` этого не замечает.

## Что делать при реальной интеграции

1. **Пакет и инициализация.** Добавить Steamworks.NET, поднять API в `SteamPlatformService`
   (инициализация в конструкторе, `SteamAPI.RunCallbacks()` - из entry point контейнера).
2. **Достижения.** `SteamPlatformService : IPlatformService` - `UnlockAchievementAsync`
   ложится на `SteamUserStats.SetAchievement` + `StoreStats`. Повторная выдача, как и в
   `LoggingPlatformService`, - не ошибка.
3. **Облачные сохранения.** `SteamCloudSaveService : ISaveService` - `SaveAsync` на
   `SteamRemoteStorage.FileWrite`, `LoadAsync` на `FileRead`, `ExistsAsync` на `FileExists`.
   Ключ слота уже живёт в `SaveSlotDefinition` и для облака становится именем файла в Cloud -
   менять не придётся ничего, включая формат: `GameSnapshot` версионирован.
4. **Composition root.** Заменить две строки в `GameLifetimeScope.Configure`. Всё.

Разумная страховка на будущее - `FallbackSaveService`, который пробует облако и уходит на диск,
когда Steam недоступен. Это тоже реализация `ISaveService` и тоже правка одной строки
в composition root.

## Где в игровом цикле логично выдавать достижения

Игровой цикл уже публикует события в шину, так что достижения не потребуют новых крючков
в системах - хватит одного подписчика (`AchievementListener`) рядом с `Platform`:

| Событие | Достижение |
| --- | --- |
| `CargoInspected` с вердиктом "пропустить" на ящике, чей заявленный тип не совпал с истинным | первый удавшийся обман инспектора |
| `ContractCompleted` | первый закрытый заказ |
| `ContractFailed` | первый сорванный заказ |
| `WentBankrupt` | первое банкротство |
| `GameSaved` | (не достижение, но та же схема - сюда же ляжет синхронизация с облаком) |

Важно, что ни `ContractManager`, ни `InspectorAI` про достижения знать не будут: подписчик
живёт отдельно и получает `IPlatformService` инъекцией.
