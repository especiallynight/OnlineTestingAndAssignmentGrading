/*
 * course-details.js — карточка курса: задания, тесты и запись на курс.
 * Все данные загружаются из CourseService через ApiGateway.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    course: null,
    assignments: [],
    materials: [],
    questionsCount: {},
    role: null,
    user: null,
    enrolled: false,
    attempts: {}
  };

  function courseId() {
    return App.getQueryParam('id');
  }

  // Возвращает последнюю попытку студента по заданию, если она есть.
  function latestAttempt(assignmentId) {
    return state.attempts[assignmentId] || null;
  }

  // Редактировать курс и теорию может только преподаватель — владелец курса.
  function canEditCourse() {
    return state.role === 'teacher' &&
      state.user &&
      String(state.course.teacherId) === String(state.user.id);
  }

  // Рисует шапку курса с основной информацией и действием.
  function renderHero() {
    var container = document.getElementById('course-hero');
    if (!container) {
      return;
    }

    var course = state.course;
    var teacher = App.getUserById(course.teacherId);
    var action = '';

    if (canEditCourse()) {
      action = '<button class="btn btn-primary" type="button" id="course-action-btn">Редактировать курс</button>';
    } else if (state.role === 'student') {
      action = state.enrolled
        ? '<button class="btn btn-danger-soft" type="button" id="course-action-btn">Отписаться от курса</button>'
        : '<button class="btn btn-primary" type="button" id="course-action-btn">Записаться на курс</button>';
    }

    container.innerHTML =
      '<div class="flex-between">' +
      '<div>' +
      '<span class="pill">' + App.escapeHtml(App.statusLabel('active')) + '</span>' +
      '<h2 class="section-title">' + App.escapeHtml(course.title) + '</h2>' +
      '<p class="muted">' + App.escapeHtml(course.description || 'Описание не заполнено.') + '</p>' +
      '</div>' +
      '<div class="form-actions">' + action + '</div>' +
      '</div>' +
      '<div class="course-meta">' +
      '<span>Преподаватель: ' + App.escapeHtml(teacher ? teacher.fullName : '#' + course.teacherId) + '</span>' +
      '<span>Заданий: ' + state.assignments.length + '</span>' +
      '<span>Студентов: ' + course.studentsCount + '</span>' +
      '<span>Создан: ' + App.formatDateOnly(course.createdAt) + '</span>' +
      '</div>';

    var button = document.getElementById('course-action-btn');
    if (!button) {
      return;
    }

    if (state.role === 'teacher') {
      button.addEventListener('click', openCourseEditor);
    } else {
      button.addEventListener('click', toggleEnrollment);
    }
  }

  function openCourseEditor() {
    App.openModal({
      title: 'Редактирование курса',
      submitLabel: 'Сохранить',
      body:
        '<div class="form-field">' +
        '<label for="edit-course-name">Название</label>' +
        '<input id="edit-course-name" name="name" type="text" minlength="3" maxlength="100" required value="' +
        App.escapeHtml(state.course.title) + '">' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="edit-course-description">Описание</label>' +
        '<textarea id="edit-course-description" name="description" maxlength="150" rows="4">' +
        App.escapeHtml(state.course.description) + '</textarea>' +
        '</div>',
      onSubmit: async function (form) {
        var values = App.formValues(form);
        await App.api.courses.update(state.course.id, {
          name: values.name.trim(),
          description: values.description.trim()
        });
        App.notify('Курс обновлён', 'success');
        await load();
      }
    });
  }

  // Записывает студента на курс или отменяет запись.
  async function toggleEnrollment() {
    var button = document.getElementById('course-action-btn');
    button.disabled = true;
    try {
      if (state.enrolled) {
        await App.api.courses.unenroll(state.course.id);
        App.notify('Вы отписались от курса', 'success');
      } else {
        await App.api.courses.enroll(state.course.id);
        App.notify('Вы записаны на курс', 'success');
      }
      await load();
    } catch (error) {
      App.notify(error.message, 'error');
      button.disabled = false;
    }
  }

  // Экранирует текст и превращает пустые строки в абзацы, а переносы — в <br>.
  function renderTheoryText(content, maxLength) {
    var text = String(content || '');
    if (maxLength && text.length > maxLength) {
      text = text.slice(0, maxLength).replace(/\s+\S*$/, '') + '…';
    }
    return text.split(/\n{2,}/).map(function (paragraph) {
      return '<p>' + App.escapeHtml(paragraph).replace(/\n/g, '<br>') + '</p>';
    }).join('');
  }

  function findMaterial(id) {
    return state.materials.find(function (material) {
      return String(material.id) === String(id);
    }) || null;
  }

  // Рисует блок теории: карточки материалов и действия преподавателя.
  function renderTheory() {
    var container = document.getElementById('course-theory');
    var actions = document.getElementById('course-theory-actions');
    if (!container) {
      return;
    }

    if (actions) {
      actions.innerHTML = canEditCourse()
        ? '<button class="btn btn-sm btn-primary" type="button" id="add-material-btn">Добавить материал</button>'
        : '';
      var addButton = document.getElementById('add-material-btn');
      if (addButton) {
        addButton.addEventListener('click', function () {
          openMaterialEditor(null);
        });
      }
    }

    if (!state.materials.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">' +
        (canEditCourse()
          ? 'Теория пока не добавлена. Нажмите «Добавить материал».'
          : 'В курсе пока нет материалов.') +
        '</p></div>';
      return;
    }

    container.innerHTML = state.materials.map(function (material, index) {
      var controls = canEditCourse()
        ? '<div class="theory-actions">' +
          '<button class="btn btn-sm btn-outline" type="button" data-edit="' + material.id + '">Редактировать</button>' +
          '<button class="btn btn-sm btn-danger-soft" type="button" data-delete="' + material.id + '">Удалить</button>' +
          '</div>'
        : '';

      return '<article class="theory-card">' +
        '<div class="theory-head">' +
        '<span class="module-number">' + (index + 1) + '</span>' +
        '<h3>' + App.escapeHtml(material.title) + '</h3>' +
        '</div>' +
        '<div class="theory-preview">' + renderTheoryText(material.content, 280) + '</div>' +
        '<div class="theory-foot">' +
        '<button class="btn btn-sm btn-soft" type="button" data-read="' + material.id + '">Читать</button>' +
        controls +
        '</div>' +
        '</article>';
    }).join('');

    Array.prototype.forEach.call(container.querySelectorAll('[data-read]'), function (button) {
      button.addEventListener('click', function () {
        openMaterialReader(findMaterial(button.getAttribute('data-read')));
      });
    });
    Array.prototype.forEach.call(container.querySelectorAll('[data-edit]'), function (button) {
      button.addEventListener('click', function () {
        openMaterialEditor(findMaterial(button.getAttribute('data-edit')));
      });
    });
    Array.prototype.forEach.call(container.querySelectorAll('[data-delete]'), function (button) {
      button.addEventListener('click', function () {
        removeMaterial(findMaterial(button.getAttribute('data-delete')));
      });
    });
  }

  // Открывает материал целиком в модальном окне.
  function openMaterialReader(material) {
    if (!material) {
      return;
    }
    App.openModal({
      title: material.title,
      submitLabel: 'Закрыть',
      body: '<div class="theory-content">' + renderTheoryText(material.content) + '</div>',
      onSubmit: function () {
        return Promise.resolve();
      }
    });
  }

  // Создаёт новый материал или редактирует существующий.
  function openMaterialEditor(material) {
    App.openModal({
      title: material ? 'Редактирование материала' : 'Новый материал',
      submitLabel: material ? 'Сохранить' : 'Добавить',
      body:
        '<div class="form-field">' +
        '<label for="material-title">Заголовок</label>' +
        '<input id="material-title" name="title" type="text" minlength="3" maxlength="150" required value="' +
        App.escapeHtml(material ? material.title : '') + '">' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="material-content">Текст материала</label>' +
        '<textarea id="material-content" name="content" rows="12" minlength="10" maxlength="20000" required>' +
        App.escapeHtml(material ? material.content : '') + '</textarea>' +
        '<p class="form-hint">Пустая строка разделяет абзацы. HTML-теги выводятся как текст.</p>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="material-order">Порядок</label>' +
        '<input id="material-order" name="sortOrder" type="number" min="0" max="1000" value="' +
        (material ? material.sortOrder : state.materials.length + 1) + '">' +
        '</div>',
      onSubmit: async function (form) {
        var values = App.formValues(form);
        var payload = {
          title: values.title.trim(),
          content: values.content.trim(),
          sortOrder: parseInt(values.sortOrder, 10) || 0
        };

        if (material) {
          await App.api.materials.update(state.course.id, material.id, payload);
          App.notify('Материал обновлён', 'success');
        } else {
          await App.api.materials.create(state.course.id, payload);
          App.notify('Материал добавлен', 'success');
        }

        await loadMaterials();
        renderTheory();
      }
    });
  }

  // Удаляет материал после подтверждения.
  async function removeMaterial(material) {
    if (!material) {
      return;
    }
    if (!window.confirm('Удалить материал «' + material.title + '»?')) {
      return;
    }
    try {
      await App.api.materials.remove(state.course.id, material.id);
      App.notify('Материал удалён', 'success');
      await loadMaterials();
      renderTheory();
    } catch (error) {
      App.notify(error.message, 'error');
    }
  }

  // Загружает теорию курса.
  async function loadMaterials() {
    state.materials = await App.api.materials.list(state.course.id);
  }

  // Рисует задания курса.
  function renderAssignments() {
    var container = document.getElementById('course-modules');
    if (!container) {
      return;
    }

    if (!state.assignments.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">В курсе пока нет заданий.</p></div>';
      return;
    }

    container.innerHTML = state.assignments.map(function (assignment, index) {
      var questions = state.questionsCount[assignment.id] || 0;
      var action;
      if (state.role === 'teacher') {
        action = '<a class="btn btn-sm btn-outline" href="' +
          App.url('test-editor.html') + '?id=' + assignment.id + '">Редактировать</a>';
      } else if (state.enrolled) {
        var attempt = latestAttempt(assignment.id);
        if (attempt && (attempt.status === 'completed' || attempt.status === 'expired')) {
          action = '<span class="muted small">' +
            (attempt.status === 'completed' ? 'Тест пройден' : 'Время попытки истекло') +
            '</span>';
        } else if (attempt) {
          action = '<a class="btn btn-sm btn-soft" href="' +
            App.url('test-taking.html') + '?id=' + assignment.id + '">Продолжить тест</a>';
        } else {
          action = '<a class="btn btn-sm btn-soft" href="' +
            App.url('test-taking.html') + '?id=' + assignment.id + '">Начать тест</a>';
        }
      } else {
        action = '<span class="muted small">Запишитесь на курс, чтобы пройти тест</span>';
      }

      return '<article class="module-card">' +
        '<div class="module-head">' +
        '<span class="module-number">' + (index + 1) + '</span>' +
        '<div><h3>' + App.escapeHtml(assignment.title) + '</h3>' +
        '<p class="muted small">' + App.escapeHtml(assignment.description || 'Описание не заполнено.') + '</p></div>' +
        '</div>' +
        '<div class="course-meta">' +
        '<span>Вопросов: ' + questions + '</span>' +
        '<span>Максимум баллов: ' + assignment.maxScore + '</span>' +
        '<span>Время: ' + assignment.timeLimitMinutes + ' мин</span>' +
        '</div>' +
        '<div class="form-actions">' + action + '</div>' +
        '</article>';
    }).join('');
  }

  // Рисует список тестов курса со ссылками по роли.
  function renderTests() {
    App.renderCards('course-tests', state.assignments.map(function (assignment) {
      var page = state.role === 'teacher' ? 'test-editor.html' : 'test-taking.html';
      var subtitle = assignment.timeLimitMinutes + ' минут · ' +
        (state.questionsCount[assignment.id] || 0) + ' вопросов';
      var href = '';
      var hrefLabel = 'Начать тест';

      if (state.role === 'teacher') {
        href = App.url(page) + '?id=' + assignment.id;
        hrefLabel = 'Редактировать тест';
      } else if (state.enrolled) {
        var attempt = latestAttempt(assignment.id);
        if (attempt && (attempt.status === 'completed' || attempt.status === 'expired')) {
          subtitle += attempt.status === 'completed'
            ? ' · Тест пройден'
            : ' · Время попытки истекло';
        } else {
          href = App.url(page) + '?id=' + assignment.id;
          hrefLabel = attempt ? 'Продолжить тест' : 'Начать тест';
        }
      }

      return {
        title: assignment.title,
        subtitle: subtitle,
        text: assignment.description || 'Описание не заполнено.',
        badge: 'published',
        href: href,
        hrefLabel: hrefLabel
      };
    }));
  }

  // Показывает карточку преподавателя курса.
  function renderTeacher() {
    var container = document.getElementById('course-teacher');
    var teacher = App.getUserById(state.course.teacherId);
    if (!container) {
      return;
    }

    if (!teacher) {
      container.innerHTML = '<p class="muted">Преподаватель #' + state.course.teacherId + '</p>';
      return;
    }

    container.innerHTML =
      '<h3 class="card-title">' + App.escapeHtml(teacher.fullName) + '</h3>' +
      '<p class="card-subtitle">' + App.escapeHtml(App.statusLabel(teacher.role)) + '</p>' +
      '<p class="muted small">' + App.escapeHtml(teacher.email) + '</p>';
  }

  async function load() {
    var id = courseId();
    if (!id) {
      App.showError('course-hero', new Error('Не указан идентификатор курса'));
      return;
    }

    state.role = App.getRole();
    state.user = App.getUser();

    state.course = await App.api.courses.get(id);
    state.assignments = await App.api.assignments.list(id);

    try {
      await loadMaterials();
    } catch (error) {
      state.materials = [];
    }

    try {
      var users = await App.api.users.lookup();
      App.cacheUsers(users);
    } catch (error) {
      App.cacheUsers([]);
    }

    if (state.role === 'student') {
      try {
        var enrollment = await App.api.courses.enrollment(id);
        state.enrolled = !!enrollment.enrolled;
      } catch (error) {
        state.enrolled = false;
      }

      try {
        var attempts = await App.api.attempts.list();
        state.attempts = {};
        attempts.forEach(function (attempt) {
          var current = state.attempts[attempt.assignmentId];
          if (!current || new Date(attempt.startedAt || 0) > new Date(current.startedAt || 0)) {
            state.attempts[attempt.assignmentId] = attempt;
          }
        });
      } catch (error) {
        state.attempts = {};
      }
    }

    state.questionsCount = {};
    await Promise.all(state.assignments.map(async function (assignment) {
      try {
        var questions = await App.api.questions.list(assignment.id);
        state.questionsCount[assignment.id] = questions.length;
      } catch (error) {
        state.questionsCount[assignment.id] = 0;
      }
    }));

    document.title = state.course.title + ' — OnlineTesting';
    document.getElementById('course-page-title').textContent = state.course.title;

    renderHero();
    renderTheory();
    renderAssignments();
    renderTests();
    renderTeacher();
  }

  async function init() {
    if (!App.requireRole(['student', 'teacher'])) {
      return;
    }

    try {
      await load();
    } catch (error) {
      App.showError('course-hero', error);
    }
  }

  init();
})();
