(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('[data-site-settings-form]');
        if (!form) {
            return;
        }

        if (window.AdminRichEditor && typeof window.AdminRichEditor.initEditor === 'function') {
            // Initialize editor for ShortDescription
            var shortDescEditor = form.querySelector('#shortDescriptionEditor');
            if (shortDescEditor) {
                var shortDescTextarea = form.querySelector('textarea[data-editor-textarea][name="ShortDescription"]');
                if (shortDescTextarea) {
                    window.AdminRichEditor.initEditor(shortDescEditor, shortDescTextarea);
                }
            }

            // Initialize editor for TermsAndConditions
            var termsEditor = form.querySelector('#termsEditor');
            if (termsEditor) {
                var termsTextarea = form.querySelector('textarea[data-editor-textarea][name="TermsAndConditions"]');
                if (termsTextarea) {
                    window.AdminRichEditor.initEditor(termsEditor, termsTextarea);
                }
            }
        }

        setupLogoUploader(form);
        setupFaviconUploader(form);
    });

    function setupLogoUploader(form) {
        var uploader = form.querySelector('[data-logo-uploader]');
        if (!uploader) {
            return;
        }

        var input = uploader.querySelector('[data-logo-input]');
        var preview = uploader.querySelector('[data-logo-preview]');
        var placeholder = uploader.querySelector('[data-logo-placeholder]');
        var filename = uploader.querySelector('[data-logo-filename]');
        var removeCheckbox = uploader.querySelector('[data-logo-remove]');

        if (!input || !preview || !placeholder || !filename) {
            return;
        }

        input.addEventListener('change', function (e) {
            var file = e.target.files[0];
            if (!file) {
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                preview.style.backgroundImage = "url('" + e.target.result + "')";
                preview.classList.remove('featured-upload__preview--empty');
                placeholder.style.display = 'none';
                filename.textContent = file.name;
            };
            reader.readAsDataURL(file);
        });

        if (removeCheckbox) {
            removeCheckbox.addEventListener('change', function () {
                if (this.checked) {
                    preview.style.backgroundImage = '';
                    preview.classList.add('featured-upload__preview--empty');
                    placeholder.style.display = 'flex';
                    filename.textContent = 'لوگو انتخاب نشده است';
                    if (input) {
                        input.value = '';
                    }
                }
            });
        }
    }

    function setupFaviconUploader(form) {
        var uploader = form.querySelector('[data-favicon-uploader]');
        if (!uploader) {
            return;
        }

        var input = uploader.querySelector('[data-favicon-input]');
        var preview = uploader.querySelector('[data-favicon-preview]');
        var placeholder = uploader.querySelector('[data-favicon-placeholder]');
        var filename = uploader.querySelector('[data-favicon-filename]');
        var removeCheckbox = uploader.querySelector('[data-favicon-remove]');

        if (!input || !preview || !placeholder || !filename) {
            return;
        }

        input.addEventListener('change', function (e) {
            var file = e.target.files[0];
            if (!file) {
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                preview.style.backgroundImage = "url('" + e.target.result + "')";
                preview.classList.remove('featured-upload__preview--empty');
                placeholder.style.display = 'none';
                filename.textContent = file.name;
            };
            reader.readAsDataURL(file);
        });

        if (removeCheckbox) {
            removeCheckbox.addEventListener('change', function () {
                if (this.checked) {
                    preview.style.backgroundImage = '';
                    preview.classList.add('featured-upload__preview--empty');
                    placeholder.style.display = 'flex';
                    filename.textContent = 'Favicon انتخاب نشده است';
                    if (input) {
                        input.value = '';
                    }
                }
            });
        }
    }

})();

