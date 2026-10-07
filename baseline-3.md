# Baseline, прогон 3 (задача 1, без инструкций)

| Параметр | Значение |
|---|---|
| Агент / модель | Claude Code, Opus 5.5 |
| Стартовая точка | `main` (`7317e43`). От `clean-base` отличается только новой версией `check.js`; читать его агенту запрещено, код проекта тот же |
| Ссылка на сессию | https://claude.ai/code/session_01HyENJ1U5MRm19ksQo29erf |
| Промпт | `prompts/task1.md`, без изменений, без файлов и подсказок |
| Ветка результата | `baseline-attempt-3` |

## Результат проверки

```
$ node check.js --base clean-base
Началась проверка конвенций в режиме DIFF против базы: clean-base...

НАЙДЕНО НАРУШЕНИЙ КОНВЕНЦИЙ: 8

[1] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[2] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[3] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[4] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[5] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...)
[6] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...)
[7] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...)
[8] README.md: В контроллерах объявлен новый HTTP-эндпоинт [Http*], но файл README.md не был обновлен
```

**Нарушений: 8** (4× `try/catch` в контроллере, 3× `InvalidOperationException` в домене, 1× эндпоинт без README).

Изменено 10 файлов кода и тестов:
`CarsController.cs`, `ICarRepository.cs`, `ICarCatalogService.cs`, `CarCatalogService.cs`, `Car.cs`, `CarConfiguration.cs`, `CarRepository.cs`, `CarsControllerTests.cs`, `CarCatalogServiceTests.cs` (новый), `CarTests.cs`.

## Тесты (по словам агента)

**Tests**
- New domain tests in `CarTests.cs`.
- New `tests/CarRental.UnitTests/Application/CarCatalogServiceTests.cs`, using Moq.
- 11 new integration tests in `CarsControllerTests.cs`. They cover the full delete → hidden → restore flow, that the row stays in the database, the invalid operations, 404s, and the 401/403 checks.

## Краткое итоговое сообщение агента

Soft deletion for cars is done and pushed to claude/sharp-wozniak-x228ib. All tests pass: 96 unit tests and 60 integration tests, the integration ones running against a real PostgreSQL started by Testcontainers.

## Наблюдения (вручную)

- Как в прогонах 1 и 2: ошибки через `try/catch` в контроллере (копирует существующий `ChangeCarStatus`), бизнес-ошибки домена через `InvalidOperationException`.
- В `Car.cs` три проверки. Одна из них — запрет удалять авто в каком-то статусе (судя по тексту исключения, вероятно, арендованное). Этого нет в требованиях, как и в прогоне 2.
- `README.md`, `RentalRequestService`, `EfPersistenceTests` не трогал.
