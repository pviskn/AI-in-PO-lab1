# Задача 2 без инструкций: отмена заявки клиентом

| Параметр | Значение |
|---|---|
| Агент / модель | Claude Code, Opus 5.5 |
| Стартовая точка | `task2-baseline` = `clean-base` (`411636c`), без инструкций |
| Ссылка на сессию | https://claude.ai/code/session_018hwVSrxr1NqbaH8GvY6TTw |
| Промпт | `prompts/task2.md`, тот же, что для `task2-with`, без файлов и подсказок |
| Ветка результата | `task2-baseline`, коммиты `bc93465` (фича) и `3c0abba` (возврат прав файла `.editorconfig`) |

## Результат проверки

```
$ node check.js --base clean-base
Началась проверка конвенций в режиме DIFF против базы: clean-base...

НАЙДЕНО НАРУШЕНИЙ КОНВЕНЦИЙ: 6

[1] src/CarRental.Api/Controllers/RentalRequestsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[2] src/CarRental.Api/Controllers/RentalRequestsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[3] src/CarRental.Application/Services/RentalRequestService.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[4] src/CarRental.Application/Services/RentalRequestService.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[5] src/CarRental.Domain/Entities/RentalRequest.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[6] src/CarRental.Domain/Entities/RentalRequest.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
```

**Нарушений: 6** (с инструкциями на той же задаче: 0).

Изменено 10 файлов, +431 / −1 строк:
`README.md`, `RentalRequestsController.cs`, `IRentalRequestService.cs`, `RentalRequestService.cs`, `RentalRequest.cs`, `RentalRequestStatus.cs`, `RentalRequestRepository.cs`, `RentalRequestsControllerTests.cs`, `RentalRequestServiceTests.cs` (новый), `RentalRequestTests.cs`.

## Тесты (по словам агента)

- Unit: 96/96
- Integration: 56/56 (PostgreSQL через Testcontainers)
- Отключены только 4 анализатора `SourceKit.Analyzers.*` (нужен Roslyn 4.9, в SDK из apt 4.8). StyleCop и warnings-as-errors работали и поймали одну ошибку порядка членов, агент её исправил.

## Сравнение с прогоном с инструкциями (`task2-with`)

| Правило | Без инструкций | С инструкциями |
|---|---|---|
| Нет `try/catch` в контроллере | ❌ 2 `catch` (`KeyNotFoundException`, `InvalidOperationException`), скопирован стиль соседних действий | ✅ 0 |
| Собственные исключения вместо `InvalidOperationException` | ❌ 4 `InvalidOperationException` (2 в домене, 2 в сервисе) | ✅ 2 новых наследника `DomainException` |
| Маппинг новых исключений в middleware | — (новых исключений нет) | ✅ |
| Эндпоинт в README | ✅ (таблица, диаграмма, таблица ролей) | ✅ |
| **Итого нарушений** | **6** | **0** |

## Итоговое сообщение агента (кратко)

- `POST /api/rental-requests/{id}/cancel`, роль Client. 200 с обновлённой заявкой (в отличие от `reject`, который возвращает 204), 400 для чужой заявки или неверного статуса, 404 если заявки нет.
- Статус `Cancelled` сохраняется и виден в списке. Approve / reject / complete отклоняют отменённую заявку, это закрыто тестами.
- **Сам нашёл и исправил связанную проблему:** проверка пересечения бронирований игнорировала только `Rejected`, поэтому отменённая заявка продолжала бы блокировать авто на эти даты. Добавлено исключение `Cancelled` в `RentalRequestRepository.cs`.
- Миграция не нужна: статус хранится строкой.

## Наблюдения

- Ошибки те же, что в baseline задачи 1: агент копирует `try/catch` и `InvalidOperationException` из соседнего кода. Значит, проблема не в задаче, а в том, что правило нельзя вывести из кода проекта.
- **Важно для отчёта:** прогон без инструкций нашёл функциональную проблему (отменённая заявка блокирует авто), а прогон с инструкциями её **не** нашёл: `RentalRequestRepository.cs` в `task2-with` не менялся. Инструкции улучшают соответствие конвенциям, но не гарантируют полноту бизнес-логики. Наш `check.js` такие вещи не проверяет.
