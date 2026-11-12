/*
 * my-results.js — история попыток и оценки студента.
 * Данные фильтруются по текущему пользователю из localStorage.
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

  // Возвращает лучший результат в процентах.
  function bestResult(attempts) {
    var best = 0;
    attempts.forEach(function (attempt) {
      if (!attempt.maxScore) {
        return;
      }
      var percent = Math.round((attempt.score / attempt.maxScore) * 100);
      if (percent > best) {
        best = percent;
      }
    });
    return best ? best + '%' : '—';
  }

  // Рисует карточки со сводными показателями студента.
  function renderStats(attempts) {
    var checked = attempts.filter(function (attempt) {
      return attempt.status === 'checked';
    });

    App.renderCards('results-stats', [
      { title: 'Всего попыток', value: attempts.length, subtitle: 'Включая незавершённые' },
      { title: 'Проверено', value: checked.length, subtitle: 'Работы с оценкой' },
      { title: 'Средняя оценка', value: averageGrade(attempts), subtitle: 'По пятибалльной шкале' },
      { title: 'Лучший результат', value: bestResult(attempts), subtitle: 'Доля верных баллов' }
    ]);
  }

  // Рисует таблицу попыток студента.
  function renderTable(attempts) {
    App.renderTable('results-table', [
      {
        label: 'Тест',
        render: function (attempt) {
          var test = App.getTestById(attempt.testId);
          return '<span class="table-title">' +
            App.escapeHtml(test ? test.title : 'Тест') + '</span>';
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
        label: 'Завершено',
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
        label: 'Оценка',
        render: function (attempt) {
          return attempt.grade ? App.renderBadge('grade-' + attempt.grade) : '—';
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
          return '<button class="btn btn-sm btn-outline" type="button" data-attempt="' +
            attempt.id + '">Подробнее</button>';
        }
      }
    ], attempts);
  }

  // Навешивает заглушку на кнопки просмотра подробностей.
  function bindTableActions() {
    var container = document.getElementById('results-table');
    if (!container) {
      return;
    }
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-attempt]');
      if (button) {
        App.stub('Подробности попытки');
      }
    });
  }

  // Инициализирует страницу результатов.
  function init() {
    if (!App.requireRole(['student'])) {
      return;
    }

    var user = App.getUser();
    if (!user) {
      return;
    }

    var attempts = window.Mock.attempts.filter(function (attempt) {
      return attempt.studentId === user.id;
    }).sort(function (first, second) {
      return new Date(second.startedAt) - new Date(first.startedAt);
    });

    renderStats(attempts);
    renderTable(attempts);
    bindTableActions();
  }

  init();
})();
