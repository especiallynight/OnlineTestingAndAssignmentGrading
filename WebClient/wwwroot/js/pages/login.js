/*
 * login.js — страница входа (мок).
 * Пароль не проверяется: выбираем пользователя из Mock.users
 * и сохраняем его роль в localStorage.
 */
(function () {
  'use strict';

  // Заполняет выпадающий список пользователей, группируя их по ролям.
  function fillUserSelect() {
    var select = document.getElementById('login-user');
    if (!select) {
      return;
    }

    var groups = [
      { role: 'student', title: 'Студенты' },
      { role: 'teacher', title: 'Преподаватели' },
      { role: 'admin', title: 'Администраторы' }
    ];

    select.innerHTML = groups.map(function (group) {
      var options = window.Mock.users.filter(function (user) {
        return user.role === group.role;
      }).map(function (user) {
        return '<option value="' + user.id + '">' +
          window.App.escapeHtml(user.fullName) + ' — ' +
          window.App.escapeHtml(user.email) +
          '</option>';
      }).join('');

      if (!options) {
        return '';
      }
      return '<optgroup label="' + group.title + '">' + options + '</optgroup>';
    }).join('');
  }

  // Показывает список демо-учёток справа от формы.
  function renderDemoAccounts() {
    var container = document.getElementById('demo-accounts');
    if (!container) {
      return;
    }
    container.innerHTML = window.Mock.users.map(function (user) {
      return '<li><span>' + window.App.escapeHtml(user.fullName) + '</span>' +
        '<span class="muted">' + window.App.statusLabel(user.role) + '</span></li>';
    }).join('');
  }

  // Обрабатывает вход: сохраняет пользователя и роль, уводит на дашборд.
  function handleLogin(event) {
    event.preventDefault();

    var select = document.getElementById('login-user');
    if (!select) {
      return;
    }

    var user = window.App.getUserById(select.value);
    if (!user) {
      alert('Выберите пользователя из списка.');
      return;
    }

    window.App.setUser(user);
    window.App.setRole(user.role);
    location.href = window.App.url('index.html');
  }

  // Инициализирует страницу входа.
  function init() {
    fillUserSelect();
    renderDemoAccounts();

    var form = document.getElementById('login-form');
    if (form) {
      form.addEventListener('submit', handleLogin);
    }
  }

  init();
})();
