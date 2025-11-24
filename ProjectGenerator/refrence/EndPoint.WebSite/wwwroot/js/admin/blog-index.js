(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        setupBlogDeleteModal();
        initialiseJalaliPickers();
        bindFilterFormReset();
    });

    function setupBlogDeleteModal() {
        const modalElement = document.getElementById('deleteBlogModal');
        if (!modalElement) {
            return;
        }

        const namePlaceholder = modalElement.querySelector('[data-blog-delete-name]');
        const form = modalElement.querySelector('[data-blog-delete-form]');
        const idInput = form ? form.querySelector('input[name="id"]') : null;

        modalElement.addEventListener('show.bs.modal', function (event) {
            const trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            const blogId = trigger.getAttribute('data-blog-id');
            const blogTitle = trigger.getAttribute('data-blog-title');

            if (idInput) {
                idInput.value = blogId || '';
            }

            if (namePlaceholder) {
                namePlaceholder.textContent = blogTitle || '';
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (idInput) {
                idInput.value = '';
            }

            if (namePlaceholder) {
                namePlaceholder.textContent = '';
            }
        });
    }

    function initialiseJalaliPickers() {
        if (window.jalaliDatepicker && typeof window.jalaliDatepicker.startWatch === 'function') {
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

        const persianDigitMap = new Map([
            ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
            ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9']
        ]);

        const toEnglishDigits = (value) => value.replace(/[۰-۹]/g, (digit) => persianDigitMap.get(digit) || digit);

        const normaliseDateValue = (value) => {
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

            const applyNormalised = () => {
                const normalised = normaliseDateValue(input.value);
                target.value = normalised;
                if (normalised) {
                    input.value = normalised.replace(/-/g, '/');
                } else if (!input.value) {
                    target.value = '';
                }
            };

            const applyInitial = () => {
                const existing = (target.value || '').trim();
                if (existing && !input.value) {
                    input.value = existing.replace(/-/g, '/');
                }
                applyNormalised();
            };

            input.addEventListener('change', applyNormalised);
            input.addEventListener('input', applyNormalised);

            clearButton?.addEventListener('click', function (event) {
                event.preventDefault();
                input.value = '';
                target.value = '';
                applyNormalised();
            });

            openButton?.addEventListener('click', function (event) {
                event.preventDefault();
                input.focus();
                if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                    window.jalaliDatepicker.show(input);
                }
            });

            applyInitial();
        });
    }

    function bindFilterFormReset() {
        const filterForm = document.querySelector('[data-blog-filter-form]');
        if (!filterForm) {
            return;
        }

        filterForm.addEventListener('reset', function () {
            window.setTimeout(() => {
                filterForm.querySelectorAll('[data-jalali-input]').forEach(input => {
                    input.value = '';
                });
                filterForm.querySelectorAll('[data-jalali-target]').forEach(input => {
                    input.value = '';
                });
            }, 0);
        });
    }
})();
