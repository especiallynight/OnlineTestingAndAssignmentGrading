/*
 * grading.js — очередь работ на ручной проверке.
 * Оценка в макете не выставляется: кнопка показывает заглушку.
 */
(function () {
  'use strict';

  var App = window.App;

  // Считает среднюю оценку по проверенным попыткам.
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

  // Рисует сводные карточки по проверке работ.
  function renderStats(allAttempts, pendingAttempts) {
    var checked = allAttempts.filter(function (attempt) {
      return attempt.status === 'checked';
    });

    App.renderCards('grading-stats', [
      { title: 'На проверке', value: pendingAttempts.length, subtitle: 'Ожидают оценки преподавателя' },
      { title: 'Проверено', value: checked.length, subtitle: 'Работы с выставленной оценкой' },
      { title: 'Всего попыток', value: allAttempts.length, subtitle: 'По всем студентам' },
      { title: 'Средняя оценка', value: averageGrade(allAttempts), subtitle: 'По проверенным работам' }
    ]);
  }

  // Рисует таблицу работ, ожидающих проверки.
  function renderTable(pendingAttempts) {
    App.renderTable('grading-table', [
      {
        label: 'Студент',
        render: function (attempt) {
          var student = App.getUserById(attempt.studentId);
          if (!student) {
            return '—';
          }
          return '<span class="table-title">' + App.escapeHtml(student.fullName) + '</span>' +
            '<span class="muted small"> · ' + App.escapeHtml(student.group) + '</span>';
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
        label: 'Курс',
        render: function (attempt) {
          var test = App.getTestById(attempt.testId);
          var course = test ? App.getCourseById(test.courseId) : null;
          return App.escapeHtml(course ? course.title : '—');
        }
      },
      {
        label: 'Отправлено',
        render: function (attempt) {
          return App.formatDate(attempt.finishedAt);
        }
      },
      {
        label: 'Баллы',
        render: function (attempt) {
          return attempt.score + ' / ' + attempt.maxScore;
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
        render: function (attempt) {
          return '<button class="btn btn-sm btn-soft" type="button" data-grade="' +
            attempt.id + '">Оценить</button>';
        }
      }
    ], pendingAttempts);
  }

  // Навешивает заглушку на кнопки оценки.
  function bindActions() {
    var container = document.getElementById('grading-table');
    if (!container) {
      return;
    }
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-grade]');
      if (button) {
        App.stub('Оценить работу');
      }
    });

    var actions = document.getElementById('grading-actions');
    if (actions) {
      actions.innerHTML = '<button class="btn btn-primary" type="button" id="check-all-btn">Проверить всё</button>';
      document.getElementById('check-all-btn').addEventListener('click', function () {
        App.stub('Проверить всё');
      });
    }
  }

  // Инициализирует страницу проверки работ.
  function init() {
    if (!App.requireRole(['teacher'])) {
      return;
    }

    var allAttempts = window.Mock.attempts;
    var pendingAttempts = allAttempts.filter(function (attempt) {
      return attempt.status === 'pending';
    });

    renderStats(allAttempts, pendingAttempts);
    renderTable(pendingAttempts);
    bindActions();
  }

  init();
})();
