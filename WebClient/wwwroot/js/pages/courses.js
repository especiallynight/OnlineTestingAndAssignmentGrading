/*
 * courses.js — каталог курсов.
 * Данные берутся из Mock.courses, фильтры меняют только отображение.
 */
(function () {
  'use strict';

  var App = window.App;

  // Заполняет фильтр преподавателей.
  function fillTeacherFilter() {
    var select = document.getElementById('course-teacher-filter');
    if (!select) {
      return;
    }

    var teachers = window.Mock.users.filter(function (user) {
      return user.role === 'teacher';
    });

    select.innerHTML = '<option value="all">Все преподаватели</option>' +
      teachers.map(function (teacher) {
        return '<option value="' + teacher.id + '">' +
          App.escapeHtml(teacher.fullName) + '</option>';
      }).join('');
  }

  // Возвращает курсы с учётом выбранных фильтров.
  function getFilteredCourses() {
    var statusSelect = document.getElementById('course-status-filter');
    var teacherSelect = document.getElementById('course-teacher-filter');
    var status = statusSelect ? statusSelect.value : 'all';
    var teacherId = teacherSelect ? teacherSelect.value : 'all';

    return window.Mock.courses.filter(function (course) {
      var statusMatches = status === 'all' || course.status === status;
      var teacherMatches = teacherId === 'all' || course.teacherId === teacherId;
      return statusMatches && teacherMatches;
    });
  }

  // Отображает карточки курсов и их количество.
  function renderCourses() {
    var courses = getFilteredCourses();
    var count = document.getElementById('courses-count');
    if (count) {
      count.textContent = 'Найдено курсов: ' + courses.length;
    }

    App.renderCards('courses-grid', courses.map(function (course) {
      var teacher = App.getUserById(course.teacherId);
      return {
        title: course.title,
        subtitle: teacher ? teacher.fullName : 'Преподаватель не назначен',
        text: course.description,
        badge: course.status,
        meta: [
          'Модулей: ' + course.modulesCount,
          'Тестов: ' + course.testsCount,
          'Студентов: ' + course.studentsCount,
          'Недель: ' + course.durationWeeks
        ],
        href: App.url('course-details.html') + '?id=' + course.id,
        hrefLabel: 'Открыть курс'
      };
    }));
  }

  // Добавляет кнопку создания курса для преподавателя.
  function renderActions(role) {
    var actions = document.getElementById('courses-actions');
    if (!actions || role !== 'teacher') {
      return;
    }
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="create-course-btn">Создать курс</button>';
    document.getElementById('create-course-btn').addEventListener('click', function () {
      App.stub('Создать курс');
    });
  }

  // Сбрасывает фильтры к исходным значениям.
  function resetFilters() {
    document.getElementById('course-status-filter').value = 'all';
    document.getElementById('course-teacher-filter').value = 'all';
    renderCourses();
  }

  // Инициализирует страницу каталога.
  function init() {
    if (!App.requireRole(['student', 'teacher'])) {
      return;
    }

    fillTeacherFilter();
    renderActions(App.getRole());
    renderCourses();

    document.getElementById('course-status-filter').addEventListener('change', renderCourses);
    document.getElementById('course-teacher-filter').addEventListener('change', renderCourses);
    document.getElementById('courses-reset').addEventListener('click', resetFilters);
  }

  init();
})();
