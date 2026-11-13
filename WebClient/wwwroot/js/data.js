/*
 * data.js — мок-данные для всего макета.
 * Здесь нет бизнес-логики: только реалистичные записи на русском языке.
 * Позже эти данные будут приходить из соответствующих микросервисов.
 */
window.Mock = {
  // Пользователи системы: студенты, преподаватели и администратор.
  users: [
    {
      id: 'u1',
      fullName: 'Анна Смирнова',
      email: 'anna.smirnova@student.ru',
      role: 'student',
      group: 'ИС-21',
      joinedAt: '2025-09-01'
    },
    {
      id: 'u2',
      fullName: 'Дмитрий Ковалёв',
      email: 'dmitry.kovalev@student.ru',
      role: 'student',
      group: 'ИС-21',
      joinedAt: '2025-09-01'
    },
    {
      id: 'u3',
      fullName: 'Мария Петрова',
      email: 'maria.petrova@student.ru',
      role: 'student',
      group: 'ИС-22',
      joinedAt: '2025-09-01'
    },
    {
      id: 'u4',
      fullName: 'Иван Соколов',
      email: 'ivan.sokolov@student.ru',
      role: 'student',
      group: 'ИС-21',
      joinedAt: '2025-09-02'
    },
    {
      id: 'u5',
      fullName: 'Елизавета Новикова',
      email: 'elizaveta.novikova@student.ru',
      role: 'student',
      group: 'ИС-22',
      joinedAt: '2025-09-02'
    },
    {
      id: 'u6',
      fullName: 'Алексей Морозов',
      email: 'a.morozov@teacher.ru',
      role: 'teacher',
      group: 'Кафедра ИТ',
      joinedAt: '2023-02-10'
    },
    {
      id: 'u7',
      fullName: 'Ольга Виноградова',
      email: 'o.vinogradova@teacher.ru',
      role: 'teacher',
      group: 'Кафедра ИТ',
      joinedAt: '2022-09-15'
    },
    {
      id: 'u8',
      fullName: 'Сергей Админов',
      email: 'admin@onlinetesting.ru',
      role: 'admin',
      group: 'Администрация',
      joinedAt: '2021-01-12'
    }
  ],

  // Учебные курсы с привязкой к преподавателю.
  courses: [
    {
      id: 'c1',
      title: 'Основы программирования на C#',
      description: 'Базовый курс по языку C#: типы данных, управляющие конструкции, ООП и работа с коллекциями.',
      teacherId: 'u6',
      studentsCount: 28,
      testsCount: 2,
      modulesCount: 3,
      durationWeeks: 16,
      status: 'active'
    },
    {
      id: 'c2',
      title: 'Веб-разработка: HTML, CSS, JavaScript',
      description: 'Создание адаптивных интерфейсов, основы DOM, события и работа с сетевыми запросами.',
      teacherId: 'u6',
      studentsCount: 24,
      testsCount: 1,
      modulesCount: 2,
      durationWeeks: 14,
      status: 'active'
    },
    {
      id: 'c3',
      title: 'Базы данных и SQL',
      description: 'Реляционная модель, нормализация, проектирование схем и написание сложных SQL-запросов.',
      teacherId: 'u7',
      studentsCount: 31,
      testsCount: 1,
      modulesCount: 2,
      durationWeeks: 12,
      status: 'active'
    },
    {
      id: 'c4',
      title: 'Алгоритмы и структуры данных',
      description: 'Асимптотическая сложность, сортировки, поиск, деревья и хеш-таблицы.',
      teacherId: 'u6',
      studentsCount: 19,
      testsCount: 1,
      modulesCount: 1,
      durationWeeks: 16,
      status: 'active'
    },
    {
      id: 'c5',
      title: 'Архитектура микросервисов на .NET',
      description: 'Проектирование распределённых систем, взаимодействие через брокеры сообщений и Docker.',
      teacherId: 'u7',
      studentsCount: 16,
      testsCount: 1,
      modulesCount: 1,
      durationWeeks: 10,
      status: 'draft'
    },
    {
      id: 'c6',
      title: 'Тестирование и качество ПО',
      description: 'Виды тестирования, юнит-тесты, тест-дизайн и организация процесса контроля качества.',
      teacherId: 'u7',
      studentsCount: 22,
      testsCount: 0,
      modulesCount: 0,
      durationWeeks: 8,
      status: 'active'
    }
  ],

  // Модули курсов с учебными материалами.
  modules: [
    {
      id: 'm1',
      courseId: 'c1',
      order: 1,
      title: 'Введение в C# и .NET',
      description: 'Платформа .NET, среда выполнения CLR, создание первого консольного приложения.',
      materials: [
        { id: 'mat1', title: 'Конспект: платформа .NET', type: 'PDF', duration: '20 мин' },
        { id: 'mat2', title: 'Видео: установка Visual Studio', type: 'Видео', duration: '12 мин' },
        { id: 'mat3', title: 'Практика: первая программа', type: 'Задание', duration: '40 мин' }
      ]
    },
    {
      id: 'm2',
      courseId: 'c1',
      order: 2,
      title: 'Управляющие конструкции',
      description: 'Условия, циклы, обработка исключений и работа с массивами.',
      materials: [
        { id: 'mat4', title: 'Конспект: ветвления и циклы', type: 'PDF', duration: '25 мин' },
        { id: 'mat5', title: 'Практика: калькулятор', type: 'Задание', duration: '60 мин' }
      ]
    },
    {
      id: 'm3',
      courseId: 'c1',
      order: 3,
      title: 'Объектно-ориентированное программирование',
      description: 'Классы, наследование, полиморфизм, интерфейсы и инкапсуляция.',
      materials: [
        { id: 'mat6', title: 'Конспект: ООП в C#', type: 'PDF', duration: '30 мин' },
        { id: 'mat7', title: 'Видео: наследование на практике', type: 'Видео', duration: '18 мин' },
        { id: 'mat8', title: 'Лабораторная работа №3', type: 'Задание', duration: '90 мин' }
      ]
    },
    {
      id: 'm4',
      courseId: 'c2',
      order: 1,
      title: 'Основы HTML и CSS',
      description: 'Семантическая разметка, блочная модель, flexbox и адаптивная вёрстка.',
      materials: [
        { id: 'mat9', title: 'Конспект: семантический HTML', type: 'PDF', duration: '20 мин' },
        { id: 'mat10', title: 'Практика: карточка товара', type: 'Задание', duration: '45 мин' }
      ]
    },
    {
      id: 'm5',
      courseId: 'c2',
      order: 2,
      title: 'JavaScript и DOM',
      description: 'Переменные, функции, события, поиск элементов и изменение страницы.',
      materials: [
        { id: 'mat11', title: 'Конспект: основы JavaScript', type: 'PDF', duration: '35 мин' },
        { id: 'mat12', title: 'Видео: работа с DOM', type: 'Видео', duration: '22 мин' },
        { id: 'mat13', title: 'Практика: интерактивный список', type: 'Задание', duration: '70 мин' }
      ]
    },
    {
      id: 'm6',
      courseId: 'c3',
      order: 1,
      title: 'Реляционная модель данных',
      description: 'Таблицы, ключи, связи и нормальные формы.',
      materials: [
        { id: 'mat14', title: 'Конспект: реляционная модель', type: 'PDF', duration: '25 мин' },
        { id: 'mat15', title: 'Схема: учебная база данных', type: 'Схема', duration: '15 мин' }
      ]
    },
    {
      id: 'm7',
      courseId: 'c3',
      order: 2,
      title: 'SQL-запросы',
      description: 'SELECT, фильтрация, агрегация, JOIN и подзапросы.',
      materials: [
        { id: 'mat16', title: 'Конспект: SELECT и JOIN', type: 'PDF', duration: '40 мин' },
        { id: 'mat17', title: 'Практика: аналитические запросы', type: 'Задание', duration: '80 мин' }
      ]
    },
    {
      id: 'm8',
      courseId: 'c4',
      order: 1,
      title: 'Сортировки и поиск',
      description: 'Пузырьковая сортировка, быстрая сортировка, бинарный поиск и оценка сложности.',
      materials: [
        { id: 'mat18', title: 'Конспект: алгоритмы сортировки', type: 'PDF', duration: '30 мин' },
        { id: 'mat19', title: 'Видео: бинарный поиск', type: 'Видео', duration: '16 мин' }
      ]
    }
  ],

  // Тесты, привязанные к курсам и модулям.
  tests: [
    {
      id: 't1',
      courseId: 'c1',
      moduleId: 'm1',
      title: 'Вводный тест по C#',
      description: 'Проверка базовых знаний платформы .NET и синтаксиса языка.',
      durationMin: 30,
      questionsCount: 4,
      attemptsCount: 24,
      averageScore: 4.2,
      status: 'published',
      createdAt: '2026-05-12'
    },
    {
      id: 't2',
      courseId: 'c1',
      moduleId: 'm3',
      title: 'ООП: классы и наследование',
      description: 'Классы, модификаторы доступа, наследование и полиморфизм.',
      durationMin: 45,
      questionsCount: 3,
      attemptsCount: 17,
      averageScore: 3.8,
      status: 'published',
      createdAt: '2026-06-02'
    },
    {
      id: 't3',
      courseId: 'c2',
      moduleId: 'm5',
      title: 'JavaScript: основы',
      description: 'Типы данных, массивы, функции и работа с DOM.',
      durationMin: 40,
      questionsCount: 3,
      attemptsCount: 21,
      averageScore: 4.5,
      status: 'published',
      createdAt: '2026-06-10'
    },
    {
      id: 't4',
      courseId: 'c3',
      moduleId: 'm7',
      title: 'SQL: SELECT и JOIN',
      description: 'Составление запросов с фильтрацией, группировкой и объединением таблиц.',
      durationMin: 35,
      questionsCount: 1,
      attemptsCount: 29,
      averageScore: 3.6,
      status: 'published',
      createdAt: '2026-06-14'
    },
    {
      id: 't5',
      courseId: 'c4',
      moduleId: 'm8',
      title: 'Сложность алгоритмов',
      description: 'Оценка времени работы алгоритмов и выбор структуры данных.',
      durationMin: 25,
      questionsCount: 2,
      attemptsCount: 14,
      averageScore: 3.9,
      status: 'published',
      createdAt: '2026-06-18'
    },
    {
      id: 't6',
      courseId: 'c5',
      moduleId: null,
      title: 'Микросервисы: архитектура',
      description: 'Черновик итогового теста по распределённым системам.',
      durationMin: 50,
      questionsCount: 0,
      attemptsCount: 0,
      averageScore: 0,
      status: 'draft',
      createdAt: '2026-07-01'
    }
  ],

  // Вопросы четырёх типов: single, multiple, text, code.
  questions: [
    {
      id: 'q1',
      testId: 't1',
      type: 'single',
      text: 'Что такое CLR?',
      points: 1,
      options: [
        'Среда выполнения общего языка',
        'Компилятор языка C#',
        'Графическая библиотека',
        'Формат хранения данных'
      ]
    },
    {
      id: 'q2',
      testId: 't1',
      type: 'multiple',
      text: 'Какие из перечисленных типов являются значимыми (value type)?',
      points: 2,
      options: ['int', 'string', 'double', 'bool']
    },
    {
      id: 'q3',
      testId: 't1',
      type: 'text',
      text: 'Кратко объясните, чем отличается класс от структуры в C#.',
      points: 2,
      options: []
    },
    {
      id: 'q4',
      testId: 't1',
      type: 'code',
      text: 'Напишите метод Sum, который принимает два целых числа и возвращает их сумму.',
      points: 3,
      language: 'C#',
      options: []
    },
    {
      id: 'q5',
      testId: 't2',
      type: 'single',
      text: 'Какое ключевое слово используется для наследования класса в C#?',
      points: 1,
      options: ['extends', 'implements', ':', 'inherits']
    },
    {
      id: 'q6',
      testId: 't2',
      type: 'multiple',
      text: 'Какие модификаторы доступа существуют в C#?',
      points: 2,
      options: ['public', 'private', 'protected', 'friendly']
    },
    {
      id: 'q7',
      testId: 't2',
      type: 'code',
      text: 'Опишите класс Student с полями Name и Group и методом PrintInfo.',
      points: 3,
      language: 'C#',
      options: []
    },
    {
      id: 'q8',
      testId: 't3',
      type: 'multiple',
      text: 'Какие методы массива не изменяют исходный массив?',
      points: 2,
      options: ['map', 'filter', 'push', 'slice']
    },
    {
      id: 'q9',
      testId: 't3',
      type: 'text',
      text: 'Что такое event loop и зачем он нужен в JavaScript?',
      points: 2,
      options: []
    },
    {
      id: 'q10',
      testId: 't4',
      type: 'code',
      text: 'Напишите SQL-запрос, который выводит имя студента и название его курса.',
      points: 3,
      language: 'SQL',
      options: []
    }
  ],

  // Попытки прохождения тестов с оценками и статусами проверки.
  attempts: [
    {
      id: 'a1',
      testId: 't1',
      studentId: 'u1',
      startedAt: '2026-06-18T10:00:00',
      finishedAt: '2026-06-18T10:24:00',
      score: 8,
      maxScore: 10,
      status: 'checked',
      grade: 4
    },
    {
      id: 'a2',
      testId: 't2',
      studentId: 'u1',
      startedAt: '2026-06-20T14:10:00',
      finishedAt: '2026-06-20T14:48:00',
      score: 7,
      maxScore: 10,
      status: 'pending',
      grade: null
    },
    {
      id: 'a3',
      testId: 't3',
      studentId: 'u2',
      startedAt: '2026-06-21T09:30:00',
      finishedAt: '2026-06-21T10:02:00',
      score: 9,
      maxScore: 10,
      status: 'checked',
      grade: 5
    },
    {
      id: 'a4',
      testId: 't4',
      studentId: 'u3',
      startedAt: '2026-06-22T11:00:00',
      finishedAt: '2026-06-22T11:33:00',
      score: 6,
      maxScore: 10,
      status: 'checked',
      grade: 3
    },
    {
      id: 'a5',
      testId: 't1',
      studentId: 'u4',
      startedAt: '2026-06-18T12:00:00',
      finishedAt: '2026-06-18T12:19:00',
      score: 10,
      maxScore: 10,
      status: 'checked',
      grade: 5
    },
    {
      id: 'a6',
      testId: 't5',
      studentId: 'u5',
      startedAt: '2026-06-23T15:20:00',
      finishedAt: '2026-06-23T15:42:00',
      score: 4,
      maxScore: 10,
      status: 'checked',
      grade: 2
    },
    {
      id: 'a7',
      testId: 't3',
      studentId: 'u1',
      startedAt: '2026-06-24T16:00:00',
      finishedAt: null,
      score: 0,
      maxScore: 10,
      status: 'in-progress',
      grade: null
    },
    {
      id: 'a8',
      testId: 't2',
      studentId: 'u2',
      startedAt: '2026-06-24T09:00:00',
      finishedAt: '2026-06-24T09:41:00',
      score: 8,
      maxScore: 10,
      status: 'pending',
      grade: null
    }
  ],

  // Проверка кода с вердиктами системы проверки.
  submissions: [
    {
      id: 's1',
      studentId: 'u1',
      task: 'Сумма двух чисел',
      language: 'C#',
      submittedAt: '2026-06-19T10:12:00',
      verdict: 'OK',
      timeMs: 40,
      memoryMb: 32
    },
    {
      id: 's2',
      studentId: 'u2',
      task: 'Разворот строки',
      language: 'JavaScript',
      submittedAt: '2026-06-19T11:05:00',
      verdict: 'WA',
      timeMs: 55,
      memoryMb: 28
    },
    {
      id: 's3',
      studentId: 'u3',
      task: 'Проверка палиндрома',
      language: 'Python',
      submittedAt: '2026-06-20T13:40:00',
      verdict: 'TLE',
      timeMs: 5000,
      memoryMb: 41
    },
    {
      id: 's4',
      studentId: 'u4',
      task: 'FizzBuzz',
      language: 'C#',
      submittedAt: '2026-06-20T15:18:00',
      verdict: 'OK',
      timeMs: 38,
      memoryMb: 30
    },
    {
      id: 's5',
      studentId: 'u1',
      task: 'Поиск максимума в массиве',
      language: 'C#',
      submittedAt: '2026-06-21T09:22:00',
      verdict: 'CE',
      timeMs: 0,
      memoryMb: 0
    },
    {
      id: 's6',
      studentId: 'u5',
      task: 'SQL: топ студентов по баллам',
      language: 'SQL',
      submittedAt: '2026-06-21T12:47:00',
      verdict: 'OK',
      timeMs: 120,
      memoryMb: 18
    },
    {
      id: 's7',
      studentId: 'u2',
      task: 'Сортировка пузырьком',
      language: 'JavaScript',
      submittedAt: '2026-06-22T10:31:00',
      verdict: 'WA',
      timeMs: 61,
      memoryMb: 27
    },
    {
      id: 's8',
      studentId: 'u3',
      task: 'Бинарный поиск',
      language: 'Python',
      submittedAt: '2026-06-22T14:09:00',
      verdict: 'OK',
      timeMs: 45,
      memoryMb: 34
    }
  ],

  // Сводная аналитика по курсам для диаграмм и таблиц.
  analytics: {
    summary: {
      students: 47,
      teachers: 3,
      courses: 6,
      tests: 6,
      averageScore: 4.3,
      passRate: 82
    },
    courses: [
      { courseId: 'c1', averageScore: 4.4, passRate: 88, attempts: 120, students: 28 },
      { courseId: 'c2', averageScore: 4.6, passRate: 91, attempts: 96, students: 24 },
      { courseId: 'c3', averageScore: 3.6, passRate: 71, attempts: 112, students: 31 },
      { courseId: 'c4', averageScore: 4.0, passRate: 80, attempts: 54, students: 19 },
      { courseId: 'c5', averageScore: 3.9, passRate: 76, attempts: 38, students: 16 },
      { courseId: 'c6', averageScore: 4.2, passRate: 84, attempts: 27, students: 22 }
    ]
  }
};
