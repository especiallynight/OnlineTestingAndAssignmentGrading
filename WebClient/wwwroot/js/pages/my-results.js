/*
 * my-results.js — история попыток и оценки студента.
 * Попытки берутся из ExamService, оценки — из GradingService.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    attempts: [],
    assignments: {},
    courses: {},
    grades: {}
  };

  function percent(score, maxScore) {
    var max = Number(maxScore);
    if (!max) {
      return null;
    }
    return Math.round((Number(score) / max) * 100);
  }

  function renderStats() {
    var graded = state.attempts.filter(function (attempt) {
      return !!state.grades[attempt.id];
    });
    var percents = graded.map(function (attempt) {
      var grade = state.grades[attempt.id];
      return percent(grade.score, grade.maxScore);
    }).filter(function (value) {
      return value !== null;
    });

    var average = percents.length
      ? Math.round(percents.reduce(function (sum, value) { return sum + value; }, 0) / percents.length)
      : null;
    var best = percents.length ? Math.max.apply(null, percents) : null;

    App.renderCards('results-stats', [
      { title: 'Всего попыток', value: state.attempts.length, subtitle: 'Все прохождения тестов' },
      { title: 'Проверено', value: graded.length, subtitle: 'Оценки выставлены' },
      {
        title: 'Средний результат',
        value: average === null ? '—' : average + '%',
        subtitle: 'По проверенным работам'
      },
      {
        title: 'Лучший результат',
        value: best === null ? '—' : best + '%',
        subtitle: 'Максимум среди попыток'
      }
    ]);
  }

  function renderTable() {
    App.renderTable('results-table', [
      {
        label: 'Тест',
        render: function (attempt) {
          var assignment = state.assignments[attempt.assignmentId];
          return '<span class="table-title">' +
            App.escapeHtml(assignment ? assignment.title : 'Задание #' + attempt.assignmentId) +
            '</span>';
        }
      },
      {
        label: 'Курс',
        render: function (attempt) {
          var assignment = state.assignments[attempt.assignmentId];
          var course = assignment ? state.courses[assignment.courseId] : null;
          return App.escapeHtml(course ? course.title : '—');
        }
      },
      {
        label: 'Начата',
        render: function (attempt) {
          return App.formatDate(attempt.startedAt);
        }
      },
      {
        label: 'Статус',
        render: function (attempt) {
          return App.renderBadge(attempt.status);
        }
      },
      {
        label: 'Баллы',
        render: function (attempt) {
          var grade = state.grades[attempt.id];
          if (!grade) {
            return '<span class="muted">—</span>';
          }
          return App.escapeHtml(grade.score + ' / ' + grade.maxScore);
        }
      },
      {
        label: 'Результат',
        render: function (attempt) {
          var grade = state.grades[attempt.id];
          if (!grade) {
            return App.renderBadge('pending');
          }
          var value = percent(grade.score, grade.maxScore);
          return '<strong>' + (value === null ? '—' : value + '%') + '</strong>';
        }
      },
      {
        label: 'Действие',
        render: function (attempt) {
          var assignment = state.assignments[attempt.assignmentId];
          if (!assignment) {
            return '';
          }
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('test-taking.html') + '?id=' + assignment.id + '">Пройти снова</a>';
        }
      }
    ], state.attempts);
  }

  async function loadGrades() {
    state.grades = {};
    await Promise.all(state.attempts.map(async function (attempt) {
      try {
        state.grades[attempt.id] = await App.api.grades.byAttempt(attempt.id);
      } catch (error) {
        state.grades[attempt.id] = null;
      }
    }));
  }

  async function load() {
    state.attempts = await App.api.attempts.list();
    state.attempts.sort(function (first, second) {
      return new Date(second.startedAt || 0) - new Date(first.startedAt || 0);
    });

    state.assignments = {};
    var assignmentIds = {};
    state.attempts.forEach(function (attempt) {
      assignmentIds[attempt.assignmentId] = true;
    });

    await Promise.all(Object.keys(assignmentIds).map(async function (id) {
      try {
        var assignment = await App.api.assignments.get(id);
        state.assignments[assignment.id] = assignment;
      } catch (error) {
        // Пропускаем задание, если оно было удалено.
      }
    }));

    state.courses = {};
    try {
      var courses = await App.api.courses.list();
      courses.forEach(function (course) {
        state.courses[course.id] = course;
      });
    } catch (error) {
      state.courses = {};
    }

    await loadGrades();
    renderStats();
    renderTable();
  }

  async function init() {
    if (!App.requireRole(['student'])) {
      return;
    }

    var actions = document.getElementById('results-actions');
    if (actions) {
      actions.innerHTML = '<button class="btn btn-outline" type="button" id="results-refresh">Обновить</button>';
      document.getElementById('results-refresh').addEventListener('click', function () {
        load().then(function () {
          App.notify('Результаты обновлены', 'success');
        }).catch(function (error) {
          App.notify(error.message, 'error');
        });
      });
    }

    try {
      await load();
    } catch (error) {
      App.showError('results-table', error);
    }
  }

  init();
})();
