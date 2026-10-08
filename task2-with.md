# Задача 2 с инструкциями: отмена заявки клиентом

| Параметр | Значение |
|---|---|
| Агент / модель | Claude Code, Opus 5.5 |
| Стартовая точка | `task2-with` = `7246edd` (`clean-base` + `AGENTS.md`, `CLAUDE.md`, skill `implement-api-endpoint`), инструкции **не менялись** после задачи 1 |
| Ссылка на сессию | https://claude.ai/code/session_0153TZvdKF2786ieV2HLq5eb |
| Промпт | `prompts/task2.md`, без файлов и подсказок |
| Ветка результата | `task2-with`, коммиты `a56e541` (фича) и `bed6453` (возврат прав файла `.editorconfig`) |

## Результат проверки

```
$ node check.js --base clean-base
Началась проверка конвенций в режиме DIFF против базы: clean-base...

Нарушений архитектурных конвенций не обнаружено
```

**Нарушений: 0**

Изменено 12 файлов, +372 / −1 строк:
`README.md`, `RentalRequestsController.cs`, `ExceptionHandlingMiddleware.cs`, `IRentalRequestService.cs`, `RentalRequestService.cs`, `RentalRequest.cs`, `RentalRequestStatus.cs`, `RentalRequestNotCancellableException.cs` (новый), `RentalRequestNotOwnedException.cs` (новый), `RentalRequestsControllerTests.cs`, `RentalRequestServiceTests.cs` (новый), `RentalRequestTests.cs`.

## Тесты (по словам агента)

- Unit: 95/95
- Integration: 54/54 (PostgreSQL через Testcontainers, образ с зеркала из-за лимита Docker Hub)
- Анализаторы SourceKit при сборке были отключены: SDK из apt слишком старый (Roslyn 4.8). Код ими не проверен.

## Как правила сработали на новой задаче

| Правило (AGENTS.md / SKILL.md) | Результат |
|---|---|
| Нет `try/catch` в контроллере | ✅ действие `CancelRequest` только вызывает сервис и возвращает 204, хотя в этом же `RentalRequestsController` 9 старых блоков `catch` |
| Собственные исключения-наследники `DomainException` | ✅ `RentalRequestNotOwnedException`, `RentalRequestNotCancellableException` |
| Новые исключения замаплены в middleware | ✅ обе → 400 в `ExceptionHandlingMiddleware` |
| Эндпоинт в README | ✅ строка в таблице + ветка `CANCELLED` в диаграмме жизненного цикла |
| Бизнес-логика в домене | ✅ `RentalRequest.Cancel(userId)` проверяет владельца и статус |
| `Async` + `CancellationToken`, `SaveChangesAsync` | ✅ `CancelRequestAsync(requestId, clientId, cancellationToken)`, запись через `_uow.SaveChangesAsync` |
| Тесты в нужных проектах | ✅ unit (домен + сервис через Moq) и 5 интеграционных |

## Итоговое сообщение агента (кратко)

- `POST /api/rental-requests/{id}/cancel`, только роль Client. 204 при успехе (как `reject`), 400 если заявка чужая или не в статусе Pending, 404 если заявки нет.
- Статус `Cancelled` сохраняется и виден в `GET /api/rental-requests`. Approve / reject / complete уже не принимают отменённую заявку (400), это закрыто тестами.
- Миграция не нужна: статус хранится строкой.
- Файлы в Windows-1251 отредактированы с сохранением кодировки.

## Наблюдения

- Правило про `try/catch` сработало в «опасном» месте: в том же контроллере лежат 9 старых блоков `catch`, и агент их не скопировал.
- В контроллере есть `throw new UnauthorizedAccessException(...)`, если пользователь не определён. Это повторяет существующий паттерн проекта (`UnauthorizedAccessException` замаплен в middleware на 401) и не является бизнес-ошибкой, поэтому скрипт это нарушением не считает.
- Чужая заявка возвращает 400, а не 403. Агент объяснил это тем, что так в проекте обрабатываются другие недопустимые бизнес-операции. Это решение под вопросом, но к нашим правилам оно не относится.
