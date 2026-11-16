/*
 * login.js — страница входа.
 * Проверяет учётные данные через IdentityService и сохраняет JWT-сессию.
 */
(function () {
  'use strict';

  var App = window.App;

  var DEMO_ACCOUNTS = [
    { email: 'student@demo.local', role: 'student', title: 'Студент' },
    { email: 'teacher@demo.local', role: 'teacher', title: 'Преподаватель' },
    { email: 'admin@demo.local', role: 'admin', title: 'Администратор' }
  ];

  var DEMO_PASSWORD = 'Demo123!';

  // Показывает демо-учётки с кнопками автозаполнения формы.
  function renderDemoAccounts() {
    var container = document.getElementById('demo-accounts');
    if (!container) {
      return;
    }

    container.innerHTML = DEMO_ACCOUNTS.map(function (account) {
      return '<button class="demo-account" type="button" data-demo-email="' +
        App.escapeHtml(account.email) + '">' +
        '<span class="demo-account-role">' + App.escapeHtml(account.title) + '</span>' +
        '<span class="muted small">' + App.escapeHtml(account.email) + '</span>' +
        '</button>';
    }).join('');

    container.addEventListener('click', function (event) {
      var button = event.target.closest('[data-demo-email]');
      if (!button) {
        return;
      }
      document.getElementById('login-email').value = button.getAttribute('data-demo-email');
      document.getElementById('login-password').value = DEMO_PASSWORD;
      document.getElementById('login-email').focus();
    });
  }

  function setStatus(message, kind) {
    var status = document.getElementById('login-status');
    if (!status) {
      return;
    }
    status.textContent = message;
    status.className = 'login-note' + (kind ? ' login-note-' + kind : '');
  }

  // Обрабатывает вход через реальный API.
  async function handleLogin(event) {
    event.preventDefault();

    var email = document.getElementById('login-email').value.trim();
    var password = document.getElementById('login-password').value;
    var submit = document.getElementById('login-submit');

    if (!email || !password) {
      setStatus('Укажите почту и пароль.', 'error');
      return;
    }

    submit.disabled = true;
    submit.textContent = 'Вход…';
    setStatus('Проверяем учётные данные…');

    try {
      var user = await App.api.auth.login(email, password);
      setStatus('Вход выполнен. Перенаправляем на дашборд…', 'success');
      location.href = App.url('index.html');
    } catch (error) {
      setStatus(error.message || 'Не удалось войти.', 'error');
      submit.disabled = false;
      submit.textContent = 'Войти в систему';
    }
  }

  function init() {
    renderDemoAccounts();

    var emailInput = document.getElementById('login-email');
    var current = App.getUser();
    if (emailInput && current && current.email) {
      emailInput.value = current.email;
    }

    App.bindForm('login-form', handleLogin);
  }

  init();
})();
