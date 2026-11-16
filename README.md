# Онлайн-тестирование и проверка заданий

[![CI](https://github.com/especiallynight/OnlineTestingAndAssignmentGrading/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/especiallynight/OnlineTestingAndAssignmentGrading/actions/workflows/ci.yml)

Система для автоматизации обучения и контроля знаний в колледжах и университетах. Преподаватели создают курсы, теорию, тесты и задания, студенты изучают материалы и проходят тесты, а система проверяет ответы, считает баллы и показывает аналитику.

Проект выполнен по теме №2 «Онлайн-тестирование и проверка заданий» учебной практики.

## Технологический стек

- **.NET 10**, ASP.NET Core Web API
- **Веб-клиент:** ASP.NET Core + статические HTML/CSS/JS
- **PostgreSQL 16** — у каждого сервиса своя база данных
- **RabbitMQ + MassTransit** — асинхронная проверка, паттерн Outbox
- **Redis** — кэширование курсов, вопросов и аналитики
- **Docker Compose** — запуск всей системы одной командой
- **xUnit + Moq** — автоматические тесты
- **Serilog, Swagger/OpenAPI, health checks**

## Архитектура

Веб-клиент обращается к сервисам только через API Gateway. Проверка отправленных тестов идёт асинхронно: ExamService пишет событие `TestSubmittedEvent` в Outbox и публикует его в RabbitMQ, GradingService проверяет ответы и возвращает `GradingCompleted` / `GradingFailed`.

Код студентов выполняется в изолированном контейнере CodeExecutionService.

Подробное описание: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

Контракты API: [docs/api-contracts.md](docs/api-contracts.md)

## Быстрый старт

**Требования:** .NET SDK 10.0, Docker Desktop с Docker Compose v2, не менее 4 ГБ свободной памяти.

```bash
git clone https://github.com/especiallynight/OnlineTestingAndAssignmentGrading.git
cd OnlineTestingAndAssignmentGrading
```

Linux/macOS:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Запуск:

```bash
docker compose up --build
```

После запуска откройте **http://localhost:5000**.

| Компонент | Адрес |
|---|---|
| Веб-клиент | http://localhost:5000 |
| API Gateway | http://localhost:5006 |
| Identity (Swagger) | http://localhost:5005 |
| Course (Swagger) | http://localhost:5001 |
| Exam (Swagger) | http://localhost:5002 |
| Grading (Swagger) | http://localhost:5003 |
| Analytics (Swagger) | http://localhost:5004 |
| CodeExecution (Swagger) | http://localhost:5203 |
| RabbitMQ UI | http://localhost:15672 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

## Тестовые учётные записи

Пароли предназначены только для демонстрации.

| Роль | Email | Пароль |
|---|---|---|
| Admin | admin@demo.local | Demo123! |
| Teacher | teacher@demo.local | Demo123! |
| Student | student@demo.local | Demo123! |

## Демонстрационные данные

На чистой базе CourseService создаёт два курса и три задания: «Вводный тест по C#», «Тест по основам JavaScript» и задание на код «Задача: сумма двух чисел». В каждом курсе есть теория, студент записан на оба курса.

## Реализованная функциональность

- Регистрация, вход, JWT, refresh-токены и роли Student, Teacher, Admin.
- Курсы, запись студентов на курс, задания и вопросы четырёх типов (SingleChoice, MultipleChoice, Text, Code).
- Разделение тестов и заданий на код.
- Теория курса: преподаватель добавляет, редактирует и удаляет учебные материалы.
- Попытки с серверным таймером: сохранение и обновление ответов.
- Автоматическое истечение попытки на сервере.
- Асинхронная проверка через RabbitMQ + MassTransit Outbox.
- Автопроверка кода в изолированном контейнере с ограничениями CPU, памяти и времени.
- Ручное оценивание завершённых попыток преподавателем.
- Статистика успеваемости, кэширование в Redis и SignalR-уведомления о результатах.
- Веб-клиент для трёх ролей: дашборд, курсы, теория, тесты, проверка работ, аналитика.
- Запуск всей системы одной командой `docker compose up --build`.

## Тесты и CI

Запуск тестов:

```bash
dotnet test
```

В проекте 50 тестов: юнит-тесты сервисов (курсы, теория, попытки, оценивание, аналитика, идентификация) и интеграционные тесты WebClient.

Сборка и тесты запускаются в GitHub Actions при каждом push и Pull Request:

[.github/workflows/ci.yml](.github/workflows/ci.yml)

Нагрузочный тест (k6):

```bash
k6 run docs/load-test.js
```

Адреса можно переопределить через `BASE_URL` и `WEB_URL`.

## Структура репозитория

```text
OnlineTesting.sln
AnalyticsService/
ApiGateway/
CodeExecutionService/
CourseService/
ExamService/
GradingService/
IdentityService/
Shared/
WebClient/
OnlineTesting.Tests/
docs/
```

## Ограничения и планы развития

- В Docker-стеке код студентов собирается и запускается внутри контейнера CodeExecutionService (`CodeRunner=process`); на хосте используется `DockerCodeRunner` с отдельным контейнером.
- SignalR-хаб доступен через API Gateway по адресу `/hubs/grading`, но веб-клиент пока не подписан на события хаба и обновляет результаты запросом.
- Проверка кода выполняется синхронно внутри HTTP-запроса студента; очередь для неё не используется.
- Веб-клиент статический: без сборки frontend-фреймворка, чтобы упростить запуск проекта.

## Использование ИИ-инструментов

При подготовке проекта ИИ-ассистент использовался как помощник: разбор ошибок, подготовка документации, черновики тестов и проверка соответствия требованиям. Все изменения проверены запуском сборки, тестов и демонстрационных сценариев.

## План работ

[docs/PLAN.md](docs/PLAN.md)
