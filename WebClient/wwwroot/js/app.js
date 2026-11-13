/*
 * app.js — глобальный объект window.App с общими хелперами.
 * Здесь только вспомогательные функции для отрисовки макета:
 * работа с ролью, таблицами, карточками, бейджами и форматированием.
 */
window.App = (function () {
  'use strict';

  var ROLE_KEY = 'onlineTestingRole';
  var USER_KEY = 'onlineTestingUser';
  var TOKEN_KEY = 'jwt';

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

  // Возвращает сохранённую роль или null, если пользователь ещё не вошёл.
  function getRole() {
    return localStorage.getItem(ROLE_KEY);
  }

  // Сохраняет роль в localStorage.
  function setRole(role) {
    localStorage.setItem(ROLE_KEY, role);
  }

  // Возвращает объект текущего пользователя из мока по сохранённому id.
  function getUser() {
    var id = localStorage.getItem(USER_KEY);
    if (!id) {
      return null;
    }
    return getUserById(id);
  }

  // Сохраняет идентификатор выбранного пользователя.
  function setUser(user) {
    localStorage.setItem(USER_KEY, user.id);
  }

  // Очищает данные сессии (используется кнопкой «Выйти»).
  function clearSession() {
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(TOKEN_KEY);
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

  // Выводит сообщение об отсутствии доступа в основную область страницы.
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

  // Экранирует HTML, чтобы данные из мока не ломали разметку.
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

  // Рисует таблицу в контейнер по описанию колонок и массиву строк.
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

  // Рисует набор карточек: статистику, курсы, тесты и другие блоки.
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

  // Возвращает цветной бейдж для вердикта, статуса или роли.
  function renderBadge(status) {
    if (!status) {
      return '<span class="badge badge-muted">—</span>';
    }
    var cssClass = badgeClass(status);
    return '<span class="badge ' + cssClass + '">' + escapeHtml(statusLabel(status)) + '</span>';
  }

  // Определяет CSS-класс бейджа по значению статуса.
  function badgeClass(status) {
    var success = ['OK', 'checked', 'graded', 'active', 'published', 'grade-5', 'grade-4'];
    var warning = ['WA', 'pending', 'draft', 'grade-3'];
    var danger = ['TLE', 'blocked', 'grade-2'];
    var info = ['CE', 'in-progress'];

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

  // Переводит технический статус в понятную русскую подпись.
  function statusLabel(status) {
    var labels = {
      OK: 'OK — принято',
      WA: 'WA — неверный ответ',
      TLE: 'TLE — превышено время',
      CE: 'CE — ошибка компиляции',
      checked: 'Проверено',
      pending: 'На проверке',
      'in-progress': 'В процессе',
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

  // Показывает заглушку вместо реального действия с backend.
  function stub(actionName) {
    alert('«' + actionName + '»: действие появится после подключения API.');
  }

  // Навешивает на форму отмену отправки и заглушку действия.
  function bindForm(formId, actionName) {
    var form = document.getElementById(formId);
    if (!form) {
      return;
    }
    form.addEventListener('submit', function (event) {
      event.preventDefault();
      stub(actionName);
    });
  }

  // Форматирует ISO-дату в российском формате (дата и время).
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

  // Преобразует процент в CSS-класс ширины полоски (шаг 5%).
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

  // Возвращает пользователя по id.
  function getUserById(id) {
    return window.Mock.users.filter(function (user) {
      return user.id === id;
    })[0] || null;
  }

  // Возвращает курс по id.
  function getCourseById(id) {
    return window.Mock.courses.filter(function (course) {
      return course.id === id;
    })[0] || null;
  }

  // Возвращает тест по id.
  function getTestById(id) {
    return window.Mock.tests.filter(function (test) {
      return test.id === id;
    })[0] || null;
  }

  // Возвращает модуль по id.
  function getModuleById(id) {
    return window.Mock.modules.filter(function (module) {
      return module.id === id;
    })[0] || null;
  }

  // Читает значение параметра из адресной строки.
  function getQueryParam(name) {
    var params = new URLSearchParams(location.search);
    return params.get(name);
  }

  // Универсальный запрос к ApiGateway. Используется при подключении backend.
  async function request(endpoint, options) {
    var settings = options || {};
    var headers = Object.assign({ 'Content-Type': 'application/json' }, settings.headers || {});
    var token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      headers.Authorization = 'Bearer ' + token;
    }

    var response = await fetch(apiBase() + endpoint, Object.assign({}, settings, { headers: headers }));

    if (response.status === 401) {
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
      var message = data && data.message ? data.message : (typeof data === 'string' ? data : 'Ошибка сервера');
      throw new Error(message);
    }

    return data;
  }

  return {
    apiBase: apiBase,
    request: request,
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
    stub: stub,
    bindForm: bindForm,
    formatDate: formatDate,
    barWidth: barWidth,
    getUserById: getUserById,
    getCourseById: getCourseById,
    getTestById: getTestById,
    getModuleById: getModuleById,
    getQueryParam: getQueryParam
  };
})();
