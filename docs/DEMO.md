# Сценарии демонстрации работы системы

Документ описывает, что и в каком порядке показывать, чтобы доказать
работоспособность системы. Каждый сценарий содержит шаги, ожидаемый
результат и, где полезно, техническую проверку через БД, RabbitMQ или логи.

## 0. Демонстрационные данные

После чистого запуска в базе есть только два курса и три задания:

| Курс | Задание | Тип | Вопросов | Баллов | Время |
|---|---|---|---|---|---|
| Основы программирования на C# | Вводный тест по C# | тест | 3 | 10 | 20 мин |
| Основы программирования на C# | Задача: сумма двух чисел | задание на код | 1 | 10 | 30 мин |
| Веб-разработка: HTML, CSS и JavaScript | Тест по основам JavaScript | тест | 3 | 10 | 20 мин |

В каждом курсе есть теория (4 материала), студент `student@demo.local`
записан на оба курса.

Демо-аккаунты (пароль `Demo123!`):

| Роль | Email |
|---|---|
| Студент | student@demo.local |
| Преподаватель | teacher@demo.local |
| Администратор | admin@demo.local |

Если нужно вернуть демо-данные к исходному виду, очистите базы и
перезапустите `course-service` — сид сработает только на пустой базе:

```powershell
docker compose stop course-service exam-service grading-service analytics-service
@'
TRUNCATE TABLE "Enrollments", "Questions", "Assignments", "CourseMaterials", "Courses" RESTART IDENTITY CASCADE;
'@ | docker compose exec -T postgres psql -U postgres -d DBCourse
@'
TRUNCATE TABLE "Answers", "ExamAttempts", "OutboxMessage", "OutboxState", "InboxState" RESTART IDENTITY CASCADE;
'@ | docker compose exec -T postgres psql -U postgres -d DBExam
@'
TRUNCATE TABLE "AttemptGrades", "OutboxMessage", "OutboxState", "InboxState" RESTART IDENTITY CASCADE;
'@ | docker compose exec -T postgres psql -U postgres -d DBGrade
@'
TRUNCATE TABLE "StudentStatistics" RESTART IDENTITY;
'@ | docker compose exec -T postgres psql -U postgres -d DBAnal
docker compose exec -T redis redis-cli FLUSHALL
docker compose up -d --no-build course-service exam-service grading-service analytics-service
```

## 1. Подготовка стенда

```powershell
docker compose up -d --build
docker compose ps
```

Ожидаемо: все контейнеры `Up`, у `postgres`, `rabbitmq` и `redis` —
`healthy`. Веб-клиент: <http://localhost:5000>.

Проверка health-эндпоинтов:

```powershell
curl http://localhost:5001/health   # CourseService
curl http://localhost:5002/health   # ExamService
curl http://localhost:5003/health   # GradingService
curl http://localhost:5004/health   # AnalyticsService
curl http://localhost:5005/health   # IdentityService
curl http://localhost:5203/health   # CodeExecutionService
```

Ожидаемо: каждый отвечает `{"status":"healthy"}`.

## 2. Вход и роли

1. Откройте <http://localhost:5000/pages/login.html>.
2. Нажмите демо-кнопку **«Студент»** → **Войти**.
3. В шапке видно имя и роль, меню: «Дашборд», «Курсы», «Мои результаты»,
   «Проверка кода».
4. Выйдите и войдите как **«Преподаватель»**.

Ожидаемо: у преподавателя появляются действия «Создать курс»,
«Добавить теорию», «+ Новый тест»; у студента — запись на курс и
прохождение заданий.

Техническая проверка: JWT выдаётся `IdentityService`, запросы идут
через `ApiGateway`; в DevTools → Network видно `Authorization: Bearer …`.

## 3. Преподаватель создаёт тест и вопросы

1. Войдите как преподаватель, откройте **Курсы** → «Основы
   программирования на C#».
2. В блоке «Тесты» нажмите **«+ Новый тест»**.
3. Заполните только: **Название** — «Проверочный тест», **Описание**,
   **Длительность, минут** — 10, **Максимум баллов** — 5.
4. Нажмите **«Создать тест»** — сразу откроется редактор теста.
5. Добавьте вопросы:
   - **+ Один вариант ответа**: текст «Столица Франции», варианты
     `a — Париж`, `b — Лондон`, правильный — `a`;
   - **+ Несколько вариантов ответа**: «Какие числа чётные?»,
     `a — 2`, `b — 3`, `c — 4`, правильные — `a`, `c`.
6. Вернитесь к карточке курса.

Ожидаемо: тест появился в разделе «Тесты» с кнопкой «Редактировать
тест»; в окне создания нет выбора курса — курс берётся из карточки.

## 4. Теория курса

