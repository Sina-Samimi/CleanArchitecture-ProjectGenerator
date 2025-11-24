(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const modalHost = document.querySelector('[data-discount-modal]');
        if (!modalHost) {
            return;
        }

        function showModal(html) {
            modalHost.innerHTML = html;
            bindDiscountForm(modalHost);

            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const instance = bootstrap.Modal.getOrCreateInstance(modalHost);
                instance.show();
            }
        }

        async function loadModal(url) {
            try {
                const response = await fetch(url, {
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error(`Failed to load modal (${response.status})`);
                }

                const html = await response.text();
                showModal(html);
            } catch (error) {
                console.error('خطا در بارگذاری فرم کد تخفیف', error);
            }
        }

        function updateDiscountValueMeta(form) {
            const select = form.querySelector('[data-discount-type-select]');
            const label = form.querySelector('[data-discount-value-label]');
            const unit = form.querySelector('[data-discount-value-unit]');
            const input = form.querySelector('[data-discount-value]');

            if (!select) {
                return;
            }

            const refresh = () => {
                const type = parseInt(select.value, 10);
                if (type === 2) { // FixedAmount
                    label && (label.textContent = 'مقدار تخفیف (تومان)');
                    unit && (unit.textContent = 'تومان');
                    input && input.setAttribute('step', '1000');
                } else {
                    label && (label.textContent = 'مقدار تخفیف (%)');
                    unit && (unit.textContent = '%');
                    input && input.setAttribute('step', '0.1');
                }
            };

            select.addEventListener('change', refresh);
            refresh();
        }

        function updateGroupRowState(row) {
            const typeSelect = row.querySelector('[data-group-type]');
            const valueWrapper = row.querySelector('[data-group-value-wrapper]');
            const valueInput = row.querySelector('[data-group-value]');

            if (!typeSelect || !valueWrapper) {
                return;
            }

            const hasOverride = typeSelect.value !== '';
            valueWrapper.classList.toggle('is-disabled', !hasOverride);

            if (valueInput) {
                valueInput.disabled = !hasOverride;
                if (!hasOverride) {
                    valueInput.value = '';
                }
            }
        }

        const englishDigitMap = new Map([
            ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
            ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9'],
            ['٠', '0'], ['١', '1'], ['٢', '2'], ['٣', '3'], ['٤', '4'],
            ['٥', '5'], ['٦', '6'], ['٧', '7'], ['٨', '8'], ['٩', '9']
        ]);

        function toEnglishDigits(value) {
            if (!value) {
                return '';
            }

            return value.replace(/[۰-۹٠-٩]/g, digit => englishDigitMap.get(digit) ?? digit);
        }

        function normalizeDateValue(value) {
            const result = { normalized: '', sanitized: '' };

            if (!value) {
                return result;
            }

            const sanitized = toEnglishDigits(value)
                .replace(/\u200f/g, '')
                .replace(/\u200e/g, '')
                .replace(/\s+/g, '')
                .replace(/\./g, '/')
                .replace(/-/g, '/');

            result.sanitized = sanitized;

            const match = sanitized.match(/(\d{4})[\/](\d{1,2})[\/](\d{1,2})/);
            if (!match) {
                return result;
            }

            const [, year, month, day] = match;
            const pad = (part, length) => part.padStart(length, '0');
            result.normalized = `${pad(year, 4)}-${pad(month, 2)}-${pad(day, 2)}`;
            return result;
        }

        function normalizeTimeValue(value) {
            const result = { normalized: '', sanitized: '' };

            if (!value) {
                return result;
            }

            const sanitized = toEnglishDigits(value)
                .replace(/\u200f/g, '')
                .replace(/\u200e/g, '')
                .replace(/\s+/g, '')
                .replace(/\./g, ':')
                .replace(/-/g, ':')
                .trim();

            result.sanitized = sanitized;

            if (!sanitized) {
                return result;
            }

            let candidate = sanitized;
            if (candidate.length === 3 && !candidate.includes(':')) {
                candidate = `${candidate.slice(0, 1)}:${candidate.slice(1)}`;
            } else if (candidate.length === 4 && !candidate.includes(':')) {
                candidate = `${candidate.slice(0, 2)}:${candidate.slice(2)}`;
            }

            const match = candidate.match(/^(\d{1,2}):(\d{1,2})$/);
            if (!match) {
                return result;
            }

            let hours = parseInt(match[1], 10);
            let minutes = parseInt(match[2], 10);

            if (Number.isNaN(hours) || Number.isNaN(minutes)) {
                return result;
            }

            hours = Math.min(Math.max(hours, 0), 23);
            minutes = Math.min(Math.max(minutes, 0), 59);

            result.normalized = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
            return result;
        }

        function bindSchedulePickers(form) {
            if (!form) {
                return;
            }

            if (window.jalaliDatepicker?.startWatch) {
                window.jalaliDatepicker.startWatch({
                    date: true,
                    time: false,
                    persianDigits: true,
                    showCloseBtn: 'dynamic',
                    topSpace: 10,
                    bottomSpace: 30,
                    overflowSpace: 10
                });
            }

            form.querySelectorAll('[data-discount-schedule]').forEach(container => {
                const jalaliInput = container.querySelector('[data-jalali-input]');
                const jalaliTarget = container.querySelector('[data-jalali-target]');
                const jalaliClear = container.querySelector('[data-jalali-clear]');
                const jalaliOpen = container.querySelector('[data-jalali-open]');
                const timeInput = container.querySelector('[data-time-input]');
                const timeTarget = container.querySelector('[data-time-target]');
                const timeClear = container.querySelector('[data-time-clear]');
                const scheduleType = container.getAttribute('data-discount-schedule');
                const optionalTime = scheduleType === 'end';

                const applyNormalizedDate = () => {
                    if (!jalaliTarget || !jalaliInput) {
                        return;
                    }

                    const { normalized, sanitized } = normalizeDateValue(jalaliInput.value);
                    jalaliTarget.value = normalized || sanitized;

                    jalaliInput.value = sanitized || '';
                };

                const applyNormalizedTime = () => {
                    if (!timeTarget || !timeInput) {
                        return;
                    }

                    const { normalized, sanitized } = normalizeTimeValue(timeInput.value);
                    timeTarget.value = normalized || sanitized;

                    if (normalized) {
                        timeInput.value = normalized;
                    } else if (!optionalTime && !timeInput.value) {
                        timeInput.value = '00:00';
                        timeTarget.value = '00:00';
                    } else if (!normalized && sanitized) {
                        timeInput.value = sanitized;
                    }
                };

                const applyInitialValues = () => {
                    if (jalaliTarget && jalaliInput && jalaliTarget.value && !jalaliInput.value) {
                        jalaliInput.value = jalaliTarget.value.replace(/-/g, '/');
                    }

                    if (timeTarget && timeInput && timeTarget.value && !timeInput.value) {
                        timeInput.value = timeTarget.value;
                    }

                    applyNormalizedDate();
                    applyNormalizedTime();
                };

                jalaliInput?.addEventListener('change', applyNormalizedDate);
                jalaliInput?.addEventListener('input', applyNormalizedDate);

                jalaliClear?.addEventListener('click', event => {
                    event.preventDefault();
                    if (jalaliInput) {
                        jalaliInput.value = '';
                    }
                    if (jalaliTarget) {
                        jalaliTarget.value = '';
                    }
                });

                jalaliOpen?.addEventListener('click', event => {
                    event.preventDefault();
                    if (jalaliInput) {
                        jalaliInput.focus();
                        if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                            window.jalaliDatepicker.show(jalaliInput);
                        }
                    }
                });

                timeInput?.addEventListener('blur', applyNormalizedTime);
                timeInput?.addEventListener('change', applyNormalizedTime);
                timeInput?.addEventListener('input', () => {
                    if (!optionalTime && !timeInput.value) {
                        return;
                    }

                    applyNormalizedTime();
                });

                timeClear?.addEventListener('click', event => {
                    event.preventDefault();
                    if (!timeInput || !timeTarget) {
                        return;
                    }

                    if (optionalTime) {
                        timeInput.value = '';
                        timeTarget.value = '';
                    } else {
                        timeInput.value = '00:00';
                        timeTarget.value = '00:00';
                    }
                });

                applyInitialValues();
            });
        }

        function reindexGroupRows(container) {
            const rows = Array.from(container.querySelectorAll('[data-group-row]'));
            rows.forEach((row, index) => {
                const inputs = row.querySelectorAll('[name^="GroupRules["]');
                inputs.forEach(input => {
                    const name = input.getAttribute('name');
                    if (!name) {
                        return;
                    }

                    const updatedName = name.replace(/GroupRules\[(\d+)\]/g, `GroupRules[${index}]`);
                    input.setAttribute('name', updatedName);

                    const id = input.getAttribute('id');
                    if (id) {
                        const updatedId = id.replace(/GroupRules_(\d+)__/g, `GroupRules_${index}__`);
                        input.setAttribute('id', updatedId);
                    }
                });

                const labels = row.querySelectorAll('label[for]');
                labels.forEach(label => {
                    const target = label.getAttribute('for');
                    if (!target) {
                        return;
                    }

                    const updatedTarget = target.replace(/GroupRules_(\d+)__/g, `GroupRules_${index}__`);
                    label.setAttribute('for', updatedTarget);
                });

                const validations = row.querySelectorAll('[data-valmsg-for]');
                validations.forEach(element => {
                    const field = element.getAttribute('data-valmsg-for');
                    if (!field) {
                        return;
                    }

                    const updatedField = field.replace(/GroupRules\[(\d+)\]/g, `GroupRules[${index}]`);
                    element.setAttribute('data-valmsg-for', updatedField);
                });

                const headerLabel = row.querySelector('.discount-group-form__header span');
                if (headerLabel) {
                    headerLabel.textContent = `گروه شماره ${index + 1}`;
                }
            });
        }

        function refreshEmptyState(form) {
            const container = form.querySelector('[data-discount-group-list]');
            const emptyState = form.querySelector('[data-discount-groups-empty]');
            if (!container || !emptyState) {
                return;
            }

            const hasRows = container.querySelector('[data-group-row]') !== null;
            if (hasRows) {
                emptyState.setAttribute('hidden', 'hidden');
            } else {
                emptyState.removeAttribute('hidden');
            }
        }

        function bindGroupRow(row, container) {
            const removeButton = row.querySelector('[data-discount-group-remove]');
            const typeSelect = row.querySelector('[data-group-type]');

            removeButton?.addEventListener('click', event => {
                event.preventDefault();
                row.remove();
                reindexGroupRows(container);
                refreshEmptyState(container.closest('form'));
            });

            typeSelect?.addEventListener('change', () => updateGroupRowState(row));
            updateGroupRowState(row);
        }

        function bindGroupManager(form) {
            const container = form.querySelector('[data-discount-group-list]');
            const template = form.querySelector('[data-discount-group-template]');
            const addButton = form.querySelector('[data-discount-group-add]');

            if (!container || !template) {
                return;
            }

            const rows = Array.from(container.querySelectorAll('[data-group-row]'));
            rows.forEach(row => bindGroupRow(row, container));
            reindexGroupRows(container);
            refreshEmptyState(form);

            addButton?.addEventListener('click', event => {
                event.preventDefault();
                const clone = template.content.firstElementChild.cloneNode(true);
                container.appendChild(clone);
                reindexGroupRows(container);
                bindGroupRow(clone, container);
                refreshEmptyState(form);
            });
        }

        function bindFormSubmission(form) {
            const submitButton = form.querySelector('[type="submit"]');

            form.addEventListener('submit', async event => {
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
                    showModal(html);
                } catch (error) {
                    console.error('خطا در ذخیره کد تخفیف', error);
                } finally {
                    submitButton?.removeAttribute('disabled');
                }
            });
        }

        function bindDiscountForm(root) {
            const form = root.querySelector('[data-discount-form]');
            if (!form) {
                return;
            }

            updateDiscountValueMeta(form);
            bindSchedulePickers(form);
            bindGroupManager(form);
            bindFormSubmission(form);
        }

        modalHost.addEventListener('hidden.bs.modal', () => {
            modalHost.innerHTML = '';
        });

        document.addEventListener('click', event => {
            const trigger = event.target.closest('[data-discount-modal-trigger]');
            if (!trigger) {
                return;
            }

            event.preventDefault();
            const url = trigger.getAttribute('data-modal-url') || trigger.getAttribute('href');
            if (!url) {
                return;
            }

            loadModal(url);
        });
    });
})();
