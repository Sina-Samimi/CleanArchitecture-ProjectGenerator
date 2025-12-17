(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('[data-page-form]');
        if (!form) {
            return;
        }

        if (window.AdminRichEditor && typeof window.AdminRichEditor.init === 'function') {
            window.AdminRichEditor.init(form);
        }
        setupFeaturedUploader(form);
    });

    function setupFeaturedUploader(form) {
        var uploader = form.querySelector('[data-featured-uploader]');
        if (!uploader) {
            return;
        }

        var input = uploader.querySelector('[data-featured-input]');
        var preview = uploader.querySelector('[data-featured-preview]');
        var placeholder = uploader.querySelector('[data-featured-placeholder]');
        var filename = uploader.querySelector('[data-featured-filename]');
        var pathInput = form.querySelector('[data-featured-path]');
        var removeCheckbox = uploader.querySelector('[data-featured-remove]');

        if (!input || !preview || !placeholder || !filename || !pathInput) {
            return;
        }

        input.addEventListener('change', function (e) {
            var file = e.target.files[0];
            if (!file) {
                return;
            }

            if (!file.type.startsWith('image/')) {
                alert('لطفاً یک فایل تصویری انتخاب کنید.');
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                preview.style.backgroundImage = 'url(' + e.target.result + ')';
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
                    filename.textContent = 'تصویر انتخاب نشده است';
                } else {
                    var currentPath = pathInput.value;
                    if (currentPath) {
                        preview.style.backgroundImage = 'url(' + currentPath + ')';
                        preview.classList.remove('featured-upload__preview--empty');
                        placeholder.style.display = 'none';
                        filename.textContent = 'تصویر فعلی انتخاب شده';
                    }
                }
            });
        }
    }
})();

