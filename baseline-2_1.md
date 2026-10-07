# Baseline, прогон 2 (задача 1, без инструкций)

| Параметр | Значение |
|---|---|
| Агент / модель | Claude Code, Opus 5.5 |
| Стартовая точка | `clean-base` (`411636c`) |
| Ссылка на сессию | https://claude.ai/code/session_01Xjwy7fpYLMGkLm1sjNJNGc |
| Промпт | `prompts/task1.md`, без изменений, без файлов и подсказок |
| Ветка результата | `baseline-attempt-2` |

## Результат проверки

### Итоговая проверка (`check.js` из `main`, коммит `7317e43`, режим diff)

```
$ node check.js --base clean-base
Началась проверка конвенций в режиме DIFF против базы: clean-base...

НАЙДЕНО НАРУШЕНИЙ КОНВЕНЦИЙ: 8

[1] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[2] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[3] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[4] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[5] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[6] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[7] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[8] README.md: В контроллерах объявлен новый HTTP-эндпоинт [Http*], но файл README.md не был обновлен в текущем коммите/диффе
```

**Нарушений: 8**

### Первая проверка (старая версия `check.js`, без режима diff), для истории

```
$ node check.js
Нарушений архитектурных конвенций не найдено
```

Старая версия проверяла только «видимые из кода» правила (слои, DTO, `Async` + `CancellationToken`, `SaveChangesAsync`), и агент их соблюдал. После неё команда добавила правила про `try/catch` в контроллерах, собственные исключения домена и README и перевела скрипт в режим diff. Числа в отчёте берутся из итоговой проверки.

Изменено 12 файлов, +567 / −3 строк:
`CarsController.cs`, `ICarRepository.cs`, `ICarCatalogService.cs`, `CarCatalogService.cs`, `Car.cs`, `CarConfiguration.cs`, `CarRepository.cs`, `CarsControllerTests.cs`, `EfPersistenceTests.cs`, `RentalRequestsControllerTests.cs`, `CarCatalogServiceTests.cs` (новый), `CarTests.cs`.

## Тесты (по словам агента)

- Unit: 95/95 прошли
- Integration: 65/65 прошли (PostgreSQL через Testcontainers)

## Ход работы (сообщения агента по ходу)

- Изучил кодовую базу (Clean Architecture, .NET 8, EF Core + Postgres, исходники в cp1251), проверил DI.
- Файлы в cp1251 правил через Python, чтобы сохранить кодировку.
- Загрузка .NET с официального хоста заблокирована. Пробовал apt: SDK из Ubuntu со старым Roslyn 4.8. В итоге скопировал SDK из Docker-образа `mcr.microsoft.com/dotnet/sdk:8.0`.
- Сам удалил один свой тавтологичный тест сервиса и заменил его тестом репозитория против EF.
- Исправил порядок членов в тестах по требованию StyleCop.
- `.editorconfig` поменял права файла во время сборки, агент откатил это перед коммитом.
- Сам запустил Docker-демон для интеграционных тестов.

## Итоговое сообщение агента (кратко)

**Реализация:**
- `Car`: nullable `DeletedAt`, методы `SoftDelete` / `Restore`. Схема создаётся через `EnsureCreated`, миграций нет, поэтому колонка добавлена только в `CarConfiguration`.
- `CarRepository` исключает удалённые авто из списка, get-by-id и get-by-VIN. Поэтому `GET /api/cars/{id}`, смена статуса и создание заявки на удалённое авто возвращают 404. Для delete и restore есть отдельный поиск, который видит удалённые авто.
- Новые эндпоинты (Manager/Admin): `DELETE /api/cars/{id}` и `POST /api/cars/{id}/restore`. Ответы: 204 при успехе, 404 для неизвестного id, 401 без логина, 403 для клиента, 400 при недопустимой операции (как в существующем эндпоинте смены статуса).
- Поведение для неудалённых авто не изменилось, все старые тесты проходят.

**Правила, которые агент добавил сам (не было в требованиях):**
- Арендованное авто удалить нельзя (400).
- VIN удалённого авто остаётся занятым (409 при добавлении нового авто с этим VIN).

**Тесты:** 13 unit-тестов (`Car` и сервис), тесты БД (запись сохраняется, удалённые фильтруются), 14 тестов эндпоинтов.

## Наблюдения (вручную, скрипт не ловит)

- Ошибки снова через `try/catch` по `KeyNotFoundException` / `InvalidOperationException` в контроллере (4 блока `catch`), а не через `ExceptionHandlingMiddleware`.
- Бизнес-ошибки бросаются как `InvalidOperationException`, а не через собственные исключения домена.
- Добавил бизнес-правило, которого не было в требованиях (запрет удалять арендованное авто). В прогоне 1 было наоборот: удаление разрешено. Поведение между прогонами отличается.
- В отличие от прогона 1, `README.md` и `RentalRequestService` не трогал.
