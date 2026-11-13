/*
 * grading.js — проверка работ преподавателем.
 * Попытки берутся из ExamService, оценки — из GradingService.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    role: null,
    user: null,
    attempts: [],
    assignments: [],
    grades: {},
    filter: 'all'
  };

  function scorePercent(score, maxScore) {
    var max = Number(maxScore);
    if (!max) {
      return 0;
    }
    return Math.round((Number(score) / max) * 100);
  }

  // Приводит ответ студента к читаемому виду.
  function formatAnswer(answer) {
    if (!answer) {
      return '<span class="muted">Нет ответа</span>';
    }
    var data = answer.data;
    if (data === null || data === undefined || data === '') {
      return '<span class="muted">Нет ответа</span>';
    }
    try {
      var parsed = JSON.parse(data);
      if (Array.isArray(parsed)) {
        return App.escapeHtml(parsed.join(', '));
      }
      if (typeof parsed === 'string') {
        return App.escapeHtml(parsed);
      }
      return App.escapeHtml(String(data));
    } catch (error) {
      return '<code class="code-block">' + App.escapeHtml(String(data)) + '</code>';
    }
  }

  function renderStats() {
    var graded = state.attempts.filter(function (attempt) {
      return !!state.grades[attempt.id];
    });
    var pending = state.attempts.filter(function (attempt) {
      return !state.grades[attempt.id];
    });
    var percents = graded.map(function (attempt) {
      var grade = state.grades[attempt.id];
      return scorePercent(grade.score, grade.maxScore);
    });
    var average = percents.length
      ? Math.round(percents.reduce(function (sum, value) { return sum + value; }, 0) / percents.length)
      : null;

    App.renderCards('grading-stats', [
      { title: 'Всего попыток', value: state.attempts.length, subtitle: 'По вашим курсам' },
      { title: 'Проверено', value: graded.length, subtitle: 'Оценка выставлена' },
      { title: 'Без оценки', value: pending.length, subtitle: 'Ожидают проверки' },
      {
        title: 'Средний результат',
        value: average === null ? '—' : average + '%',
        subtitle: 'По проверенным работам'
      }
    ]);
  }

  function filteredAttempts() {
    if (state.filter === 'graded') {
      return state.attempts.filter(function (attempt) {
        return !!state.grades[attempt.id];
      });
    }
    if (state.filter === 'pending') {
      return state.attempts.filter(function (attempt) {
        return !state.grades[attempt.id];
      });
    }
    return state.attempts;
  }

  function renderTable() {
    var rows = filteredAttempts();

    App.renderTable('grading-table', [
      {
        label: 'Студент',
        render: function (attempt) {
          var student = App.getUserById(attempt.studentId);
          return App.escapeHtml(student ? student.fullName : 'Студент #' + attempt.studentId);
        }
      },
      {
        label: 'Тест',
        render: function (attempt) {
          var assignment = App.getAssignmentById(attempt.assignmentId);
          return App.escapeHtml(assignment ? assignment.title : 'Задание #' + attempt.assignmentId);
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
        label: 'Результат',
        render: function (attempt) {
          var grade = state.grades[attempt.id];
          if (!grade) {
            return App.renderBadge('pending');
          }
          return App.escapeHtml(grade.score + ' / ' + grade.maxScore) +
            ' <span class="muted small">(' + scorePercent(grade.score, grade.maxScore) + '%)</span>';
        }
      },
      {
        label: 'Действие',
        render: function (attempt) {
          return '<button class="btn btn-sm btn-soft" type="button" data-review="' +
            attempt.id + '">Открыть</button>';
        }
      }
    ], rows);
  }

  // Строит содержимое модального окна с ответами и формой оценки.
  function buildReviewBody(attempt, assignment, questions, answers, grade) {
    var answersByQuestion = {};
    answers.forEach(function (answer) {
      answersByQuestion[String(answer.questionId)] = answer;
    });

    var questionsHtml = questions.length
      ? questions.map(function (question, index) {
          var answer = answersByQuestion[String(question.id)];
          var correct = question.correctKeys && question.correctKeys.length
            ? question.correctKeys.join(', ')
            : (question.type === 'code' ? 'автопроверка' : '—');

          return '<article class="question-card">' +
            '<div class="flex-between">' +
            '<span class="question-type">Вопрос ' + (index + 1) + ' · ' + App.escapeHtml(question.apiType) + '</span>' +
            (answer ? App.renderBadge(answer.executionStatus || answer.status) : App.renderBadge('not-submitted')) +
            '</div>' +
            '<p class="question-text">' + App.escapeHtml(question.text) + '</p>' +
            '<p class="muted small">Ответ студента: ' + formatAnswer(answer) + '</p>' +
            '<p class="muted small">Правильный ответ: ' + App.escapeHtml(correct) + '</p>' +
            (answer && answer.executionStatus
              ? '<p class="muted small">Тесты: ' + (answer.passedTests || 0) + ' из ' +
                (answer.totalTests || 0) + ', балл: ' + (answer.score === null ? '—' : answer.score) + '</p>'
              : '') +
            (answer && answer.executionError
              ? '<p class="muted small">Ошибка: ' + App.escapeHtml(answer.executionError) + '</p>'
              : '') +
            '</article>';
        }).join('')
      : '<p class="muted">В задании нет вопросов.</p>';

    var gradeHtml;
    if (grade) {
      gradeHtml =
        '<div class="notice">' +
        'Оценка: <strong>' + App.escapeHtml(grade.score + ' из ' + grade.maxScore) +
        '</strong> (' + scorePercent(grade.score, grade.maxScore) + '%). ' +
        'Проверено: ' + App.formatDate(grade.checkedAt) + '.' +
        (grade.feedback ? '<br>Комментарий: ' + App.escapeHtml(grade.feedback) : '') +
        '</div>';
    } else {
      gradeHtml =
        '<div class="form-field">' +
        '<label for="grade-score">Балл (из ' + (assignment ? assignment.maxScore : 0) + ')</label>' +
        '<input id="grade-score" name="score" type="number" min="0" max="' +
        (assignment ? assignment.maxScore : 100) + '" step="0.5" required>' +
        '</div>' +
        '<div class="form-field">' +
        '<label for="grade-comment">Комментарий</label>' +
        '<textarea id="grade-comment" name="comment" rows="3" maxlength="300"></textarea>' +
        '</div>';
    }

    return '<div class="review-summary">' +
      '<p><strong>Студент:</strong> ' + App.escapeHtml(
        (App.getUserById(attempt.studentId) || {}).fullName || '#' + attempt.studentId) + '</p>' +
      '<p><strong>Тест:</strong> ' + App.escapeHtml(assignment ? assignment.title : '#' + attempt.assignmentId) + '</p>' +
      '<p><strong>Статус попытки:</strong> ' + App.escapeHtml(App.statusLabel(attempt.status)) + '</p>' +
      '</div>' +
      gradeHtml +
      '<h3 class="section-title">Ответы</h3>' +
      questionsHtml;
  }

  async function openReview(attemptId) {
    var attempt = state.attempts.filter(function (item) {
      return String(item.id) === String(attemptId);
    })[0];
    if (!attempt) {
      return;
    }

    var assignment = App.getAssignmentById(attempt.assignmentId);
    var questions = [];
    var answers = [];
    var grade = state.grades[attempt.id] || null;

    try {
      answers = await App.api.attempts.answers(attempt.id);
    } catch (error) {
      answers = [];
    }

    if (assignment) {
      try {
        questions = state.role === 'teacher'
          ? await App.api.questions.manage(assignment.id)
          : await App.api.questions.list(assignment.id);
      } catch (error) {
        questions = [];
      }
    }

    App.openModal({
      title: 'Работа студента',
      submitLabel: grade ? 'Закрыть' : 'Выставить оценку',
      body: buildReviewBody(attempt, assignment, questions, answers, grade),
      onSubmit: async function (form) {
        if (grade) {
          return;
        }
        var values = App.formValues(form);
        var score = Number(values.score);
        var maxScore = assignment ? Number(assignment.maxScore) : 0;
        if (isNaN(score) || score < 0 || score > maxScore) {
          throw new Error('Укажите балл от 0 до ' + maxScore + '.');
        }
        await App.api.grades.create({
          attemptId: attempt.id,
          studentId: attempt.studentId,
          courseId: assignment ? assignment.courseId : 0,
          score: score,
          maxScore: maxScore,
          gradedAt: new Date().toISOString()
        });
        App.notify('Оценка выставлена', 'success');
        await loadGrades();
        renderStats();
        renderTable();
      }
    });
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

  function bindTable() {
    var container = document.getElementById('grading-table');
    if (!container) {
      return;
    }
    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-review]');
      if (!button) {
        return;
      }
      openReview(button.getAttribute('data-review')).catch(function (error) {
        App.notify(error.message, 'error');
      });
    });
  }

  async function load() {
    state.role = App.getRole();
    state.user = App.getUser();

    var courses = await App.api.courses.list();
    var relevantCourses = state.role === 'teacher'
      ? courses.filter(function (course) { return course.teacherId === state.user.id; })
      : courses;

    var lists = await Promise.all(relevantCourses.map(function (course) {
      return App.api.assignments.list(course.id).catch(function () { return []; });
    }));

    state.assignments = [];
    lists.forEach(function (list) {
      state.assignments = state.assignments.concat(list);
    });
    App.cacheAssignments(state.assignments);

    var assignmentIds = {};
    state.assignments.forEach(function (assignment) {
      assignmentIds[String(assignment.id)] = true;
    });

    var attempts = await App.api.attempts.list();
    state.attempts = attempts.filter(function (attempt) {
      return state.role !== 'teacher' || assignmentIds[String(attempt.assignmentId)];
    });
    state.attempts.sort(function (first, second) {
      return new Date(second.startedAt || 0) - new Date(first.startedAt || 0);
    });

    try {
      App.cacheUsers(await App.api.users.lookup());
    } catch (error) {
      App.cacheUsers([]);
    }

    await loadGrades();
    renderStats();
    renderTable();
  }

  async function init() {
    if (!App.requireRole(['teacher', 'admin'])) {
      return;
    }

    var actions = document.getElementById('grading-actions');
    if (actions) {
      actions.innerHTML = '<button class="btn btn-outline" type="button" id="grading-refresh">Обновить</button>';
      document.getElementById('grading-refresh').addEventListener('click', function () {
        load().then(function () {
          App.notify('Список работ обновлён', 'success');
        }).catch(function (error) {
          App.notify(error.message, 'error');
        });
      });
    }

    var filter = document.getElementById('grading-filter');
    if (filter) {
      filter.addEventListener('change', function (event) {
        state.filter = event.target.value;
        renderTable();
      });
    }

    bindTable();

    try {
      await load();
    } catch (error) {
      App.showError('grading-table', error);
    }
  }

  init();
})();
