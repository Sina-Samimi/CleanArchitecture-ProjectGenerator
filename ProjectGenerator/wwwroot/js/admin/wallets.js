(function () {
    'use strict';

    const formWrapper = document.querySelector('[data-wallet-charge-form]');
    if (!formWrapper) {
        return;
    }

    initialiseUserSelect(formWrapper);
    preventDoubleSubmit(formWrapper);

    function initialiseUserSelect(wrapper) {
        const select = wrapper.querySelector('[data-wallet-user-select]');
        if (!select || !window.jQuery || typeof window.jQuery.fn.select2 !== 'function') {
            return;
        }

        const $element = window.jQuery(select);
        if ($element.hasClass('select2-hidden-accessible')) {
            $element.select2('destroy');
        }

        const placeholder = select.dataset.placeholder || 'انتخاب کاربر سیستم';
        const allowClear = select.dataset.allowClear === 'true';
        const dropdownParentSelector = select.dataset.dropdownParent;
        let $dropdownParent;

        if (dropdownParentSelector) {
            if (dropdownParentSelector === '[data-wallet-charge-form]') {
                $dropdownParent = window.jQuery(wrapper);
            } else {
                $dropdownParent = window.jQuery(dropdownParentSelector);
            }
        }

        $element.select2({
            dir: document.dir || 'rtl',
            width: '100%',
            placeholder: placeholder,
            allowClear: allowClear,
            dropdownParent: $dropdownParent && $dropdownParent.length ? $dropdownParent : undefined,
            language: {
                noResults: function () {
                    return 'کاربری یافت نشد';
                },
                searching: function () {
                    return 'در حال جستجو...';
                }
            }
        });
    }

    function preventDoubleSubmit(wrapper) {
        const form = wrapper.querySelector('form');
        if (!form) {
            return;
        }

        form.addEventListener('submit', () => {
            const submitButtons = form.querySelectorAll('button[type="submit"]');
            submitButtons.forEach(button => {
                button.disabled = true;
                button.classList.add('disabled');
            });
        });
    }
})();
