# Baseline, прогон 1 (задача 1, без инструкций)

| Параметр | Значение |
|---|---|
| Агент / модель | Claude Code, Opus 5.5 |
| Стартовая точка | `clean-base` (`411636c`) |
| Ссылка на сессию | https://claude.ai/code/session_01YYzts6ngBpXVbPtQiRuLWJ |
| Промпт | `prompts/task1.md`, без изменений, без файлов и подсказок |
| Ветка результата | `baseline-attempt-1` |

## Результат проверки

### Итоговая проверка (`check.js` из `main`, коммит `7317e43`, режим diff)

```
$ node check.js --base clean-base
Началась проверка конвенций в режиме DIFF против базы: clean-base...

НАЙДЕНО НАРУШЕНИЙ КОНВЕНЦИЙ: 7

[1] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[2] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[3] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (KeyNotFoundException ex)"
[4] src/CarRental.Api/Controllers/CarsController.cs: Запрещен try/catch в контроллере на строке: "catch (InvalidOperationException ex)"
[5] src/CarRental.Application/Services/RentalRequestService.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[6] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
[7] src/CarRental.Domain/Entities/Car.cs: Запрещено генерировать базовое исключение: throw new InvalidOperationException(...). Используйте кастомные доменные исключения
```

**Нарушений: 7**

## Тесты (по словам агента)

- Unit: 99/99 прошли
- Integration: 66/66 прошли (PostgreSQL через Testcontainers)

## Ход работы (сообщения агента по ходу)

- Заметил, что `check.js` недоступен для чтения, и не трогал его.
- Ставил .NET 8 SDK. SDK из дистрибутива оказался со слишком старым компилятором (Roslyn 4.8, а анализаторам нужен 4.9+), поэтому собирал более новым Roslyn из NuGet, не меняя репозиторий.
- Файлы в cp1251 правил через Python, чтобы сохранить кодировку.
- Пакет `Itmo.Dev.Editorconfig` при сборке перезаписывает `.editorconfig`. Агент вернул файл перед коммитом.
- Исправил ошибки стиль-анализаторов в своих тестах.
- Сам запустил Docker-демон для интеграционных тестов.

## Итоговое сообщение агента (кратко)

**Реализация:**
- `Car`: nullable `DeletedAt`, методы `SoftDelete(...)` и `Restore()`. Физически запись не удаляется.
- Репозиторий скрывает удалённые авто при получении по id, по VIN и в списке. `GET /api/cars` их не показывает, `GET /api/cars/{id}` и смена статуса возвращают 404. Создание заявки на удалённое авто тоже даёт 404.
- Новые эндпоинты (Manager/Admin): `DELETE /api/cars/{id}` и `POST /api/cars/{id}/restore`. Ответы: 204 при успехе, 404 если авто нет, 400 при недопустимой операции.
- Для неудалённых авто формат ответов и маршруты не изменились.

**Решения, которые агент вынес на проверку:**
- VIN остаётся занятым после удаления: добавление авто с VIN удалённого даёт 409, вернуть авто можно только через restore.
- Одобрение заявки на удалённое авто возвращает 400 (добавлена проверка в `RentalRequestService`, иначе было бы 500).
- Арендованное авто удалить можно: требования этот случай не описывают.
- Фильтрация в репозитории, а не глобальный EF query filter. Глобальный фильтр скрыл бы авто из существующих заявок и сломал бы очистку в тестах.
- Миграцию не добавлял: в проекте нет папки миграций, база создаётся из модели.

**Тесты:** unit-тесты для методов `Car`, новый `CarCatalogServiceTests.cs`, интеграционные тесты обоих эндпоинтов (успех, 401/403/404/400, случай с VIN) и скрытия удалённых авто. Обновлена таблица эндпоинтов в README.

## Наблюдения

- Ошибки ловит `try/catch` по `KeyNotFoundException` / `InvalidOperationException` прямо в контроллере, как в существующем `ChangeCarStatus`, а не через `ExceptionHandlingMiddleware`.
- Бизнес-ошибки домена бросает как `InvalidOperationException`, а не через собственные исключения домена.
- Вышел за рамки задачи: правка `RentalRequestService` и `README.md`.
