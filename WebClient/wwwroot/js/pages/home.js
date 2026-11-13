/*
 * home.js — дашборд. Содержимое зависит от роли пользователя.
 * Данные загружаются из микросервисов через ApiGateway.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    role: null,
    user: null,
    users: [],
    courses: [],
    assignments: [],
    attempts: [],
    grades: {}
  };

  function percent(score, maxScore) {
    var max = Number(maxScore);
    if (!max) {
      return null;
    }
    return Math.round((Number(score) / max) * 100);
  }

  function averagePercent(gradedAttempts) {
    var values = gradedAttempts.map(function (attempt) {
      var grade = state.grades[attempt.id];
      return grade ? percent(grade.score, grade.maxScore) : null;
    }).filter(function (value) {
      return value !== null;
    });
    if (!values.length) {
      return '—';
    }
    return Math.round(values.reduce(function (sum, value) {
      return sum + value;
    }, 0) / values.length) + '%';
  }

  function myAssignments() {
    var courseIds = {};
    state.courses.forEach(function (course) {
      courseIds[String(course.id)] = true;
    });
    return state.assignments.filter(function (assignment) {
      return courseIds[String(assignment.courseId)];
    });
  }

  function relevantAttempts() {
    if (state.role !== 'teacher') {
      return state.attempts;
    }
    var assignmentIds = {};
    myAssignments().forEach(function (assignment) {
      assignmentIds[String(assignment.id)] = true;
    });
    return state.attempts.filter(function (attempt) {
      return assignmentIds[String(attempt.assignmentId)];
    });
  }

  async function loadGrades(attempts) {
    state.grades = {};
    await Promise.all(attempts.map(async function (attempt) {
      try {
        state.grades[attempt.id] = await App.api.grades.byAttempt(attempt.id);
      } catch (error) {
        state.grades[attempt.id] = null;
      }
    }));
  }

  function renderHeading() {
    var subtitle = document.getElementById('dashboard-subtitle');
    var actions = document.getElementById('dashboard-actions');
    if (!subtitle || !actions) {
      return;
    }

    subtitle.textContent = 'Здравствуйте, ' + state.user.fullName +
      '. Сводка по вашей роли в системе онлайн-тестирования.';

    if (state.role === 'student') {
      actions.innerHTML = '<a class="btn btn-primary" href="' +
        App.url('courses.html') + '">Перейти к курсам</a>';
    } else if (state.role === 'teacher') {
      actions.innerHTML = '<a class="btn btn-primary" href="' +
        App.url('test-editor.html') + '">Создать тест</a>';
    } else {
      actions.innerHTML = '<a class="btn btn-primary" href="' +
        App.url('admin-users.html') + '">Управление пользователями</a>';
    }
  }

  function renderStudent() {
    var assignments = myAssignments();
    var graded = state.attempts.filter(function (attempt) {
      return !!state.grades[attempt.id];
    });

    App.renderCards('dashboard-stats', [
      { title: 'Мои курсы', value: state.courses.length, subtitle: 'Активные записи' },
      { title: 'Доступные тесты', value: assignments.length, subtitle: 'В ваших курсах' },
      { title: 'Мои попытки', value: state.attempts.length, subtitle: 'Все прохождения' },
      { title: 'Средний результат', value: averagePercent(graded), subtitle: 'По проверенным работам' }
    ]);

    document.getElementById('dashboard-main-title').textContent = 'Мои курсы';
    document.getElementById('dashboard-main-note').textContent =
      'Откройте курс, чтобы пройти тесты';

    App.renderTable('dashboard-main-table', [
      {
        label: 'Курс',
        render: function (course) {
          return '<span class="table-title">' + App.escapeHtml(course.title) + '</span>';
        }
      },
      {
        label: 'Преподаватель',
        render: function (course) {
          var teacher = App.getUserById(course.teacherId);
          return App.escapeHtml(teacher ? teacher.fullName : '#' + course.teacherId);
        }
      },
      {
        label: 'Тестов',
        render: function (course) {
          var count = assignments.filter(function (assignment) {
            return String(assignment.courseId) === String(course.id);
          }).length;
          return String(count);
        }
      },
      {
        label: 'Студентов',
        key: 'studentsCount'
      },
      {
        label: 'Действие',
        render: function (course) {
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('course-details.html') + '?id=' + course.id + '">Открыть</a>';
        }
      }
    ], state.courses);

    document.getElementById('dashboard-secondary-title').textContent = 'Последние результаты';

    var recent = state.attempts.slice().sort(function (first, second) {
      return new Date(second.startedAt || 0) - new Date(first.startedAt || 0);
    }).slice(0, 6);

    App.renderCards('dashboard-secondary', recent.map(function (attempt) {
      var assignment = App.getAssignmentById(attempt.assignmentId);
      var grade = state.grades[attempt.id];
      return {
        title: assignment ? assignment.title : 'Задание #' + attempt.assignmentId,
        subtitle: App.formatDate(attempt.startedAt),
        meta: [
          grade ? 'Баллы: ' + grade.score + ' из ' + grade.maxScore
            : 'Оценка ещё не выставлена',
          'Статус: ' + App.statusLabel(attempt.status)
        ],
        badge: grade ? 'checked' : attempt.status,
        href: App.url('my-results.html'),
        hrefLabel: 'Все результаты'
      };
    }));
  }

  function renderTeacher() {
    var assignments = myAssignments();
    var attempts = relevantAttempts().slice().sort(function (first, second) {
      return new Date(second.startedAt || 0) - new Date(first.startedAt || 0);
    });
    var pending = attempts.filter(function (attempt) {
      return !state.grades[attempt.id];
    });
    var students = state.users.filter(function (user) {
      return user.role === 'student';
    });

    App.renderCards('dashboard-stats', [
      { title: 'Мои курсы', value: state.courses.length, subtitle: 'Курсы, где вы преподаватель' },
      { title: 'Мои тесты', value: assignments.length, subtitle: 'Задания в ваших курсах' },
      { title: 'Попытки студентов', value: attempts.length, subtitle: 'По вашим тестам' },
      { title: 'Без оценки', value: pending.length, subtitle: 'Ожидают проверки' }
    ]);

    document.getElementById('dashboard-main-title').textContent = 'Последние работы';
    document.getElementById('dashboard-main-note').textContent =
      'Студентов в системе: ' + students.length;

    App.renderTable('dashboard-main-table', [
      {
        label: 'Студент',
        render: function (attempt) {
          var student = App.getUserById(attempt.studentId);
          return App.escapeHtml(student ? student.fullName : '#' + attempt.studentId);
        }
      },
      {
        label: 'Тест',
        render: function (attempt) {
          var assignment = App.getAssignmentById(attempt.assignmentId);
          return App.escapeHtml(assignment ? assignment.title : '#' + attempt.assignmentId);
        }
      },
      {
        label: 'Начата',
        render: function (attempt) {
          return App.formatDate(attempt.startedAt);
        }
      },
      {
        label: 'Результат',
        render: function (attempt) {
          var grade = state.grades[attempt.id];
          if (!grade) {
            return App.renderBadge('pending');
          }
          return App.escapeHtml(grade.score + ' / ' + grade.maxScore);
        }
      },
      {
        label: 'Действие',
        render: function () {
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('grading.html') + '">Проверить</a>';
        }
      }
    ], attempts.slice(0, 8));

    document.getElementById('dashboard-secondary-title').textContent = 'Мои курсы';

    App.renderCards('dashboard-secondary', state.courses.map(function (course) {
      var count = assignments.filter(function (assignment) {
        return String(assignment.courseId) === String(course.id);
      }).length;
      return {
        title: course.title,
        subtitle: 'Тестов: ' + count,
        meta: [
          'Студентов: ' + course.studentsCount,
          'Создан: ' + App.formatDateOnly(course.createdAt)
        ],
        href: App.url('course-details.html') + '?id=' + course.id,
        hrefLabel: 'Открыть курс'
      };
    }));
  }

  function renderAdmin() {
    var students = state.users.filter(function (user) {
      return user.role === 'student';
    });
    var teachers = state.users.filter(function (user) {
      return user.role === 'teacher';
    });

    App.renderCards('dashboard-stats', [
      { title: 'Пользователей', value: state.users.length, subtitle: 'Все учётные записи' },
      { title: 'Студентов', value: students.length, subtitle: 'Роль «Студент»' },
      { title: 'Преподавателей', value: teachers.length, subtitle: 'Роль «Преподаватель»' },
      { title: 'Курсов', value: state.courses.length, subtitle: 'В каталоге' }
    ]);

    document.getElementById('dashboard-main-title').textContent = 'Пользователи системы';
    document.getElementById('dashboard-main-note').textContent =
      'Полный список доступен в разделе «Пользователи»';

    App.renderTable('dashboard-main-table', [
      { label: 'ФИО', key: 'fullName' },
      { label: 'Почта', key: 'email' },
      {
        label: 'Роль',
        render: function (user) {
          return App.renderBadge(user.role);
        }
      },
      {
        label: 'Действие',
        render: function () {
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('admin-users.html') + '">Открыть</a>';
        }
      }
    ], state.users.slice(0, 10));

    document.getElementById('dashboard-secondary-title').textContent = 'Курсы платформы';

    App.renderCards('dashboard-secondary', state.courses.slice(0, 6).map(function (course) {
      var teacher = App.getUserById(course.teacherId);
      return {
        title: course.title,
        subtitle: teacher ? teacher.fullName : 'Преподаватель #' + course.teacherId,
        meta: [
          'Тестов: ' + course.assignmentsCount,
          'Студентов: ' + course.studentsCount
        ],
        href: App.url('analytics.html'),
        hrefLabel: 'Смотреть аналитику'
      };
    }));
  }

  async function load() {
    state.role = App.getRole();
    state.user = App.getUser();

    try {
      state.users = await App.api.users.lookup();
    } catch (error) {
      state.users = [];
    }

    if (state.role === 'student') {
      state.courses = await App.api.courses.enrolled();
    } else {
      var courses = await App.api.courses.list();
      state.courses = state.role === 'teacher'
        ? courses.filter(function (course) { return course.teacherId === state.user.id; })
        : courses;
    }

    var lists = await Promise.all(state.courses.map(function (course) {
      return App.api.assignments.list(course.id).catch(function () { return []; });
    }));
    state.assignments = [];
    lists.forEach(function (list) {
      state.assignments = state.assignments.concat(list);
    });
    App.cacheAssignments(state.assignments);

    state.attempts = await App.api.attempts.list();
    if (state.role === 'student') {
      state.attempts = state.attempts.filter(function (attempt) {
        return String(attempt.studentId) === String(state.user.id);
      });
    } else if (state.role === 'teacher') {
      state.attempts = relevantAttempts();
    }

    await loadGrades(state.attempts);

    renderHeading();
    if (state.role === 'student') {
      renderStudent();
    } else if (state.role === 'teacher') {
      renderTeacher();
    } else {
      renderAdmin();
    }
  }

  function init() {
    if (!App.requireRole(['student', 'teacher', 'admin'])) {
      return;
    }

    var user = App.getUser();
    if (!user) {
      location.href = App.url('login.html');
      return;
    }

    load().catch(function (error) {
      App.showError('dashboard-main-table', error);
    });
  }

  init();
})();
