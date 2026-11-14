/*
 * app.js — глобальный объект window.App.
 * Содержит работу с JWT-сессией, HTTP-клиент ApiGateway,
 * преобразование DTO микросервисов и общие хелперы отрисовки.
 */
window.App = (function () {
  'use strict';

  var ROLE_KEY = 'onlineTestingRole';
  var USER_KEY = 'onlineTestingUser';
  var TOKEN_KEY = 'jwt';
  var REFRESH_KEY = 'refreshToken';

  // Базовый адрес ApiGateway. Можно переопределить через window.API_BASE.
  // В Docker клиент открыт на порту 5000, а gateway — на 5006;
  // при локальном запуске WebClient используется порт gateway 5130.
  function apiBase() {
    if (window.API_BASE) {
      return window.API_BASE;
    }
    return location.port === '5000'
      ? 'http://localhost:5006'
      : 'http://localhost:5130';
  }

  // ---------- Сессия ----------

  function getToken() {
    return localStorage.getItem(TOKEN_KEY);
  }

  function getRole() {
    return localStorage.getItem(ROLE_KEY);
  }

  function setRole(role) {
    if (role) {
      localStorage.setItem(ROLE_KEY, role);
    } else {
      localStorage.removeItem(ROLE_KEY);
    }
  }

  // Возвращает объект текущего пользователя, сохранённый при входе.
  function getUser() {
    var raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      var parsed = JSON.parse(raw);
      return parsed && parsed.id ? parsed : null;
    } catch (error) {
      return null;
    }
  }

  function setUser(user) {
    if (user) {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } else {
      localStorage.removeItem(USER_KEY);
    }
  }

  function clearSession() {
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
  }

  // Строит относительный путь к странице с учётом нахождения в /pages/.
  function url(page) {
    var inPages = location.pathname.indexOf('/pages/') !== -1;
    if (page === 'index.html') {
      return inPages ? '../index.html' : 'index.html';
    }
    return inPages ? page : 'pages/' + page;
  }

  // Проверяет роль: без роли уводит на вход, с чужой ролью показывает «нет доступа».
  function requireRole(roles) {
    var role = getRole();
    if (!role) {
      location.href = url('login.html');
      return false;
    }
    if (roles && roles.length && roles.indexOf(role) === -1) {
      showAccessDenied();
      return false;
    }
    return true;
  }

  function showAccessDenied() {
    var main = document.querySelector('main');
    if (!main) {
      return;
    }
    main.innerHTML =
      '<div class="container">' +
      '<div class="card empty-state">' +
      '<h2>Нет доступа</h2>' +
      '<p class="muted">У вашей роли нет прав на просмотр этой страницы.</p>' +
      '<a class="btn btn-primary" href="' + url('index.html') + '">Вернуться на дашборд</a>' +
      '</div></div>';
  }

  // ---------- HTTP ----------

  function decodeToken(token) {
    if (!token) {
      return {};
    }
    try {
      var part = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      var padding = (4 - (part.length % 4)) % 4;
      var padded = part + new Array(padding + 1).join('=');
      return JSON.parse(decodeURIComponent(escape(atob(padded))));
    } catch (error) {
      return {};
    }
  }

  function extractError(data) {
    if (!data) {
      return 'Ошибка сервера';
    }
    if (typeof data === 'string') {
      return data;
    }
    if (data.errors && typeof data.errors === 'object') {
      var first = Object.keys(data.errors)[0];
      if (first && data.errors[first] && data.errors[first].length) {
        return data.errors[first][0];
      }
    }
    return data.message || data.detail || data.title || 'Ошибка сервера';
  }

  async function refreshTokens() {
    var refreshToken = localStorage.getItem(REFRESH_KEY);
    if (!refreshToken) {
      return false;
    }
    try {
      var response = await fetch(apiBase() + '/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: refreshToken })
      });
      if (!response.ok) {
        return false;
      }
      var data = await response.json();
      localStorage.setItem(TOKEN_KEY, data.accessToken);
      localStorage.setItem(REFRESH_KEY, data.refreshToken);
      return true;
    } catch (error) {
      return false;
    }
  }

  function buildRequestOptions(settings) {
    var headers = Object.assign({ 'Content-Type': 'application/json' }, settings.headers || {});
    var token = getToken();
    if (token && !settings.anonymous) {
      headers.Authorization = 'Bearer ' + token;
    }

    var body = settings.body;
    if (body !== undefined && body !== null && typeof body !== 'string') {
      body = JSON.stringify(body);
    }

    var options = Object.assign({}, settings, { headers: headers, body: body });
    delete options.anonymous;
    delete options.retried;
    return options;
  }

  // Универсальный запрос к ApiGateway с обновлением JWT при истечении срока.
  async function request(endpoint, options) {
    var settings = options || {};
    var response = await fetch(apiBase() + endpoint, buildRequestOptions(settings));

    if (response.status === 401 && !settings.anonymous && !settings.retried) {
      var refreshed = await refreshTokens();
      if (refreshed) {
        settings.retried = true;
        response = await fetch(apiBase() + endpoint, buildRequestOptions(settings));
      }
    }

    if (response.status === 401 && !settings.anonymous) {
      clearSession();
      location.href = url('login.html');
      throw new Error('Требуется повторный вход');
    }

    if (response.status === 204) {
      return true;
    }

    var text = await response.text();
    var data = null;
    if (text) {
      try {
        data = JSON.parse(text);
      } catch (error) {
        data = text;
      }
    }

    if (!response.ok) {
      var error = new Error(extractError(data));
      error.status = response.status;
      throw error;
    }

    return data;
  }

  // ---------- Преобразование DTO ----------

  function roleFromNumber(value) {
    if (value === 0 || value === '0' || value === 'Student' || value === 'student') {
      return 'student';
    }
    if (value === 1 || value === '1' || value === 'Teacher' || value === 'teacher') {
      return 'teacher';
    }
    if (value === 2 || value === '2' || value === 'Admin' || value === 'admin') {
      return 'admin';
    }
    return 'student';
  }

  function roleToNumber(value) {
    return { student: 0, teacher: 1, admin: 2 }[value] || 0;
  }

  function questionTypeFromApi(value) {
    return {
      SingleChoice: 'single',
      MultipleChoice: 'multiple',
      Code: 'code'
    }[value] || 'single';
  }

  function questionTypeToApi(value) {
    return {
      single: 'SingleChoice',
      multiple: 'MultipleChoice',
      code: 'Code'
    }[value] || 'SingleChoice';
  }

  function attemptStatusFromApi(value) {
    return {
      InProgress: 'in-progress',
      Completed: 'completed',
      Expired: 'expired',
      0: 'in-progress',
      1: 'completed',
      2: 'expired'
    }[value] || 'in-progress';
  }

  function answerStatusFromApi(value) {
    return {
      Saved: 'saved',
      Submitted: 'submitted',
      Graded: 'graded',
      0: 'saved',
      1: 'submitted',
      2: 'graded'
    }[value] || 'saved';
  }

  function userFullName(user) {
    if (!user) {
      return '—';
    }
    var parts = [user.firstName, user.lastName].filter(function (part) {
      return part && String(part).trim();
    });
    return parts.length ? parts.join(' ') : (user.email || '—');
  }

  function normalizeUser(user) {
    return {
      id: user.userId,
      userId: user.userId,
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      fullName: userFullName(user),
      email: user.email || '',
      role: roleFromNumber(user.role)
    };
  }

  function normalizeCourse(course) {
    return {
      id: course.courseId,
      courseId: course.courseId,
      title: course.name,
      name: course.name,
      description: course.description || '',
      teacherId: course.teacherId,
      studentsCount: course.studentsCount || 0,
      assignmentsCount: course.assignmentsCount || 0,
      testsCount: course.assignmentsCount || 0,
      modulesCount: 0,
      durationWeeks: 0,
      status: 'active',
      createdAt: course.createdAt
    };
  }

  function normalizeMaterial(material) {
    return {
      id: material.courseMaterialId,
      courseMaterialId: material.courseMaterialId,
      courseId: material.courseId,
      title: material.title,
      content: material.content || '',
      sortOrder: material.sortOrder || 0,
      createdAt: material.createdAt
    };
  }

  function normalizeAssignment(assignment) {
    return {
      id: assignment.assignmentId,
      assignmentId: assignment.assignmentId,
      courseId: assignment.courseId,
      title: assignment.name,
      name: assignment.name,
      description: assignment.description || '',
      maxScore: assignment.maxScore || 0,
      durationMin: assignment.timeLimitMinutes || 0,
      timeLimitMinutes: assignment.timeLimitMinutes || 0,
      status: 'published'
    };
  }

  // Варианты ответа приходят объектом { "a": "текст" }.
  function optionEntries(options) {
    if (!options) {
      return [];
    }
    if (Array.isArray(options)) {
      return options.map(function (text, index) {
        return { key: String(index), text: String(text) };
      });
    }
    if (typeof options === 'object') {
      return Object.keys(options).map(function (key) {
        return { key: key, text: String(options[key]) };
      });
    }
    return [];
  }

  function normalizeQuestion(question) {
    var options = optionEntries(question.options);
    var correct = question.correct_answer;
    var correctKeys = [];
    if (Array.isArray(correct)) {
      correctKeys = correct.map(String);
    } else if (correct !== null && correct !== undefined && correct !== '') {
      correctKeys = [String(correct)];
    }

    return {
      id: question.question_id,
      questionId: question.question_id,
      assignmentId: question.assignment_id,
      type: questionTypeFromApi(question.type),
      apiType: question.type,
      text: question.text,
      options: options,
      correctKeys: correctKeys,
      testCases: question.test_cases || [],
      points: 1
    };
  }

  function normalizeAttempt(attempt) {
    return {
      id: attempt.attemptId,
      attemptId: attempt.attemptId,
      studentId: attempt.studentId,
      assignmentId: attempt.assignmentId,
      testId: attempt.assignmentId,
      startedAt: attempt.startedAt,
      finishedAt: attempt.deadlineAt,
      deadlineAt: attempt.deadlineAt,
      status: attemptStatusFromApi(attempt.status),
      apiStatus: attempt.status,
      score: null,
      maxScore: null,
      grade: null
    };
  }

  function normalizeGrade(grade) {
    if (!grade) {
      return null;
    }
    return {
      id: grade.attemptGradeId,
      attemptId: grade.attemptId,
      score: Number(grade.score),
      maxScore: Number(grade.maxScore),
      checkedAt: grade.checkedAt,
      feedback: grade.feedback || ''
    };
  }

  // ---------- Кэш справочников для хелперов отрисовки ----------

  var cache = {
    users: [],
    courses: [],
    assignments: [],
    questions: []
  };

  function cacheUsers(users) {
    cache.users = users || [];
  }

  function cacheCourses(courses) {
    cache.courses = courses || [];
  }

  function cacheAssignments(assignments) {
    cache.assignments = assignments || [];
  }

  function getUserById(id) {
    return cache.users.filter(function (user) {
      return String(user.id) === String(id);
    })[0] || null;
  }

  function getCourseById(id) {
    return cache.courses.filter(function (course) {
      return String(course.id) === String(id);
    })[0] || null;
  }

  function getAssignmentById(id) {
    return cache.assignments.filter(function (assignment) {
      return String(assignment.id) === String(id);
    })[0] || null;
  }

  function getQuestionById(id) {
    return cache.questions.filter(function (question) {
      return String(question.id) === String(id);
    })[0] || null;
  }

  // ---------- API-клиент ----------

  var api = {
    auth: {
      login: async function (email, password) {
        var data = await request('/api/auth/login', {
          method: 'POST',
          body: { email: email, password: password },
          anonymous: true
        });
        localStorage.setItem(TOKEN_KEY, data.accessToken);
        localStorage.setItem(REFRESH_KEY, data.refreshToken);

        var payload = decodeToken(data.accessToken);
        var userId = Number(payload.sub || payload.nameid || 0);
        var user = await api.users.get(userId);
        setUser(user);
        setRole(user.role);
        return user;
      },
      logout: async function () {
        try {
          await request('/api/auth/logout', { method: 'POST' });
        } catch (error) {
          // Выход должен работать даже при недоступном сервисе.
        }
        clearSession();
      }
    },

    users: {
      list: async function () {
        var users = (await request('/api/users') || []).map(normalizeUser);
        cacheUsers(users);
        return users;
      },
      lookup: async function () {
        var users = (await request('/api/users/lookup') || []).map(normalizeUser);
        cacheUsers(users);
        return users;
      },
      get: async function (id) {
        return normalizeUser(await request('/api/users/' + id));
      },
      create: function (payload) {
        return request('/api/users', { method: 'POST', body: payload });
      },
      setRole: function (id, role) {
        return request('/api/users/' + id + '/role', {
          method: 'PUT',
          body: { role: role }
        });
      },
      applications: function () {
        return request('/api/auth/teacher-applications');
      },
      approve: function (id) {
        return request('/api/auth/teacher-applications/' + id + '/approve', { method: 'PUT' });
      },
      reject: function (id) {
        return request('/api/auth/teacher-applications/' + id + '/reject', { method: 'PUT' });
      }
    },

    courses: {
      list: async function () {
        var courses = (await request('/api/courses') || []).map(normalizeCourse);
        cacheCourses(courses);
        return courses;
      },
      enrolled: async function () {
        var courses = (await request('/api/courses/enrolled') || []).map(normalizeCourse);
        return courses;
      },
      get: async function (id) {
        return normalizeCourse(await request('/api/courses/' + id));
      },
      create: function (payload) {
        return request('/api/courses', { method: 'POST', body: payload });
      },
      update: function (id, payload) {
        return request('/api/courses/' + id, { method: 'PUT', body: payload });
      },
      remove: function (id) {
        return request('/api/courses/' + id, { method: 'DELETE' });
      },
      enroll: function (id) {
        return request('/api/courses/' + id + '/enroll', { method: 'POST' });
      },
      unenroll: function (id) {
        return request('/api/courses/' + id + '/enroll', { method: 'DELETE' });
      },
      enrollment: function (id) {
        return request('/api/courses/' + id + '/enrollment');
      }
    },

    materials: {
      list: async function (courseId) {
        return (await request('/api/courses/' + courseId + '/materials') || [])
          .map(normalizeMaterial);
      },
      create: function (courseId, payload) {
        return request('/api/courses/' + courseId + '/materials', {
          method: 'POST',
          body: payload
        });
      },
      update: function (courseId, id, payload) {
        return request('/api/courses/' + courseId + '/materials/' + id, {
          method: 'PUT',
          body: payload
        });
      },
      remove: function (courseId, id) {
        return request('/api/courses/' + courseId + '/materials/' + id, {
          method: 'DELETE'
        });
      }
    },

    assignments: {
      list: async function (courseId) {
        var assignments = (await request('/api/courses/' + courseId + '/assignments') || [])
          .map(normalizeAssignment);
        cacheAssignments(cache.assignments
          .filter(function (assignment) {
            return String(assignment.courseId) !== String(courseId);
          })
          .concat(assignments));
        return assignments;
      },
      get: async function (id) {
        return normalizeAssignment(await request('/api/assignments/' + id));
      },
      create: function (courseId, payload) {
        return request('/api/courses/' + courseId + '/assignments', { method: 'POST', body: payload });
      },
      update: function (id, payload) {
        return request('/api/assignments/' + id, { method: 'PUT', body: payload });
      },
      remove: function (id) {
        return request('/api/assignments/' + id, { method: 'DELETE' });
      }
    },

    questions: {
      list: async function (assignmentId) {
        var questions = (await request('/api/assignments/' + assignmentId + '/questions') || [])
          .map(normalizeQuestion);
        cache.questions = cache.questions
          .filter(function (question) { return question.assignmentId !== assignmentId; })
          .concat(questions);
        return questions;
      },
      manage: async function (assignmentId) {
        var questions = (await request('/api/assignments/' + assignmentId + '/questions/manage') || [])
          .map(normalizeQuestion);
        cache.questions = cache.questions
          .filter(function (question) { return question.assignmentId !== assignmentId; })
          .concat(questions);
        return questions;
      },
      create: function (assignmentId, payload) {
        return request('/api/assignments/' + assignmentId + '/questions', {
          method: 'POST',
          body: payload
        });
      },
      update: function (id, payload) {
        return request('/api/questions/' + id, { method: 'PUT', body: payload });
      },
      remove: function (id) {
        return request('/api/questions/' + id, { method: 'DELETE' });
      }
    },

    attempts: {
      list: async function () {
        return (await request('/api/exam_attempts') || []).map(normalizeAttempt);
      },
      get: async function (id) {
        return normalizeAttempt(await request('/api/exam_attempts/attempt/' + id));
      },
      create: async function (assignmentId) {
        return normalizeAttempt(await request('/api/exam_attempts', {
          method: 'POST',
          body: { assignmentId: assignmentId }
        }));
      },
      start: async function (id, timeLimitMinutes) {
        return normalizeAttempt(await request('/api/exam_attempts/' + id + '/start', {
          method: 'POST',
          body: { timeLimitMinutes: timeLimitMinutes }
        }));
      },
      stop: async function (id) {
        return normalizeAttempt(await request('/api/exam_attempts/' + id + '/stop', { method: 'PUT' }));
      },
      answers: async function (id) {
        var answers = await request('/api/exam_attempts/' + id + '/answers') || [];
        return answers.map(function (answer) {
          return {
            id: answer.answerId,
            answerId: answer.answerId,
            attemptId: answer.attemptId,
            questionId: answer.questionId,
            data: answer.answerData,
            answerData: answer.answerData,
            submittedAt: answer.submittedAt,
            status: answerStatusFromApi(answer.status),
            executionStatus: answer.executionStatus || '',
            passedTests: answer.passedTests,
            totalTests: answer.totalTests,
            score: answer.score === null || answer.score === undefined ? null : Number(answer.score),
            executionError: answer.executionError || ''
          };
        });
      },
      saveAnswer: async function (attemptId, questionId, answerData) {
        var answer = await request('/api/exam_attempts/' + attemptId + '/answers', {
          method: 'POST',
          body: { questionId: questionId, answerData: answerData }
        });
        return answer.answerId;
      },
      updateAnswer: function (answerId, answerData) {
        return request('/api/answers/' + answerId, {
          method: 'PUT',
          body: { answerData: answerData }
        });
      },
      submitCodeAnswer: function (answerId) {
        return request('/api/answers/' + answerId + '/submit', { method: 'POST' });
      }
    },

    grades: {
      byAttempt: async function (attemptId) {
        try {
          return normalizeGrade(await request('/api/grades/attempt/' + attemptId));
        } catch (error) {
          if (error.status === 404) {
            return null;
          }
          throw error;
        }
      },
      create: function (payload) {
        return request('/api/grades', { method: 'POST', body: payload });
      }
    },

    analytics: {
      course: function (courseId) {
        return request('/api/analytics/courses/' + courseId);
      },
      student: function (studentId) {
        return request('/api/analytics/students/' + studentId);
      }
    }
  };

  // ---------- Отрисовка ----------

  function escapeHtml(value) {
    if (value === null || value === undefined) {
      return '';
    }
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function renderTable(containerId, columns, rows) {
    var container = document.getElementById(containerId);
    if (!container) {
      return;
    }
    if (!rows || !rows.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">Записей пока нет.</p></div>';
      return;
    }

    var head = columns.map(function (column) {
      return '<th>' + escapeHtml(column.label) + '</th>';
    }).join('');

    var body = rows.map(function (row) {
      var cells = columns.map(function (column) {
        var content = column.render
          ? column.render(row)
          : escapeHtml(row[column.key]);
        var cellClass = column.className ? ' class="' + column.className + '"' : '';
        return '<td' + cellClass + '>' + content + '</td>';
      }).join('');
      return '<tr>' + cells + '</tr>';
    }).join('');

    container.innerHTML =
      '<div class="table-wrap"><table class="data-table">' +
      '<thead><tr>' + head + '</tr></thead>' +
      '<tbody>' + body + '</tbody>' +
      '</table></div>';
  }

  function renderCards(containerId, items) {
    var container = document.getElementById(containerId);
    if (!container) {
      return;
    }
    if (!items || !items.length) {
      container.innerHTML = '<div class="empty-state"><p class="muted">Данных пока нет.</p></div>';
      return;
    }

    var html = items.map(function (item) {
      var badge = item.badge
        ? '<div class="card-badge">' + renderBadge(item.badge) + '</div>'
        : '';
      var value = item.value !== undefined && item.value !== null
        ? '<div class="stat-value">' + escapeHtml(item.value) + '</div>'
        : '';
      var subtitle = item.subtitle
        ? '<p class="card-subtitle">' + escapeHtml(item.subtitle) + '</p>'
        : '';
      var text = item.text
        ? '<p class="card-text">' + escapeHtml(item.text) + '</p>'
        : '';
      var meta = item.meta && item.meta.length
        ? '<ul class="card-meta">' + item.meta.map(function (entry) {
            return '<li>' + escapeHtml(entry) + '</li>';
          }).join('') + '</ul>'
        : '';
      var link = item.href
        ? '<a class="card-link" href="' + item.href + '">' +
          escapeHtml(item.hrefLabel || 'Подробнее') + ' →</a>'
        : '';
      var extraClass = item.className ? ' ' + item.className : '';

      return '<article class="card' + extraClass + '">' +
        badge +
        '<h3 class="card-title">' + escapeHtml(item.title) + '</h3>' +
        value +
        subtitle +
        text +
        meta +
        link +
        '</article>';
    }).join('');

    container.innerHTML = html;
  }

  function renderBadge(status) {
    if (!status) {
      return '<span class="badge badge-muted">—</span>';
    }
    var cssClass = badgeClass(status);
    return '<span class="badge ' + cssClass + '">' + escapeHtml(statusLabel(status)) + '</span>';
  }

  function badgeClass(status) {
    var success = ['OK', 'checked', 'graded', 'active', 'published', 'completed', 'grade-5', 'grade-4'];
    var warning = ['WA', 'pending', 'draft', 'grade-3', 'in-progress'];
    var danger = ['TLE', 'blocked', 'grade-2', 'expired'];
    var info = ['CE', 'submitted', 'saved'];

    if (success.indexOf(status) !== -1) {
      return 'badge-success';
    }
    if (warning.indexOf(status) !== -1) {
      return 'badge-warning';
    }
    if (danger.indexOf(status) !== -1) {
      return 'badge-danger';
    }
    if (info.indexOf(status) !== -1) {
      return 'badge-info';
    }
    return 'badge-muted';
  }

  function statusLabel(status) {
    var labels = {
      OK: 'OK — принято',
      WA: 'WA — неверный ответ',
      TLE: 'TLE — превышено время',
      CE: 'CE — ошибка компиляции',
      checked: 'Проверено',
      pending: 'На проверке',
      submitted: 'Отправлено',
      saved: 'Сохранено',
      'not-submitted': 'Не отправлено',
      'in-progress': 'В процессе',
      completed: 'Завершено',
      expired: 'Время истекло',
      published: 'Опубликован',
      draft: 'Черновик',
      active: 'Активен',
      blocked: 'Заблокирован',
      student: 'Студент',
      teacher: 'Преподаватель',
      admin: 'Администратор',
      'grade-5': 'Отлично (5)',
      'grade-4': 'Хорошо (4)',
      'grade-3': 'Удовлетворительно (3)',
      'grade-2': 'Неудовлетворительно (2)'
    };
    return labels[status] || status;
  }

  // Навешивает обработчик отправки формы. Вторым аргументом приходит сама форма.
  function bindForm(formId, handler) {
    var form = document.getElementById(formId);
    if (!form) {
      return;
    }
    form.addEventListener('submit', function (event) {
      event.preventDefault();
      handler(event, form);
    });
  }

  function formatDate(iso) {
    if (!iso) {
      return '—';
    }
    var date = new Date(iso);
    if (isNaN(date.getTime())) {
      return '—';
    }
    return date.toLocaleString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  function formatDateOnly(iso) {
    if (!iso) {
      return '—';
    }
    var date = new Date(iso);
    if (isNaN(date.getTime())) {
      return '—';
    }
    return date.toLocaleDateString('ru-RU');
  }

  function barWidth(percent) {
    var value = Number(percent) || 0;
    if (value < 0) {
      value = 0;
    }
    if (value > 100) {
      value = 100;
    }
    return 'w-' + (Math.round(value / 5) * 5);
  }

  function getQueryParam(name) {
    var params = new URLSearchParams(location.search);
    return params.get(name);
  }

  // Показывает всплывающее уведомление об успехе или ошибке.
  function notify(message, kind) {
    var host = document.getElementById('app-toast');
    if (!host) {
      host = document.createElement('div');
      host.id = 'app-toast';
      document.body.appendChild(host);
    }
    host.className = 'app-toast app-toast-' + (kind || 'info');
    host.textContent = message;
    host.classList.add('app-toast-visible');
    clearTimeout(host._timer);
    host._timer = setTimeout(function () {
      host.classList.remove('app-toast-visible');
    }, 4500);
  }

  // Показывает сообщение об ошибке внутри контейнера.
  function showError(containerId, error) {
    var container = typeof containerId === 'string'
      ? document.getElementById(containerId)
      : containerId;
    if (!container) {
      notify(error && error.message ? error.message : String(error), 'error');
      return;
    }
    container.innerHTML = '<div class="empty-state"><p class="muted">' +
      escapeHtml(error && error.message ? error.message : String(error)) +
      '</p></div>';
  }

  // Ограничивает частоту вызовов (например, при поиске).
  function debounce(fn, wait) {
    var timer = null;
    return function () {
      var args = arguments;
      var context = this;
      clearTimeout(timer);
      timer = setTimeout(function () {
        fn.apply(context, args);
      }, wait);
    };
  }

  // Собирает значения именованных полей формы в объект.
  function formValues(form) {
    var values = {};
    Array.prototype.forEach.call(form.elements, function (element) {
      if (!element.name) {
        return;
      }
      if (element.type === 'checkbox') {
        if (!values[element.name]) {
          values[element.name] = [];
        }
        if (element.checked) {
          values[element.name].push(element.value);
        }
        return;
      }
      if (element.type === 'radio') {
        if (element.checked) {
          values[element.name] = element.value;
        }
        return;
      }
      values[element.name] = element.value;
    });
    return values;
  }

  // Открывает модальное окно с формой. onSubmit(form) может быть async.
  function openModal(options) {
    var overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.innerHTML =
      '<div class="modal" role="dialog" aria-modal="true">' +
      '<div class="modal-head">' +
      '<h2>' + escapeHtml(options.title || 'Форма') + '</h2>' +
      '<button class="btn btn-sm btn-outline" type="button" data-close>Закрыть</button>' +
      '</div>' +
      '<form class="form-stack" id="modal-form">' +
      (options.body || '') +
      '<div class="form-actions">' +
      '<button class="btn btn-primary" type="submit">' +
      escapeHtml(options.submitLabel || 'Сохранить') + '</button>' +
      '<span class="form-hint modal-error" data-error></span>' +
      '</div>' +
      '</form>' +
      '</div>';

    document.body.appendChild(overlay);

    function close() {
      overlay.remove();
    }

    overlay.addEventListener('click', function (event) {
      if (event.target === overlay || event.target.closest('[data-close]')) {
        close();
      }
    });

    var form = overlay.querySelector('form');
    form.addEventListener('submit', async function (event) {
      event.preventDefault();
      var submit = form.querySelector('button[type="submit"]');
      var errorHost = overlay.querySelector('[data-error]');
      submit.disabled = true;
      errorHost.textContent = '';
      try {
        await options.onSubmit(form);
        close();
      } catch (error) {
        errorHost.textContent = error.message || 'Не удалось сохранить изменения';
        submit.disabled = false;
      }
    });

    var first = overlay.querySelector('input, textarea, select');
    if (first) {
      first.focus();
    }

    return { close: close, element: overlay, form: form };
  }

  return {
    apiBase: apiBase,
    request: request,
    api: api,
    getToken: getToken,
    getRole: getRole,
    setRole: setRole,
    getUser: getUser,
    setUser: setUser,
    clearSession: clearSession,
    url: url,
    requireRole: requireRole,
    escapeHtml: escapeHtml,
    renderTable: renderTable,
    renderCards: renderCards,
    renderBadge: renderBadge,
    statusLabel: statusLabel,
    bindForm: bindForm,
    formatDate: formatDate,
    formatDateOnly: formatDateOnly,
    barWidth: barWidth,
    getQueryParam: getQueryParam,
    notify: notify,
    showError: showError,
    debounce: debounce,
    formValues: formValues,
    openModal: openModal,
    roleFromNumber: roleFromNumber,
    roleToNumber: roleToNumber,
    questionTypeFromApi: questionTypeFromApi,
    questionTypeToApi: questionTypeToApi,
    attemptStatusFromApi: attemptStatusFromApi,
    answerStatusFromApi: answerStatusFromApi,
    optionEntries: optionEntries,
    normalizeUser: normalizeUser,
    normalizeCourse: normalizeCourse,
    normalizeAssignment: normalizeAssignment,
    normalizeQuestion: normalizeQuestion,
    normalizeAttempt: normalizeAttempt,
    normalizeGrade: normalizeGrade,
    cacheUsers: cacheUsers,
    cacheCourses: cacheCourses,
    cacheAssignments: cacheAssignments,
    getUserById: getUserById,
    getCourseById: getCourseById,
    getTestById: getAssignmentById,
    getAssignmentById: getAssignmentById,
    getQuestionById: getQuestionById
  };
})();
