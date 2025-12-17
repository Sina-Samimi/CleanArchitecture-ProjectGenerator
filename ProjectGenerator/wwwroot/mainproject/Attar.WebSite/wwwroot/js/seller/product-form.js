(function () {
    'use strict';

    const form = document.querySelector('[data-seller-product-form]');
    if (!form) {
        return;
    }

    const typeSelect = form.querySelector('[data-seller-product-type]');
    const digitalSection = form.querySelector('[data-digital-section]');
    const digitalInput = form.querySelector('[data-digital-input]');
    const inventorySection = form.querySelector('[data-inventory-section]');
    const inventoryToggle = form.querySelector('[data-inventory-toggle]');
    const inventoryQuantity = form.querySelector('[data-inventory-quantity]');
    const tagContainer = form.querySelector('[data-tag-input]');
    const featuredPicker = form.querySelector('[data-featured-picker]');

    function normaliseDigits(value) {
        if (!value) {
            return '';
        }
        return value.replace(/[۰-۹]/g, d => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d)).replace(/[٠-٩]/g, d => '٠١٢٣٤٥٦٧٨٩'.indexOf(d));
    }

    function updateInventoryState() {
        if (!inventoryQuantity) {
            return;
        }
        const enabled = inventoryToggle ? inventoryToggle.checked : false;
        inventoryQuantity.disabled = !enabled;
        if (!enabled) {
            inventoryQuantity.value = '0';
        }
    }

    function updateTypeVisibility() {
        if (!typeSelect) {
            return;
        }

        const value = normaliseDigits(typeSelect.value || '');
        const isDigital = value === '2' || value === 'Digital';

        if (digitalSection) {
            digitalSection.classList.toggle('d-none', !isDigital);
        }

        if (inventorySection) {
            inventorySection.classList.toggle('d-none', isDigital);
        }

        if (isDigital) {
            if (inventoryToggle) {
                inventoryToggle.checked = false;
            }
            if (inventoryQuantity) {
                inventoryQuantity.value = '0';
            }
            updateInventoryState();
        }

        if (!isDigital && digitalInput && digitalSection && !digitalSection.classList.contains('d-none')) {
            digitalInput.value = digitalInput.value || '';
        }
    }

    function initialiseTags() {
        if (!tagContainer) {
            return;
        }

        const entry = tagContainer.querySelector('[data-tag-entry]');
        const storage = tagContainer.querySelector('[data-tag-storage]');
        const chips = tagContainer.querySelector('[data-tag-chips]');
        const addButton = tagContainer.querySelector('[data-tag-add]');

        if (!entry || !storage || !chips) {
            return;
        }

        const maxTagLength = 50;
        const separators = /[،,;|\n\r]+/;
        const tags = new Set(
            (storage.value || '')
                .split(separators)
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

        form.addEventListener('submit', () => {
            if (entry.value.trim()) {
                addTag(entry.value);
                entry.value = '';
            }
        });

        if (addButton) {
            addButton.addEventListener('click', event => {
                event.preventDefault();
                if (entry.value.trim()) {
                    addTag(entry.value);
                    entry.value = '';
                    entry.focus();
                }
            });
        }

        updateStorage();
    }

    function initialiseFeaturedImagePicker() {
        if (!featuredPicker) {
            return;
        }

        const pathInput = featuredPicker.querySelector('[data-featured-path]');
        const fileInput = featuredPicker.querySelector('[data-featured-file]');
        const image = featuredPicker.querySelector('[data-featured-image]');
        const placeholder = featuredPicker.querySelector('[data-featured-placeholder]');
        const clearButton = featuredPicker.querySelector('[data-featured-clear]');

        if ((!pathInput && !fileInput) || !image || !placeholder) {
            return;
        }

        let objectUrl = null;

        function setPreview(value) {
            const trimmed = (value || '').trim();
            if (trimmed) {
                image.src = trimmed;
                image.classList.remove('d-none');
                placeholder.classList.add('d-none');
            } else {
                image.src = '';
                image.classList.add('d-none');
                placeholder.classList.remove('d-none');
            }
        }

        function updatePreview() {
            if (fileInput && fileInput.files && fileInput.files.length > 0) {
                if (objectUrl) {
                    URL.revokeObjectURL(objectUrl);
                }

                objectUrl = URL.createObjectURL(fileInput.files[0]);
                setPreview(objectUrl);
                return;
            }

            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
                objectUrl = null;
            }

            const value = pathInput ? pathInput.value : '';
            setPreview(value);
        }

        if (pathInput) {
            pathInput.addEventListener('input', updatePreview);
        }

        if (fileInput) {
            fileInput.addEventListener('change', updatePreview);
        }

        if (clearButton) {
            clearButton.addEventListener('click', event => {
                event.preventDefault();

                if (pathInput) {
                    pathInput.value = '';
                }

                if (fileInput) {
                    fileInput.value = '';
                }

                updatePreview();
            });
        }

        updatePreview();
    }

    if (typeSelect) {
        typeSelect.addEventListener('change', updateTypeVisibility);
        updateTypeVisibility();
    }

    if (inventoryToggle) {
        inventoryToggle.addEventListener('change', updateInventoryState);
        updateInventoryState();
    }

    initialiseTags();
    initialiseFeaturedImagePicker();

    if (window.AdminRichEditor && typeof window.AdminRichEditor.init === 'function') {
        window.AdminRichEditor.init(form);
    }
})();
