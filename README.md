# Онлайн-тестирование и проверка заданий

[![CI](https://github.com/especiallynight/OnlineTestingAndAssignmentGrading/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/especiallynight/OnlineTestingAndAssignmentGrading/actions/workflows/ci.yml)

Система для автоматизации обучения и контроля знаний в колледжах и университетах.
Преподаватели создают курсы, теорию, тесты и задания, студенты изучают материалы и проходят тесты,
а система проверяет ответы, считает баллы и показывает аналитику. Проект выполнен по теме №2
«Онлайн-тестирование и проверка заданий» учебной практики.

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

Веб-клиент обращается к сервисам только через API Gateway. Проверка отправленных тестов идёт
асинхронно: ExamService пишет событие `TestSubmittedEvent` в Outbox и публикует его в RabbitMQ,
GradingService проверяет ответы и возвращает `GradingCompleted` / `GradingFailed`.
Код студентов выполняется в изолированном контейнере CodeExecutionService.

Подробное описание: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

Контракты API: [docs/api-contracts.md](docs/api-contracts.md)

## Быстрый старт

**Требования:** .NET SDK 10.0, Docker Desktop с Docker Compose v2, не менее 4 ГБ свободной памяти.

```bash
git clone https://github.com/especiallynight/OnlineTestingAndAssignmentGrading.git
cd OnlineTestingAndAssignmentGrading

# Linux/macOS
cp .env.example .env

# Windows PowerShell
Copy-Item .env.example .env

docker compose up --build
```

После запуска откройте **http://localhost:5000** и войдите под одной из тестовых учётных записей.

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
| RabbitMQ UI | http://localhost:15672 (guest/guest) |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

## Тестовые учётные записи

Пароли только для демонстрации, реальные секреты в репозиторий не попадают.

| Роль | Email | Пароль |
|---|---|---|
| Admin | admin@demo.local | Demo123! |
| Teacher | teacher@demo.local | Demo123! |
| Student | student@demo.local | Demo123! |

## Реализованная функциональность

- Регистрация, вход, JWT, refresh-токены и роли Student, Teacher, Admin
- Курсы, запись студентов на курс, задания и вопросы четырёх типов
  (SingleChoice, MultipleChoice, Text, Code)
- **Теория курса:** преподаватель добавляет, редактирует и удаляет учебные материалы,
  студент читает их на странице курса до прохождения теста
- Попытки с серверным таймером: один тест — одна попытка, сохранение и обновление ответов
- Асинхронная проверка через RabbitMQ + MassTransit Outbox
- Автопроверка кода в изолированном контейнере с ограничениями CPU, памяти и времени
- Ручное оценивание завершённых попыток преподавателем
- Статистика успеваемости, кэширование в Redis и SignalR-уведомления о результатах
- Веб-клиент для трёх ролей: дашборд, курсы, теория, тесты, проверка работ, аналитика
- Запуск всей системы одной командой `docker compose up --build`

## Тесты и CI

```bash
dotnet test
```

В проекте **48 тестов**: юнит-тесты сервисов (курсы, теория, попытки, оценивание, аналитика,
идентификация) и интеграционные тесты WebClient, которые проверяют отдачу HTML-страниц,
CSS/JS и порядок подключения скриптов.

Сборка и тесты запускаются в GitHub Actions при каждом push и Pull Request:
[.github/workflows/ci.yml](.github/workflows/ci.yml).

Нагрузочный тест (k6): `k6 run docs/load-test.js`
(адреса задаются через `-e BASE_URL=... -e WEB_URL=...`).

## Структура репозитория

```
OnlineTesting.sln          решение со всеми проектами
AnalyticsService/          статистика и SignalR-хаб
ApiGateway/                YARP/Ocelot-шлюз
CodeExecutionService/      изолированный запуск кода студентов
CourseService/             курсы, теория, задания, вопросы
ExamService/               попытки, ответы, серверный таймер
GradingService/            проверка и оценивание
IdentityService/           пользователи, JWT, роли
Shared/                    общие контракты событий
WebClient/                 веб-клиент
OnlineTesting.Tests/       автоматические тесты
docs/                      архитектура, план, API, ER-диаграмма, нагрузочный тест
```

## План работ

[docs/PLAN.md](docs/PLAN.md)

## Ограничения и планы развития

- В Docker-стеке код студентов собирается и запускается внутри контейнера CodeExecutionService
  (`CodeRunner=process`); на хосте используется `DockerCodeRunner` с отдельным контейнером.
- SignalR-хаб доступен через API Gateway по адресу `/hubs/grading`, но веб-клиент пока
  не подписан на события хаба и обновляет результаты запросом.
- Проверка кода выполняется синхронно внутри HTTP-запроса студента; очередь для неё не используется.
- Веб-клиент статический: без сборки frontend-фреймворка, чтобы упростить запуск проекта.

## Использование ИИ-инструментов

При подготовке проекта ИИ-ассистент использовался как помощник: разбор ошибок, подготовка
документации, черновики тестов и проверка соответствия требованиям. Все изменения проверены
запуском сборки, тестов и демонстрационных сценариев.
