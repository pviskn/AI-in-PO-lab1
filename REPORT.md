# Context engineering — AGENTS.md и skill

## Выполнили:

**[ФИО студента 1]** (группа [номер])  
*Роль: Взаимодействие с агентом, выполнение задач, запуск попыток*

**Сармусокова А.С.** (467364)  
*Роль: Check-скрипт, метрики, анализ результатов*

**[ФИО студента 3]** (группа [номер])  
*Роль: AGENTS.md, skills, конвенции, промпты*

## Описание проекта CarRental
Бэкенд-система управления арендой автомобилей

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

## Постановка задач
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



---

## Шаг 2. Формулировка конвенций
<!-- Заполняет: Человек 3 -->
Описание процесса reverse engineering: какие правила были выявлены при анализе существующего кода, почему они важны, как они помогают поддерживать чистоту архитектуры.

Список выявленных конвенций (5-7 правил).

---

## Шаг 3. Создание инструкций для агента

### 3.1. AGENTS.md
<!-- Заполняет: Человек 3 -->
Описание структуры и содержания файла AGENTS.md. Ключевые разделы и правила.

### 3.2. Reusable Skill
<!-- Заполняет: Человек 3 -->
Описание созданного skill (например, `implement-api-endpoint/SKILL.md`). Какую задачу он решает, какие шаги содержит.

### 3.3. Промпт для агента
<!-- Заполняет: Человек 3 -->
Финальная версия промпта, который использовался с учетом AGENTS.md и skill.

---

## Шаг 4. Результаты с контекстом (AGENTS.md + skill)

### 4.1. Прогон с правилами (ветка `with-agents-md`)
<!-- Заполняет: Человек 1 -->
- Промпт, который был дан агенту (с учетом новых инструкций)
- Краткое описание того, что агент сделал
- Отличия от baseline-прогонов

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
<!-- Заполняет: Человек 1 -->
Описание второй задачи (структурно похожей, но другой из списка примеров).

### 5.2. Результаты на второй задаче
<!-- Заполняет: Человек 1 -->
- Промпт для второй задачи
- Что сделал агент

<!-- Заполняет: Человек 2 -->
- Количество нарушений конвенций
- Сравнение с baseline для этой задачи (если делался)

### 5.3. Выводы о генерализуемости
<!-- Заполняет: Человек 2 -->
- Работают ли правила универсально или только для конкретной задачи
- Какие правила оказались полезными, а какие нет

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

---

