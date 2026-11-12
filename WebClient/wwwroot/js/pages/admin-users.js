/*
 * admin-users.js — управление пользователями и ролями.
 * Создание и изменение ролей в макете не сохраняются.
 */
(function () {
  'use strict';

  var App = window.App;

  // Форматирует дату регистрации без времени.
  function formatDateOnly(iso) {
    if (!iso) {
      return '—';
    }
    var parts = String(iso).slice(0, 10).split('-');
    if (parts.length !== 3) {
      return iso;
    }
    return parts[2] + '.' + parts[1] + '.' + parts[0];
  }

  // Рисует карточки со статистикой по ролям.
  function renderStats(users) {
    var students = users.filter(function (user) {
      return user.role === 'student';
    });
    var teachers = users.filter(function (user) {
      return user.role === 'teacher';
    });

    App.renderCards('users-stats', [
      { title: 'Всего пользователей', value: users.length, subtitle: 'Все учётные записи' },
      { title: 'Студентов', value: students.length, subtitle: 'Роль «Студент»' },
      { title: 'Преподавателей', value: teachers.length, subtitle: 'Роль «Преподаватель»' },
      { title: 'Администраторов', value: users.length - students.length - teachers.length, subtitle: 'Роль «Администратор»' }
    ]);
  }

  // Рисует таблицу пользователей с ролями.
  function renderTable(users) {
    App.renderTable('users-table', [
      {
        label: 'ФИО',
        render: function (user) {
          return '<span class="table-title">' + App.escapeHtml(user.fullName) + '</span>';
        }
      },
      { label: 'Почта', key: 'email' },
      {
        label: 'Роль',
        render: function (user) {
          return App.renderBadge(user.role);
        }
      },
      { label: 'Группа / кафедра', key: 'group' },
      {
        label: 'Дата регистрации',
        render: function (user) {
          return formatDateOnly(user.joinedAt);
        }
      },
      {
        label: 'Действие',
        render: function (user) {
          return '<div class="table-actions">' +
            '<button class="btn btn-sm btn-outline" type="button" data-user="' + user.id + '">' +
            'Изменить роль</button>' +
            '</div>';
        }
      }
    ], users);
  }

  // Навешивает заглушки на действия с пользователями.
  function bindActions() {
    var container = document.getElementById('users-table');
    if (container) {
      container.addEventListener('click', function (event) {
        var button = event.target.closest('[data-user]');
        if (button) {
          App.stub('Изменить роль пользователя');
        }
      });
    }

    var actions = document.getElementById('users-actions');
    if (actions) {
      actions.innerHTML = '<button class="btn btn-primary" type="button" id="invite-user-btn">' +
        'Пригласить пользователя</button>';
      document.getElementById('invite-user-btn').addEventListener('click', function () {
        App.stub('Пригласить пользователя');
      });
    }
  }

  // Инициализирует страницу пользователей.
  function init() {
    if (!App.requireRole(['admin'])) {
      return;
    }

    var users = window.Mock.users;
    renderStats(users);
    renderTable(users);
    bindActions();

    // Создание пользователя — заглушка до подключения IdentityService.
    App.bindForm('user-form', 'Создать пользователя');
  }

  init();
})();
