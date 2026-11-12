/*
 * test-editor.js — конструктор теста для преподавателя.
 * Показывает параметры теста и вопросы, действия — заглушки.
 */
(function () {
  'use strict';

  var App = window.App;

  // Русские названия типов вопросов.
  var TYPE_LABELS = {
    single: 'Один вариант',
    multiple: 'Несколько вариантов',
    text: 'Свободный ответ',
    code: 'Задача на код'
  };

  // Возвращает курсы текущего преподавателя.
  function getTeacherCourses(user) {
    return window.Mock.courses.filter(function (course) {
      return course.teacherId === user.id;
    });
  }

  // Заполняет выпадающий список тестов преподавателя.
  function fillTestSelect(tests) {
    var select = document.getElementById('editor-test-select');
    if (!select) {
      return;
    }
    select.innerHTML = tests.map(function (test) {
      return '<option value="' + test.id + '">' +
        App.escapeHtml(test.title) + '</option>';
    }).join('');
  }

  // Заполняет список курсов для формы настроек.
  function fillCourseSelect(courses) {
    var select = document.getElementById('test-course-input');
    if (!select) {
      return;
    }
    select.innerHTML = courses.map(function (course) {
      return '<option value="' + course.id + '">' +
        App.escapeHtml(course.title) + '</option>';
    }).join('');
  }

  // Заполняет поля формы данными выбранного теста.
  function renderSettings(test) {
    if (!test) {
      return;
    }
    document.getElementById('test-title-input').value = test.title;
    document.getElementById('test-course-input').value = test.courseId;
    document.getElementById('test-duration-input').value = test.durationMin;
    document.getElementById('test-status-input').value = test.status;
    document.getElementById('test-description-input').value = test.description;
  }

  // Рисует карточку одного вопроса с кнопками редактирования и удаления.
  function renderQuestionCard(question) {
    var optionsPreview = question.options && question.options.length
      ? '<div class="card-meta">' + question.options.map(function (option) {
          return '<span class="pill">' + App.escapeHtml(option) + '</span>';
        }).join('') + '</div>'
      : '<p class="muted small">' + (
          question.type === 'code'
            ? 'Язык решения: ' + App.escapeHtml(question.language || 'C#')
            : 'Ответ вводится в свободной форме'
        ) + '</p>';

    return '<article class="question-card">' +
      '<div class="flex-between">' +
      '<span class="question-type">' + App.escapeHtml(TYPE_LABELS[question.type] || question.type) + '</span>' +
      '<span class="question-points">' + question.points + ' б.</span>' +
      '</div>' +
      '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
      optionsPreview +
      '<div class="form-actions">' +
      '<button class="btn btn-sm btn-outline" type="button" data-action="Редактировать вопрос">Редактировать</button>' +
      '<button class="btn btn-sm btn-danger-soft" type="button" data-action="Удалить вопрос">Удалить</button>' +
      '</div>' +
      '</article>';
  }

  // Рисует вопросы выбранного теста.
  function renderQuestions(test) {
    var container = document.getElementById('question-list');
    var counter = document.getElementById('questions-count');
    if (!container) {
      return;
    }

    var questions = test ? window.Mock.questions.filter(function (question) {
      return question.testId === test.id;
    }) : [];

    if (counter) {
      counter.textContent = 'Всего вопросов: ' + questions.length;
    }

    if (!questions.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">' +
        'В этом тесте пока нет вопросов. Добавьте первый вопрос кнопками выше.</p></div>';
      return;
    }

    container.innerHTML = questions.map(renderQuestionCard).join('');
  }

  // Рисует кнопки добавления вопросов четырёх типов.
  function renderQuestionActions() {
    var container = document.getElementById('question-actions');
    if (!container) {
      return;
    }

    var buttons = [
      { type: 'single', label: '+ Один вариант' },
      { type: 'multiple', label: '+ Несколько вариантов' },
      { type: 'text', label: '+ Текстовый ответ' },
      { type: 'code', label: '+ Задача на код' }
    ];

    container.innerHTML = buttons.map(function (button) {
      return '<button class="btn btn-soft btn-sm" type="button" data-add="' +
        button.type + '">' + button.label + '</button>';
    }).join('');

    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-add]');
      if (button) {
        App.stub('Добавить вопрос: ' + (TYPE_LABELS[button.getAttribute('data-add')] || ''));
      }
    });
  }

  // Навешивает заглушки на кнопки внутри списка вопросов.
  function bindQuestionList() {
    var container = document.getElementById('question-list');
    if (!container) {
      return;
    }
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-action]');
      if (button) {
        App.stub(button.getAttribute('data-action'));
      }
    });
  }

  // Инициализирует конструктор теста.
  function init() {
    if (!App.requireRole(['teacher'])) {
      return;
    }

    var user = App.getUser();
    if (!user) {
      return;
    }

    var courses = getTeacherCourses(user);
    var courseIds = courses.map(function (course) {
      return course.id;
    });
    var tests = window.Mock.tests.filter(function (test) {
      return courseIds.indexOf(test.courseId) !== -1;
    });

    if (!tests.length) {
      document.getElementById('question-list').innerHTML =
        '<div class="empty-state"><p class="muted">У ваших курсов пока нет тестов.</p></div>';
      return;
    }

    fillTestSelect(tests);
    fillCourseSelect(courses);

    var requestedId = App.getQueryParam('id');
    var selected = tests.filter(function (test) {
      return test.id === requestedId;
    })[0] || tests[0];

    document.getElementById('editor-test-select').value = selected.id;
    renderSettings(selected);
    renderQuestions(selected);
    renderQuestionActions();
    bindQuestionList();

    // Переключение теста обновляет форму и список вопросов.
    document.getElementById('editor-test-select').addEventListener('change', function (event) {
      var test = App.getTestById(event.target.value);
      renderSettings(test);
      renderQuestions(test);
    });

    // Сохранение теста и предпросмотр — заглушки.
    App.bindForm('test-settings-form', 'Сохранить тест');
    document.getElementById('test-preview-btn').addEventListener('click', function () {
      App.stub('Предпросмотр теста');
    });

    // Кнопка публикации в шапке страницы.
    var actions = document.getElementById('editor-actions');
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="publish-test-btn">Опубликовать тест</button>';
    document.getElementById('publish-test-btn').addEventListener('click', function () {
      App.stub('Опубликовать тест');
    });
  }

  init();
})();
