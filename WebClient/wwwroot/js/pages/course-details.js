/*
 * course-details.js — карточка курса: модули, материалы и тесты.
 * Курс выбирается по параметру id, иначе показывается первый из мока.
 */
(function () {
  'use strict';

  var App = window.App;

  // Находит курс по query-параметру id.
  function resolveCourse() {
    var id = App.getQueryParam('id');
    return (id ? App.getCourseById(id) : null) || window.Mock.courses[0] || null;
  }

  // Рисует шапку курса с основной информацией и действием.
  function renderHero(course, role) {
    var container = document.getElementById('course-hero');
    var teacher = App.getUserById(course.teacherId);
    if (!container) {
      return;
    }

    container.innerHTML =
      '<div class="flex-between">' +
      '<div>' +
      '<span class="pill">' + App.escapeHtml(App.statusLabel(course.status)) + '</span>' +
      '<h2 class="section-title">' + App.escapeHtml(course.title) + '</h2>' +
      '<p class="muted">' + App.escapeHtml(course.description) + '</p>' +
      '</div>' +
      '<button class="btn btn-primary" type="button" id="course-action-btn"></button>' +
      '</div>' +
      '<div class="course-meta">' +
      '<span>Преподаватель: ' + App.escapeHtml(teacher ? teacher.fullName : '—') + '</span>' +
      '<span>Модулей: ' + course.modulesCount + '</span>' +
      '<span>Тестов: ' + course.testsCount + '</span>' +
      '<span>Студентов: ' + course.studentsCount + '</span>' +
      '<span>Длительность: ' + course.durationWeeks + ' недель</span>' +
      '</div>';

    var actionBtn = document.getElementById('course-action-btn');
    actionBtn.textContent = role === 'teacher' ? 'Редактировать курс' : 'Записаться на курс';
    actionBtn.addEventListener('click', function () {
      App.stub(actionBtn.textContent);
    });
  }

  // Рисует модули курса со списком материалов.
  function renderModules(course) {
    var container = document.getElementById('course-modules');
    if (!container) {
      return;
    }

    var modules = window.Mock.modules.filter(function (module) {
      return module.courseId === course.id;
    }).sort(function (first, second) {
      return first.order - second.order;
    });

    if (!modules.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">В курсе пока нет модулей.</p></div>';
      return;
    }

    container.innerHTML = modules.map(function (module) {
      var materials = module.materials.map(function (material) {
        return '<li class="material-item">' +
          '<span class="material-name">' +
          '<span class="material-type">' + App.escapeHtml(material.type) + '</span>' +
          '<span>' + App.escapeHtml(material.title) + '</span>' +
          '</span>' +
          '<span class="muted small">' + App.escapeHtml(material.duration) + '</span>' +
          '</li>';
      }).join('');

      return '<article class="module-card">' +
        '<div class="module-head">' +
        '<span class="module-number">' + module.order + '</span>' +
        '<div><h3>' + App.escapeHtml(module.title) + '</h3>' +
        '<p class="muted small">' + App.escapeHtml(module.description) + '</p></div>' +
        '</div>' +
        '<ul class="material-list">' + materials + '</ul>' +
        '</article>';
    }).join('');
  }

  // Рисует список тестов курса со ссылками по роли.
  function renderTests(course, role) {
    var tests = window.Mock.tests.filter(function (test) {
      return test.courseId === course.id;
    });

    App.renderCards('course-tests', tests.map(function (test) {
      var page = role === 'teacher' ? 'test-editor.html' : 'test-taking.html';
      return {
        title: test.title,
        subtitle: test.durationMin + ' минут · ' + test.questionsCount + ' вопросов',
        text: test.description,
        badge: test.status,
        href: test.status === 'published' || role === 'teacher'
          ? App.url(page) + '?id=' + test.id
          : '',
        hrefLabel: role === 'teacher' ? 'Редактировать тест' : 'Начать тест'
      };
    }));
  }

  // Показывает карточку преподавателя курса.
  function renderTeacher(course) {
    var container = document.getElementById('course-teacher');
    var teacher = App.getUserById(course.teacherId);
    if (!container || !teacher) {
      return;
    }

    container.innerHTML =
      '<h3 class="card-title">' + App.escapeHtml(teacher.fullName) + '</h3>' +
      '<p class="card-subtitle">' + App.escapeHtml(teacher.group) + '</p>' +
      '<p class="muted small">' + App.escapeHtml(teacher.email) + '</p>';
  }

  // Инициализирует страницу курса.
  function init() {
    if (!App.requireRole(['student', 'teacher'])) {
      return;
    }

    var course = resolveCourse();
    if (!course) {
      return;
    }

    var role = App.getRole();
    document.title = course.title + ' — OnlineTesting';
    document.getElementById('course-page-title').textContent = course.title;

    renderHero(course, role);
    renderModules(course);
    renderTests(course, role);
    renderTeacher(course);
  }

  init();
})();
