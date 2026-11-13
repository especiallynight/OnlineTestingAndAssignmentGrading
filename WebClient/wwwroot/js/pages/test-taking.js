/*
 * test-taking.js — прохождение теста студентом.
 * Попытка создаётся в ExamService, ответы сохраняются по мере ввода.
 */
(function () {
  'use strict';

  var App = window.App;

  var TYPE_LABELS = {
    single: 'Один вариант ответа',
    multiple: 'Несколько вариантов ответа',
    code: 'Задача на программирование'
  };

  var state = {
    assignment: null,
    course: null,
    questions: [],
    attempt: null,
    answers: {},
    debouncers: {},
    timerId: null,
    finished: false
  };

  function pad(value) {
    return value < 10 ? '0' + value : String(value);
  }

  function renderTestInfo() {
    document.title = state.assignment.title + ' — OnlineTesting';
    document.getElementById('test-title').textContent = state.assignment.title;
    document.getElementById('test-subtitle').textContent =
      state.assignment.description || 'Тест без описания.';

    var parts = [];
    if (state.course) {
      parts.push('Курс: ' + state.course.title);
    }
    parts.push('Вопросов: ' + state.questions.length);
    parts.push('Время: ' + state.assignment.timeLimitMinutes + ' мин');
    parts.push('Максимум баллов: ' + state.assignment.maxScore);
    document.getElementById('test-meta').textContent = parts.join(' · ');
  }

  function renderOptions(question) {
    var inputType = question.type === 'single' ? 'radio' : 'checkbox';

    return '<div class="options-list">' + question.options.map(function (option) {
      return '<label class="option-row">' +
        '<input type="' + inputType + '" name="q-' + question.id + '" value="' +
        App.escapeHtml(option.key) + '">' +
        '<span class="option-key">' + App.escapeHtml(option.key) + '</span>' +
        '<span>' + App.escapeHtml(option.text) + '</span>' +
        '</label>';
    }).join('') + '</div>';
  }

  function renderCode(question) {
    return '<div class="form-field">' +
      '<label for="q-' + question.id + '">Исходный код (C#)</label>' +
      '<textarea id="q-' + question.id + '" class="code-editor" rows="10" spellcheck="false" ' +
      'placeholder="// Напишите решение задачи"></textarea>' +
      '</div>' +
      '<div class="form-actions">' +
      '<button class="btn btn-sm btn-outline" type="button" data-check-code="' + question.id + '">' +
      'Проверить код</button>' +
      '<span class="form-hint" data-code-result="' + question.id + '"></span>' +
      '</div>';
  }

  function renderQuestions() {
    var container = document.getElementById('question-list');
    if (!state.questions.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">' +
        'В этом тесте нет вопросов. Сообщите преподавателю.</p></div>';
      return;
    }

    container.innerHTML = state.questions.map(function (question, index) {
      return '<article class="question-card" data-question="' + question.id + '">' +
        '<div class="flex-between">' +
        '<span class="question-type">Вопрос ' + (index + 1) + ' · ' +
        App.escapeHtml(TYPE_LABELS[question.type] || question.type) + '</span>' +
        '<span class="question-points">' + App.escapeHtml(question.apiType) + '</span>' +
        '</div>' +
        '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
        (question.type === 'code' ? renderCode(question) : renderOptions(question)) +
        '</article>';
    }).join('');

    bindAnswers();
  }

  function collectAnswer(question) {
    if (question.type === 'code') {
      var textarea = document.getElementById('q-' + question.id);
      var code = textarea ? textarea.value : '';
      return code.trim() ? code : null;
    }

    if (question.type === 'single') {
      var selected = document.querySelector(
        'input[name="q-' + question.id + '"]:checked');
      return selected ? JSON.stringify(selected.value) : null;
    }

    var checked = document.querySelectorAll(
      'input[name="q-' + question.id + '"]:checked');
    if (!checked.length) {
      return null;
    }
    var keys = Array.prototype.map.call(checked, function (input) {
      return input.value;
    }).sort();
    return JSON.stringify(keys);
  }

  // Сохраняет ответ на вопрос, создавая или обновляя запись в ExamService.
  async function saveQuestion(question) {
    var entry = state.answers[question.id] || (state.answers[question.id] = {});
    if (entry.promise) {
      entry.dirty = true;
      return;
    }

    var data = collectAnswer(question);
    if (data === null || data === entry.saved) {
      return;
    }

    var promise = (async function () {
      if (entry.answerId) {
        await App.api.attempts.updateAnswer(entry.answerId, data);
      } else {
        entry.answerId = await App.api.attempts.saveAnswer(
          state.attempt.id, question.id, data);
      }
      entry.saved = data;
    })();

    entry.promise = promise;
    try {
      await promise;
    } finally {
      entry.promise = null;
    }

    if (entry.dirty) {
      entry.dirty = false;
      await saveQuestion(question);
    }
  }

  // Ждёт завершения сохранений, запущенных по debounce.
  async function waitForPendingSaves() {
    var pending = Object.keys(state.answers).map(function (id) {
      return state.answers[id].promise;
    }).filter(function (promise) {
      return !!promise;
    });

    if (!pending.length) {
      return;
    }

    await Promise.all(pending.map(function (promise) {
      return promise.catch(function () {});
    }));
    await waitForPendingSaves();
  }

  function scheduleSave(question) {
    if (!state.debouncers[question.id]) {
      state.debouncers[question.id] = App.debounce(function () {
        saveQuestion(question).catch(function (error) {
          App.notify(error.message, 'error');
        });
      }, question.type === 'code' ? 900 : 350);
    }
    state.debouncers[question.id]();
  }

  // Восстанавливает ответы, сохранённые в предыдущем заходе в тест.
  async function restoreAnswers() {
    var savedAnswers;
    try {
      savedAnswers = await App.api.attempts.answers(state.attempt.id);
    } catch (error) {
      return;
    }

    savedAnswers.forEach(function (answer) {
      var question = state.questions.filter(function (item) {
        return String(item.id) === String(answer.questionId);
      })[0];
      if (!question) {
        return;
      }

      state.answers[question.id] = {
        answerId: answer.answerId,
        saved: answer.data
      };

      if (question.type === 'code') {
        var textarea = document.getElementById('q-' + question.id);
        if (textarea) {
          textarea.value = answer.data || '';
        }
        if (answer.executionStatus) {
          var resultHost = document.querySelector(
            '[data-code-result="' + question.id + '"]');
          if (resultHost) {
            resultHost.textContent = 'Вердикт: ' + answer.executionStatus +
              ', тесты: ' + (answer.passedTests || 0) +
              ' из ' + (answer.totalTests || 0);
          }
        }
        return;
      }

      var values = [];
      try {
        var parsed = JSON.parse(answer.data);
        values = Array.isArray(parsed) ? parsed : [parsed];
      } catch (error) {
        values = [answer.data];
      }

      values.forEach(function (value) {
        var input = document.querySelector(
          'input[name="q-' + question.id + '"][value="' + value + '"]');
        if (input) {
          input.checked = true;
        }
      });
    });
  }

  function bindAnswers() {
    state.questions.forEach(function (question) {
      var card = document.querySelector('[data-question="' + question.id + '"]');
      if (!card) {
        return;
      }
      if (question.type === 'code') {
        var textarea = document.getElementById('q-' + question.id);
        textarea.addEventListener('input', function () {
          scheduleSave(question);
        });
      } else {
        card.addEventListener('change', function () {
          scheduleSave(question);
        });
      }
    });

    var container = document.getElementById('question-list');
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-check-code]');
      if (!button) {
        return;
      }
      checkCode(button.getAttribute('data-check-code'), button)
        .catch(function (error) {
          App.notify(error.message, 'error');
        });
    });
  }

  // Отправляет код на проверку в CodeExecutionService через ExamService.
  async function checkCode(questionId, button) {
    var question = state.questions.filter(function (item) {
      return String(item.id) === String(questionId);
    })[0];
    if (!question) {
      return;
    }

    var resultHost = document.querySelector('[data-code-result="' + questionId + '"]');
    button.disabled = true;
    if (resultHost) {
      resultHost.textContent = 'Проверяем…';
    }

    try {
      await waitForPendingSaves();
      await saveQuestion(question);
      var entry = state.answers[question.id] || {};
      if (!entry.answerId) {
        throw new Error('Сначала напишите решение.');
      }
      var result = await App.api.attempts.submitCodeAnswer(entry.answerId);
      if (resultHost) {
        resultHost.textContent = 'Вердикт: ' + (result.executionStatus || '—') +
          ', тесты: ' + (result.passedTests || 0) + ' из ' + (result.totalTests || 0);
      }
      App.notify('Код проверен: ' + (result.executionStatus || 'нет вердикта'), 'success');
    } catch (error) {
      if (resultHost) {
        resultHost.textContent = '';
      }
      throw error;
    } finally {
      button.disabled = false;
    }
  }

  function startTimer() {
    var timer = document.getElementById('test-timer');
    if (!timer) {
      return;
    }

    var deadline = state.attempt.deadlineAt
      ? new Date(state.attempt.deadlineAt).getTime()
      : null;

    if (!deadline) {
      timer.textContent = '—';
      return;
    }

    function tick() {
      var remaining = deadline - Date.now();
      if (remaining <= 0) {
        timer.textContent = '00:00';
        clearInterval(state.timerId);
        finish(true);
        return;
      }
      var total = Math.floor(remaining / 1000);
      timer.textContent = pad(Math.floor(total / 60)) + ':' + pad(total % 60);
    }

    tick();
    state.timerId = setInterval(tick, 1000);
  }

  // Завершает попытку и отправляет ответы на проверку.
  async function finish(auto) {
    if (state.finished) {
      return;
    }
    state.finished = true;

    var submit = document.getElementById('test-submit');
    if (submit) {
      submit.disabled = true;
    }

    try {
      await waitForPendingSaves();

      // Сохраняем ответы по одному. Если попытка уже закрыта (409),
      // завершение всё равно должно пройти.
      for (var i = 0; i < state.questions.length; i++) {
        try {
          await saveQuestion(state.questions[i]);
        } catch (error) {
          if (!error || error.status !== 409) {
            throw error;
          }
        }
      }
      await waitForPendingSaves();

      try {
        await App.api.attempts.stop(state.attempt.id);
      } catch (error) {
        if (!error || error.status !== 409) {
          throw error;
        }
      }
      clearInterval(state.timerId);
      App.notify(auto
        ? 'Время вышло. Работа отправлена.'
        : 'Работа отправлена. Оценка появится в «Моих результатах».', 'success');
      setTimeout(function () {
        location.href = App.url('my-results.html');
      }, 1200);
    } catch (error) {
      state.finished = false;
      if (submit) {
        submit.disabled = false;
      }
      App.notify(error.message, 'error');
    }
  }

  function handleSubmit(event) {
    event.preventDefault();
    if (!window.confirm('Завершить попытку и отправить ответы?')) {
      return;
    }
    finish(false);
  }

  async function load() {
    var id = App.getQueryParam('id');
    if (!id) {
      throw new Error('Не указан идентификатор теста.');
    }

    state.assignment = await App.api.assignments.get(id);
    state.questions = await App.api.questions.list(id);

    try {
      state.course = await App.api.courses.get(state.assignment.courseId);
    } catch (error) {
      state.course = null;
    }

    renderTestInfo();
    renderQuestions();

    state.attempt = await App.api.attempts.create(state.assignment.id);
    await restoreAnswers();
    startTimer();
  }

  async function init() {
    if (!App.requireRole(['student'])) {
      return;
    }

    App.bindForm('test-form', handleSubmit);

    try {
      await load();
    } catch (error) {
      document.getElementById('question-list').innerHTML =
        '<div class="empty-state"><p class="muted">' +
        App.escapeHtml(error.message) + '</p></div>';
      var submit = document.getElementById('test-submit');
      if (submit) {
        submit.disabled = true;
      }
    }
  }

  init();
})();
