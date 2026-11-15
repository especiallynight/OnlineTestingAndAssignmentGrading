
/*
 * course-details.js — карточка курса: задания, тесты и запись на курс.
 * Все данные загружаются из CourseService через ApiGateway.
 */
(function () {
    'use strict';

    var App = window.App;

    var state = {
        course: null,
        assignments: [],
        materials: [],
        questionsCount: {},
        questionsByAssignment: {},
        role: null,
        user: null,
        enrolled: false,
        attempts: {},
        selectedTest: null,
        questions: []
    };

    function courseId() {
        return App.getQueryParam('id');
    }

    // Возвращает последнюю попытку студента по заданию.
    function latestAttempt(assignmentId) {
        return state.attempts[assignmentId] || null;
    }

    // Вопросы выбранного задания.
    function assignmentQuestions(assignmentId) {
        return state.questionsByAssignment[assignmentId] || [];
    }

    // Задание на код — это тест, все вопросы которого имеют тип Code.
    // Такие задания показываются в разделе «Задания», остальные — в «Тестах».
    function isCodeAssignment(assignmentId) {
        var questions = assignmentQuestions(assignmentId);

        return questions.length > 0 && questions.every(function (question) {
            return question.type === 'code';
        });
    }

    // Проверяет, является ли пользователь преподавателем-владельцем курса.
    function canEditCourse() {
        return state.role === 'teacher' &&
            state.user &&
            state.course &&
            String(state.course.teacherId) === String(state.user.id);
    }

    // Шапка курса.
    function renderHero() {
        var container = document.getElementById('course-hero');

        if (!container) {
            return;
        }

        var course = state.course;
        var teacher = App.getUserById(course.teacherId);
        var action = '';

        if (canEditCourse()) {
            action =
                '<button class="btn btn-outline" type="button" id="course-action-btn">Редактировать курс</button>';
        } else if (state.role === 'student') {
            action = state.enrolled
                ? '<button class="btn btn-danger-soft" type="button" id="course-action-btn">Отписаться от курса</button>'
                : '<button class="btn btn-primary" type="button" id="course-action-btn">Записаться на курс</button>';
        }

        container.innerHTML =
            '<div class="flex-between">' +
            '<div>' +
            '<span class="pill">' + App.escapeHtml(App.statusLabel('active')) + '</span>' +
            '<h2 class="section-title">' + App.escapeHtml(course.title) + '</h2>' +
            '<p class="muted">' + App.escapeHtml(course.description || 'Описание не заполнено.') + '</p>' +
            '</div>' +
            '<div class="form-actions">' + action + '</div>' +
            '</div>' +
            '<div class="course-meta">' +
            '<span>Преподаватель: ' +
            App.escapeHtml(teacher ? teacher.fullName : '#' + course.teacherId) +
            '</span>' +
            '<span>Заданий: ' + state.assignments.length + '</span>' +
            '<span>Студентов: ' + course.studentsCount + '</span>' +
            '<span>Создан: ' + App.formatDateOnly(course.createdAt) + '</span>' +
            '</div>';


        var button = document.getElementById('course-action-btn');

        if (button) {
            if (state.role === 'teacher') {
                button.addEventListener('click', openCourseEditor);
            } else {
                button.addEventListener('click', toggleEnrollment);
            }
        }
    }

    // Редактирование курса.
    function openCourseEditor() {
        App.openModal({
            title: 'Редактирование курса',
            submitLabel: 'Сохранить',
            body:
                '<div class="form-field">' +
                '<label for="edit-course-name">Название</label>' +
                '<input id="edit-course-name" name="name" type="text" minlength="3" maxlength="100" required value="' +
                App.escapeHtml(state.course.title) + '">' +
                '</div>' +
                '<div class="form-field">' +
                '<label for="edit-course-description">Описание</label>' +
                '<textarea id="edit-course-description" name="description" maxlength="150" rows="4">' +
                App.escapeHtml(state.course.description || '') +
                '</textarea>' +
                '</div>',
            onSubmit: async function (form) {
                var values = App.formValues(form);

                await App.api.courses.update(state.course.id, {
                    name: values.name.trim(),
                    description: values.description.trim()
                });

                App.notify('Курс обновлён', 'success');
                await load();
            }
        });
    }

    // Запись студента на курс или отмена записи.
    async function toggleEnrollment() {
        var button = document.getElementById('course-action-btn');

        if (button) {
            button.disabled = true;
        }

        try {
            if (state.enrolled) {
                await App.api.courses.unenroll(state.course.id);
                App.notify('Вы отписались от курса', 'success');
            } else {
                await App.api.courses.enroll(state.course.id);
                App.notify('Вы записаны на курс', 'success');
            }

            await load();
        } catch (error) {
            App.notify(error.message, 'error');

            if (button) {
                button.disabled = false;
            }
        }
    }

    // Экранирует текст и превращает пустые строки в абзацы.
    function renderTheoryText(content, maxLength) {
        var text = String(content || '');

        if (maxLength && text.length > maxLength) {
            text = text.slice(0, maxLength).replace(/\s+\S*$/, '') + '…';
        }

        return text.split(/\n{2,}/).map(function (paragraph) {
            return '<p>' +
                App.escapeHtml(paragraph).replace(/\n/g, '<br>') +
                '</p>';
        }).join('');
    }

    function findMaterial(id) {
        return state.materials.find(function (material) {
            return String(material.id) === String(id);
        }) || null;
    }

    // Отображение теории курса.
    function renderTheory() {
        var container = document.getElementById('course-theory');
        var actions = document.getElementById('course-theory-actions');

        if (!container) {
            return;
        }

        if (actions) {
            actions.innerHTML = canEditCourse()
                ? '<button class="btn btn-sm btn-primary" type="button" id="add-material-btn">Добавить материал</button>'
                : '';

            var addButton = document.getElementById('add-material-btn');

            if (addButton) {
                addButton.addEventListener('click', function () {
                    openMaterialEditor(null);
                });
            }
        }

        if (!state.materials.length) {
            container.innerHTML =
                '<div class="empty-state"><p class="muted">' +
                (canEditCourse()
                    ? 'Теория пока не добавлена. Нажмите «Добавить материал».'
                    : 'В курсе пока нет материалов.') +
                '</p></div>';

            return;
        }

        container.innerHTML = state.materials.map(function (material, index) {
            var controls = canEditCourse()
                ? '<div class="theory-actions">' +
                '<button class="btn btn-sm btn-outline" type="button" data-edit="' +
                material.id + '">Редактировать</button>' +
                '<button class="btn btn-sm btn-danger-soft" type="button" data-delete="' +
                material.id + '">Удалить</button>' +
                '</div>'
                : '';

            return '<article class="theory-card">' +
                '<div class="theory-head">' +
                '<span class="module-number">' + (index + 1) + '</span>' +
                '<h3>' + App.escapeHtml(material.title) + '</h3>' +
                '</div>' +
                '<div class="theory-preview">' +
                renderTheoryText(material.content, 280) +
                '</div>' +
                '<div class="theory-foot">' +
                '<button class="btn btn-sm btn-soft" type="button" data-read="' +
                material.id + '">Читать</button>' +
                controls +
                '</div>' +
                '</article>';
        }).join('');

        Array.prototype.forEach.call(container.querySelectorAll('[data-read]'), function (button) {
            button.addEventListener('click', function () {
                openMaterialReader(findMaterial(button.getAttribute('data-read')));
            });
        });

        Array.prototype.forEach.call(container.querySelectorAll('[data-edit]'), function (button) {
            button.addEventListener('click', function () {
                openMaterialEditor(findMaterial(button.getAttribute('data-edit')));
            });
        });

        Array.prototype.forEach.call(container.querySelectorAll('[data-delete]'), function (button) {
            button.addEventListener('click', function () {
                removeMaterial(findMaterial(button.getAttribute('data-delete')));
            });
        });
    }

    // Чтение материала.
    function openMaterialReader(material) {
        if (!material) {
            return;
        }

        App.openModal({
            title: material.title,
            submitLabel: 'Закрыть',
            body: '<div class="theory-content">' +
                renderTheoryText(material.content) +
                '</div>',
            onSubmit: function () {
                return Promise.resolve();
            }
        });
    }

    // Создание или редактирование материала.
    function openMaterialEditor(material) {
        App.openModal({
            title: material ? 'Редактирование материала' : 'Новый материал',
            submitLabel: material ? 'Сохранить' : 'Добавить',
            body:
                '<div class="form-field">' +
                '<label for="material-title">Заголовок</label>' +
                '<input id="material-title" name="title" type="text" minlength="3" maxlength="150" required value="' +
                App.escapeHtml(material ? material.title : '') + '">' +
                '</div>' +
                '<div class="form-field">' +
                '<label for="material-content">Текст материала</label>' +
                '<textarea id="material-content" name="content" rows="12" minlength="10" maxlength="20000" required>' +
                App.escapeHtml(material ? material.content : '') +
                '</textarea>' +
                '<p class="form-hint">Пустая строка разделяет абзацы. HTML-теги выводятся как текст.</p>' +
                '</div>' +
                '<div class="form-field">' +
                '<label for="material-order">Порядок</label>' +
                '<input id="material-order" name="sortOrder" type="number" min="0" max="1000" value="' +
                (material ? material.sortOrder : state.materials.length + 1) + '">' +
                '</div>',
            onSubmit: async function (form) {
                var values = App.formValues(form);

                var payload = {
                    title: values.title.trim(),
                    content: values.content.trim(),
                    sortOrder: parseInt(values.sortOrder, 10) || 0
                };

                if (material) {
                    await App.api.materials.update(state.course.id, material.id, payload);
                    App.notify('Материал обновлён', 'success');
                } else {
                    await App.api.materials.create(state.course.id, payload);
                    App.notify('Материал добавлен', 'success');
                }

                await loadMaterials();
                renderTheory();
            }
        });
    }

    // Удаление материала.
    async function removeMaterial(material) {
        if (!material) {
            return;
        }

        if (!window.confirm('Удалить материал «' + material.title + '»?')) {
            return;
        }

        try {
            await App.api.materials.remove(state.course.id, material.id);
            App.notify('Материал удалён', 'success');

            await loadMaterials();
            renderTheory();
        } catch (error) {
            App.notify(error.message, 'error');
        }
    }

    // Загрузка теории курса.
    async function loadMaterials() {
        state.materials = await App.api.materials.list(state.course.id);
    }

    // Создание теста.
    function openCreateTest() {
        if (!canEditCourse()) {
            return;
        }

        App.openModal({
            title: 'Новый тест',
            submitLabel: 'Создать тест',
            body:
                '<div class="form-field">' +
                '<label>Название</label>' +
                '<input name="name" type="text" minlength="3" maxlength="100" required>' +
                '</div>' +
                '<div class="form-field">' +
                '<label>Описание</label>' +
                '<textarea name="description" maxlength="170" rows="3"></textarea>' +
                '</div>' +
                '<div class="form-field">' +
                '<label>Длительность, минут</label>' +
                '<input name="timeLimitMinutes" type="number" min="1" max="1440" value="30" required>' +
                '</div>' +
                '<div class="form-field">' +
                '<label>Максимум баллов</label>' +
                '<input name="maxScore" type="number" min="1" max="1000" value="10" required>' +
                '</div>',
            onSubmit: async function (form) {
                var values = App.formValues(form);

                var created = await App.api.assignments.create(state.course.id, {
                    name: values.name.trim(),
                    description: values.description.trim(),
                    maxScore: Number(values.maxScore),
                    timeLimitMinutes: Number(values.timeLimitMinutes)
                });

                await load();

                var newId = created && (
                    created.assignmentId ||
                    created.id ||
                    created.assignment_id
                );

                if (newId) {
                    await selectCourseTest(newId);
                } else {
                    App.notify('Тест создан. Выберите его в списке заданий для редактирования.', 'success');
                }
            }
        });
    }

    // Создание payload вопроса для API.
    function collectQuestionPayload(type, form) {
        var values = App.formValues(form);

        var payload = {
            questionText: values.questionText.trim(),
            questionType: App.questionTypeToApi(type),
            options: {},
            correctAnswer: null,
            testCases: null
        };

        if (!payload.questionText) {
            throw new Error('Введите текст вопроса');
        }

        if (type === 'single' || type === 'multiple') {
            var options = {};
            var correctKeys = [];

            ['a', 'b', 'c', 'd'].forEach(function (key) {
                var text = String(values['option_' + key] || '').trim();

                if (text) {
                    options[key] = text;
                }
            });

            if (Object.keys(options).length < 2) {
                throw new Error('Добавьте минимум два варианта ответа');
            }

            correctKeys = Array.isArray(values.correctAnswer)
                ? values.correctAnswer
                : values.correctAnswer
                    ? [values.correctAnswer]
                    : [];

            if (!correctKeys.length) {
                throw new Error('Отметьте правильный ответ');
            }

            correctKeys.forEach(function (key) {
                if (!options[key]) {
                    throw new Error('Правильный вариант ответа не заполнен');
                }
            });

            if (type === 'single' && correctKeys.length !== 1) {
                throw new Error('Для этого вопроса выберите только один правильный ответ');
            }

            payload.options = options;
            payload.correctAnswer = type === 'single'
                ? correctKeys[0]
                : correctKeys;
        } else if (type === 'code') {
            var rows = form.querySelectorAll('.test-case-row');

            payload.testCases = Array.prototype.map.call(rows, function (row) {
                var input = row.querySelector('[name="testInput"]');
                var output = row.querySelector('[name="expectedOutput"]');

                return {
                    input: input.value,
                    expectedOutput: output.value.trim()
                };
            });

            if (!payload.testCases.length) {
                throw new Error('Добавьте хотя бы один тестовый случай');
            }

            if (payload.testCases.some(function (testCase) {
                return !testCase.expectedOutput;
            })) {
                throw new Error('Укажите ожидаемый результат для каждого тестового случая');
            }
        }

        return payload;
    }

    // HTML строки тестового случая для задачи на программирование.
    // Каждое поле обёрнуто в .form-field, чтобы подпись стояла над своим
    // полем, а кнопка удаления была справа и не ломала сетку.
    function renderTestCaseRow(testCase) {
        testCase = testCase || {};

        return '<div class="test-case-row" data-test-case>' +
            '<div class="form-field">' +
            '<label>Входные данные</label>' +
            '<textarea name="testInput" rows="2">' +
            App.escapeHtml(testCase.input || '') +
            '</textarea>' +
            '</div>' +
            '<div class="form-field">' +
            '<label>Ожидаемый результат</label>' +
            '<textarea name="expectedOutput" rows="2" required>' +
            App.escapeHtml(testCase.expectedOutput || '') +
            '</textarea>' +
            '</div>' +
            '<button type="button" class="btn btn-sm btn-danger-soft" data-remove-case>Удалить</button>' +
            '</div>';
    }

    // Создание или редактирование вопроса.
    function openQuestionEditor(type, question) {
        if (!state.selectedTest || !canEditCourse()) {
            return;
        }

        var isEdit = !!question;
        var options = {};

        if (question && question.options) {
            question.options.forEach(function (option) {
                options[option.key] = option.text;
            });
        }

        var correctKeys = question ? question.correctKeys || [] : [];
        var answerType = type === 'multiple' ? 'checkbox' : 'radio';

        var body =
            '<div class="form-field">' +
            '<label>Текст вопроса</label>' +
            '<textarea name="questionText" rows="3" maxlength="1000" required>' +
            App.escapeHtml(question ? question.text : '') +
            '</textarea>' +
            '</div>';

        if (type === 'single' || type === 'multiple') {
            body += '<p class="muted small">Заполните варианты ответа и отметьте правильный.</p>';

            ['a', 'b', 'c', 'd'].forEach(function (key) {
                var checked = correctKeys.indexOf(key) !== -1 ? ' checked' : '';

                body +=
                    '<div class="form-field">' +
                    '<label>Вариант ' + key.toUpperCase() + '</label>' +
                    '<div class="flex-between">' +
                    '<input name="option_' + key + '" type="text" maxlength="300" value="' +
                    App.escapeHtml(options[key] || '') + '">' +
                    '<label class="small">' +
                    '<input type="' + answerType + '" name="correctAnswer" value="' + key + '"' + checked + '> Правильный' +
                    '</label>' +
                    '</div>' +
                    '</div>';
            });
        }

        if (type === 'code') {
            var cases = question && question.testCases && question.testCases.length
                ? question.testCases
                : [{ input: '', expectedOutput: '' }];

            body +=
                '<p class="muted small">Добавьте входные данные и ожидаемый результат для каждого тестового случая.</p>' +
                '<div class="test-case-list" id="question-test-cases">' +
                cases.map(renderTestCaseRow).join('') +
                '</div>' +
                '<div class="test-case-actions">' +
                '<button type="button" class="btn btn-sm btn-outline" id="add-test-case">Добавить тестовый случай</button>' +
                '</div>';
        }

        App.openModal({
            title: isEdit ? 'Редактирование вопроса' : 'Новый вопрос',
            submitLabel: isEdit ? 'Сохранить вопрос' : 'Добавить вопрос',
            body: body,
            onSubmit: async function (form) {
                var payload = collectQuestionPayload(type, form);

                if (isEdit) {
                    await App.api.questions.update(question.id, payload);
                    App.notify('Вопрос обновлён', 'success');
                } else {
                    await App.api.questions.create(state.selectedTest.id, payload);
                    App.notify('Вопрос добавлен', 'success');
                }

                await selectCourseTest(state.selectedTest.id);
            }
        });

        // Обработчики тестовых случаев добавляются после создания модального окна.
        var modal = document.querySelector('.modal-overlay:last-child');

        if (modal && type === 'code') {
            var addCaseButton = modal.querySelector('#add-test-case');
            var casesContainer = modal.querySelector('#question-test-cases');

            addCaseButton.addEventListener('click', function () {
                casesContainer.insertAdjacentHTML('beforeend', renderTestCaseRow());
            });

            casesContainer.addEventListener('click', function (event) {
                var removeButton = event.target.closest('[data-remove-case]');

                if (removeButton) {
                    var rows = casesContainer.querySelectorAll('.test-case-row');

                    if (rows.length <= 1) {
                        App.notify('Должен остаться хотя бы один тестовый случай', 'error');
                        return;
                    }

                    removeButton.closest('.test-case-row').remove();
                }
            });
        }
    }

    // Выбор теста и загрузка его вопросов.
    // Выбор теста и загрузка его вопросов.
    async function selectCourseTest(id) {
        if (!canEditCourse()) {
            return;
        }

        state.selectedTest = state.assignments.find(function (assignment) {
            return String(assignment.id) === String(id);
        }) || null;

        var editor = document.getElementById('course-test-editor');

        if (!state.selectedTest) {
            state.questions = [];

            if (editor) {
                editor.hidden = true;
            }

            return;
        }

        try {
            state.questions = await App.api.questions.manage(state.selectedTest.id);
            state.questionsByAssignment[state.selectedTest.id] = state.questions;
            state.questionsCount[state.selectedTest.id] = state.questions.length;

            renderCourseTestEditor();
            renderAssignments();
            renderTests();
        } catch (error) {
            App.notify(error.message, 'error');
        }
    }

    // Отображение кнопок добавления вопросов.
    function renderQuestionActions() {
        var container = document.getElementById('test-editor-question-actions');

        if (!container) {
            return;
        }

        container.innerHTML =
            '<button class="btn btn-sm btn-outline" type="button" data-add-question="single">+ Один вариант</button>' +
            '<button class="btn btn-sm btn-outline" type="button" data-add-question="multiple">+ Несколько вариантов</button>' +
            '<button class="btn btn-sm btn-outline" type="button" data-add-question="code">+ Задача на код</button>';

        container.querySelectorAll('[data-add-question]').forEach(function (button) {
            button.addEventListener('click', function () {
                openQuestionEditor(button.getAttribute('data-add-question'), null);
            });
        });
    }

    // Отображение редактора выбранного теста.
    function renderCourseTestEditor() {
        var editor = document.getElementById('course-test-editor');
        var title = document.getElementById('test-editor-title');
        var settings = document.getElementById('test-editor-settings');
        var counter = document.getElementById('test-editor-question-count');
        var questions = document.getElementById('test-editor-questions');

        if (!editor || !state.selectedTest) {
            return;
        }

        editor.hidden = false;
        title.textContent = 'Редактор теста: ' + state.selectedTest.title;

        settings.innerHTML =
            '<form id="course-test-settings-form">' +
            '<div class="form-field">' +
            '<label>Название</label>' +
            '<input name="title" type="text" minlength="3" maxlength="100" required value="' +
            App.escapeHtml(state.selectedTest.title) + '">' +
            '</div>' +
            '<div class="form-field">' +
            '<label>Описание</label>' +
            '<textarea name="description" maxlength="170" rows="3">' +
            App.escapeHtml(state.selectedTest.description || '') +
            '</textarea>' +
            '</div>' +
            '<div class="form-field">' +
            '<label>Длительность, минут</label>' +
            '<input name="durationMin" type="number" min="1" max="1440" required value="' +
            state.selectedTest.timeLimitMinutes + '">' +
            '</div>' +
            '<div class="form-field">' +
            '<label>Максимум баллов</label>' +
            '<input name="maxScore" type="number" min="1" max="1000" required value="' +
            state.selectedTest.maxScore + '">' +
            '</div>' +
            '<button class="btn btn-primary" type="submit">Сохранить настройки</button>' +
            '</form>';

        counter.textContent = 'Всего вопросов: ' + state.questions.length;

        renderQuestionActions();

        if (!state.questions.length) {
            questions.innerHTML =
                '<div class="empty-state"><p class="muted">В этом тесте пока нет вопросов.</p></div>';
        } else {
            questions.innerHTML = state.questions.map(function (question, index) {
                var answerHtml = '';

                if (question.options && question.options.length) {
                    answerHtml =
                        '<ul>' +
                        question.options.map(function (option) {
                            var correct = question.correctKeys.indexOf(option.key) !== -1;

                            return '<li>' +
                                App.escapeHtml(option.text) +
                                (correct ? ' <strong>(правильный ответ)</strong>' : '') +
                                '</li>';
                        }).join('') +
                        '</ul>';
                }

                if (question.type === 'code') {
                    answerHtml = '<p class="muted small">Тестовых случаев: ' +
                        (question.testCases || []).length + '</p>';
                }

                return '<article class="question-card">' +
                    '<div class="flex-between">' +
                    '<strong>Вопрос ' + (index + 1) + '</strong>' +
                    '<span class="badge badge-muted">' +
                    App.escapeHtml({
                        single: 'Один вариант',
                        multiple: 'Несколько вариантов',
                        code: 'Задача на код'
                    }[question.type] || question.type) +
                    '</span>' +
                    '</div>' +
                    '<p>' + App.escapeHtml(question.text) + '</p>' +
                    answerHtml +
                    '<div class="form-actions">' +
                    '<button class="btn btn-sm btn-outline" type="button" data-edit-question="' +
                    question.id + '">Редактировать</button>' +
                    '<button class="btn btn-sm btn-danger-soft" type="button" data-delete-question="' +
                    question.id + '">Удалить</button>' +
                    '</div>' +
                    '</article>';
            }).join('');
        }

        // Сохранение настроек теста.
        var form = document.getElementById('course-test-settings-form');

        form.addEventListener('submit', async function (event) {
            event.preventDefault();

            try {
                var values = App.formValues(form);
                var selectedId = state.selectedTest.id;

                await App.api.assignments.update(selectedId, {
                    name: values.title.trim(),
                    description: values.description.trim(),
                    maxScore: Number(values.maxScore),
                    timeLimitMinutes: Number(values.durationMin)
                });

                await load();
                await selectCourseTest(selectedId);

                App.notify('Настройки теста сохранены', 'success');
            } catch (error) {
                App.notify(error.message, 'error');
            }
        });

        // Редактирование вопроса.
        questions.querySelectorAll('[data-edit-question]').forEach(function (button) {
            button.addEventListener('click', function () {
                var question = App.getQuestionById(button.getAttribute('data-edit-question'));

                if (question) {
                    openQuestionEditor(question.type, question);
                }
            });
        });

        // Удаление вопроса.
        questions.querySelectorAll('[data-delete-question]').forEach(function (button) {
            button.addEventListener('click', async function () {
                var questionId = button.getAttribute('data-delete-question');

                if (!window.confirm('Удалить этот вопрос?')) {
                    return;
                }

                try {
                    await App.api.questions.remove(questionId);
                    App.notify('Вопрос удалён', 'success');
                    await selectCourseTest(state.selectedTest.id);
                } catch (error) {
                    App.notify(error.message, 'error');
                }
            });
        });
    }
    // Отображение заданий на код (все вопросы задания имеют тип Code).
    function renderAssignments() {
        var container = document.getElementById('course-modules');

        if (!container) {
            return;
        }

        var codeAssignments = state.assignments.filter(function (assignment) {
            return isCodeAssignment(assignment.id);
        });

        if (!codeAssignments.length) {
            container.innerHTML =
                '<div class="empty-state"><p class="muted">' +
                (canEditCourse()
                    ? 'Заданий на код пока нет. Добавьте в тест только вопросы типа «Задача на код», и он появится здесь.'
                    : 'В курсе пока нет заданий на код.') +
                '</p></div>';
            return;
        }

        container.innerHTML = codeAssignments.map(function (assignment, index) {
            var questions = assignmentQuestions(assignment.id).length;
            var action;

            if (canEditCourse()) {
                action =
                    '<button class="btn btn-sm btn-outline" type="button" data-edit-test="' +
                    assignment.id + '">Редактировать</button>';
            } else if (state.enrolled) {
                var attempt = latestAttempt(assignment.id);

                if (attempt && (attempt.status === 'completed' || attempt.status === 'expired')) {
                    action = '<span class="muted small">' +
                        (attempt.status === 'completed'
                            ? 'Задание выполнено'
                            : 'Время попытки истекло') +
                        '</span>';
                } else {
                    action = '<a class="btn btn-sm btn-soft" href="' +
                        App.url('test-taking.html') + '?id=' + assignment.id + '">' +
                        (attempt ? 'Продолжить задание' : 'Начать задание') +
                        '</a>';
                }
            } else {
                action = '<span class="muted small">Запишитесь на курс, чтобы выполнить задание</span>';
            }

            return '<article class="module-card">' +
                '<div class="module-head">' +
                '<span class="module-number">' + (index + 1) + '</span>' +
                '<div>' +
                '<h3>' + App.escapeHtml(assignment.title) + '</h3>' +
                '<p class="muted small">' +
                App.escapeHtml(assignment.description || 'Описание не заполнено.') +
                '</p>' +
                '</div>' +
                '</div>' +
                '<div class="course-meta">' +
                '<span>Задач: ' + questions + '</span>' +
                '<span>Максимум баллов: ' + assignment.maxScore + '</span>' +
                '<span>Время: ' + assignment.timeLimitMinutes + ' мин</span>' +
                '</div>' +
                '<div class="form-actions">' + action + '</div>' +
                '</article>';
        }).join('');

        Array.prototype.forEach.call(
            container.querySelectorAll('[data-edit-test]'),
            function (button) {
                button.addEventListener('click', function () {
                    selectCourseTest(button.getAttribute('data-edit-test'));
                });
            }
        );
    }

    // Отображение списка тестов курса.
    function renderTests() {
        var actions = document.getElementById('course-test-actions');

        if (actions) {
            actions.innerHTML = canEditCourse()
                ? '<button class="btn btn-primary" type="button" id="add-course-test">+ Создать тест</button>'
                : '';

            var addButton = document.getElementById('add-course-test');

            if (addButton) {
                addButton.addEventListener('click', openCreateTest);
            }
        }

        var tests = state.assignments.filter(function (assignment) {
            return !isCodeAssignment(assignment.id);
        });

        App.renderCards('course-tests', tests.map(function (assignment) {
            var questions = state.questionsCount[assignment.id] || 0;
            var subtitle = assignment.timeLimitMinutes + ' ' +
                App.plural(assignment.timeLimitMinutes, 'минута', 'минуты', 'минут') +
                ' · ' + questions + ' ' +
                App.plural(questions, 'вопрос', 'вопроса', 'вопросов');

            var href = '';
            var hrefLabel = '';

            if (canEditCourse()) {
                href = App.url('course-details.html') + '?id=' + state.course.id +
                    '&edit=' + assignment.id;
                hrefLabel = 'Редактировать тест';
            } else if (state.role === 'student' && state.enrolled) {
                var attempt = latestAttempt(assignment.id);

                if (attempt && (attempt.status === 'completed' || attempt.status === 'expired')) {
                    subtitle += attempt.status === 'completed'
                        ? ' · Тест пройден'
                        : ' · Время попытки истекло';
                } else {
                    href = App.url('test-taking.html') + '?id=' + assignment.id;
                    hrefLabel = attempt ? 'Продолжить тест' : 'Начать тест';
                }
            } else if (state.role === 'student') {
                subtitle += ' · Запишитесь на курс';
            }

            return {
                title: assignment.title,
                subtitle: subtitle,
                text: assignment.description || 'Описание не заполнено.',
                badge: 'published',
                href: href,
                hrefLabel: hrefLabel
            };
        }));
    }

    // Отображение преподавателя курса.
    function renderTeacher() {
        var container = document.getElementById('course-teacher');
        var teacher = App.getUserById(state.course.teacherId);

        if (!container) {
            return;
        }

        if (!teacher) {
            container.innerHTML =
                '<p class="muted">Преподаватель #' + state.course.teacherId + '</p>';
            return;
        }

        container.innerHTML =
            '<h3 class="card-title">' + App.escapeHtml(teacher.fullName) + '</h3>' +
            '<p class="card-subtitle">' + App.escapeHtml(App.statusLabel(teacher.role)) + '</p>' +
            '<p class="muted small">' + App.escapeHtml(teacher.email) + '</p>';
    }

    // Загрузка данных курса.
    async function load() {
        var id = courseId();

        if (!id) {
            App.showError('course-hero', new Error('Не указан идентификатор курса'));
            return;
        }

        state.role = App.getRole();
        state.user = App.getUser();

        state.course = await App.api.courses.get(id);
        state.assignments = await App.api.assignments.list(id);

        try {
            await loadMaterials();
        } catch (error) {
            state.materials = [];
        }

        try {
            var users = await App.api.users.lookup();
            App.cacheUsers(users);
        } catch (error) {
            App.cacheUsers([]);
        }

        if (state.role === 'student') {
            try {
                var enrollment = await App.api.courses.enrollment(id);
                state.enrolled = !!enrollment.enrolled;
            } catch (error) {
                state.enrolled = false;
            }

            try {
                var attempts = await App.api.attempts.list();

                state.attempts = {};

                attempts.forEach(function (attempt) {
                    var current = state.attempts[attempt.assignmentId];

                    if (!current ||
                        new Date(attempt.startedAt || 0) > new Date(current.startedAt || 0)) {
                        state.attempts[attempt.assignmentId] = attempt;
                    }
                });
            } catch (error) {
                state.attempts = {};
            }
        }

        state.questionsCount = {};
        state.questionsByAssignment = {};

        await Promise.all(state.assignments.map(async function (assignment) {
            try {
                var questions = await App.api.questions.list(assignment.id);
                state.questionsByAssignment[assignment.id] = questions;
                state.questionsCount[assignment.id] = questions.length;
            } catch (error) {
                state.questionsByAssignment[assignment.id] = [];
                state.questionsCount[assignment.id] = 0;
            }
        }));

        document.title = state.course.title + ' — OnlineTesting';

        document.getElementById('course-page-title').textContent =
            state.course.title;

        renderHero();
        renderTheory();
        renderAssignments();
        renderTests();
        renderTeacher();
    }

    // Инициализация страницы.
    async function init() {
        if (!App.requireRole(['student', 'teacher'])) {
            return;
        }

        try {
            await load();

            var editId = App.getQueryParam('edit');

            if (editId && canEditCourse()) {
                await selectCourseTest(editId);
            }
        } catch (error) {
            App.showError('course-hero', error);
        }
    }

    init();
})();
