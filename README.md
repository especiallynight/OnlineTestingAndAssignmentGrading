## Название проекта

Онлайн-тестирование и проверка заданий.

Система предназначена для автоматизации процесса обучения и контроля знаний в колледжах и университетах. Она решает задачу организации проверки знаний: преподаватели создают курсы, тесты и задания, а студенты проходят их в онлайн-формате.



## Технологический стек



версия .NET: 10.0

Веб-клиент: ASP.NET Core MVC

СУБД: PostgreSQL
брокер сообщений: RabbitMQ

Docker: 4.56.0



## Архитектура



[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)



## Быстрый старт



1\. Скопировать `.env.example` в `.env`

2\. `docker compose up --build`

3\. Отдельно запустить проверку кода: `dotnet run --project CodeExecutionService`



| Компонент | Адрес |

|---|---|

| Веб-клиент | http://localhost:5000 |

| API Gateway | http://localhost:5006 |

| Identity (Swagger) | http://localhost:5005 |

| Course | http://localhost:5001 |

| Exam | http://localhost:5002 |

| Grading | http://localhost:5003 |

| Analytics | http://localhost:5004 |

| RabbitMQ UI | http://localhost:15672 (guest/guest) |



## Тестовые учётные записи



| Роль | Email | Пароль |

|---|---|---|

| Admin | admin@demo.local | Demo123! |

| Teacher | teacher@demo.local | Demo123! |

| Student | student@demo.local | Demo123! |

## 

## Реализованная функциональность


- Регистрация, вход, JWT и роли (Student, Teacher, Admin)
- Курсы, задания и вопросы трёх типов (SingleChoice, MultipleChoice, Code)
- Попытки с серверным таймером, сохранение и обновление ответов
- Автопроверка кода в изолированном Docker-контейнере
- Асинхронная проверка через RabbitMQ + MassTransit Outbox, оценивание
- Статистика успеваемости и SignalR-уведомления о результатах
- Запуск всей системы одной командой docker compose up --build



## Тесты и CI



Как запустить тесты: dotnet test

Статус сборки (бейдж GitHub Actions).



## Структура репозитория



[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - описание архитектуры

[docs/PLAN.md](docs/PLAN.md) - план работ

.gitignore - исключения для Git

README.md - описание проекта и инструкции



## План работ



[docs/PLAN.md](docs/PLAN.md)



## Ограничения и планы развития



- Веб-клиент в разработке: основной сценарий пока демонстрируется через Swagger
- Сервис проверки кода запускается на хосте (нужен доступ к Docker), не в compose
- Redis зарегистрирован, но кэширование ещё не используется
- ExamService не получает событие результата обратно: оценку отдаёт GradingService
- Проверка кода выполняется синхронно внутри HTTP-запроса студента

