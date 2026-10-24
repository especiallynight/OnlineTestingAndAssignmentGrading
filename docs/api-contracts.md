# API-контракты микросервисов

Все приведённые ниже маршруты относятся к API соответствующих сервисов. Внешний клиент обращается к ним через API Gateway.

Формат запросов и ответов — JSON. Для защищённых эндпоинтов используется заголовок:

    Authorization: Bearer <JWT>

### Обозначения ролей

- **Все авторизованные пользователи** — пользователь с действительным JWT.
- **Student** — студент.
- **Teacher** — преподаватель.
- **Admin** — администратор.
- **Без авторизации** — endpoint доступен без JWT.


---

## 1. CourseService

### Ответственность
CourseService отвечает за управление курсами, заданиями и учебными материалами. Преподаватели могут создавать и редактировать курсы и задания, студенты — просматривать доступные материалы.

При создании курса сервис сохраняет идентификатор преподавателя, создавшего курс. При изменении курса или задания сервис проверяет, что запрос выполняет преподаватель, имеющий право на изменение соответствующего ресурса.

*   **База данных:** `CourseDB`.
*   **Таблицы:** `Courses`, `Assignments`, `Questions`.
*   **Кэширование:** Redis используется для кэширования часто запрашиваемых данных курсов. После изменения данных соответствующий кэш должен обновляться.
*   **Публикуемые события:** отсутствуют в текущем проектировании.
*   **Потребляемые события:** отсутствуют в текущем проектировании.

### Основные эндпоинты

| Метод | Endpoint | Назначение | Доступ |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/courses` | Получить список курсов | Все авторизованные пользователи |
| `GET` | `/api/courses/{id}` | Получить курс | Все авторизованные пользователи |
| `POST` | `/api/courses` | Создать курс | Teacher |
| `PUT` | `/api/courses/{id}` | Изменить курс | Teacher |
| `GET` | `/api/courses/{id}/assignments` | Получить задания курса | Все авторизованные пользователи |
| `POST` | `/api/courses/{id}/assignments` | Создать задание в курсе | Teacher |
| `GET` | `/api/assignments/{id}` | Получить задание | Все авторизованные пользователи |
| `PUT` | `/api/assignments/{id}` | Изменить задание | Teacher |
| `GET` | `/api/assignments/{id}/questions` | Получить вопросы задания | Все авторизованные пользователи |
| `POST` | `/api/assignments/{id}/questions` | Создать вопрос в задании | Teacher |
| `GET` | `/api/questions/{id}` | Получить вопрос | Все авторизованные пользователи |
| `PUT` | `/api/questions/{id}` | Изменить вопрос | Teacher |

### Таблица Questions

Таблица `Questions` хранится в `CourseDB` и содержит вопросы, относящиеся к заданиям.

| Поле | Тип | Назначение |
| :--- | :--- | :--- |
| `question_id` | `int`, PK | Идентификатор вопроса |
| `assignment_id` | `int`, FK | Идентификатор задания из `Assignments` |
| `text` | `varchar` | Текст вопроса |
| `type` | `varchar` | Тип вопроса, например `SingleChoice` или `Code` |
| `options` | `jsonb` или `text` | Варианты ответа или дополнительные данные вопроса |

`question_id` используется для однозначного определения вопроса при сохранении ответа студента и последующей проверке. В других базах данных этот идентификатор хранится как логическая ссылка, без внешнего ключа между базами разных сервисов.

### Контракты API

#### 1. Получение списка курсов

`GET /api/courses`

Запрос не содержит тела.

**Успешный ответ:** `200 OK`

    [
      {
        "id": 1,
        "name": "Основы программирования",
        "description": "Введение в программирование"
      }
    ]

**Возможные ошибки:** `401 Unauthorized`.

#### 2. Получение курса

`GET /api/courses/{id}`

**Успешный ответ:** `200 OK`

    {
      "id": 1,
      "name": "Основы программирования",
      "description": "Введение в программирование"
    }

**Возможные ошибки:** `401 Unauthorized`, `404 Not Found`.

#### 3. Создание курса

`POST /api/courses` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "name": "Основы программирования",
      "description": "Введение в программирование"
    }

**Успешный ответ:** `201 Created`

    {
      "id": 1,
      "name": "Основы программирования",
      "description": "Введение в программирование"
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`.

#### 4. Изменение курса

`PUT /api/courses/{id}` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "name": "Основы программирования",
      "description": "Обновлённое описание курса"
    }