1. На странице курса прокрутите к разделу **«Теория»**.
2. Нажмите **«Читать»** у материала «Введение в .NET и C#».
3. Под преподавателем нажмите **«Добавить материал»**, введите заголовок
   и текст, сохраните.

Ожидаемо: материал появляется в списке сразу; студент видит теорию до
прохождения теста. Проверка: `GET /api/courses/1/materials`.

## 5. Студент проходит тест, оценка ставится автоматически

1. Войдите как студент, откройте курс «Основы программирования на C#».
2. В разделе «Тесты» нажмите **«Начать тест»** у «Вводный тест по C#».
3. Ответьте на вопросы (например, намеренно один неверно) и нажмите
   **«Завершить попытку»**.
4. Откройте **«Мои результаты»**.

Ожидаемо:
- ответы сохраняются по мере ввода (`PUT /api/exam_attempts/.../answers`);
- после завершения попытка получает статус `Completed`, а оценка
  появляется через 1–2 секунды — её считает `GradingService` и присылает
  `AnalyticsService`;
- в «Моих результатах» видны баллы и процент.

Техническая проверка:

```powershell
@'
SELECT "AttemptId", "Status", "DeadlineAt" FROM "ExamAttempts" ORDER BY "AttemptId" DESC LIMIT 1;
'@ | docker compose exec -T postgres psql -U postgres -d DBExam
@'
SELECT "AttemptId", "Score", "MaxScore" FROM "AttemptGrades" ORDER BY "AttemptId" DESC LIMIT 1;
'@ | docker compose exec -T postgres psql -U postgres -d DBGrade
```

