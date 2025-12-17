(function (global) {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('[data-seller-form]');
        if (!form) {
            return;
        }

        if (global.AdminRichEditor && typeof global.AdminRichEditor.init === 'function') {
            global.AdminRichEditor.init(form, '.seller-bio-editor');
        }

        initialiseSelect2(form);
        initialiseAvatarUploader(form);
    });

    function initialiseSelect2(form) {
        var selectElement = form.querySelector('#UserId');
        if (!selectElement) {
            return;
        }

        var $ = global.jQuery;
        if (!$ || !$.fn || typeof $.fn.select2 !== 'function') {
            return;
        }

        var $element = $(selectElement);
        if ($element.hasClass('select2-hidden-accessible')) {
            $element.select2('destroy');
        }

        var placeholder = selectElement.dataset.placeholder || 'انتخاب کاربر سیستم';
        var allowClear = selectElement.dataset.allowClear === 'true';
        var dropdownParentSelector = selectElement.dataset.dropdownParent;
        var $dropdownParent = dropdownParentSelector ? $(dropdownParentSelector) : $element.parent();

        $element.select2({
            dir: document.dir || 'rtl',
            width: '100%',
            placeholder: placeholder,
            allowClear: allowClear,
            dropdownParent: $dropdownParent.length ? $dropdownParent : undefined,
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

    function initialiseAvatarUploader(form) {
        var uploader = form.querySelector('[data-avatar-uploader]');
        if (!uploader) {
            return;
        }

        var fileInput = uploader.querySelector('[data-avatar-input]');
        var previewContainer = uploader.querySelector('[data-avatar-preview]');
        var previewImage = previewContainer ? previewContainer.querySelector('[data-avatar-img]') : null;
        var placeholder = previewContainer ? previewContainer.querySelector('[data-avatar-placeholder]') : null;
        var clearButton = uploader.querySelector('[data-avatar-clear]');
        var urlInput = form.querySelector('[data-avatar-url-input]');
        var originalInput = form.querySelector('[data-avatar-original]');
        var activeObjectUrl = null;

        function showImage(src) {
            if (!previewContainer) {
                return;
            }

            if (!previewImage) {
                previewImage = document.createElement('img');
                previewImage.className = 'w-100 h-100 object-fit-cover';
                previewImage.setAttribute('data-avatar-img', '');
                previewContainer.appendChild(previewImage);
            }

            previewImage.src = src;
            previewImage.alt = form.querySelector('[name="DisplayName"]').value || 'Teacher avatar';

            if (placeholder) {
                placeholder.classList.add('d-none');
            }
        }

        function showPlaceholder() {
            if (!previewContainer) {
                return;
            }

            if (previewImage) {
                previewImage.remove();
                previewImage = null;
            }

            if (placeholder) {
                placeholder.classList.remove('d-none');
            } else {
                var placeholderElement = document.createElement('div');
                placeholderElement.className = 'd-flex align-items-center justify-content-center h-100 text-muted flex-column';
                placeholderElement.setAttribute('data-avatar-placeholder', '');
                placeholderElement.innerHTML = '<i class="bi bi-camera" aria-hidden="true"></i><small>تصویر معرفی انتخاب نشده است</small>';
                previewContainer.appendChild(placeholderElement);
                placeholder = placeholderElement;
            }
        }

        function revokeActiveUrl() {
            if (activeObjectUrl) {
                URL.revokeObjectURL(activeObjectUrl);
                activeObjectUrl = null;
            }
        }

        function resetIfNeeded() {
            var hasFile = fileInput && fileInput.files && fileInput.files.length > 0;
            var hasUrl = urlInput && urlInput.value && urlInput.value.trim() !== '';

            if (!hasFile && !hasUrl) {
                showPlaceholder();
            }
        }

        if (originalInput && !originalInput.value) {
            originalInput.value = urlInput && urlInput.value ? urlInput.value : '';
        }

        if (urlInput && urlInput.value) {
            showImage(urlInput.value);
        }

        if (fileInput) {
            fileInput.addEventListener('change', function () {
                revokeActiveUrl();

                if (!fileInput.files || fileInput.files.length === 0) {
                    resetIfNeeded();
                    return;
                }

                var file = fileInput.files[0];
                activeObjectUrl = URL.createObjectURL(file);
                showImage(activeObjectUrl);
            });
        }

        if (urlInput) {
            urlInput.addEventListener('input', function () {
                if (!urlInput.value) {
                    resetIfNeeded();
                    return;
                }

                revokeActiveUrl();
                showImage(urlInput.value);
            });
        }

        if (clearButton) {
            clearButton.addEventListener('click', function () {
                if (fileInput) {
                    fileInput.value = '';
                }

                revokeActiveUrl();
                showPlaceholder();
            });
        }

        form.addEventListener('reset', function () {
            revokeActiveUrl();
            if (fileInput) {
                fileInput.value = '';
            }

            if (urlInput) {
                urlInput.value = originalInput ? originalInput.value : '';
            }

            if (urlInput && urlInput.value) {
                showImage(urlInput.value);
            } else {
                showPlaceholder();
            }
        });
    }
})(typeof window !== 'undefined' ? window : (typeof globalThis !== 'undefined' ? globalThis : this));
