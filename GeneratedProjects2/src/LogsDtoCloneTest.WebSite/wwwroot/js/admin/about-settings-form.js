(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('form[enctype="multipart/form-data"]');
        if (!form) {
            return;
        }

        if (window.AdminRichEditor && typeof window.AdminRichEditor.initEditor === 'function') {
            // Initialize editor for Description
            var descriptionEditor = form.querySelector('#descriptionEditor');
            if (descriptionEditor) {
                var descriptionTextarea = form.querySelector('textarea[data-editor-textarea][name="Description"]');
                if (descriptionTextarea) {
                    window.AdminRichEditor.initEditor(descriptionEditor, descriptionTextarea);
                }
            }

            // Initialize editor for Vision
            var visionEditor = form.querySelector('#visionEditor');
            if (visionEditor) {
                var visionTextarea = form.querySelector('textarea[data-editor-textarea][name="Vision"]');
                if (visionTextarea) {
                    window.AdminRichEditor.initEditor(visionEditor, visionTextarea);
                }
            }

            // Initialize editor for Mission
            var missionEditor = form.querySelector('#missionEditor');
            if (missionEditor) {
                var missionTextarea = form.querySelector('textarea[data-editor-textarea][name="Mission"]');
                if (missionTextarea) {
                    window.AdminRichEditor.initEditor(missionEditor, missionTextarea);
                }
            }
        }

        setupImageUploader(form);
    });

    function setupImageUploader(form) {
        var uploader = form.querySelector('[data-image-uploader]');
        if (!uploader) {
            return;
        }

        var input = uploader.querySelector('[data-image-input]');
        var preview = uploader.querySelector('[data-image-preview]');
        var placeholder = uploader.querySelector('[data-image-placeholder]');
        var filename = uploader.querySelector('[data-image-filename]');
        var removeCheckbox = uploader.querySelector('[data-image-remove]');

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
                    filename.textContent = 'تصویر انتخاب نشده است';
                    if (input) {
                        input.value = '';
                    }
                }
            });
        }
    }

})();