Дополнительно в RabbitMQ UI (<http://localhost:15672>, guest/guest) видно
очереди `TestSubmittedEvent` и `AttemptGradedEvent`.

## 6. Автопроверка кода

1. Под студентом откройте курс → раздел **«Задания на код»** →
   **«Начать задание»** у «Задача: сумма двух чисел».
2. Видно: редактор C#, условие, 3 примера (ввод/вывод), кнопка
   **«Проверить код»**.
3. Проверьте три случая:

   **Верное решение (ожидаем OK):**
   ```csharp
   var parts = Console.ReadLine().Split(' ');
   Console.WriteLine(int.Parse(parts[0]) + int.Parse(parts[1]));
   ```

   **Неверное решение (ожидаем WA):**
   ```csharp
   Console.WriteLine(0);
   ```

   **Ошибка компиляции (ожидаем CE):**
   ```csharp
   Console.WriteLine(;
   ```

4. Нажмите **«Завершить задание»**.

Ожидаемо: вердикты `OK / WA / CE`, число пройденных тестов, балл за
задание; результат сохраняется в ответе и попадает в итоговую оценку.
Полный набор из 5 тест-кейсов скрыт от студента, поэтому захардкодить
ответ под примеры нельзя.

Техническая проверка: `ExamService` вызывает
`GET /internal/questions/{id}/test-cases` у `CourseService`, затем
`POST /api/code-execution/run` у `CodeExecutionService`; в БД
`Answers` заполнены `CodeExecutionResult` и `CodeScore`.

## 7. Одна попытка на тест

1. Студент завершает тест.
2. Снова откройте карточку курса.

Ожидаемо: вместо кнопки «Начать тест» — «Тест пройден», ссылки на
повторное прохождение нет. Прямой `POST /api/exam_attempts` с тем же
`assignmentId` возвращает **409** «Вы уже проходили этот тест».

## 8. Автоматическое истечение попытки

1. Преподаватель создаёт тест с длительностью **1 минута** и одним
   вопросом.
2. Студент нажимает «Начать тест» и **не завершает** его.
3. Подождите 1–2 минуты и обновите карточку курса.

Ожидаемо:
- статус попытки на сервере становится `Expired` без действий студента;
- в карточке — «Время попытки истекло», кнопки «Продолжить» нет;
- страница прохождения пишет «Вы уже проходили этот тест»;
- `POST /api/exam_attempts` возвращает **409**.

Техническая проверка:

```powershell
@'
SELECT "AttemptId", "Status", "DeadlineAt" FROM "ExamAttempts" ORDER BY "AttemptId" DESC LIMIT 3;
'@ | docker compose exec -T postgres psql -U postgres -d DBExam
docker compose logs --tail 20 exam-service | Select-String "Expired attempts"
```

Для ускорения демонстрации можно сдвинуть дедлайн вручную:

```powershell
@'
UPDATE "ExamAttempts" SET "DeadlineAt" = now() - interval '1 minute' WHERE "Status" = 'InProgress';
'@ | docker compose exec -T postgres psql -U postgres -d DBExam
```

Через 5 секунд фоновый `AttemptExpirationService` переведёт попытку в
`Expired` и запишет в лог `Expired attempts: 1`.

## 9. Ручное оценивание преподавателем

1. Войдите как преподаватель, откройте **«Проверка работ»**.
2. Выберите завершённую попытку студента, выставьте балл и комментарий,
   сохраните.
3. Студент открывает **«Мои результаты»**.

Ожидаемо: преподаватель видит только завершённые и просроченные
попытки; выставленная оценка сразу видна студенту.

## 10. Надёжность: RabbitMQ и Outbox

Сценарий доказывает, что проверка не теряется при недоступности
`GradingService`.

1. Остановите сервис проверки:
   ```powershell
   docker compose stop grading-service
   ```
2. Студент проходит и завершает тест.
3. Откройте RabbitMQ UI → очередь `TestSubmittedEvent`.

Ожидаемо: сообщение лежит в очереди, оценка ещё не появилась, но
попытка уже сохранена в `ExamDB` (Outbox + RabbitMQ).

4. Поднимите сервис:
   ```powershell
   docker compose up -d --no-build grading-service
   ```
5. Через несколько секунд обновите «Мои результаты».

Ожидаемо: `GradingService` забирает сообщение, считает балл, публикует
`AttemptGradedEvent` через свой Outbox, и оценка появляется без
повторного прохождения теста.

Техническая проверка:

```powershell
@'
SELECT count(*) AS pending FROM "OutboxMessage" WHERE "SentTime" IS NULL;
'@ | docker compose exec -T postgres psql -U postgres -d DBExam
@'
SELECT count(*) AS inbox FROM "InboxState";
'@ | docker compose exec -T postgres psql -U postgres -d DBGrade
```

## 11. Аналитика и уведомления

1. Под преподавателем откройте **«Аналитика»**.
2. Выберите курс.

Ожидаемо: средний процент по студентам, распределение попыток по
статусам, таблица студентов. Данные кэшируются в Redis и
инвалидируются после новой оценки.

Проверка SignalR: `AnalyticsService` после `AttemptGradedEvent`
отправляет `ReceiveGradeUpdate` в `/gradingHub` через API Gateway.
Проверить можно в DevTools → WS или в логах `analytics-service`.

## 12. Проверка кода студентом

1. Под студентом откройте **«Проверка кода»**.
2. Видны отправленные решения с вердиктами (`OK`, `WA`, `CE`, `TLE`,
   `RE`), числом пройденных тестов и исходным кодом.

Ожидаемо: студент видит свои решения, преподаватель — решения по своим
курсам.

## 13. Заявка студента на роль преподавателя

1. Войдите как `student@demo.local`. На дашборде появится блок
   **«Стать преподавателем»**.
2. Нажмите **«Подать заявку»** — блок сменится на «Заявка отправлена
   и ожидает рассмотрения» со статусом **«На проверке»**.
3. Войдите как `admin@demo.local`, откройте **«Пользователи»**,
   раздел **«Заявки на роль преподавателя»**.
4. Нажмите **«Одобрить»**. Пользователь получит роль преподавателя,
   заявка исчезнет из списка.
5. Повторный вход под `student@demo.local` — теперь доступно меню
   преподавателя.

Ожидаемо: повторная заявка при активной отклоняется (409), после
отклонения можно подать новую.

Техническая проверка:

```powershell
@'
SELECT "UserId", "Status" FROM "TeacherApplications";
'@ | docker compose exec -T postgres psql -U postgres -d DBIdentity
```

Статус в JSON — число: `0` Pending, `1` Approved, `2` Rejected.

## 14. Чек-лист для защиты

- [ ] Все контейнеры запускаются одной командой, health-эндпоинты отвечают.
- [ ] Вход под тремя ролями, разные меню и права.
- [ ] Преподаватель создаёт курс, теорию, тест и вопросы.
- [ ] Студент записывается на курс и проходит тест.
- [ ] Студент подаёт заявку на преподавателя, админ её одобряет.
- [ ] Ответы сохраняются на сервере, оценка считается асинхронно.
- [ ] Задание на код проверяется в изолированном контейнере (OK/WA/CE).
- [ ] Одна попытка на тест, повторная — 409.
- [ ] Попытка истекает сама, «Продолжить» недоступно.
- [ ] Преподаватель оценивает работу вручную.
- [ ] Оценка не теряется при остановке `GradingService` (Outbox + RabbitMQ).
- [ ] Аналитика показывает статистику, кэш Redis работает.
- [ ] Тесты проходят: `dotnet test` — 50 из 50.

## 15. Полезные адреса

| Компонент | Адрес |
|---|---|
| Веб-клиент | http://localhost:5000 |
| API Gateway (Swagger) | http://localhost:5006/swagger |
| CourseService | http://localhost:5001/swagger |
| ExamService | http://localhost:5002/swagger |
| GradingService | http://localhost:5003/swagger |
| AnalyticsService | http://localhost:5004/swagger |
| CodeExecutionService | http://localhost:5203/swagger |
| RabbitMQ UI | http://localhost:15672 (guest/guest) |
