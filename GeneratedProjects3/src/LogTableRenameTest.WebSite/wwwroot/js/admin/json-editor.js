(function (global) {
    'use strict';

    if (!global) {
        return;
    }

    function initJsonEditor(textarea) {
        if (!textarea) {
            return;
        }

        // Create wrapper
        var wrapper = document.createElement('div');
        wrapper.className = 'json-editor-wrapper';

        // Create toolbar
        var toolbar = document.createElement('div');
        toolbar.className = 'json-editor-toolbar';

        // Format button
        var formatBtn = document.createElement('button');
        formatBtn.type = 'button';
        formatBtn.className = 'btn btn-sm btn-outline-primary';
        formatBtn.innerHTML = '<i class="bi bi-code-square me-1"></i>فرمت کردن';
        formatBtn.addEventListener('click', function () {
            try {
                var value = textarea.value.trim();
                if (!value) {
                    return;
                }
                var parsed = JSON.parse(value);
                textarea.value = JSON.stringify(parsed, null, 2);
                hideError(textarea);
            } catch (e) {
                showError(textarea, 'خطا در فرمت کردن JSON: ' + e.message);
            }
        });

        // Validate button
        var validateBtn = document.createElement('button');
        validateBtn.type = 'button';
        validateBtn.className = 'btn btn-sm btn-outline-success';
        validateBtn.innerHTML = '<i class="bi bi-check-circle me-1"></i>اعتبارسنجی';
        validateBtn.addEventListener('click', function () {
            validateJson(textarea);
        });

        // Clear button
        var clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.className = 'btn btn-sm btn-outline-secondary';
        clearBtn.innerHTML = '<i class="bi bi-x-circle me-1"></i>پاک کردن';
        clearBtn.addEventListener('click', function () {
            if (confirm('آیا از پاک کردن محتوا اطمینان دارید؟')) {
                textarea.value = '';
                hideError(textarea);
            }
        });

        toolbar.appendChild(formatBtn);
        toolbar.appendChild(validateBtn);
        toolbar.appendChild(clearBtn);

        // Create error message container
        var errorContainer = document.createElement('div');
        errorContainer.className = 'json-editor-error';
        errorContainer.setAttribute('data-error-container', '');

        // Wrap textarea
        var parent = textarea.parentElement;
        parent.insertBefore(wrapper, textarea);
        wrapper.appendChild(toolbar);
        wrapper.appendChild(textarea);
        wrapper.appendChild(errorContainer);

        // Auto-validate on blur
        textarea.addEventListener('blur', function () {
            if (textarea.value.trim()) {
                validateJson(textarea);
            } else {
                hideError(textarea);
            }
        });

        // Auto-format on paste (with delay)
        var pasteTimeout;
        textarea.addEventListener('paste', function () {
            clearTimeout(pasteTimeout);
            pasteTimeout = setTimeout(function () {
                try {
                    var value = textarea.value.trim();
                    if (value && value.startsWith('{') || value.startsWith('[')) {
                        var parsed = JSON.parse(value);
                        textarea.value = JSON.stringify(parsed, null, 2);
                        hideError(textarea);
                    }
                } catch (e) {
                    // Ignore paste errors
                }
            }, 100);
        });

        // Initial validation if content exists
        if (textarea.value.trim()) {
            validateJson(textarea);
        }
    }

    function validateJson(textarea) {
        var value = textarea.value.trim();
        if (!value) {
            hideError(textarea);
            return true;
        }

        try {
            JSON.parse(value);
            hideError(textarea);
            return true;
        } catch (e) {
            showError(textarea, 'JSON نامعتبر: ' + e.message);
            return false;
        }
    }

    function showError(textarea, message) {
        var wrapper = textarea.closest('.json-editor-wrapper');
        if (!wrapper) {
            return;
        }

        var errorContainer = wrapper.querySelector('[data-error-container]');
        if (errorContainer) {
            errorContainer.textContent = message;
            errorContainer.style.display = 'block';
            textarea.classList.add('is-invalid');
        }
    }

    function hideError(textarea) {
        var wrapper = textarea.closest('.json-editor-wrapper');
        if (!wrapper) {
            return;
        }

        var errorContainer = wrapper.querySelector('[data-error-container]');
        if (errorContainer) {
            errorContainer.style.display = 'none';
            textarea.classList.remove('is-invalid');
        }
    }

    function initAllJsonEditors(form) {
        if (!form) {
            return;
        }

        var jsonTextareas = form.querySelectorAll('textarea[data-json-editor]');
        jsonTextareas.forEach(function (textarea) {
            initJsonEditor(textarea);
        });
    }

    // Export
    global.JsonEditor = {
        init: initAllJsonEditors,
        initEditor: initJsonEditor
    };
})(typeof window !== 'undefined' ? window : null);

