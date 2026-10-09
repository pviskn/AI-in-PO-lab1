# Context engineering — AGENTS.md и skill

## Выполнили:

**Скоринцева Т.А.** (416740)  
*Роль: Взаимодействие с агентом, выполнение задач, запуск попыток*

**Сармусокова А.С.** (467364)  
*Роль: Check-скрипт, метрики, анализ результатов*

**Панас А.А.** (467006)  
*Роль: AGENTS.md, skills, конвенции, промпты*

## Описание проекта CarRental
CarRental — это бэкенд-система управления арендой автомобилей на .NET 8, построенная по принципам Clean Architecture с четкой изоляцией слоев Domain, Application, Infrastructure и API. 

Система реализует ролевую модель доступа (Client, Manager, Admin) и строгие бизнес-правила, включая автоматическую проверку минимального возраста и стажа водителя в зависимости от категории автомобиля. 

Основная логика охватывает полный жизненный цикл заявок: от создания и модерации до завершения с динамическим расчетом итоговой стоимости, который включает базовый тариф, штрафы за просрочку и повреждения. 

Проект обеспечивает высокую надежность и поддерживаемость за счет полного покрытия юнит- и интеграционными тестами, а также централизованной обработки ошибок и валидации данных на всех уровнях приложения.

## Структура решения

```
CarRental.sln
├── src/
│   ├── CarRental.Domain/           # Ядро: сущности, исключения, value objects
│   ├── CarRental.Application/      # Сценарии: интерфейсы, DTO
│   ├── CarRental.Infrastructure/   # Реализация: EF Core, репозитории, сервисы
│   └── CarRental.Api/              # ASP.NET Core Web API: контроллеры, middleware
└── tests/
    ├── CarRental.IntegrationTests/ # Интеграционные тесты (Testcontainers + xUnit)
    └── CarRental.UnitTests/        # Юнит-тесты (Moq + FluentAssertions + xUnit)
```

С более подробным описанием можно ознакомиться в README.md

## Выбранные задачи
1. Мягкое удаление сущности машины 
2. Отмена заявки клиентом

---

## Шаг 1. Baseline — результаты без правил

Промпт 1 задачи выглядел таким образом:
```
Soft deletion for cars
Requirements:
- a car must not be physically deleted from the database
- add a nullable deletion timestamp (DeletedAt) or similar soft-delete flag to the car model
- soft-deleted cars shouldn't show up in the regular car list
- a normal request for a soft-deleted car by id must behave as if the car does not exist
- add an endpoint for Manager/Admin to soft-delete a car
- add an endpoint for Manager/Admin to bring a soft-deleted car back
- deleting an already deleted car and restoring a car that is not deleted must be handled as invalid operations
- keep the existing public API behavior unchanged for cars that are not deleted
- add/update tests for the new behavior
- inspect the existing code and implement the feature consistently with the project
When soft-deletion is finished run relevant tests
```

### 1.1. Прогон 1 (ветка `baseline-attempt-1`)

### 1.2. Прогон 2 (ветка `baseline-attempt-2`)

### 1.3. Прогон 3 (ветка `baseline-attempt-3`)

