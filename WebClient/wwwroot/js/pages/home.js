/*
 * home.js — дашборд. Содержимое зависит от роли пользователя.
 * Данные берутся из window.Mock, отрисовка — через хелперы App.
 */
(function () {
  'use strict';

  var App = window.App;

  // Возвращает среднюю оценку по проверенным попыткам.
  function averageGrade(attempts) {
    var graded = attempts.filter(function (attempt) {
      return attempt.grade !== null && attempt.grade !== undefined;
    });
    if (!graded.length) {
      return '—';
    }
    var sum = graded.reduce(function (total, attempt) {
      return total + attempt.grade;
    }, 0);
    return (sum / graded.length).toFixed(1);
  }

  // Формирует заголовок и подпись дашборда для текущего пользователя.
  function renderHeading(user, role) {
    var subtitle = document.getElementById('dashboard-subtitle');
    var actions = document.getElementById('dashboard-actions');
    if (!subtitle || !actions) {
      return;
    }

    subtitle.textContent = 'Здравствуйте, ' + user.fullName + '. ' +
      'Это учебный макет системы онлайн-тестирования.';

    if (role === 'student') {
      actions.innerHTML = '<a class="btn btn-primary" href="' + App.url('courses.html') + '">Перейти к курсам</a>';
    } else if (role === 'teacher') {
      actions.innerHTML = '<a class="btn btn-primary" href="' + App.url('test-editor.html') + '">Создать тест</a>';
    } else {
      actions.innerHTML = '<a class="btn btn-primary" href="' + App.url('admin-users.html') + '">Управление пользователями</a>';
    }
  }

  // Рисует дашборд студента.
  function renderStudentDashboard(user) {
    var myAttempts = window.Mock.attempts.filter(function (attempt) {
      return attempt.studentId === user.id;
    });
    var mySubmissions = window.Mock.submissions.filter(function (submission) {
      return submission.studentId === user.id;
    });
    var publishedTests = window.Mock.tests.filter(function (test) {
      return test.status === 'published';
    });

    App.renderCards('dashboard-stats', [
      { title: 'Доступные тесты', value: publishedTests.length, subtitle: 'Опубликованные тесты курсов' },
      { title: 'Мои попытки', value: myAttempts.length, subtitle: 'Всего прохождений' },
      { title: 'Средняя оценка', value: averageGrade(myAttempts), subtitle: 'По проверенным работам' },
      { title: 'Посылки кода', value: mySubmissions.length, subtitle: 'Отправленные решения' }
    ]);

    var mainTitle = document.getElementById('dashboard-main-title');
    var mainNote = document.getElementById('dashboard-main-note');
    mainTitle.textContent = 'Доступные тесты';
    mainNote.textContent = 'Нажмите «Начать», чтобы открыть макет прохождения теста';

    App.renderTable('dashboard-main-table', [
      {
        label: 'Тест',
        render: function (test) {
          return '<span class="table-title">' + App.escapeHtml(test.title) + '</span>';
        }
      },
      {
        label: 'Курс',
        render: function (test) {
          var course = App.getCourseById(test.courseId);
          return App.escapeHtml(course ? course.title : '—');
        }
      },
      {
        label: 'Длительность',
        render: function (test) {
          return App.escapeHtml(test.durationMin) + ' мин';
        }
      },
      {
        label: 'Вопросов',
        key: 'questionsCount'
      },
      {
        label: 'Действие',
        render: function (test) {
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('test-taking.html') + '?id=' + test.id + '">Начать</a>';
        }
      }
    ], publishedTests);

    var secondaryTitle = document.getElementById('dashboard-secondary-title');
    secondaryTitle.textContent = 'Последние результаты';

    var attempts = myAttempts.slice().sort(function (first, second) {
      return new Date(second.startedAt) - new Date(first.startedAt);
    });

    App.renderCards('dashboard-secondary', attempts.map(function (attempt) {
      var test = App.getTestById(attempt.testId);
      return {
        title: test ? test.title : 'Тест',
        subtitle: App.formatDate(attempt.finishedAt || attempt.startedAt),
        meta: ['Баллы: ' + attempt.score + ' из ' + attempt.maxScore],
        badge: attempt.grade ? 'grade-' + attempt.grade : attempt.status,
        href: App.url('my-results.html'),
        hrefLabel: 'Все результаты'
      };
    }));
  }

  // Рисует дашборд преподавателя.
  function renderTeacherDashboard(user) {
    var myCourses = window.Mock.courses.filter(function (course) {
      return course.teacherId === user.id;
    });
    var myCoursesIds = myCourses.map(function (course) {
      return course.id;
    });
    var myTests = window.Mock.tests.filter(function (test) {
      return myCoursesIds.indexOf(test.courseId) !== -1;
    });
    var pendingAttempts = window.Mock.attempts.filter(function (attempt) {
      return attempt.status === 'pending';
    });
    var studentsCount = window.Mock.users.filter(function (item) {
      return item.role === 'student';
    }).length;

    App.renderCards('dashboard-stats', [
      { title: 'Мои курсы', value: myCourses.length, subtitle: 'Курсы, где вы преподаватель' },
      { title: 'Мои тесты', value: myTests.length, subtitle: 'Включая черновики' },
      { title: 'На проверке', value: pendingAttempts.length, subtitle: 'Работы ожидают оценки' },
      { title: 'Студентов', value: studentsCount, subtitle: 'В системе' }
    ]);

    document.getElementById('dashboard-main-title').textContent = 'Работы на проверке';
    document.getElementById('dashboard-main-note').textContent = 'Развёрнутые ответы ожидают решения преподавателя';

    App.renderTable('dashboard-main-table', [
      {
        label: 'Студент',
        render: function (attempt) {
          var student = App.getUserById(attempt.studentId);
          return App.escapeHtml(student ? student.fullName : '—');
        }
      },
      {
        label: 'Тест',
        render: function (attempt) {
          var test = App.getTestById(attempt.testId);
          return App.escapeHtml(test ? test.title : '—');
        }
      },
      {
        label: 'Отправлено',
        render: function (attempt) {
          return App.formatDate(attempt.finishedAt);
        }
      },
      {
        label: 'Статус',
        render: function (attempt) {
          return App.renderBadge(attempt.status);
        }
      },
      {
        label: 'Действие',
        render: function () {
          return '<a class="btn btn-sm btn-soft" href="' + App.url('grading.html') + '">Оценить</a>';
        }
      }
    ], pendingAttempts);

    document.getElementById('dashboard-secondary-title').textContent = 'Мои курсы';

    App.renderCards('dashboard-secondary', myCourses.map(function (course) {
      return {
        title: course.title,
        subtitle: App.statusLabel(course.status),
        meta: [
          'Модулей: ' + course.modulesCount,
          'Тестов: ' + course.testsCount,
          'Студентов: ' + course.studentsCount
        ],
        href: App.url('course-details.html') + '?id=' + course.id,
        hrefLabel: 'Открыть курс'
      };
    }));
  }

  // Рисует дашборд администратора.
  function renderAdminDashboard() {
    var users = window.Mock.users;
    var students = users.filter(function (user) {
      return user.role === 'student';
    });
    var teachers = users.filter(function (user) {
      return user.role === 'teacher';
    });
    var admins = users.filter(function (user) {
      return user.role === 'admin';
    });

    App.renderCards('dashboard-stats', [
      { title: 'Пользователей', value: users.length, subtitle: 'Все учётные записи' },
      { title: 'Студентов', value: students.length, subtitle: 'Активные студенты' },
      { title: 'Преподавателей', value: teachers.length, subtitle: 'Ведут курсы' },
      { title: 'Администраторов', value: admins.length, subtitle: 'Управление системой' }
    ]);

    document.getElementById('dashboard-main-title').textContent = 'Пользователи системы';
    document.getElementById('dashboard-main-note').textContent = 'Полный список доступен в разделе «Пользователи»';

    App.renderTable('dashboard-main-table', [
      { label: 'ФИО', key: 'fullName' },
      { label: 'Почта', key: 'email' },
      {
        label: 'Роль',
        render: function (user) {
          return App.renderBadge(user.role);
        }
      },
      { label: 'Группа / кафедра', key: 'group' },
      {
        label: 'Действие',
        render: function () {
          return '<a class="btn btn-sm btn-soft" href="' + App.url('admin-users.html') + '">Открыть</a>';
        }
      }
    ], users);

    document.getElementById('dashboard-secondary-title').textContent = 'Аналитика по курсам';

    App.renderCards('dashboard-secondary', window.Mock.analytics.courses.slice(0, 4).map(function (stat) {
      var course = App.getCourseById(stat.courseId);
      return {
        title: course ? course.title : 'Курс',
        subtitle: 'Средний балл: ' + stat.averageScore + ' из 5',
        meta: [
          'Сдача: ' + stat.passRate + '%',
          'Попыток: ' + stat.attempts
        ],
        href: App.url('analytics.html'),
        hrefLabel: 'Смотреть аналитику'
      };
    }));
  }

  // Точка входа дашборда.
  function init() {
    if (!App.requireRole(['student', 'teacher', 'admin'])) {
      return;
    }

    var role = App.getRole();
    var user = App.getUser();
    if (!user) {
      location.href = App.url('login.html');
      return;
    }

    renderHeading(user, role);

    if (role === 'student') {
      renderStudentDashboard(user);
    } else if (role === 'teacher') {
      renderTeacherDashboard(user);
    } else {
      renderAdminDashboard();
    }
  }

  init();
})();
