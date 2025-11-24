(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const searchInput = document.querySelector('[data-users-search]');
        const rows = Array.from(document.querySelectorAll('[data-user-row]'));
        const counter = document.querySelector('[data-users-counter]');
        const emptyState = document.querySelector('[data-users-empty]');

        const updateView = () => {
            const term = (searchInput?.value ?? '').trim().toLowerCase();
            let visible = 0;

            rows.forEach(row => {
                const text = row.dataset.searchText ?? '';
                const matches = term.length === 0 || text.includes(term);
                row.style.display = matches ? '' : 'none';
                if (matches) {
                    visible += 1;
                }
            });

            if (counter) {
                counter.textContent = visible;
            }

            if (emptyState) {
                emptyState.hidden = visible !== 0;
            }
        };

        if (searchInput) {
            searchInput.addEventListener('input', updateView);
            updateView();
        }

        if (window.jalaliDatepicker?.startWatch) {
            jalaliDatepicker.startWatch({
                date: true,
                persianDigits: true,
                changeMonthRotateYear: true,
                showCloseBtn: 'dynamic',
                topSpace: 10,
                bottomSpace: 30,
                overflowSpace: 10,
            });
        }

        const persianDigitMap = new Map([
            ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
            ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9']
        ]);

        const toEnglishDigits = (value) => value.replace(/[۰-۹]/g, (digit) => persianDigitMap.get(digit) ?? digit);

        const normalizeDateValue = (value) => {
            if (!value) {
                return '';
            }

            const sanitized = toEnglishDigits(value)
                .replace(/\u200f/g, '')
                .replace(/\s+/g, '')
                .replace(/\./g, '/')
                .replace(/-/g, '/');

            const match = sanitized.match(/(\d{4})[\/](\d{1,2})[\/](\d{1,2})/);
            if (!match) {
                return '';
            }

            const [, year, month, day] = match;
            const pad = (part, length) => part.padStart(length, '0');
            return `${pad(year, 4)}-${pad(month, 2)}-${pad(day, 2)}`;
        };

        document.querySelectorAll('[data-jalali-picker]').forEach(container => {
            const input = container.querySelector('[data-jalali-input]');
            const target = container.querySelector('[data-jalali-target]');
            const clearButton = container.querySelector('[data-jalali-clear]');
            const openButton = container.querySelector('[data-jalali-open]');

            if (!input || !target) {
                return;
            }

            const applyNormalized = () => {
                const normalized = normalizeDateValue(input.value);
                target.value = normalized;
                if (normalized) {
                    input.value = normalized.replace(/-/g, '/');
                } else if (!input.value) {
                    target.value = '';
                }
            };

            const applyInitialTarget = () => {
                const existing = (target.value ?? '').trim();
                if (existing && !input.value) {
                    input.value = existing.replace(/-/g, '/');
                }
                applyNormalized();
            };

            input.addEventListener('change', applyNormalized);
            input.addEventListener('input', applyNormalized);

            clearButton?.addEventListener('click', (event) => {
                event.preventDefault();
                input.value = '';
                target.value = '';
                applyNormalized();
            });

            openButton?.addEventListener('click', (event) => {
                event.preventDefault();
                input.focus();
                if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                    window.jalaliDatepicker.show(input);
                }
            });

            applyInitialTarget();
        });

        const filterForm = document.querySelector('[data-users-filter-form]');
        if (filterForm) {
            const statusSelect = filterForm.querySelector('[data-filter-status]');
            const includeDeactivated = filterForm.querySelector('[data-filter-include-deactivated]');
            const includeDeleted = filterForm.querySelector('[data-filter-include-deleted]');

            const syncHiddenInputs = () => {
                const status = statusSelect?.value ?? 'all';
                if (includeDeactivated) {
                    includeDeactivated.value = (status === 'all' || status === 'inactive').toString();
                }
                if (includeDeleted) {
                    includeDeleted.value = (status === 'all' || status === 'deleted').toString();
                }
            };

            statusSelect?.addEventListener('change', syncHiddenInputs);
            filterForm.addEventListener('reset', () => {
                window.setTimeout(() => {
                    if (statusSelect) {
                        statusSelect.value = 'all';
                    }
                    filterForm.querySelectorAll('[data-jalali-input]').forEach(input => {
                        input.value = '';
                    });
                    filterForm.querySelectorAll('[data-jalali-target]').forEach(input => {
                        input.value = '';
                    });
                    syncHiddenInputs();
                }, 0);
            });

            syncHiddenInputs();
        }

        const modalHost = document.querySelector('[data-user-form-modal]');

        function normalisePhone(value) {
            if (!value) {
                return '';
            }

            let digits = value.replace(/\D+/g, '');

            if (digits.startsWith('0098')) {
                digits = digits.substring(4);
            }

            if (digits.startsWith('98') && digits.length >= 12) {
                digits = digits.substring(2);
            }

            if (digits.startsWith('9') && digits.length === 10) {
                digits = `0${digits}`;
            }

            if (digits.length > 11 && digits.startsWith('0')) {
                digits = digits.slice(-11);
            }

            if (digits.length === 11 && digits.startsWith('09')) {
                return digits;
            }

            return value.replace(/\D+/g, '');
        }

        function bindUserForm(root) {
            const form = root.querySelector('[data-user-form]');
            if (!form) {
                return;
            }

            const submitButton = form.querySelector('[type="submit"]');
            const phoneInput = form.querySelector('[data-user-phone]');
            const activeToggle = form.querySelector('[data-user-active-toggle]');
            const deactivationField = form.querySelector('[data-user-deactivation]');
            const deactivationContainer = form.querySelector('[data-deactivation-container]');
            const avatarInput = form.querySelector('[data-avatar-input]');
            const avatarPreview = form.querySelector('[data-avatar-preview]');
            const initialAvatarHtml = avatarPreview ? avatarPreview.innerHTML : '';

            if (phoneInput) {
                const sanitiseInput = () => {
                    const digits = phoneInput.value.replace(/\D+/g, '');
                    if (phoneInput.value !== digits) {
                        const caret = phoneInput.selectionStart;
                        phoneInput.value = digits;
                        if (typeof phoneInput.setSelectionRange === 'function' && typeof caret === 'number') {
                            const next = Math.min(caret, digits.length);
                            phoneInput.setSelectionRange(next, next);
                        }
                    }
                };

                const applyNormalised = () => {
                    phoneInput.value = normalisePhone(phoneInput.value);
                };

                phoneInput.addEventListener('input', sanitiseInput);
                phoneInput.addEventListener('blur', applyNormalised);
                applyNormalised();
            }

            if (activeToggle && deactivationContainer) {
                const toggleDeactivation = () => {
                    const isActive = activeToggle.checked;
                    deactivationContainer.toggleAttribute('hidden', isActive);
                    if (isActive && deactivationField) {
                        deactivationField.value = '';
                    }
                };

                activeToggle.addEventListener('change', toggleDeactivation);
                toggleDeactivation();
            }

            if (avatarInput && avatarPreview) {
                avatarInput.addEventListener('change', () => {
                    if (avatarInput.files && avatarInput.files.length > 0) {
                        const file = avatarInput.files[0];
                        const reader = new FileReader();
                        reader.addEventListener('load', () => {
                            avatarPreview.innerHTML = '';
                            const image = document.createElement('img');
                            image.src = reader.result;
                            image.alt = 'پیش‌نمایش آواتار';
                            avatarPreview.appendChild(image);
                        });
                        reader.readAsDataURL(file);
                    } else {
                        avatarPreview.innerHTML = initialAvatarHtml;
                    }
                });
            }

            root.querySelectorAll('[data-role-select]').forEach(selectRoot => {
                const inputs = Array.from(selectRoot.querySelectorAll('[data-role-option]'));

                const refresh = () => {
                    inputs.forEach(input => {
                        const option = input.closest('.role-select__option');
                        if (option) {
                            option.classList.toggle('is-selected', input.checked);
                        }
                    });
                };

                inputs.forEach(input => {
                    input.addEventListener('change', refresh);
                });

                refresh();
            });

            form.addEventListener('submit', async (event) => {
                event.preventDefault();

                submitButton?.setAttribute('disabled', 'disabled');

                try {
                    const action = form.getAttribute('action') ?? window.location.href;
                    const method = (form.getAttribute('method') ?? 'post').toUpperCase();
                    const formData = new FormData(form);

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
                    showUserFormModal(html);
                } catch (error) {
                    console.error('خطا در ارسال فرم کاربر', error);
                } finally {
                    submitButton?.removeAttribute('disabled');
                }
            });
        }

        function showUserFormModal(html) {
            if (!modalHost) {
                return;
            }

            modalHost.innerHTML = html;
            bindUserForm(modalHost);

            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const modalInstance = bootstrap.Modal.getOrCreateInstance(modalHost);
                modalInstance.show();
            }
        }

        async function loadUserForm(url) {
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
                showUserFormModal(html);
            } catch (error) {
                console.error('خطا در بارگذاری فرم کاربر', error);
            }
        }

        if (modalHost) {
            modalHost.addEventListener('hidden.bs.modal', () => {
                modalHost.innerHTML = '';
            });

            document.addEventListener('click', (event) => {
                const trigger = event.target.closest('[data-user-modal-trigger]');
                if (!trigger) {
                    return;
                }

                const url = trigger.getAttribute('data-modal-url') || trigger.getAttribute('href');
                if (!url) {
                    return;
                }

                event.preventDefault();
                loadUserForm(url);
            });
        }
    });
})();
