/*
 * layout.js — общая шапка, меню по роли и подвал.
 * Файл выполняется после data.js, но до app.js, поэтому
 * отрисовка запускается по событию DOMContentLoaded.
 */
(function () {
  'use strict';

  // Возвращает пункты меню для конкретной роли.
  function getMenu(role) {
    if (role === 'student') {
      return [
        { label: 'Дашборд', page: 'index.html' },
        { label: 'Курсы', page: 'courses.html' },
        { label: 'Мои результаты', page: 'my-results.html' },
        { label: 'Проверка кода', page: 'code-submissions.html' }
      ];
    }
    if (role === 'teacher') {
      return [
        { label: 'Дашборд', page: 'index.html' },
        { label: 'Курсы', page: 'courses.html' },
        { label: 'Конструктор теста', page: 'test-editor.html' },
        { label: 'Проверка работ', page: 'grading.html' },
        { label: 'Проверка кода', page: 'code-submissions.html' },
        { label: 'Аналитика', page: 'analytics.html' }
      ];
    }
    if (role === 'admin') {
      return [
        { label: 'Дашборд', page: 'index.html' },
        { label: 'Пользователи', page: 'admin-users.html' },
        { label: 'Аналитика', page: 'analytics.html' }
      ];
    }
    return [];
  }

  // Строит HTML-ссылку меню и подсвечивает текущую страницу.
  function buildNavLink(item, currentFile) {
    var href = window.App.url(item.page);
    var isActive = item.page === currentFile;
    var className = isActive ? 'nav-link nav-link-active' : 'nav-link';
    return '<a class="' + className + '" href="' + href + '">' +
      window.App.escapeHtml(item.label) + '</a>';
  }

  // Строит блок текущего пользователя с именем, ролью и кнопкой выхода.
  function buildUserBox(user, role) {
    if (!user || !role) {
      return '<div class="user-box"><span class="muted small">Гость</span></div>';
    }
    return '<div class="user-box">' +
      '<div class="user-info">' +
      '<span class="user-name">' + window.App.escapeHtml(user.fullName) + '</span>' +
      '<span class="user-role">' + window.App.statusLabel(role) + '</span>' +
      '</div>' +
      '<button class="btn btn-outline btn-sm" type="button" id="logout-btn">Выйти</button>' +
      '</div>';
  }

  // Рендерит шапку в <header id="app-header">.
  function renderHeader() {
    var header = document.getElementById('app-header');
    if (!header) {
      return;
    }

    var role = window.App.getRole();
    var user = window.App.getUser();
    var currentFile = location.pathname.split('/').pop() || 'index.html';
    var menu = getMenu(role);
    var nav = menu.map(function (item) {
      return buildNavLink(item, currentFile);
    }).join('');

    header.innerHTML =
      '<div class="container header-inner">' +
      '<a class="brand" href="' + window.App.url('index.html') + '">' +
      'Online<span>Testing</span>' +
      '</a>' +
      '<nav class="main-nav">' + nav + '</nav>' +
      buildUserBox(user, role) +
      '</div>';

    var logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
      logoutBtn.addEventListener('click', async function () {
        logoutBtn.disabled = true;
        await window.App.api.auth.logout();
        location.href = window.App.url('login.html');
      });
    }
  }

  // Рендерит подвал.
  function renderFooter() {
    var footer = document.getElementById('app-footer');
    if (!footer) {
      return;
    }
    footer.innerHTML =
      '<div class="container footer-inner">' +
      '<span>© ' + new Date().getFullYear() + ' OnlineTesting — учебный проект</span>' +
      '<span>Данные: микросервисы через ApiGateway</span>' +
      '</div>';
  }

  // Запускает отрисовку шапки и подвала после загрузки DOM.
  document.addEventListener('DOMContentLoaded', function () {
    renderHeader();
    renderFooter();
  });
})();
