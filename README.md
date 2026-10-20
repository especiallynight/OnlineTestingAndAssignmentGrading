\# Название проекта

Онлайн-тестирование и проверка заданий.

Система предназначена для автоматизации процесса обучения и контроля знаний в колледжах и университетах. Она решает задачу организации проверки знаний: преподаватели создают курсы, тесты и задания, а студенты проходят их в онлайн-формате.



\## Технологический стек



версия .NET: 10.0

Веб-клиент: ASP.NET Core MVC

СУБД: PostgreSQL 
брокер сообщений: RabbitMQ 

Docker: 4.56.0



\## Архитектура



\[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)



\## Быстрый старт



Требования: .NET SDK 10, Docker 4.56.0.

Запуск: docker compose up --build



Адреса сервисов и порты.

ASP.NET Core MVC: 



Swagger:



PostgreSQL:



RabbitMQ Management UI: 



Тестовые учётные записи.



Преподаватель:



Логин: ivanMaximovich@test.ru



Пароль: TeacherPassword



Студент:



Логин: alexandraMorozova@test.ru



Пароль: StudentPassword



\## Реализованная функциональность



Этап 1: создан репозиторий,readme, план, gitignore



\## Тесты и CI



Как запустить тесты: dotnet test

Статус сборки (бейдж GitHub Actions).



\## Структура репозитория



[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - описание архитектуры

[docs/PLAN.md](docs/PLAN.md) - план работ

.gitignore - исключения для Git

README.md - описание проекта и инструкции



\## План работ



[docs/PLAN.md](docs/PLAN.md)



\## Ограничения и планы развития



Что сделано упрощённо и что можно улучшить.