| Прогон | Ветка | Файл лога | Сессия |
| --- | --- | --- | --- |
| Baseline 1 | `baseline-attempt-1` | [baseline-1_1.md](https://github.com/pviskn/AI-in-PO-lab1/blob/baseline-attempt-1/baseline-1_1.md) | [сессия](https://claude.ai/code/session_01YYzts6ngBpXVbPtQiRuLWJ) |
| Baseline 2 | `baseline-attempt-2` | [baseline-2_1.md](https://github.com/pviskn/AI-in-PO-lab1/blob/baseline-attempt-2/baseline-2_1.md) | [сессия](https://claude.ai/code/session_01Xjwy7fpYLMGkLm1sjNJNGc) |
| Baseline 3 | `baseline-attempt-3` | [baseline-3.md](https://github.com/pviskn/AI-in-PO-lab1/blob/baseline-attempt-3/baseline-3.md) | [сессия](https://claude.ai/code/session_01HyENJ1U5MRm19ksQo29erf) |
| Задача 1 с инструкциями | `with-instructions-run-1` | [with-instructions-run-1.md](https://github.com/pviskn/AI-in-PO-lab1/blob/with-instructions-run-1/with-instructions-run-1.md) | [сессия](https://claude.ai/code/session_01ASVboaC46Fs3ZMb4HYq3wB) |
| Задача 2 без инструкций | `task2-baseline` | [task2-baseline.md](https://github.com/pviskn/AI-in-PO-lab1/blob/task2-baseline/task2-baseline.md) | [сессия](https://claude.ai/code/session_018hwVSrxr1NqbaH8GvY6TTw) |
| Задача 2 с инструкциями | `task2-with` | [task2-with.md](https://github.com/pviskn/AI-in-PO-lab1/blob/task2-with/task2-with.md) | [сессия](https://claude.ai/code/session_0153TZvdKF2786ieV2HLq5eb) |



---

## Шаг 2. Формулировка конвенций
Для начала мы вспомнили суть выбранного проекта и выделили правила, которые агент должен соблюдать при написании нового кода.

Основные конвенции проекта, которые мы составили:

- Соблюдать Clean Architecture. Проект разделён на Domain, Application, Infrastructure и API. Каждый слой отвечает за свою часть работы. Это помогает не смешивать разные задачи в одном месте.

- Не добавлять бизнес-логику в контроллеры. Контроллер должен принимать запрос, вызывать сервис и возвращать ответ. Основная логика должна находиться в Application или Domain.

- Использовать DTO. Контроллеры не должны напрямую возвращать сущности Domain. Для передачи данных используются DTO.

- Соблюдать правила асинхронных методов. Новые асинхронные методы должны иметь окончание Async и принимать CancellationToken.

- Работать с базой данных через репозитории. Не нужно обращаться к EF Core напрямую из контроллеров и сервисов Application. После изменения данных нужно вызывать SaveChangesAsync.

- Правильно обрабатывать ошибки. В новых контроллерах не нужно добавлять try/catch. Ошибки должны обрабатываться через ExceptionHandlingMiddleware. Для бизнес-ошибок нужно использовать специальные исключения.

= Добавлять тесты. Для новой бизнес-логики нужны unit-тесты, а для новых API-эндпоинтов - integration-тесты.

Эти правила были установлены для упрощения работы и во избежание дубликатов кода или нарушения структуры проекта
## Шаг 3. Создание инструкций для агента

### 3.1. AGENTS.md
Мы создали файл AGENTS.md, в котором записали основные правила работы с проектом CarRental для агента.

Сначала описали структуру проекта и назначение каждого слоя. Затем добавили правила для контроллеров, сервисов, DTO, репозиториев и асинхронных методов. Отдельно описали обработку ошибок и добавили требования к тестам и команды для их запуска, например dotnet test.

### 3.2. Reusable Skill

Мы создали skill implement-api-endpoint/SKILL.md, который помогает агенту правильно добавлять новые API-эндпоинты.

Основные шаги skill:

- найти похожий эндпоинт в проекте и изучить его

- при необходимости создать DTO

- добавить метод в Application

- реализовать бизнес-логику

- при необходимости обновить репозитории

- добавить метод в контроллер без try/catch

- настроить обработку бизнес-ошибок через специальные исключения

- добавить новый эндпоинт в README

- написать тесты

- запустить тесты и проверить результат

### 3.3. Промпт для агента

Мы выбрали 2 задачи, которые затрагивают разные части проекта - заявки и автомобили, чтобы проверить, сможет ли агент применять одни и те же инструкции при выполнении разных задач, и написали для этого промпты

**Задача 1 - Soft deletion for cars**

Нужно добавить мягкое удаление автомобилей, автомобиль не должен удаляться из базы данных полностью, вместо этого нужно добавить поле DeletedAt. Удалённые автомобили не должны отображаться в обычном списке. Также необходимо добавить эндпоинты для удаления и восстановления автомобилей, обработать ошибки и написать тесты.

**Задача 2 - Customer cancellation of rental requests**

Нужно добавить возможность отмены заявки на аренду автомобиля.

Основные требования:

- добавить эндпоинт POST /api/rental-requests/{id}/cancel

- добавить новый статус Cancelled

- разрешить отмену только владельцу заявки

- разрешить отмену только для заявок со статусом Pending

- возвращать ошибку при попытке отменить чужую заявку или заявку с другим статусом

- возвращать HTTP 404, если заявка не найдена

- сохранять новый статус в базе данных

- не разрешать одобрять, отклонять или завершать отменённые заявки

- добавить тесты и обновить README

## Шаг 4. Результаты с контекстом (AGENTS.md + skill)

### 4.1. Прогон с правилами (ветка `with-instructions-run-1`)
- Промпт, который был дан агенту (с учетом новых инструкций)
```
Soft deletion for cars
Requirements:
- a car must not be physically deleted from the database
- add a nullable deletion timestamp (DeletedAt) or similar soft-delete flag to the car model
- soft-deleted cars shouldn't show up in the regular car list
- a normal request for a soft-deleted car by id must behave as if the car does not exist
- add an endpoint for Manager/Admin to soft-delete a car
- add an endpoint for Manager/Admin to bring a soft-deleted car back
- deleting an already deleted car and restoring a car that is not deleted must be handled as invalid operations
- keep the existing public API behavior unchanged for cars that are not deleted
- add/update tests for the new behavior
- inspect the existing code and implement the feature consistently with the project
When soft-deletion is finished run relevant tests
```
- Краткое описание того, что агент сделал: 
| --- | --- | --- | --- |
| Задача 1 с инструкциями | `with-instructions-run-1` | [with-instructions-run-1.md](https://github.com/pviskn/AI-in-PO-lab1/blob/with-instructions-run-1/with-instructions-run-1.md) | [сессия](https://claude.ai/code/session_01ASVboaC46Fs3ZMb4HYq3wB) |
- Отличия от baseline-прогонов

**Отличия от baseline-прогонов**

Промпт и модель были те же, менялась только стартовая ветка: с `AGENTS.md`, `CLAUDE.md` и skill. По результату `check.js` исчезли все три типа нарушений, которые повторялись в baseline:

| Что | Baseline (3 прогона) | С инструкциями |
| --- | --- | --- |
| `try/catch` в контроллере | 4 в каждом прогоне — агент копировал стиль существующего `ChangeCarStatus` | 0 — действия контроллера только вызывают сервис и возвращают результат, ошибки обрабатывает `ExceptionHandlingMiddleware` |
| Бизнес-ошибки | `throw new InvalidOperationException` (3 в каждом прогоне) | собственные исключения `CarAlreadyDeletedException` и `CarNotDeletedException` — наследники `DomainException` |
| Маппинг ошибок | через `catch` в контроллере | новые исключения добавлены в `ExceptionHandlingMiddleware` |
| README | новые эндпоинты не описаны в 2 из 3 прогонов | эндпоинты добавлены в таблицу |
| **Нарушений** | **7, 8, 8 (среднее 7,7)** | **0** |

Что не изменилось: в обоих случаях агент правильно соблюдал слои, использовал DTO, `Async` + `CancellationToken` и `SaveChangesAsync`, писал unit- и интеграционные тесты, и все тесты проходили. Эти правила агент выводил из соседнего кода и без инструкций.

Побочный эффект: недопустимые операции (повторное удаление, восстановление неудалённого авто) теперь возвращают 409 Conflict вместо 400, как было в baseline и в существующем коде. Инструкции не задавали HTTP-код, агент выбрал его сам.


#### Таблица количества нарушений конвенций по результатам check-скрипта до и после добавления контекста

<table>
  <thead>
    <tr>
      <th>Baseline (без правил)</th>
      <th>Нарушений</th>
      <th>Типы нарушений</th>
      <th>С контекстом<br>(AGENTS.md + skill)</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Прогон 1<br><code>baseline-attempt-1</code></td>
      <td>7</td>
      <td>
        • Запрещен try/catch в контроллере на строке (4)<br>
        • Запрещено генерировать базовое исключение (3)<br>
      </td>
      <td rowspan="3" style="vertical-align: middle; text-align: center;">
        <strong>0 нарушений</strong><br><br>
      </td>
    </tr>
    <tr>
      <td>Прогон 2<br><code>baseline-attempt-2</code></td>
      <td>8</td>
      <td>
        • Запрещен try/catch в контроллере на строке (4)<br>
        • Запрещено генерировать базовое исключение (3)<br>
        • В контроллерах объявлен новый HTTP-эндпоинт [Http*], но файл README.md не был обновлен в текущем коммите/диффе (1)<br>
      </td>
    </tr>
    <tr>
      <td>Прогон 3<br><code>baseline-attempt-3</code></td>
      <td>8</td>
      <td>
        • Запрещен try/catch в контроллере на строке (4)<br>
        • Запрещено генерировать базовое исключение (3)<br>
        • В контроллерах объявлен новый HTTP-эндпоинт [Http*], но файл README.md не был обновлен в текущем коммите/диффе (1)<br>
      </td>
    </tr>
  </tbody>
</table>


### 4.2. Анализ улучшений
- Среднее количество нарушений в Baseline: 7
- Количество нарушений с контекстом (AGENTS.md + skill): 0
Благодаря явным инструкциям в AGENTS.md и пошаговому SKILL.md, агент полностью перестал допускать следующие ошибки, которые были частыми в baseline-прогонах:
- catch (InvalidOperationException ex) в контроллере
- В доменном слое используется стандартное .NET-исключение вместо кастомного доменного
- Агент добавил новый эндпоинт (например, [HttpGet("deleted")]), но не описал его в документации
---

## Шаг 5. Проверка генерализуемости

### 5.1. Вторая задача
<!-- Заполняет: Человек 1 
Описание второй задачи (структурно похожей, но другой из списка примеров).-->

### 5.2. Результаты на второй задаче
- Промпт для второй задачи

```
# Customer cancellation of rental requests
## **Requirements:**

- add a new POST /api/rental-requests/{id}/cancel endpoint for an authenticated client to cancel their own rental request

- add Cancelled to the existing RentalRequestStatus enum

- only the client who created the rental request can cancel it

- a rental request can be cancelled only when its current status is Pending

- cancelling another client's request must be handled as an invalid business operation

- cancelling a request that is not Pending must be handled as an invalid business operation

- if the request does not exist, return HTTP 404

- after successful cancellation, persist the new status so that subsequent requests return Cancelled

- existing approve, reject and complete operations must not process a cancelled request

- keep the existing public API behavior unchanged for other requests and operations

- add/update tests for successful cancellation, wrong owner, invalid status and nonexistent request

- update the README endpoints table with the new endpoint

- check the existing code and follow the project's style

**When cancellation is finished run relevant tests**
```

- Что сделал агент:
| Задача 2 без инструкций | `task2-baseline` | [task2-baseline.md](https://github.com/pviskn/AI-in-PO-lab1/blob/task2-baseline/task2-baseline.md) | [сессия](https://claude.ai/code/session_018hwVSrxr1NqbaH8GvY6TTw) |
| Задача 2 с инструкциями | `task2-with` | [task2-with.md](https://github.com/pviskn/AI-in-PO-lab1/blob/task2-with/task2-with.md) | [сессия](https://claude.ai/code/session_0153TZvdKF2786ieV2HLq5eb) |

#### Количество нарушений конвенций на второй задаче
<table>
  <thead>
    <tr>
      <th>Baseline (без правил)</th>
      <th>Нарушений</th>
      <th>Типы нарушений</th>
      <th>С контекстом<br>(AGENTS.md + skill)</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Прогон 1<br><code>tas2-baseline</code></td>
      <td>6</td>
      <td>
        • Запрещен try/catch в контроллере на строке (2)<br>
        • Запрещено генерировать базовое исключение (4)<br>
      </td>
      <td rowspan="3" style="vertical-align: middle; text-align: center;">
        <strong>0 нарушений</strong><br><br>
      </td>
    </tr>
  </tbody>
</table>

### 5.3. Выводы о генерализуемости
Оказалось, что правила работают универсально, так как при прогоне с контекстом 2 задачи агент все выполнил без единой ошибки, а изменен был только промпт самой задачи.

---

## Описание check-скрипта
Язык/инструмент: JavaScript (Node.js), без внешних зависимостей — используются только встроенные модули fs, path и child_process.
Режим работы: Скрипт работает в режиме DIFF — анализирует не весь проект, а только изменения между базовой веткой и текущей (git diff base...HEAD). Это позволяет оценивать именно тот код, который был сгенерирован агентом в рамках задачи, а не наследие проекта.

---
Проверяемые правила:

- Направление зависимостей слоёв
- Тонкие контроллеры
- DTO вместо Domain-сущностей в API
- Async-конвенции
- Сохранение через Unit of Work
- Кастомные исключения
- Разделение тестов
- Запрет try/catch в контроллерах
- Запрет базовых бизнес-исключений
- Маппинг новых исключений
- Документация эндпоинтов

## Выводы

В трёх baseline-прогонах (задача soft delete) скрипт фиксировал в среднем 7 нарушений.
После добавления AGENTS.md и skill результат — 0 нарушений (улучшение 100%). На второй задаче нарушений тоже не было, что подтверждает универсальность правил.
Главный вывод: агент не нарушает конвенции специально — он просто их не знает. Формализация неявных правил проекта решает эту проблему.

---

