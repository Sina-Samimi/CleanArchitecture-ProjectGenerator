(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const modalHost = document.querySelector('[data-permission-modal]');
        const filterForm = document.querySelector('[data-permissions-filter-form]');
        const pageSizeForms = document.querySelectorAll('[data-permissions-page-size-form]');

        function showModal(html) {
            if (!modalHost) {
                return;
            }

            modalHost.innerHTML = html;
            bindForm(modalHost);

            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const modalInstance = bootstrap.Modal.getOrCreateInstance(modalHost);
                modalInstance.show();
            }
        }

        async function loadModal(url) {
            if (!url) {
                return;
            }

            try {
                const response = await fetch(url, {
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error(`Failed to load modal: ${response.status}`);
                }

                const html = await response.text();
                showModal(html);
            } catch (error) {
                console.error('خطا در بارگذاری مودال مجوز', error);
            }
        }

        function bindGroupSelection(form) {
            const groupSelect = form.querySelector('[data-permission-group-select]');
            const keyField = form.querySelector('[data-permission-group-key-field]');
            const groupLabelInput = form.querySelector('[data-permission-group-label]');
            const labelContainer = form.querySelector('[data-permission-group-label-container]');
            const customKeyContainer = form.querySelector('[data-permission-group-custom-key-container]');
            const customKeyInput = form.querySelector('[data-permission-group-custom-key]');

            if (!groupSelect || !keyField || !groupLabelInput) {
                return;
            }

            const entries = new Map();
            groupSelect.querySelectorAll('option').forEach(option => {
                const value = option.value?.trim();
                if (!value) {
                    return;
                }

                entries.set(value.toLowerCase(), {
                    displayName: option.dataset.displayName ?? option.textContent ?? value,
                    isCustom: option.dataset.isCustom === 'true'
                });
            });

            function toggleCustomKey(show) {
                if (!customKeyContainer) {
                    return;
                }

                if (show) {
                    customKeyContainer.classList.remove('d-none');
                } else {
                    customKeyContainer.classList.add('d-none');
                }
            }

            function applySelection() {
                const selectedValue = groupSelect.value?.trim() ?? '';

                if (!selectedValue) {
                    toggleCustomKey(true);

                    const customValue = customKeyInput?.value?.trim() ?? '';
                    keyField.value = customValue;

                    if (!customValue) {
                        groupLabelInput.removeAttribute('readonly');
                        groupLabelInput.value = '';
                        labelContainer?.classList.remove('d-none');
                        return;
                    }

                    if (!groupLabelInput.value || groupLabelInput.hasAttribute('readonly')) {
                        groupLabelInput.value = customValue;
                    }

                    groupLabelInput.removeAttribute('readonly');
                    labelContainer?.classList.remove('d-none');
                    return;
                }

                toggleCustomKey(false);
                keyField.value = selectedValue;

                const entry = entries.get(selectedValue.toLowerCase());
                if (entry) {
                    if (entry.displayName) {
                        groupLabelInput.value = entry.displayName;
                    }

                    if (entry.isCustom) {
                        groupLabelInput.removeAttribute('readonly');
                    } else {
                        groupLabelInput.setAttribute('readonly', 'readonly');
                    }
                } else {
                    groupLabelInput.removeAttribute('readonly');
                }

                labelContainer?.classList.remove('d-none');
            }

            function syncCustomKey() {
                const value = customKeyInput?.value?.trim() ?? '';
                keyField.value = value;

                if (!value) {
                    groupLabelInput.removeAttribute('readonly');
                    groupLabelInput.value = '';
                    return;
                }

                if (!groupLabelInput.value || groupLabelInput.hasAttribute('readonly')) {
                    groupLabelInput.value = value;
                }

                groupLabelInput.removeAttribute('readonly');
            }

            groupSelect.addEventListener('change', () => {
                applySelection();
                if (!groupSelect.value) {
                    syncCustomKey();
                }
            });

            customKeyInput?.addEventListener('input', () => {
                syncCustomKey();
            });

            if (!groupSelect.value && customKeyInput && !customKeyInput.value && keyField.value) {
                customKeyInput.value = keyField.value;
            }

            if (groupSelect.value) {
                applySelection();
            } else {
                syncCustomKey();
                applySelection();
            }
        }

        function bindForm(root) {
            const form = root.querySelector('[data-permission-form]');
            if (!form) {
                return;
            }

            bindGroupSelection(form);

            const submitButton = form.querySelector('[type="submit"]');

            form.addEventListener('submit', async event => {
                event.preventDefault();

                submitButton?.setAttribute('disabled', 'disabled');

                try {
                    const formData = new FormData(form);
                    const action = form.getAttribute('action') ?? window.location.href;
                    const method = (form.getAttribute('method') ?? 'post').toUpperCase();

                    const response = await fetch(action, {
                        method,
                        body: formData,
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });

                    const contentType = response.headers.get('content-type') ?? '';

                    if (contentType.includes('application/json')) {
                        const payload = await response.json();
                        if (payload.success && payload.redirectUrl) {
                            window.location.href = payload.redirectUrl;
                            return;
                        }
                    }

                    const html = await response.text();
                    showModal(html);
                } catch (error) {
                    console.error('خطا در ذخیره مجوز', error);
                } finally {
                    submitButton?.removeAttribute('disabled');
                }
            });
        }

        function bindDeleteForms() {
            const showDeleteError = (message) => {
                const errorMessage = message || 'حذف مجوز با خطا مواجه شد. لطفاً دوباره تلاش کنید.';
                if (window.AppAlert && typeof window.AppAlert.show === 'function') {
                    window.AppAlert.show({
                        title: 'خطا در حذف مجوز',
                        message: errorMessage,
                        type: 'danger',
                        confirmText: 'متوجه شدم'
                    });
                } else {
                    alert(errorMessage);
                }
            };

            document.addEventListener('submit', (event) => {
                const form = event.target.closest('[data-permission-delete-form]');
                if (!form) {
                    return;
                }

                event.preventDefault();

                const submitButton = form.querySelector('[data-permission-delete-button]');
                if (submitButton?.hasAttribute('disabled')) {
                    return;
                }

                const permissionName = form.getAttribute('data-permission-name') || 'این مجوز';

                const executeDeletion = async () => {
                    submitButton?.setAttribute('disabled', 'disabled');

                    try {
                        const formData = new FormData(form);
                        const action = form.getAttribute('action') ?? window.location.href;
                        const response = await fetch(action, {
                            method: 'POST',
                            body: formData,
                            headers: {
                                'X-Requested-With': 'XMLHttpRequest'
                            }
                        });

                        const payload = await response.json();
                        if (payload.success && payload.redirectUrl) {
                            window.location.href = payload.redirectUrl;
                            return;
                        }

                        submitButton?.removeAttribute('disabled');
                        if (payload.error) {
                            showDeleteError(payload.error);
                        }
                    } catch (error) {
                        console.error('خطا در حذف مجوز', error);
                        submitButton?.removeAttribute('disabled');
                        showDeleteError();
                    }
                };

                if (window.AppAlert && typeof window.AppAlert.show === 'function') {
                    window.AppAlert.show({
                        title: 'حذف مجوز',
                        message: `آیا از حذف «${permissionName}» اطمینان دارید؟`,
                        type: 'danger',
                        confirmText: 'بله، حذف شود',
                        cancelText: 'خیر، انصراف',
                        showCancel: true,
                        onConfirm: executeDeletion
                    });
                } else if (window.confirm(`آیا از حذف «${permissionName}» اطمینان دارید؟`)) {
                    executeDeletion();
                }
            });
        }

        if (modalHost) {
            modalHost.addEventListener('hidden.bs.modal', () => {
                modalHost.innerHTML = '';
            });
        }

        document.addEventListener('click', event => {
            const trigger = event.target.closest('[data-permission-modal-trigger]');
            if (!trigger) {
                return;
            }

            event.preventDefault();
            const url = trigger.getAttribute('data-modal-url');
            if (!url) {
                return;
            }

            loadModal(url);
        });

        document.addEventListener('click', event => {
            const toggle = event.target.closest('[data-permission-subtoggle]');
            if (!toggle) {
                return;
            }

            event.preventDefault();

            const targetSelector = toggle.getAttribute('data-target');
            if (!targetSelector) {
                return;
            }

            const target = document.querySelector(targetSelector);
            if (!target) {
                return;
            }

            const isExpanded = toggle.getAttribute('aria-expanded') === 'true';
            const collapsedLabel = toggle.getAttribute('data-collapsed-label');
            const expandedLabel = toggle.getAttribute('data-expanded-label');

            if (isExpanded) {
                toggle.setAttribute('aria-expanded', 'false');
                target.setAttribute('hidden', 'hidden');
                if (collapsedLabel) {
                    toggle.textContent = collapsedLabel;
                }
            } else {
                toggle.setAttribute('aria-expanded', 'true');
                target.removeAttribute('hidden');
                if (expandedLabel) {
                    toggle.textContent = expandedLabel;
                }
            }
        });

        bindDeleteForms();

        if (filterForm) {
            const coreToggle = filterForm.querySelector('[data-permissions-filter-core-toggle]');
            const coreField = filterForm.querySelector('[data-permissions-filter-core-field]');
            const customToggle = filterForm.querySelector('[data-permissions-filter-custom-toggle]');
            const customField = filterForm.querySelector('[data-permissions-filter-custom-field]');

            const syncToggleValue = (toggle, field) => {
                if (!field) {
                    return;
                }

                const isChecked = toggle instanceof HTMLInputElement && toggle.checked;
                field.value = isChecked ? 'true' : 'false';
            };

            if (coreToggle && coreField) {
                coreToggle.addEventListener('change', () => {
                    syncToggleValue(coreToggle, coreField);
                });
                syncToggleValue(coreToggle, coreField);
            }

            if (customToggle && customField) {
                customToggle.addEventListener('change', () => {
                    syncToggleValue(customToggle, customField);
                });
                syncToggleValue(customToggle, customField);
            }

            filterForm.addEventListener('submit', () => {
                syncToggleValue(coreToggle, coreField);
                syncToggleValue(customToggle, customField);

                const pageField = filterForm.querySelector('input[name="page"]');
                if (pageField instanceof HTMLInputElement) {
                    pageField.value = '1';
                }
            });
        }

        pageSizeForms.forEach(form => {
            const select = form.querySelector('select[name="pageSize"]');
            if (!select) {
                return;
            }

            select.addEventListener('change', () => {
                const pageField = form.querySelector('input[name="page"]');
                if (pageField instanceof HTMLInputElement) {
                    pageField.value = '1';
                }

                form.submit();
            });
        });
    });
})();
