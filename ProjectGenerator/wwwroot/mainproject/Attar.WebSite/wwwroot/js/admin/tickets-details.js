(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initialiseAdminSelect();
    });

    function initialiseAdminSelect() {
        const select = document.getElementById('assignedToId');
        if (!select) {
            return;
        }

        const $ = window.jQuery;
        if (!$ || !$.fn || typeof $.fn.select2 !== 'function') {
            return;
        }

        const $element = $(select);
        if ($element.hasClass('select2-hidden-accessible')) {
            $element.select2('destroy');
        }

        const placeholder = select.dataset.placeholder || 'انتخاب کارشناس (اختیاری)';
        const allowClear = select.dataset.allowClear === 'true';

        $element.select2({
            dir: document.dir || 'rtl',
            width: '100%',
            placeholder: placeholder,
            allowClear: allowClear,
            language: {
                noResults: function () {
                    return 'کارشناسی یافت نشد';
                },
                searching: function () {
                    return 'در حال جستجو...';
                }
            }
        });
    }
})();
