/*
 * code-submissions.js — посылки кода и вердикты.
 * Студент видит свои решения, преподаватель — все.
 */
(function () {
  'use strict';

  var App = window.App;

  // Возвращает посылки с учётом роли и выбранного вердикта.
  function getFilteredSubmissions(user, role) {
    var select = document.getElementById('verdict-filter');
    var verdict = select ? select.value : 'all';

    return window.Mock.submissions.filter(function (submission) {
      var ownerMatches = role !== 'student' || submission.studentId === user.id;
      var verdictMatches = verdict === 'all' || submission.verdict === verdict;
      return ownerMatches && verdictMatches;
    });
  }

  // Рисует сводные показатели по посылкам.
  function renderStats(submissions) {
    var accepted = submissions.filter(function (submission) {
      return submission.verdict === 'OK';
    });
    var failed = submissions.filter(function (submission) {
      return submission.verdict === 'WA' || submission.verdict === 'TLE' || submission.verdict === 'CE';
    });
    var rate = submissions.length
      ? Math.round((accepted.length / submissions.length) * 100) + '%'
      : '—';

    App.renderCards('submissions-stats', [
      { title: 'Всего посылок', value: submissions.length, subtitle: 'Отправленные решения' },
      { title: 'Принято (OK)', value: accepted.length, subtitle: 'Успешные вердикты' },
      { title: 'С ошибками', value: failed.length, subtitle: 'WA, TLE или CE' },
      { title: 'Доля успешных', value: rate, subtitle: 'От всех посылок' }
    ]);
  }

  // Рисует таблицу посылок; преподаватель дополнительно видит студента.
  function renderTable(submissions, role) {
    var columns = [];

    if (role === 'teacher') {
      columns.push({
        label: 'Студент',
        render: function (submission) {
          var student = App.getUserById(submission.studentId);
          return App.escapeHtml(student ? student.fullName : '—');
        }
      });
    }

    columns.push(
      {
        label: 'Задача',
        render: function (submission) {
          return '<span class="table-title">' + App.escapeHtml(submission.task) + '</span>';
        }
      },
      { label: 'Язык', key: 'language' },
      {
        label: 'Отправлено',
        render: function (submission) {
          return App.formatDate(submission.submittedAt);
        }
      },
      {
        label: 'Время',
        render: function (submission) {
          return submission.timeMs ? submission.timeMs + ' мс' : '—';
        }
      },
      {
        label: 'Память',
        render: function (submission) {
          return submission.memoryMb ? submission.memoryMb + ' МБ' : '—';
        }
      },
      {
        label: 'Вердикт',
        render: function (submission) {
          return App.renderBadge(submission.verdict);
        }
      },
      {
        label: 'Действие',
        render: function (submission) {
          return '<button class="btn btn-sm btn-outline" type="button" data-submission="' +
            submission.id + '">Подробнее</button>';
        }
      }
    );

    App.renderTable('submissions-table', columns, submissions);
  }

  // Добавляет кнопку действия в шапку страницы.
  function renderActions(role) {
    var actions = document.getElementById('submissions-actions');
    if (!actions) {
      return;
    }
    var label = role === 'teacher' ? 'Проверить код' : 'Отправить решение';
    actions.innerHTML = '<button class="btn btn-primary" type="button" id="submission-action-btn">' +
      label + '</button>';
    document.getElementById('submission-action-btn').addEventListener('click', function () {
      App.stub(label);
    });
  }

  // Навешивает заглушку на кнопки подробностей.
  function bindTableActions() {
    var container = document.getElementById('submissions-table');
    if (!container) {
      return;
    }
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-submission]');
      if (button) {
        App.stub('Подробности посылки');
      }
    });
  }

  // Инициализирует страницу посылок кода.
  function init() {
    if (!App.requireRole(['student', 'teacher'])) {
      return;
    }

    var user = App.getUser();
    if (!user) {
      return;
    }

    var role = App.getRole();
    renderActions(role);

    // Перерисовка при смене фильтра вердиктов.
    function refresh() {
      var submissions = getFilteredSubmissions(user, role);
      renderStats(submissions);
      renderTable(submissions, role);
    }

    refresh();
    bindTableActions();

    document.getElementById('verdict-filter').addEventListener('change', refresh);
  }

  init();
})();
