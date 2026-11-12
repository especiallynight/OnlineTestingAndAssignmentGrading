/*
 * test-taking.js — прохождение теста (только внешний вид).
 * Показывает все четыре типа вопросов из мока, ответы не проверяются.
 */
(function () {
  'use strict';

  var App = window.App;

  // Русские подписи для типов вопросов.
  var TYPE_LABELS = {
    single: 'Один вариант ответа',
    multiple: 'Несколько вариантов ответа',
    text: 'Свободный ответ',
    code: 'Задача на программирование'
  };

  // Находит тест по query-параметру id.
  function resolveTest() {
    var id = App.getQueryParam('id');
    return (id ? App.getTestById(id) : null) || window.Mock.tests[0] || null;
  }

  // Рисует шапку теста: название, курс, длительность и количество вопросов.
  function renderTestInfo(test) {
    var course = App.getCourseById(test.courseId);
    document.title = test.title + ' — OnlineTesting';
    document.getElementById('test-title').textContent = test.title;
    document.getElementById('test-subtitle').textContent = test.description;
    document.getElementById('test-meta').textContent =
      (course ? course.title : 'Курс') + ' · ' +
      test.questionsCount + ' вопросов · ' +
      test.durationMin + ' минут';
  }

  // Возвращает разметку вариантов для вопроса с одним ответом.
  function renderSingleOptions(question) {
    return question.options.map(function (option, index) {
      return '<label class="option-row">' +
        '<input type="radio" name="q-' + question.id + '" value="' + index + '">' +
        '<span>' + App.escapeHtml(option) + '</span>' +
        '</label>';
    }).join('');
  }

  // Возвращает разметку вариантов для вопроса с несколькими ответами.
  function renderMultipleOptions(question) {
    return question.options.map(function (option, index) {
      return '<label class="option-row">' +
        '<input type="checkbox" name="q-' + question.id + '" value="' + index + '">' +
        '<span>' + App.escapeHtml(option) + '</span>' +
        '</label>';
    }).join('');
  }

  // Возвращает поле для свободного текстового ответа.
  function renderTextAnswer(question) {
    return '<input class="answer-input" type="text" name="q-' + question.id + '" ' +
      'placeholder="Введите ответ">';
  }

  // Возвращает редактор кода для задачи на программирование.
  function renderCodeAnswer(question) {
    return '<span class="code-label">Язык: ' + App.escapeHtml(question.language || 'C#') + '</span>' +
      '<textarea class="code-editor" name="q-' + question.id + '" ' +
      'placeholder="// Напишите решение здесь"></textarea>';
  }

  // Собирает карточку одного вопроса.
  function renderQuestion(question, index) {
    var answerHtml = '';
    if (question.type === 'single') {
      answerHtml = '<div class="options-list">' + renderSingleOptions(question) + '</div>';
    } else if (question.type === 'multiple') {
      answerHtml = '<div class="options-list">' + renderMultipleOptions(question) + '</div>';
    } else if (question.type === 'text') {
      answerHtml = renderTextAnswer(question);
    } else {
      answerHtml = renderCodeAnswer(question);
    }

    return '<article class="question-card">' +
      '<div class="question-head">' +
      '<span class="question-number">' + (index + 1) + '</span>' +
      '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
      '<span class="question-points">' + question.points + ' б.</span>' +
      '</div>' +
      '<span class="question-type">' + App.escapeHtml(TYPE_LABELS[question.type] || question.type) + '</span>' +
      answerHtml +
      '</article>';
  }

  // Рисует все вопросы выбранного теста.
  function renderQuestions(test) {
    var container = document.getElementById('question-list');
    if (!container) {
      return;
    }

    var questions = window.Mock.questions.filter(function (question) {
      return question.testId === test.id;
    });

    if (!questions.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">В этом тесте пока нет вопросов.</p></div>';
      return;
    }

    container.innerHTML = questions.map(renderQuestion).join('');
  }

  // Инициализирует страницу прохождения теста.
  function init() {
    if (!App.requireRole(['student'])) {
      return;
    }

    var test = resolveTest();
    if (!test) {
      return;
    }

    renderTestInfo(test);
    renderQuestions(test);

    // Отправка ответов — заглушка до подключения ExamService.
    App.bindForm('test-form', 'Отправить ответы');
  }

  init();
})();
