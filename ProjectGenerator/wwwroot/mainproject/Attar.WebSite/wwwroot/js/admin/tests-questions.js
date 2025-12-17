(() => {
    'use strict';

    const state = {
        optionCount: 0,
        editOptionCount: 0
    };

const selectors = {
    addContainer: '[data-options-container]',
    editContainer: '[data-edit-options]',
    editModal: '#editQuestionModal',
    editFormFields: {
        id: '#editQuestionId',
        text: '#editQuestionText',
        type: '#editQuestionType',
        order: '#editQuestionOrder',
        score: '#editQuestionScore'
    }
};

    const namePattern = /Options\[\d+\]/g;

    function enhanceOptionNode(node, option) {
        if (!node) {
            return;
        }

        const textInput = node.querySelector('[data-option-text]');
        if (textInput) {
            textInput.value = option?.text ?? '';
        }

        const checkbox = node.querySelector('[data-option-correct]');
        if (checkbox) {
            checkbox.checked = !!option?.isCorrect;
        }

        const idInput = node.querySelector('[data-option-id]');
        if (idInput) {
            idInput.value = option?.id ?? '';
        }

        const scoreInput = node.querySelector('[data-option-score]');
        if (scoreInput) {
            scoreInput.value = option?.score ?? '';
        }

        const explanationInput = node.querySelector('[data-option-explanation]');
        if (explanationInput) {
            explanationInput.value = option?.explanation ?? '';
        }

        const imageInput = node.querySelector('[data-option-image]');
        if (imageInput) {
            imageInput.value = option?.imageUrl ?? '';
        }
    }

    function getElement(selector) {
        return document.querySelector(selector);
    }

    function createOptionNode(index, isEdit) {
    const wrapper = document.createElement('div');
    wrapper.className = 'option-item mb-2';
    const removeHandler = isEdit
        ? 'AdminTestsQuestions.removeEditOption(this)'
        : 'AdminTestsQuestions.removeOption(this)';

    wrapper.innerHTML = `
        <div class="option-body">
            <input type="hidden" data-option-id name="Options[${index}].Id" value="" />
            <div class="input-group mb-2">
                <input type="text" data-option-text name="Options[${index}].Text" class="form-control" placeholder="متن گزینه" />
                <div class="input-group-text">
                    <input type="checkbox" data-option-correct name="Options[${index}].IsCorrect" class="form-check-input mt-0" value="true" />
                    <input type="hidden" name="Options[${index}].IsCorrect" value="false" />
                    <span class="ms-2">صحیح</span>
                </div>
                <button type="button" class="btn btn-outline-danger" onclick="${removeHandler}">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
            <div class="row g-2">
                <div class="col-md-3">
                    <div class="input-group input-group-sm">
                        <span class="input-group-text">امتیاز</span>
                        <input type="number" data-option-score name="Options[${index}].Score" class="form-control" min="0" />
                    </div>
                </div>
                <div class="col-md-5">
                    <div class="input-group input-group-sm">
                        <span class="input-group-text">توضیح</span>
                        <input type="text" data-option-explanation name="Options[${index}].Explanation" class="form-control" placeholder="توضیح گزینه" />
                    </div>
                </div>
                <div class="col-md-4">
                    <input type="text" data-option-image name="Options[${index}].ImageUrl" class="form-control form-control-sm" placeholder="آدرس تصویر (اختیاری)" />
                </div>
            </div>
        </div>`;

        return wrapper;
    }

    function renumberOptions(container) {
        if (!container) {
            return 0;
        }

        const items = Array.from(container.querySelectorAll('.option-item'));
        items.forEach((item, idx) => {
            const fields = item.querySelectorAll('[name^="Options["]');
            fields.forEach(field => {
                const currentName = field.getAttribute('name');
                if (!currentName) {
                    return;
                }
                field.setAttribute('name', currentName.replace(namePattern, `Options[${idx}]`));
            });
        });

        return items.length;
    }

function addOptionNode(container, stateKey, prefill) {
        if (!container) {
            return;
        }

    const isEdit = container.matches(selectors.editContainer);
    state[stateKey] = renumberOptions(container);
    const index = state[stateKey];
        const node = createOptionNode(index, isEdit);
        container.appendChild(node);

    enhanceOptionNode(node, prefill);

        state[stateKey] = renumberOptions(container);
    }

function removeOptionNode(button, containerSelector, stateKey) {
        if (!button) {
            return;
        }

        const item = button.closest('.option-item');
        if (!item) {
            return;
        }

        item.remove();
    const container = getElement(containerSelector);
    const isEdit = containerSelector === selectors.editContainer;

    if (container && !container.querySelector('.option-item')) {
        const fallback = createOptionNode(0, isEdit);
        container.appendChild(fallback);
        enhanceOptionNode(fallback);
    }

    state[stateKey] = renumberOptions(container);
    }

    function parseQuestionPayload(element) {
        if (!element) {
            return null;
        }

        const raw = element.getAttribute('data-question');
        if (!raw) {
            return null;
        }

        try {
            return JSON.parse(raw);
        }
        catch (error) {
            console.error('Invalid question payload', error);
            return null;
        }
    }

    function setField(selector, value) {
        const input = getElement(selector);
        if (!input) {
            return;
        }
        input.value = value ?? '';
    }

    function ensureInitialCounts() {
    const addContainer = getElement(selectors.addContainer);
    if (addContainer && !addContainer.querySelector('.option-item')) {
        const node = createOptionNode(0, false);
        addContainer.appendChild(node);
        enhanceOptionNode(node);
    }
    state.optionCount = renumberOptions(addContainer);

    const editContainer = getElement(selectors.editContainer);
    state.editOptionCount = renumberOptions(editContainer);
    }

    const api = {
        addOption() {
            ensureInitialCounts();
            addOptionNode(getElement(selectors.addContainer), 'optionCount');
        },

        removeOption(button) {
            removeOptionNode(button, selectors.addContainer, 'optionCount');
        },

        addEditOption(prefill) {
            ensureInitialCounts();
            addOptionNode(getElement(selectors.editContainer), 'editOptionCount', prefill);
        },

        removeEditOption(button) {
            removeOptionNode(button, selectors.editContainer, 'editOptionCount');
        },

        editQuestion(button) {
            ensureInitialCounts();
            const payload = parseQuestionPayload(button);
            if (!payload) {
                return;
            }

            setField(selectors.editFormFields.id, payload.id ?? '');
            setField(selectors.editFormFields.text, payload.text ?? '');
            setField(selectors.editFormFields.type, payload.questionType ?? payload.type ?? '');
            setField(selectors.editFormFields.order, payload.order ?? '');
            setField(selectors.editFormFields.score, payload.score ?? '');

            const container = getElement(selectors.editContainer);
            if (container) {
                container.innerHTML = '<h6>گزینه‌ها</h6>';
            }
            state.editOptionCount = 0;

            if (Array.isArray(payload.options) && payload.options.length > 0) {
                payload.options.forEach(option => {
                    api.addEditOption(option);
                });
            } else {
                api.addEditOption();
            }

            const modalElement = getElement(selectors.editModal);
            if (modalElement && typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
                modal.show();
            }
        }
    };

    window.AdminTestsQuestions = api;

    document.addEventListener('DOMContentLoaded', ensureInitialCounts);
})();
