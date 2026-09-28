# Лабораторная работа №2 — Система управления арендой автомобилей

## Описание

Разработайте бэкенд-систему управления арендой автомобилей на основе предоставленного шаблона.
Шаблон содержит полностью реализованные контроллеры, интерфейсы и DTO.
**Вся бизнес-логика должна быть реализована студентом.**

---

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

### Зависимости между проектами (Clean Architecture)

```
Domain ← Application ← Infrastructure ← Api
                   ↑                      ↑
              UnitTests             IntegrationTests
```

---

## Бизнес-требования

### Роли

| Роль    | Возможности |
|---------|-------------|
| CLIENT  | Просматривать каталог, создавать заявки на аренду |
| MANAGER | Всё CLIENT + добавлять авто, одобрять/отклонять/завершать заявки |
| ADMIN   | Всё MANAGER + управлять пользователями и ролями |

### Категории автомобилей и требования к водителю

| Категория | Мин. возраст | Мин. стаж |
|-----------|-------------|-----------|
| Economy   | 18 лет      | 1 год     |
| Standard  | 21 год      | 2 года    |
| Premium   | 25 лет      | 3 года    |
| Sport     | 25 лет      | 5 лет     |

### Расчёт стоимости аренды

- **Базовая стоимость** = `стоимость_в_день × количество_дней`
- **Штраф за просрочку** = `стоимость_в_день × 1.5 × дней_просрочки`
- **Штраф за повреждения** — вводится менеджером при завершении
- **Итого** = `базовая + штраф_просрочка + штраф_повреждения`

### Жизненный цикл заявки

```
PENDING → APPROVED → COMPLETED
       ↘ REJECTED
```

---

## API Endpoints

### Автомобили
| Метод  | URL                        | Роль              |
|--------|----------------------------|-------------------|
| GET    | /api/cars                  | Все               |
| GET    | /api/cars/{id}             | Все               |
| POST   | /api/cars                  | MANAGER, ADMIN    |
| PATCH  | /api/cars/{id}/status      | MANAGER, ADMIN    |

### Заявки на аренду
| Метод  | URL                                | Роль           |
|--------|------------------------------------|----------------|
| POST   | /api/rental-requests               | CLIENT         |
| GET    | /api/rental-requests               | Все авториз.   |
| POST   | /api/rental-requests/{id}/approve  | MANAGER, ADMIN |
| POST   | /api/rental-requests/{id}/reject   | MANAGER, ADMIN |
| POST   | /api/rental-requests/{id}/complete | MANAGER, ADMIN |

### Пользователи
| Метод  | URL                       | Роль  |
|--------|---------------------------|-------|
| GET    | /api/users                | ADMIN |
| POST   | /api/users                | ADMIN |
| POST   | /api/users/{id}/roles     | ADMIN |

### Аутентификация
| Метод  | URL                  | Роль  |
|--------|----------------------|-------|
| POST   | /api/auth/login      | Все   |
| POST   | /api/auth/register   | Все   |

---

## Запуск

### Предварительные требования

- .NET 8 SDK
- Docker и Docker Compose

### Шаг 1: Запустить PostgreSQL

```bash
docker compose up -d postgres
```

### Шаг 2: Применить миграции

```bash
cd src/CarRental.Api
dotnet ef database update --project ../CarRental.Infrastructure
```

### Шаг 3: Запустить API

```bash
dotnet run --project src/CarRental.Api
```

Swagger UI: http://localhost:5000

### Запуск тестов

```bash
# Все тесты
dotnet test

# Только юнит-тесты
dotnet test tests/CarRental.UnitTests

# Только интеграционные тесты (требует Docker)
dotnet test tests/CarRental.IntegrationTests
```

---

## Что нужно реализовать

### CarRental.Domain

- [ ] Поля и методы сущностей: `Car`, `User`, `RentalRequest`, `RentalContract`, `Role`
- [ ] Value Objects: `Vin` (валидация формата), `DateRange` (проверка дат, метод Overlaps)

### CarRental.Infrastructure

- [ ] Конфигурации EF Core (`Configurations/*.cs`)
- [ ] Реализации репозиториев (`Repositories/*.cs`)
- [ ] `ApplicationDbContext` — DbSet-свойства и OnModelCreating
- [ ] `UnitOfWork.SaveChangesAsync`
- [ ] `PasswordHasher` — хэширование через BCrypt
- [ ] `JwtTokenGenerator` — генерация JWT с claims
- [ ] `DependencyInjection.cs` — раскомментировать и заполнить регистрации
- [ ] Создать и применить миграции EF Core

### CarRental.Application (новые классы)

- [ ] `CarCatalogService : ICarCatalogService`
- [ ] `RentalRequestService : IRentalRequestService`
- [ ] `UserManagementService : IUserManagementService`
- [ ] `RentalPricingService : IRentalPricingService`
- [ ] `RentalEligibilityService : IRentalEligibilityService`
- [ ] `AuthService : IAuthService`

### Program.cs

- [ ] Раскомментировать регистрации сервисов Application

### Тесты (покрытие ≥ 70%)

- [ ] Юнит-тесты доменных сущностей
- [ ] Юнит-тесты сервисов Application (с Moq)
- [ ] Интеграционные тесты всех контроллеров (с Testcontainers)

---

## Критерии оценки

| Критерий                                     | Баллы |
|----------------------------------------------|-------|
| Реализация Domain (сущности, инварианты)     | 20    |
| Реализация Infrastructure (EF, репозитории) | 25    |
| Реализация Application Services             | 25    |
| Интеграционные тесты (покрытие ≥ 70%)       | 20    |
| Юнит-тесты (покрытие ≥ 70%)                 | 10    |
