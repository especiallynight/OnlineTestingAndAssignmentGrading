/*
 * admin-users.js — управление пользователями, ролями и заявками преподавателей.
 * Все действия выполняются через IdentityService.
 */
(function () {
  'use strict';

  var App = window.App;

  var ROLE_API = {
    student: 'Student',
    teacher: 'Teacher',
    admin: 'Admin'
  };

  var state = {
    user: null,
    users: [],
    applications: []
  };

  function renderStats() {
    var students = state.users.filter(function (user) { return user.role === 'student'; });
    var teachers = state.users.filter(function (user) { return user.role === 'teacher'; });
    var admins = state.users.filter(function (user) { return user.role === 'admin'; });
    var pending = state.applications.filter(function (application) {
      return application.status === 0 || application.status === 'Pending';
    });

    App.renderCards('users-stats', [
      { title: 'Всего пользователей', value: state.users.length, subtitle: 'Все учётные записи' },
      { title: 'Студентов', value: students.length, subtitle: 'Роль «Студент»' },
      { title: 'Преподавателей', value: teachers.length, subtitle: 'Роль «Преподаватель»' },
      { title: 'Администраторов', value: admins.length, subtitle: 'Роль «Администратор»' },
      { title: 'Заявок на преподавателя', value: pending.length, subtitle: 'Ожидают рассмотрения' }
    ]);
  }

  function renderTable() {
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
          var isSelf = state.user && String(user.id) === String(state.user.id);
          var options = ['student', 'teacher', 'admin'].map(function (role) {
            return '<option value="' + role + '"' +
              (user.role === role ? ' selected' : '') + '>' +
              App.escapeHtml(App.statusLabel(role)) + '</option>';
          }).join('');

          return '<select data-role-for="' + user.id + '"' +
            (isSelf ? ' disabled title="Нельзя изменить собственную роль"' : '') +
            '>' + options + '</select>';
        }
      },
      {
        label: 'Действие',
        render: function (user) {
          var isSelf = state.user && String(user.id) === String(state.user.id);
          if (isSelf) {
            return '<span class="muted small">Это вы</span>';
          }
          return '<button class="btn btn-sm btn-soft" type="button" data-save-role="' +
            user.id + '">Сохранить роль</button>';
        }
      }
    ], state.users);
  }

  function renderApplications() {
    var pending = state.applications.filter(function (application) {
      return application.status === 0 || application.status === 'Pending';
    });

    App.renderTable('applications-table', [
      { label: 'Почта', key: 'email' },
      {
        label: 'Подана',
        render: function (application) {
          return App.formatDate(application.createdAt);
        }
      },
      {
        label: 'Действие',
        render: function (application) {
          return '<div class="form-actions">' +
            '<button class="btn btn-sm btn-primary" type="button" data-approve="' +
            application.teacherApplicationId + '">Одобрить</button>' +
            '<button class="btn btn-sm btn-danger-soft" type="button" data-reject="' +
            application.teacherApplicationId + '">Отклонить</button>' +
            '</div>';
        }
      }
    ], pending);
  }

  async function loadUsers() {
    state.users = await App.api.users.list();
    renderStats();
    renderTable();
  }

  async function loadApplications() {
    try {
      state.applications = await App.api.users.applications() || [];
    } catch (error) {
      state.applications = [];
    }
    renderStats();
    renderApplications();
  }

  async function saveRole(userId) {
    var select = document.querySelector('[data-role-for="' + userId + '"]');
    if (!select) {
      return;
    }
    var role = select.value;
    await App.api.users.setRole(userId, ROLE_API[role]);
    App.notify('Роль пользователя обновлена', 'success');
    await loadUsers();
  }

  async function reviewApplication(id, approve) {
    if (approve) {
      await App.api.users.approve(id);
      App.notify('Заявка одобрена: пользователь стал преподавателем', 'success');
    } else {
      await App.api.users.reject(id);
      App.notify('Заявка отклонена', 'success');
    }
    await Promise.all([loadUsers(), loadApplications()]);
  }

  function bindEvents() {
    var table = document.getElementById('users-table');
    if (table) {
      table.addEventListener('click', function (event) {
        var button = event.target.closest('[data-save-role]');
        if (!button) {
          return;
        }
        button.disabled = true;
        saveRole(button.getAttribute('data-save-role')).catch(function (error) {
          button.disabled = false;
          App.notify(error.message, 'error');
        });
      });
    }

    var applications = document.getElementById('applications-table');
    if (applications) {
      applications.addEventListener('click', function (event) {
        var approve = event.target.closest('[data-approve]');
        var reject = event.target.closest('[data-reject]');
        if (!approve && !reject) {
          return;
        }
        var button = approve || reject;
        button.disabled = true;
        reviewApplication(
          button.getAttribute(approve ? 'data-approve' : 'data-reject'),
          !!approve
        ).catch(function (error) {
          button.disabled = false;
          App.notify(error.message, 'error');
        });
      });
    }

    App.bindForm('user-form', async function (event, form) {
      var values = App.formValues(form);
      var payload = {
        firstName: (values.firstName || '').trim(),
        lastName: (values.lastName || '').trim(),
        email: (values.email || '').trim(),
        password: values.password || '',
        role: ROLE_API[values.role] || 'Student'
      };

      if (payload.firstName.length < 2 || payload.lastName.length < 2) {
        App.notify('Укажите имя и фамилию (не менее 2 символов)', 'error');
        return;
      }
      if (payload.password.length < 6) {
        App.notify('Пароль должен быть не менее 6 символов', 'error');
        return;
      }

      var submit = form.querySelector('button[type="submit"]');
      if (submit) {
        submit.disabled = true;
      }

      try {
        await App.api.users.create(payload);
        App.notify('Пользователь создан', 'success');
        form.reset();
        await loadUsers();
      } catch (error) {
        App.notify(error.message, 'error');
      } finally {
        if (submit) {
          submit.disabled = false;
        }
      }
    });
  }

  async function init() {
    if (!App.requireRole(['admin'])) {
      return;
    }

    state.user = App.getUser();
    bindEvents();

    try {
      await Promise.all([loadUsers(), loadApplications()]);
    } catch (error) {
      App.showError('users-table', error);
    }
  }

  init();
})();
