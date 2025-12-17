(function () {
    'use strict';

    const form = document.querySelector('[data-banner-form]') || document.querySelector('.banner-form form');
    if (!form) {
        return;
    }

    // Initialize jalali date picker
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

    // Setup date pickers
    form.querySelectorAll('[data-jalali-picker]').forEach(container => {
        const input = container.querySelector('[data-jalali-input]');
        const target = container.querySelector('[data-jalali-target]');

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
            // If target has value but input is empty, set input from target
            if (existing && !input.value) {
                // Convert ISO date (YYYY-MM-DD) to display format (YYYY/MM/DD)
                input.value = existing.replace(/-/g, '/');
            }
            // If input has value (from server), normalize it to target
            if (input.value && !existing) {
                applyNormalised();
            }
        };

        input.addEventListener('change', applyNormalised);
        input.addEventListener('input', applyNormalised);

        applyInitial();
    });

    // Setup banner image uploader
    const imageUploader = form.querySelector('[data-banner-image-uploader]');
    if (imageUploader) {
        const imageInput = imageUploader.querySelector('[data-banner-image-input]');
        const imagePreview = imageUploader.querySelector('[data-banner-image-preview]');
        const imagePlaceholder = imageUploader.querySelector('[data-banner-image-placeholder]');
        const imageFilename = imageUploader.querySelector('[data-banner-image-filename]');
        const imageRemove = imageUploader.querySelector('[data-banner-image-remove]');
        const imagePath = imageUploader.querySelector('[data-banner-image-path]');

        if (imageInput) {
            imageInput.addEventListener('change', function (e) {
                const file = e.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        if (imagePreview) {
                            imagePreview.style.backgroundImage = `url('${e.target.result}')`;
                            imagePreview.classList.remove('featured-upload__preview--empty');
                        }
                        if (imagePlaceholder) {
                            imagePlaceholder.style.display = 'none';
                        }
                        if (imageFilename) {
                            imageFilename.textContent = file.name;
                        }
                    };
                    reader.readAsDataURL(file);
                }
            });
        }

        if (imageRemove) {
            imageRemove.addEventListener('change', function () {
                if (this.checked) {
                    if (imagePreview) {
                        imagePreview.style.backgroundImage = '';
                        imagePreview.classList.add('featured-upload__preview--empty');
                    }
                    if (imagePlaceholder) {
                        imagePlaceholder.style.display = 'block';
                    }
                    if (imageFilename) {
                        imageFilename.textContent = 'تصویر انتخاب نشده است';
                    }
                    if (imagePath) {
                        imagePath.value = '';
                    }
                    if (imageInput) {
                        imageInput.value = '';
                    }
                } else {
                    // Restore original image if exists
                    const originalPath = imagePath?.value;
                    if (originalPath && imagePreview) {
                        imagePreview.style.backgroundImage = `url('${originalPath}')`;
                        imagePreview.classList.remove('featured-upload__preview--empty');
                    }
                    if (originalPath && imagePlaceholder) {
                        imagePlaceholder.style.display = 'none';
                    }
                    if (originalPath && imageFilename) {
                        imageFilename.textContent = 'تصویر فعلی انتخاب شده';
                    }
                }
            });
        }
    }
})();

