(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('[data-blog-form]');
        if (!form) {
            return;
        }

        if (window.AdminRichEditor && typeof window.AdminRichEditor.init === 'function') {
            window.AdminRichEditor.init(form);
        }
        hydrateAuthorSelect(form);
        initialisePublishedAtPicker(form);
        setupFeaturedUploader(form);
        initialiseTagsInput(form);
    });

    function hydrateAuthorSelect(form) {
        var select = form.querySelector('[data-author-select]');
        if (!select) {
            return;
        }

        var sourceUrl = select.getAttribute('data-author-source');
        if (!sourceUrl) {
            return;
        }

        var emptyHint = form.querySelector('[data-author-empty]');
        var errorHint = form.querySelector('[data-author-error]');

        fetchAuthorOptions(select, sourceUrl, emptyHint, errorHint);
    }

    function fetchAuthorOptions(select, sourceUrl, emptyHint, errorHint) {
        var placeholderOption = select.querySelector('option[data-placeholder="true"]');
        var currentValue = select.value;
        var placeholderClone = placeholderOption ? placeholderOption.cloneNode(true) : null;

        select.disabled = true;

        fetch(sourceUrl, {
            headers: {
                'Accept': 'application/json'
            }
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Failed to load authors');
                }
                return response.json();
            })
            .then(function (data) {
                var authors = Array.isArray(data && data.authors) ? data.authors : [];
                var fragment = document.createDocumentFragment();

                if (placeholderClone) {
                    fragment.appendChild(placeholderClone);
                }

                authors.forEach(function (author) {
                    if (!author || !author.id || !author.name) {
                        return;
                    }

                    var option = document.createElement('option');
                    option.value = author.id;
                    option.textContent = author.name;
                    fragment.appendChild(option);
                });

                select.innerHTML = '';
                select.appendChild(fragment);

                if (currentValue) {
                    select.value = currentValue;
                }

                var hasAuthors = authors.length > 0;
                select.disabled = !hasAuthors;

                if (!hasAuthors && placeholderClone) {
                    select.value = '';
                }

                if (emptyHint) {
                    emptyHint.classList.toggle('d-none', hasAuthors);
                }

                if (errorHint) {
                    errorHint.classList.add('d-none');
                }
            })
            .catch(function () {
                var optionCount = select.querySelectorAll('option:not([data-placeholder="true"])').length;
                select.disabled = optionCount === 0;
                if (errorHint) {
                    errorHint.classList.remove('d-none');
                }
            });
    }

    function initialisePublishedAtPicker(form) {
        var pickerContainer = form.querySelector('[data-publish-picker]');
        if (!pickerContainer) {
            return;
        }

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

        var jalaliInput = pickerContainer.querySelector('[data-jalali-input]');
        var jalaliTarget = pickerContainer.querySelector('[data-jalali-target]');
        var clearButton = pickerContainer.querySelector('[data-jalali-clear]');
        var openButton = pickerContainer.querySelector('[data-jalali-open]');
        var timeInput = pickerContainer.querySelector('[data-publish-time]');
        var timeHiddenInput = pickerContainer.querySelector('[data-publish-time-hidden]');
        var isoInput = pickerContainer.querySelector('[data-publish-iso]');
        var statusSelect = form.querySelector('select[name="Status"]');

        if (!jalaliInput || !jalaliTarget || !isoInput) {
            return;
        }

        var persianDigitMap = new Map([
            ['۰', '0'], ['۱', '1'], ['۲', '2'], ['۳', '3'], ['۴', '4'],
            ['۵', '5'], ['۶', '6'], ['۷', '7'], ['۸', '8'], ['۹', '9']
        ]);

        var englishToPersianMap = new Map([
            ['0', '۰'], ['1', '۱'], ['2', '۲'], ['3', '۳'], ['4', '۴'],
            ['5', '۵'], ['6', '۶'], ['7', '۷'], ['8', '۸'], ['9', '۹']
        ]);

        var toEnglishDigits = function (value) {
            if (!value) {
                return '';
            }

            return value.replace(/[۰-۹]/g, function (digit) { return persianDigitMap.get(digit) || digit; });
        };

        var toPersianDigits = function (value) {
            if (!value) {
                return '';
            }

            return value.replace(/[0-9]/g, function (digit) { return englishToPersianMap.get(digit) || digit; });
        };

        var normaliseDateValue = function (value) {
            if (!value) {
                return '';
            }

            var sanitized = toEnglishDigits(value)
                .replace(/\u200f/g, '')
                .replace(/\s+/g, '')
                .replace(/\./g, '/')
                .replace(/-/g, '/');

            var match = sanitized.match(/(\d{4})[\/](\d{1,2})[\/](\d{1,2})/);
            if (!match) {
                return '';
            }

            var year = match[1];
            var month = match[2].padStart(2, '0');
            var day = match[3].padStart(2, '0');

            return year + '-' + month + '-' + day;
        };

        var formatDisplayValue = function (value) {
            if (!value) {
                return '';
            }

            return toPersianDigits(value.replace(/-/g, '/'));
        };

        var normaliseTimeValue = function (value) {
            if (!value) {
                return '';
            }

            var sanitized = toEnglishDigits(value).trim();
            var match = sanitized.match(/^(\d{1,2})(?::?(\d{1,2}))?$/);
            if (!match) {
                return '';
            }

            var hours = parseInt(match[1], 10);
            var minutes = typeof match[2] === 'undefined' ? 0 : parseInt(match[2], 10);

            if (Number.isNaN(hours) || Number.isNaN(minutes)) {
                return '';
            }

            hours = Math.min(Math.max(hours, 0), 23);
            minutes = Math.min(Math.max(minutes, 0), 59);

            var pad = function (part) { return String(part).padStart(2, '0'); };
            return pad(hours) + ':' + pad(minutes);
        };

        var jalaliToGregorian = function (jy, jm, jd) {
            jy = jy - 979;
            jm = jm - 1;
            jd = jd - 1;

            var jDayNo = 365 * jy + Math.floor(jy / 33) * 8 + Math.floor(((jy % 33) + 3) / 4);
            for (var i = 0; i < jm; ++i) {
                jDayNo += i < 6 ? 31 : 30;
            }
            jDayNo += jd;

            var gDayNo = jDayNo + 79;

            var gy = 1600 + 400 * Math.floor(gDayNo / 146097);
            gDayNo = gDayNo % 146097;

            if (gDayNo >= 36525) {
                gDayNo--;
                gy += 100 * Math.floor(gDayNo / 36524);
                gDayNo = gDayNo % 36524;
                if (gDayNo >= 365) {
                    gDayNo++;
                }
            }

            gy += 4 * Math.floor(gDayNo / 1461);
            gDayNo = gDayNo % 1461;

            if (gDayNo >= 366) {
                gy += Math.floor((gDayNo - 1) / 365);
                gDayNo = (gDayNo - 1) % 365;
            }

            var gd = gDayNo + 1;
            var leap = (gy % 4 === 0 && gy % 100 !== 0) || (gy % 400 === 0);
            var monthDays = [0, 31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
            var gm = 0;

            for (gm = 1; gm <= 12; gm++) {
                if (gd <= monthDays[gm]) {
                    break;
                }
                gd -= monthDays[gm];
            }

            return [gy, gm, gd];
        };

        var convertToIsoDate = function (value) {
            if (!value) {
                return '';
            }

            var match = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
            if (!match) {
                return '';
            }

            var jy = parseInt(match[1], 10);
            var jm = parseInt(match[2], 10);
            var jd = parseInt(match[3], 10);

            if ([jy, jm, jd].some(function (part) { return Number.isNaN(part); })) {
                return '';
            }

            var gregorian = jalaliToGregorian(jy, jm, jd);
            if (!Array.isArray(gregorian) || gregorian.length !== 3) {
                return '';
            }

            var pad = function (part) { return String(part).padStart(2, '0'); };
            return gregorian[0] + '-' + pad(gregorian[1]) + '-' + pad(gregorian[2]);
        };

        var applyNormalised = function () {
            var normalised = normaliseDateValue(jalaliInput.value);
            jalaliTarget.value = normalised;
            if (normalised) {
                jalaliInput.value = formatDisplayValue(normalised);
            } else if (!jalaliInput.value) {
                jalaliTarget.value = '';
            }
        };

        var applyInitial = function () {
            if (jalaliTarget.value) {
                jalaliInput.value = formatDisplayValue(jalaliTarget.value);
            }
            applyNormalised();
        };

        jalaliInput.addEventListener('change', applyNormalised);
        jalaliInput.addEventListener('input', applyNormalised);

        if (clearButton) {
            clearButton.addEventListener('click', function (event) {
                event.preventDefault();
                jalaliInput.value = '';
                jalaliTarget.value = '';
                if (timeInput) {
                    timeInput.value = '';
                }
                if (timeHiddenInput) {
                    timeHiddenInput.value = '';
                }
                isoInput.value = '';
            });
        }

        if (openButton) {
            openButton.addEventListener('click', function (event) {
                event.preventDefault();
                jalaliInput.focus();
                if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                    window.jalaliDatepicker.show(jalaliInput);
                }
            });
        }

        applyInitial();

        form.addEventListener('submit', function () {
            if (statusSelect && statusSelect.value !== 'Published') {
                isoInput.value = '';
                if (timeHiddenInput) {
                    timeHiddenInput.value = '';
                }
                return;
            }

            var dateValue = jalaliTarget.value;
            if (!dateValue) {
                isoInput.value = '';
                if (timeHiddenInput) {
                    timeHiddenInput.value = '';
                }
                return;
            }

            var isoDate = convertToIsoDate(dateValue);
            if (!isoDate) {
                isoInput.value = '';
                if (timeHiddenInput) {
                    timeHiddenInput.value = '';
                }
                return;
            }

            var timeValue = timeInput ? normaliseTimeValue(timeInput.value) : '';
            if (timeHiddenInput) {
                timeHiddenInput.value = timeValue;
            }
            isoInput.value = isoDate + 'T' + (timeValue || '00:00');
        });
    }

    function setupFeaturedUploader(form) {
        var uploader = form.querySelector('[data-featured-uploader]');
        if (!uploader) {
            return;
        }

        var fileInput = uploader.querySelector('[data-featured-input]');
        var preview = uploader.querySelector('[data-featured-preview]');
        var placeholder = uploader.querySelector('[data-featured-placeholder]');
        var fileNameLabel = uploader.querySelector('[data-featured-filename]');
        var removeToggle = uploader.querySelector('[data-featured-remove]');
        var pathInput = form.querySelector('[data-featured-path]');

        var activePreviewObjectUrl = null;

        var revokeActivePreview = function () {
            if (activePreviewObjectUrl) {
                URL.revokeObjectURL(activePreviewObjectUrl);
                activePreviewObjectUrl = null;
            }
        };

        var defaultEmptyText = 'تصویر انتخاب نشده است';
        var defaultSelectedText = 'تصویر فعلی انتخاب شده';

        var getStoredPath = function () {
            return pathInput ? (pathInput.value || '') : '';
        };

        var setPreviewImage = function (url, isObjectUrl) {
            if (isObjectUrl) {
                revokeActivePreview();
                activePreviewObjectUrl = url;
            } else {
                revokeActivePreview();
            }

            if (!preview) {
                return;
            }

            if (url) {
                preview.style.backgroundImage = 'url("' + url + '")';
                preview.classList.remove('featured-upload__preview--empty');
                if (placeholder) {
                    placeholder.setAttribute('hidden', 'hidden');
                }
            } else {
                preview.style.backgroundImage = '';
                preview.classList.add('featured-upload__preview--empty');
                if (placeholder) {
                    placeholder.removeAttribute('hidden');
                }
            }
        };

        var updateFileName = function (text) {
            if (!fileNameLabel) {
                return;
            }

            fileNameLabel.textContent = text || defaultEmptyText;
        };

        var refreshFromStoredPath = function () {
            var stored = getStoredPath();
            setPreviewImage(stored, false);
            updateFileName(stored ? defaultSelectedText : defaultEmptyText);
        };

        refreshFromStoredPath();

        if (fileInput) {
            fileInput.addEventListener('change', function () {
                if (!fileInput.files || fileInput.files.length === 0) {
                    refreshFromStoredPath();
                    return;
                }

                var file = fileInput.files[0];
                var objectUrl = URL.createObjectURL(file);
                setPreviewImage(objectUrl, true);
                updateFileName(file.name);

                if (removeToggle) {
                    removeToggle.checked = false;
                    removeToggle.removeAttribute('disabled');
                }
            });
        }

        if (removeToggle) {
            if (!getStoredPath()) {
                removeToggle.checked = false;
            }

            removeToggle.addEventListener('change', function () {
                if (removeToggle.checked) {
                    if (fileInput) {
                        fileInput.value = '';
                    }

                    setPreviewImage('', false);
                    updateFileName(defaultEmptyText);
                } else {
                    if (fileInput && fileInput.files && fileInput.files.length > 0) {
                        var file = fileInput.files[0];
                        var objectUrl = URL.createObjectURL(file);
                        setPreviewImage(objectUrl, true);
                        updateFileName(file.name);
                    } else {
                        refreshFromStoredPath();
                    }
                }
            });
        }

        var disposePreview = function () {
            revokeActivePreview();
        };

        form.addEventListener('reset', disposePreview);
        form.addEventListener('submit', disposePreview);
        window.addEventListener('pagehide', disposePreview);
    }

    function initialiseTagsInput(form) {
        var input = form.querySelector('[data-tag-input]');
        if (!input) {
            return;
        }

        var preview = form.querySelector('[data-tag-preview]');
        var MAX_TAG_LENGTH = 50;

        var normaliseTag = function (value) {
            if (!value) {
                return '';
            }

            var trimmed = value.trim();
            if (!trimmed) {
                return '';
            }

            if (trimmed.length > MAX_TAG_LENGTH) {
                trimmed = trimmed.substring(0, MAX_TAG_LENGTH);
            }

            return trimmed;
        };

        var toLowerInvariant = function (value) {
            try {
                return value.toLocaleLowerCase('en-US');
            } catch (error) {
                return value.toLowerCase();
            }
        };

        var extractTags = function (value) {
            if (!value) {
                return [];
            }

            var segments = value.split(/[،,;|\n\r]+/);
            var seen = new Set();
            var result = [];

            segments.forEach(function (segment) {
                if (!segment) {
                    return;
                }

                var tag = normaliseTag(segment);
                if (!tag) {
                    return;
                }

                var lower = toLowerInvariant(tag);
                if (seen.has(lower)) {
                    return;
                }

                seen.add(lower);
                result.push(tag);
            });

            return result;
        };

        var renderPreview = function (tags) {
            if (!preview) {
                return;
            }

            preview.innerHTML = '';

            if (!tags || tags.length === 0) {
                preview.classList.add('blog-tag-preview--empty');
                return;
            }

            preview.classList.remove('blog-tag-preview--empty');

            tags.forEach(function (tag) {
                var item = document.createElement('span');
                item.className = 'blog-tag-preview__item';
                item.textContent = tag;
                preview.appendChild(item);
            });
        };

        var normaliseValue = function (value) {
            return extractTags(value).join(', ');
        };

        renderPreview(extractTags(input.value));

        input.addEventListener('input', function () {
            renderPreview(extractTags(input.value));
        });

        input.addEventListener('blur', function () {
            input.value = normaliseValue(input.value);
            renderPreview(extractTags(input.value));
        });
    }

})();
