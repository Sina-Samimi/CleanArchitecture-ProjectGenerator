(function () {
    const doc = document;

    const persianToEnglishMap = new Map([
        ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
        ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9'],
        ['٠', '0'], ['١', '1'], ['٢', '2'], ['٣', '3'], ['٤', '4'],
        ['٥', '5'], ['٦', '6'], ['٧', '7'], ['٨', '8'], ['٩', '9']
    ]);

    const toEnglishDigits = (value) => {
        if (!value) {
            return '';
        }

        return value.replace(/[۰-۹٠-٩]/g, (digit) => persianToEnglishMap.get(digit) ?? digit);
    };

    if (window.jalaliDatepicker && typeof window.jalaliDatepicker.startWatch === 'function') {
        window.jalaliDatepicker.startWatch({ separator: '/', time: false });
    }

    const filterForm = doc.querySelector('[data-attempt-filter-form]');

    if (filterForm) {
        const pickers = Array.from(filterForm.querySelectorAll('[data-jalali-picker]'));

        const syncPicker = (picker) => {
            const input = picker.querySelector('[data-jalali-input]');
            const target = picker.querySelector('[data-jalali-target]');
            if (!input || !target) {
                return;
            }

            const normalized = toEnglishDigits(input.value.trim()).replace(/\//g, '-');
            target.value = normalized;
        };

        pickers.forEach((picker) => {
            const input = picker.querySelector('[data-jalali-input]');
            const target = picker.querySelector('[data-jalali-target]');
            const clearButton = picker.querySelector('[data-jalali-clear]');
            const openButton = picker.querySelector('[data-jalali-open]');

            if (target && input && target.value && !input.value) {
                input.value = target.value.replace(/-/g, '/');
            }

            input?.addEventListener('change', () => syncPicker(picker));
            input?.addEventListener('input', () => syncPicker(picker));

            clearButton?.addEventListener('click', (event) => {
                event.preventDefault();
                if (input) {
                    input.value = '';
                }

                if (target) {
                    target.value = '';
                }
            });

            openButton?.addEventListener('click', (event) => {
                event.preventDefault();
                if (!input) {
                    return;
                }

                input.focus();
                if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                    window.jalaliDatepicker.show(input);
                }
            });
        });

        filterForm.addEventListener('submit', () => {
            pickers.forEach(syncPicker);
        });
    }

    const deleteButtons = doc.querySelectorAll('[data-attempt-delete]');

    if (deleteButtons.length > 0) {
        const modalElement = doc.getElementById('attemptDeleteModal');
        const bootstrapModal = window.bootstrap?.Modal;

        if (modalElement && typeof bootstrapModal === 'function') {
            const confirmButton = modalElement.querySelector('[data-attempt-delete-confirm]');
            const modalInstance = bootstrapModal.getOrCreateInstance(modalElement);
            let pendingForm = null;

            modalElement.addEventListener('hidden.bs.modal', () => {
                pendingForm = null;
            });

            confirmButton?.addEventListener('click', () => {
                if (!pendingForm) {
                    modalInstance.hide();
                    return;
                }

                if (typeof pendingForm.requestSubmit === 'function') {
                    pendingForm.requestSubmit();
                } else {
                    pendingForm.submit();
                }

                modalInstance.hide();
                pendingForm = null;
            });

            deleteButtons.forEach((button) => {
                button.addEventListener('click', (event) => {
                    const form = button.closest('form');
                    if (!form) {
                        return;
                    }

                    event.preventDefault();
                    pendingForm = form;
                    modalInstance.show();
                });
            });
        } else {
            deleteButtons.forEach((button) => {
                button.addEventListener('click', (event) => {
                    if (!window.confirm('آیا از حذف این تلاش اطمینان دارید؟')) {
                        event.preventDefault();
                    }
                });
            });
        }
    }
})();