**Успешный ответ:** `200 OK`

    {
      "id": 1,
      "name": "Основы программирования",
      "description": "Обновлённое описание курса"
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 5. Получение заданий курса

`GET /api/courses/{id}/assignments`

**Успешный ответ:** `200 OK`

    [
      {
        "id": 1,
        "course_id": 1,
        "name": "Первая работа",
        "description": "Проверка базовых знаний",
        "time_limit_minutes": 30
      }
    ]

**Возможные ошибки:** `401 Unauthorized`, `404 Not Found`.

#### 6. Создание задания

`POST /api/courses/{id}/assignments` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "name": "Первая работа",
      "description": "Проверка базовых знаний",
      "max_score": 100,
      "time_limit_minutes": 30
    }

**Успешный ответ:** `201 Created`

    {
      "id": 1,
      "course_id": 1,
      "name": "Первая работа",
      "description": "Проверка базовых знаний",
      "max_score": 100,
      "time_limit_minutes": 30
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 7. Получение задания

`GET /api/assignments/{id}`

**Успешный ответ:** `200 OK`

    {
      "id": 1,
      "course_id": 1,
      "name": "Первая работа",
      "description": "Проверка базовых знаний",
      "max_score": 100,
      "time_limit_minutes": 30
    }

**Возможные ошибки:** `401 Unauthorized`, `404 Not Found`.

#### 8. Изменение задания

`PUT /api/assignments/{id}` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "name": "Первая работа",
      "description": "Обновлённое описание",
      "max_score": 100,
      "time_limit_minutes": 45
    }

**Успешный ответ:** `200 OK`

    {
      "id": 1,
      "course_id": 1,
      "name": "Первая работа",
      "description": "Обновлённое описание",
      "max_score": 100,
      "time_limit_minutes": 45
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 9. Получение вопросов задания

`GET /api/assignments/{id}/questions`

**Успешный ответ:** `200 OK`

    [
      {
        "question_id": 1,
        "assignment_id": 1,
        "text": "Какой тип данных используется для целых чисел в C#?",
        "type": "SingleChoice",
        "options": [
          "int",
          "string",
          "bool"
        ]
      }
    ]

**Возможные ошибки:** `401 Unauthorized`, `404 Not Found`.

#### 10. Создание вопроса

`POST /api/assignments/{id}/questions` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "text": "Какой тип данных используется для целых чисел в C#?",
      "type": "SingleChoice",
      "options": [
        "int",
        "string",
        "bool"
      ]
    }

**Успешный ответ:** `201 Created`

    {
      "question_id": 1,
      "assignment_id": 1,
      "text": "Какой тип данных используется для целых чисел в C#?",
      "type": "SingleChoice",
      "options": [
        "int",
        "string",
        "bool"
      ]
    }

*Примечание: `question_id` генерируется сервером и не передаётся при создании вопроса.*

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 11. Получение вопроса

`GET /api/questions/{id}`

**Успешный ответ:** `200 OK`

    {
      "question_id": 1,
      "assignment_id": 1,
      "text": "Какой тип данных используется для целых чисел в C#?",
      "type": "SingleChoice",
      "options": [
        "int",
        "string",
        "bool"
      ]
    }

**Возможные ошибки:** `401 Unauthorized`, `404 Not Found`.

#### 12. Изменение вопроса

`PUT /api/questions/{id}` *(Доступ: Teacher)*

**Тело запроса:**

    {
      "text": "Какой тип данных в C# используется для целых чисел?",
      "type": "SingleChoice",
      "options": [
        "int",
        "string",
        "bool"
      ]
    }

