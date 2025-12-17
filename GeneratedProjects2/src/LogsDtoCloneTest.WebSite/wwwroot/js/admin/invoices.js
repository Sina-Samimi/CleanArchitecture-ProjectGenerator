(function () {
    const doc = document;

    const englishDigitMap = new Map([
        ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
        ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9'],
        ['٠', '0'], ['١', '1'], ['٢', '2'], ['٣', '3'], ['٤', '4'],
        ['٥', '5'], ['٦', '6'], ['٧', '7'], ['٨', '8'], ['٩', '9']
    ]);

    function confirmHandlers() {
        const buttons = doc.querySelectorAll('[data-invoice-confirm]');
        if (!buttons.length) {
            return;
        }

        const modalElement = doc.querySelector('[data-invoice-confirm-modal]');
        const bootstrapAvailable = typeof window.bootstrap !== 'undefined' && typeof window.bootstrap.Modal === 'function';

        if (!modalElement || !bootstrapAvailable) {
            buttons.forEach(button => {
                button.addEventListener('click', event => {
                    const message = button.getAttribute('data-invoice-confirm') ?? 'آیا اطمینان دارید؟';
                    if (!window.confirm(message)) {
                        event.preventDefault();
                    }
                });
            });

            return;
        }

        const messageElement = modalElement.querySelector('[data-invoice-confirm-message]');
        const approveButton = modalElement.querySelector('[data-invoice-confirm-approve]');
        const modalInstance = window.bootstrap.Modal.getOrCreateInstance(modalElement);
        let pendingAction = null;

        const resetPending = () => {
            pendingAction = null;
            // Reset button style to default (danger)
            if (approveButton) {
                approveButton.className = 'btn btn-danger';
            }
        };

        modalElement.addEventListener('hidden.bs.modal', resetPending);

        approveButton?.addEventListener('click', () => {
            if (!pendingAction) {
                modalInstance.hide();
                return;
            }

            if (pendingAction.type === 'form' && pendingAction.form) {
                const { form, submitter } = pendingAction;
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit(submitter);
                } else {
                    form.submit();
                }
            } else if (pendingAction.type === 'link' && pendingAction.href) {
                window.location.href = pendingAction.href;
            }

            modalInstance.hide();
            resetPending();
        });

        buttons.forEach(button => {
            button.addEventListener('click', event => {
                const message = button.getAttribute('data-invoice-confirm') ?? 'آیا اطمینان دارید؟';
                const confirmStyle = button.getAttribute('data-invoice-confirm-style') ?? 'danger';
                const form = button.closest('form');
                const href = button.getAttribute('href');

                if (!form && !href) {
                    if (!window.confirm(message)) {
                        event.preventDefault();
                    }
                    return;
                }

                event.preventDefault();
                event.stopPropagation();

                if (messageElement) {
                    messageElement.textContent = message;
                }

                // Update approve button style based on action
                if (approveButton) {
                    approveButton.className = `btn btn-${confirmStyle}`;
                }

                pendingAction = form
                    ? { type: 'form', form, submitter: button }
                    : { type: 'link', href };

                modalInstance.show();
            });
        });
    }

    function replaceIndex(value, itemIndex, attributeIndex) {
        if (!value) {
            return value;
        }

        let replaced = value.replace(/Items\[\d+]/g, `Items[${itemIndex}]`)
            .replace(/Items_(\d+)_/g, `Items_${itemIndex}_`)
            .replace(/Items___\d+___/g, `Items___${itemIndex}___`);

        if (typeof attributeIndex === 'number') {
            replaced = replaced
                .replace(/Attributes\[\d+]/g, `Attributes[${attributeIndex}]`)
                .replace(/Attributes_(\d+)_/g, `Attributes_${attributeIndex}_`)
                .replace(/Attributes___\d+___/g, `Attributes___${attributeIndex}___`);
        }

        return replaced;
    }

    function reindexAttributes(itemElement, itemIndex) {
        const attributeRows = itemElement.querySelectorAll('[data-invoice-attribute]');
        const selector = '[name], [id], [for], [data-valmsg-for]';
        attributeRows.forEach((row, attributeIndex) => {
            row.setAttribute('data-index', attributeIndex.toString());
            row.querySelectorAll(selector).forEach(element => {
                if (element.hasAttribute('name')) {
                    element.setAttribute('name', replaceIndex(element.getAttribute('name'), itemIndex, attributeIndex));
                }

                if (element.hasAttribute('id')) {
                    element.setAttribute('id', replaceIndex(element.getAttribute('id'), itemIndex, attributeIndex));
                }

                if (element.hasAttribute('for')) {
                    element.setAttribute('for', replaceIndex(element.getAttribute('for'), itemIndex, attributeIndex));
                }

                if (element.hasAttribute('data-valmsg-for')) {
                    element.setAttribute('data-valmsg-for', replaceIndex(element.getAttribute('data-valmsg-for'), itemIndex, attributeIndex));
                }
            });
        });
    }

    function reindexItems(container) {
        const items = container.querySelectorAll('[data-invoice-item]');
        items.forEach((item, index) => {
            item.setAttribute('data-index', index.toString());
            const title = item.querySelector('.invoice-item-card__title');
            if (title) {
                title.textContent = `آیتم ${index + 1}`;
            }

            const selector = '[name], [id], [for], [data-valmsg-for]';
            item.querySelectorAll(selector).forEach(element => {
                if (element.hasAttribute('name')) {
                    element.setAttribute('name', replaceIndex(element.getAttribute('name'), index));
                }

                if (element.hasAttribute('id')) {
                    element.setAttribute('id', replaceIndex(element.getAttribute('id'), index));
                }

                if (element.hasAttribute('for')) {
                    element.setAttribute('for', replaceIndex(element.getAttribute('for'), index));
                }

                if (element.hasAttribute('data-valmsg-for')) {
                    element.setAttribute('data-valmsg-for', replaceIndex(element.getAttribute('data-valmsg-for'), index));
                }
            });

            reindexAttributes(item, index);
        });
    }

    function setupAttributeControls(itemElement) {
        const attributeTemplate = doc.getElementById('invoice-attribute-template');
        const addAttributeButton = itemElement.querySelector('[data-invoice-add-attribute]');
        const attributesList = itemElement.querySelector('.invoice-attributes__list');

        if (!attributeTemplate || !addAttributeButton || !attributesList) {
            return;
        }

        addAttributeButton.addEventListener('click', () => {
            const currentIndex = parseInt(itemElement.getAttribute('data-index') ?? '0', 10);
            const templateContent = attributeTemplate.innerHTML;
            const nextAttributeIndex = attributesList.querySelectorAll('[data-invoice-attribute]').length;
            const markup = templateContent
                .replace(/__index__/g, currentIndex.toString())
                .replace(/__attrIndex__/g, nextAttributeIndex.toString());

            const wrapper = doc.createElement('div');
            wrapper.innerHTML = markup.trim();
            const newRow = wrapper.firstElementChild;
            if (!newRow) {
                return;
            }

            attributesList.appendChild(newRow);
            reindexAttributes(itemElement, currentIndex);
            attachAttributeRemoval(newRow, itemElement);
        });

        attributesList.querySelectorAll('[data-invoice-attribute]').forEach(row => attachAttributeRemoval(row, itemElement));
    }

    function attachAttributeRemoval(row, itemElement) {
        const removeButton = row.querySelector('[data-invoice-remove-attribute]');
        if (!removeButton) {
            return;
        }

        removeButton.addEventListener('click', () => {
            row.remove();
            const itemIndex = parseInt(itemElement.getAttribute('data-index') ?? '0', 10);
            reindexAttributes(itemElement, itemIndex);
        });
    }

    function setupInvoiceForm() {
        const formWrapper = doc.querySelector('[data-invoice-form]');
        if (!formWrapper) {
            return;
        }

        initialiseUserSelect(formWrapper);
        initialiseDatePickers(formWrapper);

        // Ensure Select2 syncs before form submission
        const form = formWrapper.querySelector('form');
        if (form) {
            form.addEventListener('submit', function(e) {
                const userSelect = formWrapper.querySelector('[data-invoice-user-select]');
                if (userSelect && window.jQuery && window.jQuery.fn.select2) {
                    const $select = window.jQuery(userSelect);
                    if ($select.hasClass('select2-hidden-accessible')) {
                        // Trigger change to ensure Select2 value is synced
                        $select.trigger('change.select2');
                    }
                }
            });
        }

        const recalcTax = setupTaxCalculation(formWrapper);

        const itemsList = formWrapper.querySelector('.invoice-items__list');
        const addItemButton = formWrapper.querySelector('[data-invoice-add-item]');
        const itemTemplate = doc.getElementById('invoice-item-template');
        if (!itemsList || !addItemButton || !itemTemplate) {
            recalcTax();
            return;
        }

        const hookMonetaryInputs = item => {
            item.querySelectorAll('[data-invoice-item-quantity], [data-invoice-item-unitprice], [data-invoice-item-discount]').forEach(input => {
                input.addEventListener('input', recalcTax);
                input.addEventListener('change', recalcTax);
            });
        };

        const attachItemEvents = item => {
            const removeButton = item.querySelector('[data-invoice-remove-item]');
            if (removeButton) {
                removeButton.addEventListener('click', () => {
                    item.remove();
                    reindexItems(itemsList);
                    recalcTax();
                });
            }

            hookMonetaryInputs(item);
            setupAttributeControls(item);
        };

        itemsList.querySelectorAll('[data-invoice-item]').forEach(item => attachItemEvents(item));

        addItemButton.addEventListener('click', () => {
            const currentCount = itemsList.querySelectorAll('[data-invoice-item]').length;
            const markup = itemTemplate.innerHTML
                .replace(/__index__/g, currentCount.toString())
                .replace(/__displayIndex__/g, (currentCount + 1).toString());

            const wrapper = doc.createElement('div');
            wrapper.innerHTML = markup.trim();
            const newItem = wrapper.firstElementChild;
            if (!newItem) {
                return;
            }

            itemsList.appendChild(newItem);
            reindexItems(itemsList);
            attachItemEvents(newItem);
            recalcTax();
        });

        recalcTax();
    }

    function setupInvoiceFilter() {
        const filterWrapper = doc.querySelector('[data-invoice-filter]');
        if (!filterWrapper) {
            return;
        }

        initialiseUserSelect(filterWrapper);
        initialiseDatePickers(filterWrapper);

        const resetButton = filterWrapper.querySelector('[data-invoice-filter-reset]');
        if (resetButton) {
            resetButton.addEventListener('click', event => {
                event.preventDefault();
                const resetUrl = resetButton.getAttribute('data-reset-url');
                if (resetUrl) {
                    window.location.href = resetUrl;
                    return;
                }

                const form = filterWrapper.querySelector('form');
                if (form) {
                    form.reset();
                    form.submit();
                }
            });
        }
    }


    function setupInvoiceDetails() {
        const detailsWrapper = doc.querySelector('[data-invoice-details]');
        if (!detailsWrapper) {
            return;
        }

        initialiseDatePickers(detailsWrapper);
    }

    function setupTaxCalculation(formWrapper) {
        const percentInput = formWrapper.querySelector('[data-invoice-tax-percent]');
        const amountInput = formWrapper.querySelector('[data-invoice-tax-amount]');
        const displayInput = formWrapper.querySelector('[data-invoice-tax-amount-display]');

        if (!percentInput || !amountInput) {
            return () => {};
        }

        const formatter = typeof Intl !== 'undefined'
            ? new Intl.NumberFormat('fa-IR', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
            : null;

        const updateDisplay = value => {
            const rounded = roundMoney(value);
            amountInput.value = rounded.toFixed(2);

            if (displayInput) {
                displayInput.value = formatter ? formatter.format(rounded) : rounded.toFixed(2);
            }
        };

        const recalc = () => {
            const percent = Math.max(0, parseDecimal(percentInput.value));
            const totals = calculateItemTotals(formWrapper);
            let tax = 0;

            if (percent > 0 && totals.itemsTotal > 0) {
                tax = (percent * totals.itemsTotal) / 100;
            }

            updateDisplay(tax);
        };

        percentInput.addEventListener('input', recalc);
        percentInput.addEventListener('change', recalc);

        recalc();
        return recalc;
    }

    function calculateItemTotals(formWrapper) {
        const items = formWrapper.querySelectorAll('[data-invoice-item]');
        let subtotal = 0;
        let discount = 0;

        items.forEach(item => {
            const quantityInput = item.querySelector('[data-invoice-item-quantity]');
            const unitPriceInput = item.querySelector('[data-invoice-item-unitprice]');
            const discountInput = item.querySelector('[data-invoice-item-discount]');

            const quantity = Math.max(0, parseDecimal(quantityInput ? quantityInput.value : ""));
            const unitPrice = Math.max(0, parseDecimal(unitPriceInput ? unitPriceInput.value : ""));
            const lineSubtotal = quantity * unitPrice;
            subtotal += lineSubtotal;

            const rawDiscount = Math.max(0, parseDecimal(discountInput ? discountInput.value : ""));
            const itemDiscount = Math.min(rawDiscount, lineSubtotal);
            discount += itemDiscount;
        });

        subtotal = roundMoney(subtotal);
        discount = roundMoney(discount);

        let itemsTotal = subtotal - discount;
        if (itemsTotal < 0) {
            itemsTotal = 0;
        }

        itemsTotal = roundMoney(itemsTotal);
        return { subtotal, discount, itemsTotal };
    }

    function initialiseUserSelect(formWrapper) {
        const select = formWrapper.querySelector('[data-invoice-user-select]');
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

        const placeholder = select.dataset.placeholder || 'انتخاب کاربر سیستم';
        const allowClear = select.dataset.allowClear === 'true';
        const dropdownParentSelector = select.dataset.dropdownParent;
        let $dropdownParent;

        if (dropdownParentSelector) {
            if (dropdownParentSelector === '[data-invoice-form]') {
                $dropdownParent = $(formWrapper);
            } else {
                $dropdownParent = $(dropdownParentSelector);
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

    function initialiseDatePickers(formWrapper) {
        if (window.jalaliDatepicker && typeof window.jalaliDatepicker.startWatch === 'function') {
            window.jalaliDatepicker.startWatch({
                date: true,
                time: false,
                autoHide: true,
                persianDigits: true,
                showCloseBtn: 'dynamic',
                topSpace: 10,
                bottomSpace: 30,
                overflowSpace: 10
            });
        }

        formWrapper.querySelectorAll('[data-invoice-date]').forEach(container => {
            const jalaliInput = container.querySelector('[data-jalali-input]');
            const jalaliTarget = container.querySelector('[data-jalali-target]');

            const applyNormalizedDate = () => {
                if (!jalaliInput || !jalaliTarget) {
                    return;
                }

                const { normalized, sanitized } = normalizeDateValue(jalaliInput.value);
                jalaliTarget.value = normalized || sanitized;

                if (!sanitized) {
                    jalaliInput.value = '';
                } else if (!normalized) {
                    jalaliInput.value = sanitized;
                }
            };

            if (jalaliTarget && jalaliInput && jalaliTarget.value && !jalaliInput.value) {
                jalaliInput.value = jalaliTarget.value.replace(/-/g, '/');
            }

            jalaliInput?.addEventListener('change', applyNormalizedDate);
            jalaliInput?.addEventListener('input', applyNormalizedDate);

            applyNormalizedDate();
        });
    }

    function normalizeDateValue(value) {
        const result = { normalized: '', sanitized: '' };

        if (!value) {
            return result;
        }

        const sanitized = toEnglishDigits(value)
            .replace(/\u200f/g, '')
            .replace(/\u200e/g, '')
            .replace(/\s+/g, '')
            .replace(/\./g, '/')
            .replace(/-/g, '/');

        result.sanitized = sanitized;

        const match = sanitized.match(/(\d{4})[\/](\d{1,2})[\/](\d{1,2})/);
        if (!match) {
            return result;
        }

        const [, year, month, day] = match;
        const pad = (part, length) => part.padStart(length, '0');
        result.normalized = `${pad(year, 4)}-${pad(month, 2)}-${pad(day, 2)}`;
        return result;
    }


    function roundMoney(value) {
        return Math.round((Number(value) || 0) * 100) / 100;
    }

    function parseDecimal(value) {
        if (typeof value === 'number') {
            return Number.isFinite(value) ? value : 0;
        }

        if (!value) {
            return 0;
        }

        const normalized = toEnglishDigits(String(value)).replace(/[^0-9+\-.]/g, '');
        if (!normalized) {
            return 0;
        }

        const parsed = parseFloat(normalized);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function toEnglishDigits(value) {
        if (!value) {
            return '';
        }

        return value.replace(/[۰-۹٠-٩]/g, digit => englishDigitMap.get(digit) ?? digit);
    }

    confirmHandlers();
    setupInvoiceForm();
    setupInvoiceFilter();
    setupInvoiceDetails();
})();
