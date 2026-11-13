/*
 * courses.js — каталог курсов из CourseService.
 * Фильтры работают по данным, полученным через ApiGateway.
 */
(function () {
  'use strict';

  var App = window.App;
  var allCourses = [];
  var allUsers = [];

  // Заполняет фильтр преподавателей.
  function fillTeacherFilter() {
    var select = document.getElementById('course-teacher-filter');
    if (!select) {
      return;
    }

    var teachers = allUsers.filter(function (user) {
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
    var search = document.getElementById('course-search');
    var teacherSelect = document.getElementById('course-teacher-filter');
    var query = search ? search.value.trim().toLowerCase() : '';
    var teacherId = teacherSelect ? teacherSelect.value : 'all';

    return allCourses.filter(function (course) {
      var queryMatches = !query ||
        course.title.toLowerCase().indexOf(query) !== -1 ||
        course.description.toLowerCase().indexOf(query) !== -1;
      var teacherMatches = teacherId === 'all' || String(course.teacherId) === teacherId;
      return queryMatches && teacherMatches;
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
        subtitle: teacher ? teacher.fullName : 'Преподаватель #' + course.teacherId,
        text: course.description || 'Описание не заполнено.',
        badge: 'active',
        meta: [
          'Тестов: ' + course.assignmentsCount,
          'Студентов: ' + course.studentsCount,
          'Создан: ' + App.formatDateOnly(course.createdAt)
        ],
        href: App.url('course-details.html') + '?id=' + course.id,
        hrefLabel: 'Открыть курс'
      };
    }));
  }

  // Открывает форму создания курса.
  function openCreateCourse() {
    App.openModal({
      title: 'Новый курс',
      submitLabel: 'Создать курс',
      body:
        '<div class="form-field">' +
        '<label for="new-course-name">Название</label>' +
        '<input id="new-course-name" name="name" type="text" minlength="3" maxlength="100" required>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="new-course-description">Описание</label>' +
        '<textarea id="new-course-description" name="description" maxlength="150" rows="4"></textarea>' +
        '</div>',
      onSubmit: async function (form) {
        var values = App.formValues(form);
        await App.api.courses.create({
          name: values.name.trim(),
          description: values.description.trim()
        });
        App.notify('Курс создан', 'success');
        await loadCourses();
      }
    });
  }

  // Добавляет кнопку создания курса для преподавателя.
  function renderActions(role) {
    var actions = document.getElementById('courses-actions');
    if (!actions || role !== 'teacher') {
      return;
    }
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="create-course-btn">Создать курс</button>';
    document.getElementById('create-course-btn').addEventListener('click', openCreateCourse);
  }

  // Сбрасывает фильтры к исходным значениям.
  function resetFilters() {
    document.getElementById('course-search').value = '';
    document.getElementById('course-teacher-filter').value = 'all';
    renderCourses();
  }

  async function loadCourses() {
    try {
      allCourses = await App.api.courses.list();
      renderCourses();
    } catch (error) {
      App.showError('courses-grid', error);
    }
  }

  // Инициализирует страницу каталога.
  async function init() {
    if (!App.requireRole(['student', 'teacher'])) {
      return;
    }

    try {
      var users = await App.api.users.lookup();
      allUsers = users;
    } catch (error) {
      allUsers = [];
    }

    fillTeacherFilter();
    renderActions(App.getRole());
    await loadCourses();

    var search = document.getElementById('course-search');
    if (search) {
      search.addEventListener('input', App.debounce(renderCourses, 200));
    }
    document.getElementById('course-teacher-filter').addEventListener('change', renderCourses);
    document.getElementById('courses-reset').addEventListener('click', resetFilters);
  }

  init();
})();
