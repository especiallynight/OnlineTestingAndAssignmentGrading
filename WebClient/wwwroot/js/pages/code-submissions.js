/*
 * code-submissions.js — решения задач на код и вердикты проверки.
 * Студент видит свои решения, преподаватель и администратор — все.
 */
(function () {
  'use strict';

  var App = window.App;

  var state = {
    role: null,
    user: null,
    rows: [],
    filter: 'all'
  };

  function verdictOf(row) {
    if (!row.answer.executionStatus) {
      return 'not-submitted';
    }
    return row.answer.executionStatus;
  }

  function renderStats() {
    var submitted = state.rows.filter(function (row) {
      return verdictOf(row) !== 'not-submitted';
    });
    var accepted = submitted.filter(function (row) {
      return verdictOf(row) === 'OK';
    });
    var failed = submitted.filter(function (row) {
      var verdict = verdictOf(row);
      return verdict === 'WA' || verdict === 'TLE' || verdict === 'CE';
    });

    App.renderCards('submissions-stats', [
      { title: 'Решений с кодом', value: state.rows.length, subtitle: 'Ответы на задачи' },
      { title: 'Отправлено на проверку', value: submitted.length, subtitle: 'Есть вердикт' },
      { title: 'Принято (OK)', value: accepted.length, subtitle: 'Все тесты пройдены' },
      { title: 'С ошибками', value: failed.length, subtitle: 'WA, TLE или CE' }
    ]);
  }

  function filteredRows() {
    if (state.filter === 'all') {
      return state.rows;
    }
    return state.rows.filter(function (row) {
      return verdictOf(row) === state.filter;
    });
  }

  function renderTable() {
    var showStudent = state.role !== 'student';

    var columns = [];
    if (showStudent) {
      columns.push({
        label: 'Студент',
        render: function (row) {
          var student = App.getUserById(row.attempt.studentId);
          return App.escapeHtml(student ? student.fullName : '#' + row.attempt.studentId);
        }
      });
    }

    columns.push(
      {
        label: 'Тест',
        render: function (row) {
          var assignment = App.getAssignmentById(row.attempt.assignmentId);
          return App.escapeHtml(assignment ? assignment.title : '#' + row.attempt.assignmentId);
        }
      },
      {
        label: 'Вопрос',
        render: function (row) {
          return App.escapeHtml(row.question ? row.question.text : 'Вопрос #' + row.answer.questionId);
        }
      },
      {
        label: 'Отправлено',
        render: function (row) {
          return row.answer.submittedAt
            ? App.formatDate(row.answer.submittedAt)
            : '<span class="muted">—</span>';
        }
      },
      {
        label: 'Вердикт',
        render: function (row) {
          return App.renderBadge(verdictOf(row));
        }
      },
      {
        label: 'Тесты',
        render: function (row) {
          if (row.answer.totalTests === null || row.answer.totalTests === undefined) {
            return '<span class="muted">—</span>';
          }
          return App.escapeHtml((row.answer.passedTests || 0) + ' / ' + row.answer.totalTests);
        }
      },
      {
        label: 'Балл',
        render: function (row) {
          return row.answer.score === null || row.answer.score === undefined
            ? '<span class="muted">—</span>'
            : App.escapeHtml(String(row.answer.score));
        }
      },
      {
        label: 'Ошибка',
        render: function (row) {
          return row.answer.executionError
            ? '<span class="muted small">' + App.escapeHtml(row.answer.executionError) + '</span>'
            : '<span class="muted">—</span>';
        }
      }
    );

    App.renderTable('submissions-table', columns, filteredRows());
  }

  async function load() {
    state.role = App.getRole();
    state.user = App.getUser();

    var courses = await App.api.courses.list();
    if (state.role === 'teacher') {
      courses = courses.filter(function (course) {
        return course.teacherId === state.user.id;
      });
    }

    var lists = await Promise.all(courses.map(function (course) {
      return App.api.assignments.list(course.id).catch(function () { return []; });
    }));

    var assignments = [];
    lists.forEach(function (list) {
      assignments = assignments.concat(list);
    });
    App.cacheAssignments(assignments);

    var assignmentIds = {};
    assignments.forEach(function (assignment) {
      assignmentIds[String(assignment.id)] = true;
    });

    var attempts = await App.api.attempts.list();
    if (state.role === 'student') {
      attempts = attempts.filter(function (attempt) {
        return String(attempt.studentId) === String(state.user.id);
      });
    } else if (state.role === 'teacher') {
      attempts = attempts.filter(function (attempt) {
        return assignmentIds[String(attempt.assignmentId)];
      });
    }

    try {
      App.cacheUsers(await App.api.users.lookup());
    } catch (error) {
      App.cacheUsers([]);
    }

    var neededAssignments = {};
    attempts.forEach(function (attempt) {
      neededAssignments[String(attempt.assignmentId)] = true;
    });

    var questionsByAssignment = {};
    await Promise.all(Object.keys(neededAssignments).map(async function (assignmentId) {
      try {
        questionsByAssignment[assignmentId] = await App.api.questions.list(assignmentId);
      } catch (error) {
        questionsByAssignment[assignmentId] = [];
      }
    }));

    var questionById = {};
    Object.keys(questionsByAssignment).forEach(function (assignmentId) {
      questionsByAssignment[assignmentId].forEach(function (question) {
        questionById[String(question.id)] = question;
      });
    });

    var answerLists = await Promise.all(attempts.map(function (attempt) {
      return App.api.attempts.answers(attempt.id).catch(function () { return []; });
    }));

    state.rows = [];
    attempts.forEach(function (attempt, index) {
      answerLists[index].forEach(function (answer) {
        var question = questionById[String(answer.questionId)];
        if (!question || question.type !== 'code') {
          return;
        }
        state.rows.push({
          attempt: attempt,
          answer: answer,
          question: question
        });
      });
    });

    state.rows.sort(function (first, second) {
      return new Date(second.answer.submittedAt || 0) -
        new Date(first.answer.submittedAt || 0);
    });

    renderStats();
    renderTable();
  }

  function init() {
    if (!App.requireRole(['student', 'teacher', 'admin'])) {
      return;
    }

    var filter = document.getElementById('verdict-filter');
    if (filter) {
      filter.addEventListener('change', function (event) {
        state.filter = event.target.value;
        renderTable();
      });
    }

    load().catch(function (error) {
      App.showError('submissions-table', error);
    });
  }

  init();
})();
