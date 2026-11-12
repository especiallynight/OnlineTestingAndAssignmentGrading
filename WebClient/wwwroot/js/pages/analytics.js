/*
 * analytics.js — сводная аналитика для преподавателя и администратора.
 * Диаграммы построены на div-полосках без сторонних библиотек.
 */
(function () {
  'use strict';

  var App = window.App;

  // Рисует карточки со сводными показателями платформы.
  function renderStats(summary) {
    App.renderCards('analytics-stats', [
      { title: 'Студентов', value: summary.students, subtitle: 'На всех курсах' },
      { title: 'Преподавателей', value: summary.teachers, subtitle: 'Ведут обучение' },
      { title: 'Курсов', value: summary.courses, subtitle: 'В каталоге' },
      { title: 'Тестов', value: summary.tests, subtitle: 'Опубликовано и в черновиках' },
      { title: 'Средний балл', value: summary.averageScore, subtitle: 'По пятибалльной шкале' },
      { title: 'Процент сдачи', value: summary.passRate + '%', subtitle: 'Успешные попытки' }
    ]);
  }

  // Строит горизонтальную диаграмму из строк с процентной шириной.
  function renderBarChart(containerId, rows, fillClass) {
    var container = document.getElementById(containerId);
    if (!container) {
      return;
    }

    container.innerHTML = rows.map(function (row) {
      return '<div class="bar-row">' +
        '<span class="bar-label" title="' + App.escapeHtml(row.label) + '">' +
        App.escapeHtml(row.label) + '</span>' +
        '<span class="bar-track">' +
        '<span class="bar-fill ' + fillClass + ' ' + App.barWidth(row.percent) + '"></span>' +
        '</span>' +
        '<span class="bar-value">' + App.escapeHtml(row.value) + '</span>' +
        '</div>';
    }).join('');
  }

  // Рисует обе диаграммы: средний балл и процент сдачи.
  function renderCharts(courseStats) {
    var scoreRows = courseStats.map(function (stat) {
      var course = App.getCourseById(stat.courseId);
      return {
        label: course ? course.title : 'Курс',
        value: stat.averageScore.toFixed(1),
        percent: (stat.averageScore / 5) * 100
      };
    });

    var passRows = courseStats.map(function (stat) {
      var course = App.getCourseById(stat.courseId);
      return {
        label: course ? course.title : 'Курс',
        value: stat.passRate + '%',
        percent: stat.passRate
      };
    });

    renderBarChart('score-chart', scoreRows, '');
    renderBarChart('pass-chart', passRows, 'bar-fill-green');
  }

  // Возвращает цветной бейдж для процента сдачи.
  function passBadge(rate) {
    var cssClass = 'badge-success';
    if (rate < 75) {
      cssClass = 'badge-danger';
    } else if (rate < 85) {
      cssClass = 'badge-warning';
    }
    return '<span class="badge ' + cssClass + '">' + rate + '%</span>';
  }

  // Рисует таблицу детализации по курсам.
  function renderTable(courseStats) {
    App.renderTable('analytics-table', [
      {
        label: 'Курс',
        render: function (stat) {
          var course = App.getCourseById(stat.courseId);
          return '<span class="table-title">' +
            App.escapeHtml(course ? course.title : 'Курс') + '</span>';
        }
      },
      { label: 'Студентов', key: 'students' },
      { label: 'Попыток', key: 'attempts' },
      {
        label: 'Средний балл',
        render: function (stat) {
          return stat.averageScore.toFixed(1) + ' / 5';
        }
      },
      {
        label: 'Процент сдачи',
        render: function (stat) {
          return passBadge(stat.passRate);
        }
      }
    ], courseStats);
  }

  // Добавляет кнопку формирования отчёта.
  function renderActions() {
    var actions = document.getElementById('analytics-actions');
    if (!actions) {
      return;
    }
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="export-report-btn">' +
      'Экспорт отчёта</button>';
    document.getElementById('export-report-btn').addEventListener('click', function () {
      App.stub('Экспорт отчёта');
    });
  }

  // Инициализирует страницу аналитики.
  function init() {
    if (!App.requireRole(['teacher', 'admin'])) {
      return;
    }

    var analytics = window.Mock.analytics;
    renderStats(analytics.summary);
    renderCharts(analytics.courses);
    renderTable(analytics.courses);
    renderActions();
  }

  init();
})();
