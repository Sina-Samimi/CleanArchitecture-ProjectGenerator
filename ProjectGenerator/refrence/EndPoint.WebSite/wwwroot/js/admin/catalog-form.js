(function () {
    'use strict';

    const form = document.querySelector('[data-product-form]');
    if (!form) {
        return;
    }

    const statusSelect = form.querySelector('[data-publish-status]');
    const scheduleFields = form.querySelectorAll('[data-publish-schedule]');
    const jalaliInput = form.querySelector('[data-jalali-input]');
    const jalaliStorage = form.querySelector('[data-jalali-storage]');
    const jalaliOpenButton = form.querySelector('[data-jalali-open]');
    const timeInput = form.querySelector('[data-publish-time]');
    const typeSelect = form.querySelector('[data-product-type]');
    const digitalSection = form.querySelector('[data-digital-section]');
    const digitalFileInput = form.querySelector('[data-digital-file]');
    const digitalFileName = form.querySelector('[data-digital-file-name]');
    const digitalExisting = form.querySelector('[data-digital-existing]');
    const inventoryToggle = form.querySelector('[data-inventory-toggle]');
    const inventoryField = form.querySelector('[data-inventory-field]');
    const featuredInput = form.querySelector('[data-featured-input]');
    const featuredPreview = form.querySelector('[data-featured-preview]');
    const featuredPlaceholder = form.querySelector('[data-featured-placeholder]');
    const featuredFilename = form.querySelector('[data-featured-filename]');
    const featuredRemove = form.querySelector('[data-featured-remove]');
    const featuredPath = form.querySelector('[data-featured-path]');
    const galleryContainer = form.querySelector('[data-gallery-container]');
    const galleryTemplate = document.getElementById('productGalleryRowTemplate');
    const galleryAddButton = form.querySelector('[data-gallery-add]');
    const tagInputContainer = form.querySelector('[data-tag-input]');

    function startJalaliPicker() {
        if (window.jalaliDatepicker) {
            window.jalaliDatepicker.startWatch({
                autoHide: true,
                time: false,
                maxDate: '1405/12/30'
            });
        }
    }

    function updateScheduleVisibility() {
        if (!statusSelect) {
            return;
        }
        const value = statusSelect.value;
        const shouldShow = value === 'Published' || value === 'Scheduled';
        scheduleFields.forEach(field => {
            field.classList.toggle('d-none', !shouldShow);
        });
        if (!shouldShow) {
            if (jalaliInput) {
                jalaliInput.value = '';
            }
            if (jalaliStorage) {
                jalaliStorage.value = '';
            }
            if (timeInput) {
                timeInput.value = '';
            }
        }
    }

    function updateDigitalVisibility() {
        if (!typeSelect || !digitalSection) {
            return;
        }
        const value = typeSelect.value;
        const isDigital = value === 'Digital';
        digitalSection.classList.toggle('d-none', !isDigital);

        if (!isDigital) {
            if (digitalFileInput) {
                digitalFileInput.value = '';
            }
            updateDigitalFileState(null);
        } else if (digitalFileInput && digitalFileInput.files && digitalFileInput.files[0]) {
            updateDigitalFileState(digitalFileInput.files[0]);
        } else {
            updateDigitalFileState(null);
        }
    }

    function updateInventoryVisibility() {
        if (!inventoryField || !inventoryToggle) {
            return;
        }
        const enabled = inventoryToggle.checked;
        inventoryField.classList.toggle('d-none', !enabled);
        const input = inventoryField.querySelector('input');
        if (input) {
            input.disabled = !enabled;
            if (!enabled) {
                input.value = '0';
            }
        }
    }

    function updateDigitalFileState(file) {
        if (!digitalFileName) {
            return;
        }

        const defaultText = digitalFileName.dataset.defaultText || digitalFileName.textContent || '';

        if (file && file.name) {
            digitalFileName.textContent = file.name;
            if (digitalExisting) {
                digitalExisting.classList.add('d-none');
            }
        } else {
            digitalFileName.textContent = defaultText;
            if (digitalExisting) {
                digitalExisting.classList.remove('d-none');
            }
        }
    }

    function normaliseDigits(value) {
        if (!value) {
            return '';
        }
        return value.replace(/[۰-۹]/g, d => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d)).replace(/[٠-٩]/g, d => '٠١٢٣٤٥٦٧٨٩'.indexOf(d));
    }

    function setJalaliValue() {
        if (!jalaliInput || !jalaliStorage) {
            return;
        }
        const raw = normaliseDigits(jalaliInput.value).replace(/\//g, '-');
        jalaliStorage.value = raw;
    }

    function resetFeaturedPreviewUrl() {
        if (featuredPreview && featuredPreview.dataset.previewUrl) {
            URL.revokeObjectURL(featuredPreview.dataset.previewUrl);
            delete featuredPreview.dataset.previewUrl;
        }
    }

    function updateFeaturedPreview(file) {
        if (!featuredPreview) {
            return;
        }

        resetFeaturedPreviewUrl();

        const hideOriginal = featuredPreview.dataset.hideOriginal === 'true';

        if (file && file.size > 0) {
            const url = URL.createObjectURL(file);
            featuredPreview.style.backgroundImage = `url('${url}')`;
            featuredPreview.classList.remove('product-featured__preview--empty');
            if (featuredPlaceholder) {
                featuredPlaceholder.classList.add('d-none');
            }
            if (featuredFilename) {
                featuredFilename.textContent = file.name;
            }
            featuredPreview.dataset.previewUrl = url;
            featuredPreview.dataset.hideOriginal = 'false';
        } else if (!hideOriginal && featuredPath && featuredPath.value) {
            featuredPreview.style.backgroundImage = `url('${featuredPath.value}')`;
            featuredPreview.classList.remove('product-featured__preview--empty');
            if (featuredPlaceholder) {
                featuredPlaceholder.classList.add('d-none');
            }
            if (featuredFilename) {
                featuredFilename.textContent = 'تصویر فعلی انتخاب شده';
            }
            featuredPreview.dataset.hideOriginal = 'false';
        } else if (!hideOriginal && featuredPreview.dataset.originalPath) {
            featuredPreview.style.backgroundImage = `url('${featuredPreview.dataset.originalPath}')`;
            featuredPreview.classList.remove('product-featured__preview--empty');
            if (featuredPlaceholder) {
                featuredPlaceholder.classList.add('d-none');
            }
            if (featuredFilename) {
                featuredFilename.textContent = 'تصویر فعلی انتخاب شده';
            }
            featuredPreview.dataset.hideOriginal = 'false';
        } else {
            featuredPreview.style.backgroundImage = 'none';
            featuredPreview.classList.add('product-featured__preview--empty');
            if (featuredPlaceholder) {
                featuredPlaceholder.classList.remove('d-none');
            }
            if (featuredFilename) {
                featuredFilename.textContent = 'تصویر انتخاب نشده است';
            }
        }
    }

    function clearFeaturedPreview(options = {}) {
        const { clearPath = true, showOriginal = false } = options;
        resetFeaturedPreviewUrl();
        if (featuredInput) {
            featuredInput.value = '';
        }
        if (clearPath && featuredPath) {
            featuredPath.value = '';
        }
        if (featuredPreview) {
            featuredPreview.dataset.hideOriginal = showOriginal ? 'false' : 'true';
        }
        updateFeaturedPreview(null);
    }

    function attachGalleryHandlers(row) {
        if (!row) {
            return;
        }
        const fileInput = row.querySelector('[data-gallery-file]');
        const preview = row.querySelector('[data-gallery-preview]');
        const placeholder = row.querySelector('[data-gallery-placeholder]');
        const removeInput = row.querySelector('[data-gallery-remove]');
        const pathInput = row.querySelector('[data-gallery-path]');

        if (fileInput) {
            fileInput.addEventListener('change', function () {
                const file = this.files && this.files[0];
                if (!file || !preview) {
                    return;
                }
                if (preview.dataset.previewUrl) {
                    URL.revokeObjectURL(preview.dataset.previewUrl);
                    delete preview.dataset.previewUrl;
                }
                const url = URL.createObjectURL(file);
                preview.style.backgroundImage = `url('${url}')`;
                preview.classList.remove('product-gallery-row__media--empty');
                preview.dataset.previewUrl = url;
                if (placeholder) {
                    placeholder.classList.add('d-none');
                }
            });
        }

        if (removeInput) {
            removeInput.addEventListener('change', function () {
                row.classList.toggle('is-removed', this.checked);
            });
        }

        if (pathInput && preview) {
            const current = pathInput.value;
            if (current) {
                preview.style.backgroundImage = `url('${current}')`;
                preview.classList.remove('product-gallery-row__media--empty');
                if (placeholder) {
                    placeholder.classList.add('d-none');
                }
            }
        }
    }

    function initGalleryRows() {
        if (!galleryContainer) {
            return;
        }
        const rows = galleryContainer.querySelectorAll('[data-gallery-row]');
        rows.forEach(row => attachGalleryHandlers(row));
        if (!galleryContainer.dataset.galleryCount) {
            galleryContainer.dataset.galleryCount = String(rows.length);
        }
    }

    function addGalleryRow() {
        if (!galleryContainer || !galleryTemplate) {
            return;
        }
        const currentIndex = Number(galleryContainer.dataset.galleryCount || '0');
        let html = galleryTemplate.innerHTML.replace(/__INDEX__/g, currentIndex);
        const wrapper = document.createElement('div');
        wrapper.innerHTML = html.trim();
        const row = wrapper.firstElementChild;
        galleryContainer.appendChild(row);
        galleryContainer.dataset.galleryCount = String(currentIndex + 1);
        attachGalleryHandlers(row);
    }

    function initialiseTags() {
        if (!tagInputContainer) {
            return;
        }
        const entry = tagInputContainer.querySelector('[data-tag-entry]');
        const storage = tagInputContainer.querySelector('[data-tag-storage]');
        const chips = tagInputContainer.querySelector('[data-tag-chips]');

        if (!entry || !storage || !chips) {
            return;
        }

        const maxTagLength = 50;
        const tags = new Set(
            (storage.value || '')
                .split(/[،,;|\n\r]+/)
                .map(tag => tag.trim())
                .filter(Boolean)
        );

        function render() {
            chips.innerHTML = '';
            tags.forEach(tag => {
                const chip = document.createElement('span');
                chip.className = 'tag-chip';
                chip.dataset.tagChip = '';

                const text = document.createElement('span');
                text.className = 'tag-chip__text';
                text.textContent = tag;
                chip.appendChild(text);

                const remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'tag-chip__remove';
                remove.dataset.tagRemove = '';
                remove.innerHTML = '<i class="bi bi-x"></i>';
                remove.setAttribute('aria-label', `حذف ${tag}`);
                remove.addEventListener('click', () => {
                    tags.delete(tag);
                    updateStorage();
                });

                chip.appendChild(remove);
                chips.appendChild(chip);
            });
        }

        function updateStorage() {
            storage.value = Array.from(tags).join(', ');
            render();
        }

        function addTag(value) {
            const trimmed = (value || '').trim();
            if (!trimmed) {
                return;
            }
            const tag = trimmed.length > maxTagLength ? trimmed.substring(0, maxTagLength) : trimmed;
            tags.add(tag);
            updateStorage();
        }

        entry.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ',' || event.key === '،') {
                event.preventDefault();
                addTag(entry.value);
                entry.value = '';
            }
        });

        entry.addEventListener('blur', () => {
            if (entry.value.trim()) {
                addTag(entry.value);
                entry.value = '';
            }
        });

        updateStorage();
    }

    if (statusSelect) {
        statusSelect.addEventListener('change', updateScheduleVisibility);
        updateScheduleVisibility();
    }

    if (jalaliInput && jalaliStorage) {
        jalaliInput.addEventListener('change', setJalaliValue);
        jalaliInput.addEventListener('blur', setJalaliValue);
        jalaliInput.addEventListener('input', setJalaliValue);
        setJalaliValue();
    }

    if (jalaliOpenButton && jalaliInput) {
        jalaliOpenButton.addEventListener('click', () => {
            if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                window.jalaliDatepicker.show(jalaliInput);
            } else {
                jalaliInput.focus();
            }
        });
    }

    if (typeSelect) {
        typeSelect.addEventListener('change', updateDigitalVisibility);
        updateDigitalVisibility();
    }

    if (inventoryToggle) {
        inventoryToggle.addEventListener('change', updateInventoryVisibility);
        updateInventoryVisibility();
    }

    if (featuredPreview && featuredPath && !featuredPreview.dataset.originalPath) {
        featuredPreview.dataset.originalPath = featuredPath.value || '';
    }

    if (featuredInput) {
        featuredInput.addEventListener('change', function () {
            const file = this.files && this.files[0];
            updateFeaturedPreview(file || null);
        });
    }

    if (featuredRemove) {
        featuredRemove.addEventListener('change', function () {
            if (this.checked) {
                clearFeaturedPreview({ clearPath: true });
            } else {
                if (featuredPreview) {
                    featuredPreview.dataset.hideOriginal = 'false';
                }
                if (featuredPath && featuredPreview && typeof featuredPreview.dataset.originalPath === 'string') {
                    featuredPath.value = featuredPreview.dataset.originalPath;
                }
                updateFeaturedPreview(null);
            }
        });
    }

    if (digitalFileName && !digitalFileName.dataset.defaultText) {
        digitalFileName.dataset.defaultText = digitalFileName.textContent || 'فایلی انتخاب نشده است';
    }

    if (digitalFileInput) {
        digitalFileInput.addEventListener('change', function () {
            const file = this.files && this.files[0];
            updateDigitalFileState(file || null);
        });
    }

    if (form) {
        form.addEventListener('submit', setJalaliValue);
    }

    updateFeaturedPreview(null);

    if (digitalSection) {
        const initialDigitalFile = digitalFileInput && digitalFileInput.files && digitalFileInput.files[0]
            ? digitalFileInput.files[0]
            : null;
        updateDigitalFileState(initialDigitalFile);
    }

    initGalleryRows();

    if (galleryAddButton) {
        galleryAddButton.addEventListener('click', addGalleryRow);
    }

    if (window.AdminRichEditor && typeof window.AdminRichEditor.init === 'function') {
        window.AdminRichEditor.init(form);
    }

    initialiseTags();
    startJalaliPicker();
})();
