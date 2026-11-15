/*
 * test-editor.js — конструктор заданий преподавателя.
 * Задание CourseService хранит и тест, и задачу на код, вопросы лежат там же.
 */
(function () {
  'use strict';

  var App = window.App;

  var TYPE_LABELS = {
    single: 'Один вариант',
    multiple: 'Несколько вариантов',
    code: 'Задача на код'
  };

  var state = {
    user: null,
    courses: [],
    assignments: [],
    selectedCourseId: null,
    selected: null,
    questions: []
  };

  function getTeacherCourses() {
    return state.courses.filter(function (course) {
      return App.sameId(course.teacherId, state.user.id);
    });
  }

  function getSelectedCourse() {
    return getTeacherCourses().filter(function (course) {
      return App.sameId(course.id, state.selectedCourseId);
    })[0] || null;
  }

  function courseAssignments() {
    if (!state.selectedCourseId) {
      return [];
    }
    return state.assignments.filter(function (assignment) {
      return App.sameId(assignment.courseId, state.selectedCourseId);
    });
  }

  function fillCourseSelect() {
    var select = document.getElementById('editor-course-select');
    if (!select) {
      return;
    }
    select.innerHTML = getTeacherCourses().map(function (course) {
      return '<option value="' + course.id + '">' +
        App.escapeHtml(course.title) + '</option>';
    }).join('');
    if (state.selectedCourseId) {
      select.value = state.selectedCourseId;
    }
  }

  function fillTestSelect() {
    var select = document.getElementById('editor-test-select');
    if (!select) {
      return;
    }
    var assignments = courseAssignments();
    select.innerHTML = assignments.length
      ? assignments.map(function (assignment) {
          return '<option value="' + assignment.id + '">' +
            App.escapeHtml(assignment.title) + '</option>';
        }).join('')
<<<<<<< HEAD
      : '<option value="">— в курсе нет заданий —</option>';
=======
      : '<option value="">— в курсе нет тестов —</option>';
>>>>>>> 9cf917e (feat: add teacher panel to main HTML)
  }

  function renderSettings() {
    var assignment = state.selected;
    if (!assignment) {
      return;
    }
    document.getElementById('test-title-input').value = assignment.title;
    document.getElementById('test-duration-input').value = assignment.timeLimitMinutes;
    document.getElementById('test-max-score-input').value = assignment.maxScore;
    document.getElementById('test-description-input').value = assignment.description;
  }

  function renderQuestionCard(question) {
    var optionsPreview;
    if (question.options.length) {
      optionsPreview = '<div class="card-meta">' + question.options.map(function (option) {
        var correct = question.correctKeys.indexOf(option.key) !== -1;
        return '<span class="pill' + (correct ? ' pill-correct' : '') + '">' +
          App.escapeHtml(option.text) + (correct ? ' ✓' : '') + '</span>';
      }).join('') + '</div>';
    } else if (question.type === 'code') {
      optionsPreview = '<p class="muted small">Тестовых случаев: ' + question.testCases.length + '</p>';
    } else {
      optionsPreview = '<p class="muted small">Варианты не заданы</p>';
    }

    return '<article class="question-card">' +
      '<div class="flex-between">' +
      '<span class="question-type">' + App.escapeHtml(TYPE_LABELS[question.type] || question.type) + '</span>' +
      '<span class="question-points">Вопрос #' + question.id + '</span>' +
      '</div>' +
      '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
      optionsPreview +
      '<div class="form-actions">' +
      '<button class="btn btn-sm btn-outline" type="button" data-edit-question="' + question.id + '">Редактировать</button>' +
      '<button class="btn btn-sm btn-danger-soft" type="button" data-delete-question="' + question.id + '">Удалить</button>' +
      '</div>' +
      '</article>';
  }

  function renderQuestions() {
    var container = document.getElementById('question-list');
    var counter = document.getElementById('questions-count');
    if (!container) {
      return;
    }

    if (counter) {
      counter.textContent = 'Всего вопросов: ' + state.questions.length;
    }

    if (!state.questions.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">' +
        'В этом задании пока нет вопросов. Добавьте первый вопрос кнопками выше.</p></div>';
      return;
    }

    container.innerHTML = state.questions.map(renderQuestionCard).join('');
  }

  // Строит поля формы вопроса для выбранного типа.
  function buildQuestionBody(type, question) {
    var text = question ? question.text : '';
    var options = question && question.options.length
      ? question.options
      : [{ key: 'a', text: '' }, { key: 'b', text: '' }, { key: 'c', text: '' }, { key: 'd', text: '' }];

    var html =
      '<div class="form-field">' +
      '<label for="question-text">Текст вопроса (до 100 символов)</label>' +
      '<input id="question-text" name="text" type="text" minlength="3" maxlength="100" required value="' +
      App.escapeHtml(text) + '">' +
      '</div>';

    if (type === 'single' || type === 'multiple') {
      var inputType = type === 'single' ? 'radio' : 'checkbox';
      html += '<div class="form-field"><label>Варианты ответа и правильный ответ</label>';
      html += options.map(function (option) {
        var checked = question && question.correctKeys.indexOf(option.key) !== -1 ? ' checked' : '';
        return '<label class="option-row">' +
          '<input type="' + inputType + '" name="correct" value="' + option.key + '"' + checked + '>' +
          '<span class="option-key">' + option.key + '</span>' +
          '<input type="text" name="option-' + option.key + '" maxlength="120" value="' +
          App.escapeHtml(option.text) + '" placeholder="Текст варианта">' +
          '</label>';
      }).join('');
      html += '</div>';
    }

    if (type === 'code') {
      var testCases = question && question.testCases.length
        ? question.testCases
        : [{ input: '', expectedOutput: '' }];
      html +=
        '<div class="form-field">' +
        '<label>Тесты (вход → ожидаемый вывод)</label>' +
        '<div class="test-case-list" data-test-cases>' +
        testCases.map(renderTestCaseRow).join('') +
        '</div>' +
        '<button class="btn btn-sm btn-outline" type="button" data-add-test>Добавить тест</button>' +
        '<p class="form-hint">Первый тест показывается студенту как пример, остальные используются для скрытой проверки.</p>' +
        '</div>';
    }

    return html;
  }

  // Строка теста: входные данные и ожидаемый вывод.
  function renderTestCaseRow(testCase) {
    return '<div class="test-case-row" data-test-case>' +
      '<input type="text" data-test-input placeholder="Входные данные" value="' +
      App.escapeHtml(testCase.input || '') + '">' +
      '<input type="text" data-test-output placeholder="Ожидаемый вывод" value="' +
      App.escapeHtml(testCase.expectedOutput || '') + '">' +
      '<button class="btn btn-sm btn-danger-soft" type="button" data-remove-test>Удалить</button>' +
      '</div>';
  }

  // Собирает тело запроса из формы вопроса.
  function collectQuestion(type, form) {
    var values = App.formValues(form);
    var payload = {
      questionText: (values.text || '').trim(),
      questionType: App.questionTypeToApi(type)
    };

    if (type === 'single' || type === 'multiple') {
      var options = {};
      ['a', 'b', 'c', 'd'].forEach(function (key) {
        var value = (values['option-' + key] || '').trim();
        if (value) {
          options[key] = value;
        }
      });

      if (Object.keys(options).length < 2) {
        throw new Error('Заполните минимум два варианта ответа.');
      }

      var correct = values.correct;
      if (type === 'single') {
        if (!correct) {
          throw new Error('Выберите правильный вариант ответа.');
        }
        payload.correctAnswer = correct;
      } else {
        if (!correct || !correct.length) {
          throw new Error('Отметьте хотя бы один правильный вариант.');
        }
        payload.correctAnswer = correct;
      }

      payload.options = options;
      payload.testCases = null;
    } else {
      payload.options = {};
      payload.correctAnswer = null;
      var rows = Array.prototype.slice.call(
        form.querySelectorAll('[data-test-case]'));
      var testCases = rows.map(function (row) {
        var input = row.querySelector('[data-test-input]');
        var output = row.querySelector('[data-test-output]');
        return {
          input: input ? input.value.trim() : '',
          expectedOutput: output ? output.value.trim() : ''
        };
      }).filter(function (testCase) {
        return testCase.input !== '' || testCase.expectedOutput !== '';
      });

      if (!testCases.length) {
        throw new Error('Добавьте хотя бы один тест.');
      }
      if (testCases.some(function (testCase) {
        return !testCase.expectedOutput;
      })) {
        throw new Error('Укажите ожидаемый вывод для каждого теста.');
      }

      payload.testCases = testCases;
    }

    return payload;
  }

  function openQuestionEditor(type, question) {
    if (!state.selected) {
      App.notify('Сначала создайте или выберите тест', 'error');
      return;
    }

    var isEdit = !!question;
    var modal = App.openModal({
      title: (isEdit ? 'Редактирование вопроса: ' : 'Новый вопрос: ') + TYPE_LABELS[type],
      submitLabel: isEdit ? 'Сохранить вопрос' : 'Добавить вопрос',
      body: buildQuestionBody(type, question),
      onSubmit: async function (form) {
        var payload = collectQuestion(type, form);
        if (isEdit) {
          await App.api.questions.update(question.id, payload);
          App.notify('Вопрос обновлён', 'success');
        } else {
          await App.api.questions.create(state.selected.id, payload);
          App.notify('Вопрос добавлен', 'success');
        }
        await loadQuestions();
      }
    });

    if (type === 'code' && modal) {
      var list = modal.form.querySelector('[data-test-cases]');
      var addButton = modal.form.querySelector('[data-add-test]');
      if (list && addButton) {
        addButton.addEventListener('click', function () {
          list.insertAdjacentHTML('beforeend', renderTestCaseRow({
            input: '',
            expectedOutput: ''
          }));
        });
        list.addEventListener('click', function (event) {
          var remove = event.target.closest('[data-remove-test]');
          if (!remove) {
            return;
          }
          if (list.querySelectorAll('[data-test-case]').length > 1) {
            remove.closest('[data-test-case]').remove();
          } else {
            App.notify('Должен остаться хотя бы один тест', 'error');
          }
        });
      }
    }
  }

  // Рисует кнопки добавления вопросов.
  function renderQuestionActions() {
    var container = document.getElementById('question-actions');
    if (!container) {
      return;
    }

    var buttons = [
      { type: 'single', label: '+ Один вариант' },
      { type: 'multiple', label: '+ Несколько вариантов' },
      { type: 'code', label: '+ Задача на код' }
    ];

    container.innerHTML = buttons.map(function (button) {
      return '<button class="btn btn-soft btn-sm" type="button" data-add="' +
        button.type + '">' + button.label + '</button>';
    }).join('');

    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-add]');
      if (button) {
        openQuestionEditor(button.getAttribute('data-add'), null);
      }
    });
  }

  function bindQuestionList() {
    var container = document.getElementById('question-list');
    if (!container) {
      return;
    }
    container.addEventListener('click', async function (event) {
      var editButton = event.target.closest('[data-edit-question]');
      if (editButton) {
        var question = App.getQuestionById(editButton.getAttribute('data-edit-question'));
        if (question) {
          openQuestionEditor(question.type, question);
        }
        return;
      }

      var deleteButton = event.target.closest('[data-delete-question]');
      if (deleteButton) {
        var target = App.getQuestionById(deleteButton.getAttribute('data-delete-question'));
        if (!target) {
          return;
        }
        if (!window.confirm('Удалить вопрос «' + target.text + '»?')) {
          return;
        }
        try {
          await App.api.questions.remove(target.id);
          App.notify('Вопрос удалён', 'success');
          await loadQuestions();
        } catch (error) {
          App.notify(error.message, 'error');
        }
      }
    });
  }

  // Показывает предпросмотр теста.
  function openPreview() {
    if (!state.selected) {
      App.notify('Сначала создайте или выберите тест', 'error');
      return;
    }

    var body = state.questions.length
      ? state.questions.map(function (question, index) {
          return '<article class="question-card">' +
            '<div class="flex-between">' +
            '<span class="question-type">' + (index + 1) + '. ' +
            App.escapeHtml(TYPE_LABELS[question.type] || question.type) + '</span>' +
            '</div>' +
            '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
            (question.options.length
              ? '<div class="card-meta">' + question.options.map(function (option) {
                  return '<span class="pill">' + App.escapeHtml(option.text) + '</span>';
                }).join('') + '</div>'
              : '') +
            '</article>';
        }).join('')
      : '<p class="muted">В задании пока нет вопросов.</p>';

    App.openModal({
      title: 'Предпросмотр: ' + state.selected.title,
      submitLabel: 'Закрыть',
      body: body,
      onSubmit: function () {
        // Закрытие выполняет сам диалог.
      }
    });
  }

  // Создание нового теста (задания).
  function openCreateTest() {
    var courses = getTeacherCourses();
    if (!courses.length) {
      App.notify('Сначала создайте курс в каталоге', 'error');
      return;
    }

    // Курс выбирается на странице конструктора, а не в самом окне:
    // в форме остаются только параметры теста.
    var defaultCourse = getSelectedCourse() || courses[0];
    state.selectedCourseId = defaultCourse.id;

    App.openModal({
      title: 'Новый тест — ' + defaultCourse.title,
      submitLabel: 'Создать тест',
      body:
        '<div class="form-field">' +
        '<label for="new-test-name">Название</label>' +
        '<input id="new-test-name" name="name" type="text" minlength="3" maxlength="100" required>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="new-test-description">Описание</label>' +
        '<textarea id="new-test-description" name="description" maxlength="170" rows="3"></textarea>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="new-test-duration">Длительность, минут</label>' +
        '<input id="new-test-duration" name="timeLimitMinutes" type="number" min="1" max="1440" value="30" required>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="new-test-score">Максимум баллов</label>' +
        '<input id="new-test-score" name="maxScore" type="number" min="1" max="1000" value="10" required>' +
        '</div>',
      onSubmit: async function (form) {
        var values = App.formValues(form);
          var created = await App.api.assignments.create(defaultCourse.id, {
              name: values.name.trim(),
              description: values.description.trim(),
              maxScore: Number(values.maxScore),
              timeLimitMinutes: Number(values.timeLimitMinutes)
          });

          console.log('Созданное задание:', created);
          App.notify('Тест создан', 'success');
          await loadAssignments();
          await selectTest(created.assignmentId);
        await loadAssignments();
        await selectTest(created.assignmentId);
      }
    });
  }

  async function loadQuestions() {
    if (!state.selected) {
      return;
    }
    state.questions = await App.api.questions.manage(state.selected.id);
    renderQuestions();
  }

  async function selectTest(id) {
    var assignments = courseAssignments();
    state.selected = assignments.filter(function (assignment) {
      return String(assignment.id) === String(id);
    })[0] || assignments[0] || null;

    if (!state.selected) {
      document.getElementById('question-list').innerHTML =
<<<<<<< HEAD
        '<div class="empty-state"><p class="muted">В этом курсе пока нет заданий. Нажмите «Новый тест».</p></div>';
=======
        '<div class="empty-state"><p class="muted">В этом курсе пока нет тестов. Нажмите «Новый тест».</p></div>';
>>>>>>> 9cf917e (feat: add teacher panel to main HTML)
      var counter = document.getElementById('questions-count');
      if (counter) {
        counter.textContent = 'Всего вопросов: 0';
      }
      var select = document.getElementById('editor-test-select');
      if (select) {
        select.value = '';
      }
      return;
    }

    document.getElementById('editor-test-select').value = state.selected.id;
    renderSettings();
    await loadQuestions();
  }

  async function loadAssignments() {
    var courses = getTeacherCourses();
    var lists = await Promise.all(courses.map(function (course) {
      return App.api.assignments.list(course.id);
    }));

    state.assignments = [];
    lists.forEach(function (list) {
      state.assignments = state.assignments.concat(list);
    });
    App.cacheAssignments(state.assignments);

    fillTestSelect();
  }

  async function init() {
    if (!App.requireRole(['teacher'])) {
      return;
    }

    state.user = App.getUser();
    if (!state.user) {
      location.href = App.url('login.html');
      return;
    }

    var actions = document.getElementById('editor-actions');
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="create-test-btn">Новый тест</button>';
    document.getElementById('create-test-btn').addEventListener('click', openCreateTest);

    try {
      state.courses = await App.api.courses.list();

      var teacherCourses = getTeacherCourses();
      if (!teacherCourses.length) {
        document.getElementById('question-list').innerHTML =
          '<div class="empty-state"><p class="muted">Сначала создайте курс в каталоге.</p></div>';
        return;
      }

      // Курс можно передать из карточки курса: ?courseId=5.
      var requestedCourseId = App.getQueryParam('courseId');
      var requestedId = App.getQueryParam('id');
      var requestedCourse = teacherCourses.filter(function (course) {
        return App.sameId(course.id, requestedCourseId);
      })[0];

      state.selectedCourseId = requestedCourse
        ? requestedCourse.id
        : teacherCourses[0].id;

      fillCourseSelect();
      await loadAssignments();

      if (requestedId) {
        var requestedAssignment = state.assignments.filter(function (assignment) {
          return String(assignment.id) === String(requestedId);
        })[0];
        if (requestedAssignment) {
          state.selectedCourseId = requestedAssignment.courseId;
          fillCourseSelect();
          fillTestSelect();
        }
      }

      await selectTest(requestedId);
      renderQuestionActions();
      bindQuestionList();

      document.getElementById('editor-course-select').addEventListener('change', function (event) {
        state.selectedCourseId = event.target.value;
        fillTestSelect();
        var first = courseAssignments()[0];
        selectTest(first ? first.id : null).catch(function (error) {
          App.notify(error.message, 'error');
        });
      });

      document.getElementById('editor-test-select').addEventListener('change', function (event) {
        if (!event.target.value) {
          return;
        }
        selectTest(event.target.value).catch(function (error) {
          App.notify(error.message, 'error');
        });
      });

      App.bindForm('test-settings-form', async function (event, form) {
        if (!state.selected) {
          throw new Error('Сначала создайте тест в выбранном курсе.');
        }
        var values = App.formValues(form);
        try {
          await App.api.assignments.update(state.selected.id, {
            name: values.title.trim(),
            description: values.description.trim(),
            maxScore: Number(values.maxScore),
            timeLimitMinutes: Number(values.durationMin)
          });
          state.selected = await App.api.assignments.get(state.selected.id);
          App.notify('Тест сохранён', 'success');
          await loadAssignments();
        } catch (error) {
          App.notify(error.message, 'error');
        }
      });

      document.getElementById('test-preview-btn').addEventListener('click', openPreview);

      // Переход из карточки курса: сразу открываем окно нового теста.
      if (App.getQueryParam('create') === '1') {
        openCreateTest();
      }
    } catch (error) {
      App.showError('question-list', error);
    }
  }

  init();
})();