**Успешный ответ:** `200 OK`

    {
      "question_id": 1,
      "assignment_id": 1,
      "text": "Какой тип данных в C# используется для целых чисел?",
      "type": "SingleChoice",
      "options": [
        "int",
        "string",
        "bool"
      ]
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

---

## 2. ExamService

### Ответственность
ExamService отвечает за прохождение тестов: создание попыток, сохранение ответов, контроль времени и получение результатов проверки. Время начала и окончания попытки фиксируется на сервере. Клиент не может самостоятельно продлить время прохождения.

*   **База данных:** `ExamDB`.
*   **Публикуемые события:** `TestSubmittedEvent` — попытка отправлена на проверку.
*   **Потребляемые события:** `GradingCompleted`, `GradingFailed`.

### Основные эндпоинты

| Метод | Endpoint | Назначение | Доступ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/attempts` | Начать попытку | Student |
| `GET` | `/api/attempts/{id}` | Получить состояние попытки | Владелец попытки или Teacher |
| `POST` | `/api/attempts/{id}/answers` | Сохранить ответ | Владелец попытки |
| `POST` | `/api/attempts/{id}/submit` | Отправить попытку на проверку | Владелец попытки |
| `GET` | `/api/attempts/{id}/results` | Получить результат | Владелец попытки или Teacher |

### Контракты API

#### 1. Начало попытки

`POST /api/attempts` *(Доступ: Student)*

**Тело запроса:**

    {
      "assignment_id": 1
    }

**Успешный ответ:** `201 Created`

    {
      "attempt_id": 1,
      "assignment_id": 1,
      "status": "InProgress",
      "started_at": "2026-10-23T10:00:00Z",
      "deadline_at": "2026-10-23T10:30:00Z"
    }

*`student_id` определяется по данным авторизованного пользователя, а не принимается от клиента.*

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`.

#### 2. Получение состояния попытки

`GET /api/attempts/{id}`

**Успешный ответ:** `200 OK`

    {
      "attempt_id": 1,
      "assignment_id": 1,
      "status": "InProgress",
      "started_at": "2026-10-23T10:00:00Z",
      "deadline_at": "2026-10-23T10:30:00Z"
    }

**Возможные ошибки:** `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 3. Сохранение ответа

`POST /api/attempts/{id}/answers` *(Доступ: владелец попытки)*

**Тело запроса:**

    {
      "question_id": 1,
      "answer_data": "int"
    }

**Успешный ответ:** `201 Created`

    {
      "attempt_id": 1,
      "answer_id": 1,
      "question_id": 1,
      "saved": true
    }

*`question_id` указывает, на какой вопрос отвечает студент. `answer_id` создаётся сервером.*

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`.

#### 4. Отправка попытки на проверку

`POST /api/attempts/{id}/submit` *(Доступ: владелец попытки)*

Тело запроса не требуется.

**Успешный ответ:** `202 Accepted`

    {
      "attempt_id": 1,
      "status": "PendingGrading",
      "message": "Попытка отправлена на проверку"
    }

*Ответ `202 Accepted` означает, что запрос принят в обработку, но проверка ещё не завершена. Сохраняется в Outbox.*

**Возможные ошибки:** `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`.

#### 5. Получение результата

`GET /api/attempts/{id}/results`

**Успешный ответ (проверка завершена):** `200 OK`

    {
      "attempt_id": 1,
      "status": "Completed",
      "score": 85,
      "feedback": "Работа проверена"
    }

**Успешный ответ (проверка выполняется):** `200 OK`

    {
      "attempt_id": 1,
      "status": "PendingGrading",
      "score": null,
      "feedback": null
    }

**Возможные ошибки:** `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

### Событие TestSubmittedEvent

Пример содержимого:

    {
      "event_id": 1,
      "attempt_id": 1,
      "student_id": 1,
      "assignment_id": 1,
      "submitted_at": "2026-10-23T10:25:00Z",
      "answers": [
        {
          "answer_id": 1,
          "question_id": 1,
          "answer_data": "int"
        }
      ]
    }

---

## 3. GradingService

### Ответственность
GradingService выполняет автоматическую проверку ответов, рассчитывает оценку и сохраняет результаты проверки. Для заданий с программным кодом предусматривается запуск кода в изолированной среде.

*   **База данных:** `DBGrade`.
*   **Публикуемые события:** `GradingCompleted`, `GradingFailed`.
*   **Потребляемые события:** `TestSubmittedEvent`.
*   **HTTP-эндпоинты:** отсутствуют (работа только через RabbitMQ).

### Событие GradingCompleted

    {
      "event_id": 2,
      "attempt_id": 3,
      "student_id": 4,
      "assignment_id": 3,
      "status": "Completed",
      "score": 85,
      "feedback": "Работа проверена",
      "completed_at": "2026-10-23T10:26:00Z"
    }

### Событие GradingFailed

    {
      "event_id": 3,
      "attempt_id": 1,
      "student_id": 3,
      "assignment_id": 4,
      "status": "Failed",
      "error_code": "GRADING_ERROR",
      "message": "Не удалось завершить проверку"
    }

*Сервис сохраняет результат в `DBGrade` и публикует событие через Outbox. Запуск кода происходит в изолированной среде.*

---

## 4. AnalyticsService

### Ответственность
AnalyticsService собирает статистику успеваемости студентов, рассчитывает агрегированные показатели и предоставляет данные о прогрессе обучения.

*   **База данных:** `DBAnal`.
*   **Публикуемые события:** отсутствуют.
*   **Потребляемые события:** `GradingCompleted`.

### Основные эндпоинты

| Метод | Endpoint | Назначение | Доступ |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/analytics/students/{student_id}` | Получить статистику студента | Сам студент или Teacher |
| `GET` | `/api/analytics/courses/{course_id}` | Получить статистику курса | Teacher |

### Контракты API

#### 1. Статистика студента

`GET /api/analytics/students/{student_id}`

**Успешный ответ:** `200 OK`

    {
      "student_id": 1,
      "completed_attempts": 10,
      "total_score": 820,
      "average_score": 82,
      "updated_at": "2026-10-23T10:26:00Z"
    }

**Возможные ошибки:** `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

#### 2. Статистика курса

`GET /api/analytics/courses/{course_id}`

**Успешный ответ:** `200 OK`

    {
      "course_id": 1,
      "completed_attempts": 45,
      "average_score": 78,
      "updated_at": "2026-10-23T10:26:00Z"
    }

**Возможные ошибки:** `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

---

## 5. IdentityService

### Ответственность
IdentityService отвечает за регистрацию пользователей, проверку учётных данных, аутентификацию, выдачу JWT и управление ролями.

*   **База данных:** `DBIdentity`.
*   **Публикуемые события:** отсутствуют.
*   **Потребляемые события:** отсутствуют.

### Основные эндпоинты

| Метод | Endpoint | Назначение | Доступ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Регистрация пользователя | Без авторизации |
| `POST` | `/api/auth/login` | Вход пользователя | Без авторизации |
| `POST` | `/api/auth/refresh` | Обновление токенов | По действительному refresh-токену |
| `GET` | `/api/users` | Получение списка пользователей | Admin |
| `GET` | `/api/users/{id}` | Получение информации о пользователе | Сам пользователь или Admin |
| `GET` | `/api/teacher-applications` | Получение списка заявок на роль преподавателя | Admin |
| `POST` | `/api/teacher-applications/{id}/approve` | Одобрить заявку на роль преподавателя | Admin |
| `POST` | `/api/teacher-applications/{id}/reject` | Отклонить заявку на роль преподавателя | Admin |

### Контракты API

#### 1. Регистрация

`POST /api/auth/register` *(Доступ: без авторизации)*

**Тело запроса:**

    {
      "first_name": "Анна",
      "last_name": "Макарова",
      "email": "teacher@example.com",
      "password": "ExamplePassword123",
      "requested_role": "Teacher"
    }

**Успешный ответ:** `201 Created`

    {
      "user_id": 1,
      "email": "teacher@example.com",
      "requested_role": "Teacher",
      "teacher_application_status": "Pending"
    }

*При запросе роли `Teacher` создаётся заявка. Пароль хранится в виде хеша.*

**Возможные ошибки:** `400 Bad Request`, `409 Conflict`.

#### 2. Вход

`POST /api/auth/login` *(Доступ: без авторизации)*

**Тело запроса:**

    {
      "email": "student@example.com",
      "password": "ExamplePassword123"
    }

**Успешный ответ:** `200 OK`

    {
      "access_token": "<JWT>",
      "refresh_token": "<refresh-token>",
      "token_type": "Bearer",
      "expires_in": 3600
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`.

#### 3. Обновление токенов

`POST /api/auth/refresh`

**Тело запроса:**

    {
      "refresh_token": "<refresh-token>"
    }

**Успешный ответ:** `200 OK`

    {
      "access_token": "<new-JWT>",
      "refresh_token": "<new-refresh-token>",
      "token_type": "Bearer",
      "expires_in": 3600
    }

**Возможные ошибки:** `400 Bad Request`, `401 Unauthorized`.

#### 4. Получение списка пользователей

`GET /api/users` *(Доступ: Admin)*

**Успешный ответ:** `200 OK`

    [
      {
        "user_id": 1,
        "first_name": "Анна",
        "last_name": "Макарова",
        "email": "student@example.com",
        "role": "Student"
      }
    ]

#### 5. Получение пользователя

`GET /api/users/{id}` *(Доступ: сам пользователь или Admin)*

**Успешный ответ:** `200 OK`

    {
      "user_id": 1,
      "first_name": "Анна",
      "last_name": "Макарова",
      "email": "student@example.com",
      "role": "Student"
    }

#### 6. Получение списка заявок

`GET /api/teacher-applications` *(Доступ: Admin)*

**Успешный ответ:** `200 OK`

    [
      {
        "teacher_application_id": 1,
        "user_id": 1,
        "status": "Pending",
        "created_at": "2026-10-23T10:00:00Z",
        "reviewed_at": null,
        "reviewed_by": null
      }
    ]

#### 7. Одобрение заявки

`POST /api/teacher-applications/{id}/approve` *(Доступ: Admin)*

**Успешный ответ:** `200 OK`

    {
      "teacher_application_id": 1,
      "user_id": 1,
      "status": "Approved"
    }

#### 8. Отклонение заявки

`POST /api/teacher-applications/{id}/reject` *(Доступ: Admin)*

**Успешный ответ:** `200 OK`

    {
      "teacher_application_id": 1,
      "user_id": 1,
      "status": "Rejected"
    }

---
