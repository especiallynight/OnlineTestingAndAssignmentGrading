/*
 * analytics.js — аналитика по курсам.
 * Данные берутся из AnalyticsService; при отсутствии статистики показывается пустое состояние.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    role: null,
    user: null,
    courses: [],
    stats: {}
  };

  function courseStats() {
    return state.courses.map(function (course) {
      return {
        course: course,
        stats: state.stats[course.id] || null
      };
    });
  }

  function renderStats() {
    var rows = courseStats();
    var withData = rows.filter(function (row) { return !!row.stats; });
    var attempts = withData.reduce(function (sum, row) {
      return sum + Number(row.stats.completedAttempts || 0);
    }, 0);
    var average = withData.length
      ? Math.round(withData.reduce(function (sum, row) {
          return sum + Number(row.stats.averageScore || 0);
        }, 0) / withData.length)
      : null;

    App.renderCards('analytics-stats', [
      { title: 'Курсов', value: state.courses.length, subtitle: 'Доступные курсы' },
      { title: 'Курсов со статистикой', value: withData.length, subtitle: 'Есть проверенные работы' },
      { title: 'Проверенных попыток', value: attempts, subtitle: 'По всем курсам' },
      {
        title: 'Средний результат',
        value: average === null ? '—' : average + '%',
        subtitle: 'По курсам со статистикой'
      }
    ]);
  }

  // Строит горизонтальную диаграмму из строк с процентной шириной.
  function renderBarChart(containerId, rows) {
    var container = document.getElementById(containerId);
    if (!container) {
      return;
    }

    if (!rows.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">' +
        'Данных пока нет. Статистика появится после проверки работ.</p></div>';
      return;
    }

    container.innerHTML = rows.map(function (row) {
      return '<div class="bar-row">' +
        '<span class="bar-label">' + App.escapeHtml(row.label) + '</span>' +
        '<span class="bar-track">' +
        '<span class="bar-fill ' + App.barWidth(row.value) + '"></span>' +
        '</span>' +
        '<span class="bar-value">' + App.escapeHtml(row.display) + '</span>' +
        '</div>';
    }).join('');
  }

  function renderCharts() {
    var rows = courseStats();

    renderBarChart('score-chart', rows.map(function (row) {
      var value = row.stats ? Math.round(Number(row.stats.averageScore || 0)) : 0;
      return {
        label: row.course.title,
        value: value,
        display: row.stats ? value + '%' : '—'
      };
    }));

    var maxAttempts = Math.max.apply(null, rows.map(function (row) {
      return row.stats ? Number(row.stats.completedAttempts || 0) : 0;
    }).concat([1]));

    renderBarChart('pass-chart', rows.map(function (row) {
      var value = row.stats ? Number(row.stats.completedAttempts || 0) : 0;
      return {
        label: row.course.title,
        value: Math.round((value / maxAttempts) * 100),
        display: String(value)
      };
    }));
  }

  function renderTable() {
    App.renderTable('analytics-table', [
      {
        label: 'Курс',
        render: function (row) {
          return '<span class="table-title">' + App.escapeHtml(row.course.title) + '</span>';
        }
      },
      {
        label: 'Преподаватель',
        render: function (row) {
          var teacher = App.getUserById(row.course.teacherId);
          return App.escapeHtml(teacher ? teacher.fullName : '#' + row.course.teacherId);
        }
      },
      {
        label: 'Проверенных попыток',
        render: function (row) {
          return row.stats ? String(row.stats.completedAttempts) : '—';
        }
      },
      {
        label: 'Средний результат',
        render: function (row) {
          return row.stats
            ? '<strong>' + Math.round(Number(row.stats.averageScore || 0)) + '%</strong>'
            : '<span class="muted">—</span>';
        }
      },
      {
        label: 'Обновлено',
        render: function (row) {
          return row.stats ? App.formatDate(row.stats.updatedAt) : '—';
        }
      },
      {
        label: 'Действие',
        render: function (row) {
          return '<a class="btn btn-sm btn-soft" href="' +
            App.url('course-details.html') + '?id=' + row.course.id + '">Открыть курс</a>';
        }
      }
    ], courseStats());
  }

  async function load() {
    state.role = App.getRole();
    state.user = App.getUser();

    var courses = await App.api.courses.list();
    state.courses = state.role === 'teacher'
      ? courses.filter(function (course) { return course.teacherId === state.user.id; })
      : courses;

    try {
      App.cacheUsers(await App.api.users.lookup());
    } catch (error) {
      App.cacheUsers([]);
    }

    state.stats = {};
    await Promise.all(state.courses.map(async function (course) {
      try {
        state.stats[course.id] = await App.api.analytics.course(course.id);
      } catch (error) {
        state.stats[course.id] = null;
      }
    }));

    renderStats();
    renderCharts();
    renderTable();
  }

  function init() {
    if (!App.requireRole(['teacher', 'admin'])) {
      return;
    }

    var actions = document.getElementById('analytics-actions');
    if (actions) {
      actions.innerHTML = '<button class="btn btn-outline" type="button" id="analytics-refresh">Обновить</button>';
      document.getElementById('analytics-refresh').addEventListener('click', function () {
        load().then(function () {
          App.notify('Аналитика обновлена', 'success');
        }).catch(function (error) {
          App.notify(error.message, 'error');
        });
      });
    }

    load().catch(function (error) {
      App.showError('analytics-table', error);
    });
  }

  init();
})();
