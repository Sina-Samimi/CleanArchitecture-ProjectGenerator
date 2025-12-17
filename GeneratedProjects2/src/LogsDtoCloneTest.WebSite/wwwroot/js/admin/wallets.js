(function () {
    'use strict';

    const formWrapper = document.querySelector('[data-wallet-charge-form]');
    if (!formWrapper) {
        return;
    }

    initialiseUserSelect(formWrapper);
    preventDoubleSubmit(formWrapper);

    function initialiseUserSelect(wrapper) {
        const select = wrapper.querySelector('[data-wallet-user-select]');
        if (!select || !window.jQuery || typeof window.jQuery.fn.select2 !== 'function') {
            return;
        }

        const $element = window.jQuery(select);
        if ($element.hasClass('select2-hidden-accessible')) {
            $element.select2('destroy');
        }

        const placeholder = select.dataset.placeholder || 'انتخاب کاربر سیستم';
        const allowClear = select.dataset.allowClear === 'true';
        const dropdownParentSelector = select.dataset.dropdownParent;
        let $dropdownParent;

        if (dropdownParentSelector) {
            if (dropdownParentSelector === '[data-wallet-charge-form]') {
                $dropdownParent = window.jQuery(wrapper);
            } else {
                $dropdownParent = window.jQuery(dropdownParentSelector);
            }
        }

        // Get initial selected value and text for POST back scenarios
        const selectedOption = select.options[select.selectedIndex];
        const selectedValue = selectedOption && selectedOption.value ? selectedOption.value : null;
        const selectedText = selectedOption && selectedOption.value ? selectedOption.text : null;

        // Remove all options except placeholder and selected one to force AJAX mode
        // This ensures select2 shows the search box
        const placeholderOption = Array.from(select.options).find(opt => !opt.value);
        const selectedOpt = Array.from(select.options).find(opt => opt.value && opt.selected);
        
        // Clear all options
        select.innerHTML = '';
        
        // Add placeholder option
        if (placeholderOption) {
            select.appendChild(placeholderOption);
        } else {
            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = placeholder || 'انتخاب کاربر سیستم';
            select.appendChild(placeholder);
        }
        
        // Add selected option if exists (for POST back)
        if (selectedOpt && selectedOpt.value) {
            select.appendChild(selectedOpt);
        }

        $element.select2({
            dir: document.dir || 'rtl',
            width: '100%',
            placeholder: placeholder,
            allowClear: allowClear,
            dropdownParent: $dropdownParent && $dropdownParent.length ? $dropdownParent : undefined,
            minimumInputLength: 0,
            ajax: {
                url: '/Admin/Wallets/SearchUsers',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        term: params.term || ''
                    };
                },
                processResults: function (data) {
                    return {
                        results: data.results || []
                    };
                },
                cache: true
            },
            language: {
                noResults: function () {
                    return 'کاربری یافت نشد';
                },
                searching: function () {
                    return 'در حال جستجو...';
                },
                inputTooShort: function () {
                    return 'برای جستجو تایپ کنید یا لیست را مشاهده کنید';
                }
            },
            escapeMarkup: function (markup) {
                return markup;
            },
            templateResult: function (user) {
                if (user.loading) {
                    return 'در حال جستجو...';
                }
                return user.text;
            },
            templateSelection: function (user) {
                return user.text || user.id;
            }
        });

        // Set initial value if exists (for POST back scenarios)
        if (selectedValue) {
            $element.val(selectedValue).trigger('change');
            // If the selected value is not in the initial options, add it manually
            const optionExists = Array.from(select.options).some(opt => opt.value === selectedValue);
            if (!optionExists && selectedText) {
                const newOption = new Option(selectedText, selectedValue, true, true);
                $element.append(newOption).trigger('change');
            }
        }

        // Ensure search box is always visible and focused when dropdown opens
        $element.on('select2:open', function () {
            setTimeout(function () {
                const container = document.querySelector('.select2-container--open');
                if (container) {
                    // Find search container
                    const searchContainer = container.querySelector('.select2-search--dropdown');
                    if (searchContainer) {
                        // Force visibility
                        searchContainer.style.display = 'block';
                        searchContainer.style.visibility = 'visible';
                        searchContainer.style.opacity = '1';
                        searchContainer.style.height = 'auto';
                        searchContainer.style.overflow = 'visible';
                    }
                    
                    // Find search box
                    const searchBox = container.querySelector('.select2-search__field');
                    if (searchBox) {
                        // Force visibility
                        searchBox.style.display = 'block';
                        searchBox.style.visibility = 'visible';
                        searchBox.style.opacity = '1';
                        searchBox.style.width = '100%';
                        searchBox.style.height = 'auto';
                        
                        // Set placeholder
                        searchBox.placeholder = 'جستجوی کاربر...';
                        
                        // Focus on search box
                        setTimeout(function () {
                            searchBox.focus();
                            searchBox.select();
                        }, 100);
                    } else {
                        // If search box doesn't exist, create it manually
                        console.warn('Select2 search box not found, creating manually...');
                        const searchContainer = container.querySelector('.select2-search--dropdown');
                        if (searchContainer && !searchContainer.querySelector('.select2-search__field')) {
                            const input = document.createElement('input');
                            input.className = 'select2-search__field';
                            input.type = 'text';
                            input.placeholder = 'جستجوی کاربر...';
                            input.style.width = '100%';
                            input.style.padding = '8px 12px';
                            input.style.border = '1px solid #d1d5db';
                            input.style.borderRadius = '6px';
                            input.style.direction = 'rtl';
                            input.style.textAlign = 'right';
                            searchContainer.appendChild(input);
                            setTimeout(function () {
                                input.focus();
                            }, 100);
                        }
                    }
                }
            }, 100);
        });
    }

    function preventDoubleSubmit(wrapper) {
        const form = wrapper.querySelector('form');
        if (!form) {
            return;
        }

        form.addEventListener('submit', () => {
            const submitButtons = form.querySelectorAll('button[type="submit"]');
            submitButtons.forEach(button => {
                button.disabled = true;
                button.classList.add('disabled');
            });
        });
    }
})();
