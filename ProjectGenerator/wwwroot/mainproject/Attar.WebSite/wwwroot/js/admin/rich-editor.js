(function (global) {
    'use strict';

    if (!global) {
        return;
    }

    function initRichEditor(form) {
        if (!form) {
            return;
        }

        setupRichTextEditor(form);
    }

    function initEditor(editorContainer, contentInput) {
        if (!editorContainer || !contentInput) {
            return;
        }

        // Find the parent container that holds both editor and input
        var parentContainer = editorContainer.parentElement;
        if (!parentContainer) {
            parentContainer = contentInput.parentElement;
        }
        
        // Find fallback hint in the parent
        var fallbackHint = parentContainer ? parentContainer.querySelector('[data-editor-fallback]') : null;
        
        // Store original positions
        var containerParent = editorContainer.parentElement;
        var inputParent = contentInput.parentElement;
        var containerNextSibling = editorContainer.nextSibling;
        var inputNextSibling = contentInput.nextSibling;
        var fallbackParent = fallbackHint ? fallbackHint.parentElement : null;
        var fallbackNextSibling = fallbackHint ? fallbackHint.nextSibling : null;
        
        // Create a temporary wrapper that mimics a form structure
        var tempWrapper = document.createElement('div');
        
        // Temporarily move elements to wrapper
        tempWrapper.appendChild(editorContainer);
        tempWrapper.appendChild(contentInput);
        if (fallbackHint) {
            tempWrapper.appendChild(fallbackHint);
        }
        
        // Now setupRichTextEditor can find them using querySelector
        setupRichTextEditor(tempWrapper);
        
        // Move elements back to their original positions
        // در محیط Production ممکن است HTML minifier فاصله‌ها را حذف کند و nextSibling به همان textarea یا fallback تبدیل شود.
        // در این صورت، آن node حین جابه‌جایی به tempWrapper والدش عوض می‌شود و دیگر child والد قبلی نیست؛
        // برای جلوگیری از NotFoundError، فقط زمانی از insertBefore استفاده می‌کنیم که nextSibling هنوز child همان والد باشد.
        if (containerParent) {
            if (containerNextSibling && containerNextSibling.parentNode === containerParent) {
                containerParent.insertBefore(editorContainer, containerNextSibling);
            } else {
                containerParent.appendChild(editorContainer);
            }
        }
        
        if (inputParent && inputParent !== containerParent) {
            if (inputNextSibling && inputNextSibling.parentNode === inputParent) {
                inputParent.insertBefore(contentInput, inputNextSibling);
            } else {
                inputParent.appendChild(contentInput);
            }
        } else if (containerParent) {
            if (inputNextSibling && inputNextSibling.parentNode === containerParent) {
                containerParent.insertBefore(contentInput, inputNextSibling);
            } else {
                containerParent.appendChild(contentInput);
            }
        }
        
        if (fallbackHint && fallbackParent) {
            if (fallbackNextSibling && fallbackNextSibling.parentNode === fallbackParent) {
                fallbackParent.insertBefore(fallbackHint, fallbackNextSibling);
            } else {
                fallbackParent.appendChild(fallbackHint);
            }
        }
    }

    function setupRichTextEditor(form) {
        var editorContainer = form.querySelector('[data-editor-wrapper]');
        var contentInput = form.querySelector('[data-content-input]');
        var fallbackHint = form.querySelector('[data-editor-fallback]');

        if (!contentInput) {
            return;
        }

        var supportsContentEditable = typeof document !== 'undefined' && typeof document.createElement === 'function'
            ? 'contentEditable' in document.createElement('div')
            : false;

        if (!editorContainer || typeof document.execCommand !== 'function' || !supportsContentEditable) {
            enableEditorFallback(editorContainer, contentInput, fallbackHint);
            return;
        }

        if (fallbackHint) {
            fallbackHint.classList.add('d-none');
        }

        contentInput.classList.add('d-none');
        editorContainer.classList.remove('d-none');
        editorContainer.innerHTML = '';
        editorContainer.classList.add('blog-editor');

        try {
            document.execCommand('styleWithCSS', false, true);
        } catch (error) {
            // Ignore – some browsers do not allow toggling this command.
        }

        try {
            document.execCommand('defaultParagraphSeparator', false, 'p');
        } catch (error) {
            // Ignore – fallback to browser default paragraph behaviour.
        }

        var uploadUrl = editorContainer.getAttribute('data-upload-url') || '';

        var tablePickerControl = null;
        var toolbar = buildToolbar();
        var editorArea = buildEditorArea(contentInput.value);
        var imageInput = buildHiddenImageInput();
        var lastSelection = null;
        var IMAGE_MIN_WIDTH_PERCENT = 30;
        var IMAGE_MAX_WIDTH_PERCENT = 120;
        var IMAGE_DEFAULT_WIDTH_PERCENT = 100;
        var imageResizeControls = null;
        var activeImageElement = null;
        var resizePositionFrame = null;
        var updatingResizeInputs = false;
        var imageResizeListenersAttached = false;

        var applyRange = function (range) {
            if (!range) {
                return;
            }

            var selection = window.getSelection();
            if (!selection) {
                return;
            }

            editorArea.focus();
            selection.removeAllRanges();
            selection.addRange(range);
            lastSelection = range.cloneRange();
        };

        var getActiveRange = function () {
            var selection = window.getSelection();
            if (selection && selection.rangeCount > 0) {
                var activeRange = selection.getRangeAt(0);
                if (editorArea.contains(activeRange.commonAncestorContainer)) {
                    return activeRange.cloneRange();
                }
            }

            if (lastSelection) {
                return lastSelection.cloneRange();
            }

            return null;
        };

        editorContainer.appendChild(toolbar);
        editorContainer.appendChild(editorArea);
        editorContainer.appendChild(imageInput);

        var syncContent = function () {
            contentInput.value = editorArea.innerHTML;
        };

        var storeSelection = function () {
            var selection = window.getSelection();
            if (!selection || selection.rangeCount === 0) {
                return;
            }

            var range = selection.getRangeAt(0);
            if (!editorArea.contains(range.commonAncestorContainer)) {
                return;
            }

            lastSelection = range.cloneRange();
        };

        var focusEditor = function () {
            if (lastSelection) {
                applyRange(lastSelection.cloneRange());
            } else {
                editorArea.focus();
            }
        };

        editorArea.addEventListener('input', function () {
            if (activeImageElement && !activeImageElement.isConnected) {
                hideImageResizeControls();
            } else if (activeImageElement) {
                queueImageResizeControlsPosition();
            }
            syncContent();
            storeSelection();
        });
        editorArea.addEventListener('focus', storeSelection);
        editorArea.addEventListener('mouseup', storeSelection);
        editorArea.addEventListener('keyup', storeSelection);
        editorArea.addEventListener('touchend', storeSelection);

        editorArea.addEventListener('click', function (event) {
            var image = event.target && event.target.closest('img');
            if (image && editorArea.contains(image)) {
                if (image.closest('a')) {
                    event.preventDefault();
                }

                var range = document.createRange();
                range.selectNode(image);
                applyRange(range);
                storeSelection();
                showImageResizeControlsFor(image);
                return;
            }

            hideImageResizeControls();
        });

        editorArea.addEventListener('dragstart', function (event) {
            if (event.target && event.target.closest('img')) {
                event.preventDefault();
            }
        });

        document.addEventListener('selectionchange', function () {
            if (document.activeElement === editorArea) {
                storeSelection();
                if (activeImageElement && !isImageInCurrentSelection(activeImageElement)) {
                    hideImageResizeControls();
                }
            }
        });

        toolbar.addEventListener('click', function (event) {
            var button = event.target.closest('button[data-command], button[data-action]');
            if (!button) {
                return;
            }

            event.preventDefault();
            focusEditor();

            var command = button.getAttribute('data-command');
            var action = button.getAttribute('data-action');

            if (command) {
                document.execCommand(command, false, null);
                syncContent();
                storeSelection();
                if (tablePickerControl) {
                    tablePickerControl.hide();
                }
                return;
            }

            if (!action) {
                return;
            }

            var handledAction = false;

            if (action === 'createLink') {
                handleLinkCreation();
                handledAction = true;
            } else if (action === 'removeLink') {
                document.execCommand('unlink');
                handledAction = true;
            } else if (action === 'insertImage') {
                storeSelection();
                if (tablePickerControl) {
                    tablePickerControl.hide();
                }
                imageInput.click();
                return;
            } else if (action === 'removeTable') {
                handledAction = removeActiveTable() || handledAction;
            } else if (action === 'removeTableRow') {
                handledAction = removeActiveTableRow() || handledAction;
            } else if (action === 'removeTableColumn') {
                handledAction = removeActiveTableColumn() || handledAction;
            } else if (action === 'print') {
                printEditorContent();
                handledAction = true;
            }

            if (tablePickerControl) {
                tablePickerControl.hide();
            }

            if (handledAction) {
                syncContent();
                storeSelection();
            }
        });

        toolbar.addEventListener('change', function (event) {
            var target = event.target;
            if (!target || !target.dataset) {
                return;
            }

            var command = target.dataset.command;
            if (!command) {
                return;
            }

            focusEditor();

            if (command === 'formatBlock' && target.value) {
                document.execCommand('formatBlock', false, target.value);
            } else if (command === 'fontName') {
                if (target.value) {
                    document.execCommand('fontName', false, target.value);
                }
                normalizeFontTags(null, target.value);
            } else if (command === 'fontSize') {
                applyFontSize(target.value);
                target.selectedIndex = 0;
            }

            if (tablePickerControl) {
                tablePickerControl.hide();
            }

            syncContent();
            storeSelection();
        });

        toolbar.addEventListener('input', function (event) {
            var target = event.target;
            if (!target || !target.dataset) {
                return;
            }

            var command = target.dataset.command;
            if (!command) {
                return;
            }

            focusEditor();

            if (command === 'foreColor') {
                document.execCommand('foreColor', false, target.value);
            } else if (command === 'hiliteColor') {
                applyHighlightColor(target.value);
            }

            if (tablePickerControl) {
                tablePickerControl.hide();
            }

            syncContent();
            storeSelection();
        });

        document.addEventListener('pointerdown', function (event) {
            if (tablePickerControl && tablePickerControl.isOpen() && !tablePickerControl.wrapper.contains(event.target)) {
                tablePickerControl.hide();
            }

            if (imageResizeControls && !imageResizeControls.wrapper.classList.contains('d-none')) {
                if (!imageResizeControls.wrapper.contains(event.target)) {
                    var clickedImage = event.target && event.target.closest('img');
                    if (!clickedImage || !editorArea.contains(clickedImage)) {
                        hideImageResizeControls();
                    }
                }
            }
        });

        document.addEventListener('keydown', function (event) {
            var isEscape = event.key === 'Escape' || event.key === 'Esc';

            if (isEscape && tablePickerControl && tablePickerControl.isOpen()) {
                tablePickerControl.hide();
            }

            if (isEscape && imageResizeControls && !imageResizeControls.wrapper.classList.contains('d-none')) {
                hideImageResizeControls();
            }
        });

        imageInput.addEventListener('change', function () {
            if (!imageInput.files || imageInput.files.length === 0) {
                return;
            }

            focusEditor();
            storeSelection();

            var file = imageInput.files[0];
            var placeholderUrl = null;
            var placeholderImage = null;

            if (window.URL && typeof window.URL.createObjectURL === 'function') {
                try {
                    placeholderUrl = window.URL.createObjectURL(file);
                } catch (error) {
                    placeholderUrl = null;
                }
            }

            if (placeholderUrl) {
                placeholderImage = insertImageAtSelection(placeholderUrl, { isTemporary: true });
                if (placeholderImage) {
                    placeholderImage.setAttribute('data-upload-state', 'pending');
                    syncContent();
                    storeSelection();
                } else if (placeholderUrl && window.URL && typeof window.URL.revokeObjectURL === 'function') {
                    window.URL.revokeObjectURL(placeholderUrl);
                    placeholderUrl = null;
                }
            }

            uploadEditorImage(file)
                .then(function (url) {
                    if (!url) {
                        if (placeholderImage && placeholderImage.isConnected) {
                            removeImageElement(placeholderImage);
                            placeholderImage = null;
                            syncContent();
                            focusEditor();
                            storeSelection();
                        }
                        return;
                    }

                    if (placeholderImage && placeholderImage.isConnected) {
                        placeholderImage.src = url;
                        placeholderImage.removeAttribute('data-upload-state');
                        placeholderImage.removeAttribute('data-editor-temp-image');
                    } else {
                        placeholderImage = insertImageAtSelection(url);
                    }

                    if (placeholderImage) {
                        syncContent();
                        storeSelection();
                    }
                })
                .catch(function (error) {
                    console.error(error);
                    if (placeholderImage && placeholderImage.isConnected) {
                        removeImageElement(placeholderImage);
                        placeholderImage = null;
                        syncContent();
                        focusEditor();
                        storeSelection();
                    }
                    window.alert('امکان آپلود تصویر وجود ندارد. لطفاً دوباره تلاش کنید.');
                })
                .finally(function () {
                    if (placeholderUrl && window.URL && typeof window.URL.revokeObjectURL === 'function') {
                        window.URL.revokeObjectURL(placeholderUrl);
                    }
                    imageInput.value = '';
                });
        });

        form.addEventListener('submit', function () {
            hideImageResizeControls();
            syncContent();
        });

        function ensureImageResizeControls() {
            if (imageResizeControls) {
                return imageResizeControls;
            }

            var wrapper = document.createElement('div');
            wrapper.className = 'blog-editor__image-resizer d-none';
            wrapper.setAttribute('data-editor-image-resizer', 'true');

            var title = document.createElement('div');
            title.className = 'blog-editor__image-resizer__title';
            title.textContent = 'اندازه تصویر';
            wrapper.appendChild(title);

            var controlsRow = document.createElement('div');
            controlsRow.className = 'blog-editor__image-resizer__controls';

            var rangeInput = document.createElement('input');
            rangeInput.type = 'range';
            rangeInput.min = String(IMAGE_MIN_WIDTH_PERCENT);
            rangeInput.max = String(IMAGE_MAX_WIDTH_PERCENT);
            rangeInput.value = String(Math.min(IMAGE_DEFAULT_WIDTH_PERCENT, IMAGE_MAX_WIDTH_PERCENT));
            rangeInput.className = 'blog-editor__image-resizer__range';
            controlsRow.appendChild(rangeInput);

            var numberInput = document.createElement('input');
            numberInput.type = 'number';
            numberInput.min = String(IMAGE_MIN_WIDTH_PERCENT);
            numberInput.max = String(IMAGE_MAX_WIDTH_PERCENT);
            numberInput.value = String(Math.min(IMAGE_DEFAULT_WIDTH_PERCENT, IMAGE_MAX_WIDTH_PERCENT));
            numberInput.className = 'blog-editor__image-resizer__input';
            controlsRow.appendChild(numberInput);

            wrapper.appendChild(controlsRow);

            var footer = document.createElement('div');
            footer.className = 'blog-editor__image-resizer__footer';

            var hint = document.createElement('span');
            hint.className = 'blog-editor__image-resizer__hint';
            hint.textContent = 'عرض به درصد از پهنای محتوا';
            footer.appendChild(hint);

            var resetButton = document.createElement('button');
            resetButton.type = 'button';
            resetButton.className = 'blog-editor__image-resizer__reset';
            resetButton.textContent = 'بازگردانی به اندازه اصلی';
            footer.appendChild(resetButton);

            wrapper.appendChild(footer);

            rangeInput.addEventListener('input', function () {
                if (updatingResizeInputs) {
                    return;
                }

                var percent = parseFloat(rangeInput.value);
                if (isFiniteNumber(percent)) {
                    applyImageWidthPercent(percent, { source: 'range' });
                }
            });

            numberInput.addEventListener('input', function () {
                if (updatingResizeInputs) {
                    return;
                }

                var percent = parseFloat(numberInput.value);
                if (isFiniteNumber(percent)) {
                    applyImageWidthPercent(percent, { source: 'number' });
                }
            });

            numberInput.addEventListener('blur', function () {
                if (!activeImageElement) {
                    return;
                }

                var percent = parseFloat(numberInput.value);
                if (!isFiniteNumber(percent)) {
                    updateResizeInputs(readImageWidthPercent(activeImageElement));
                    return;
                }

                var sanitized = sanitizeImageWidthPercent(percent);
                updatingResizeInputs = true;
                numberInput.value = String(sanitized);
                rangeInput.value = String(sanitized);
                updatingResizeInputs = false;
            });

            resetButton.addEventListener('click', function (event) {
                event.preventDefault();
                resetImageWidth();
            });

            document.body.appendChild(wrapper);

            imageResizeControls = {
                wrapper: wrapper,
                range: rangeInput,
                input: numberInput,
                reset: resetButton
            };

            return imageResizeControls;
        }

        function isFiniteNumber(value) {
            return typeof value === 'number' && isFinite(value);
        }

        function sanitizeImageWidthPercent(value) {
            if (!isFiniteNumber(value)) {
                return IMAGE_DEFAULT_WIDTH_PERCENT;
            }

            return Math.min(IMAGE_MAX_WIDTH_PERCENT, Math.max(IMAGE_MIN_WIDTH_PERCENT, Math.round(value)));
        }

        function readImageWidthPercent(image) {
            if (!image) {
                return IMAGE_DEFAULT_WIDTH_PERCENT;
            }

            var stored = image.getAttribute('data-editor-image-width');
            if (stored) {
                var parsedStored = parseFloat(stored);
                if (isFiniteNumber(parsedStored) && parsedStored > 0) {
                    return sanitizeImageWidthPercent(parsedStored);
                }
            }

            var inlineWidth = image.style && image.style.width ? image.style.width.trim() : '';
            if (!inlineWidth) {
                inlineWidth = image.getAttribute('width') || '';
            }

            if (inlineWidth) {
                if (inlineWidth.endsWith('%')) {
                    var percentValue = parseFloat(inlineWidth);
                    if (isFiniteNumber(percentValue)) {
                        return sanitizeImageWidthPercent(percentValue);
                    }
                }

                if (inlineWidth.endsWith('px')) {
                    var pxValue = parseFloat(inlineWidth);
                    if (isFiniteNumber(pxValue) && editorArea.clientWidth > 0) {
                        var percentFromPx = (pxValue / editorArea.clientWidth) * 100;
                        return sanitizeImageWidthPercent(percentFromPx);
                    }
                }
            }

            var imageRect = image.getBoundingClientRect();
            var editorRect = editorArea.getBoundingClientRect();

            if (imageRect.width > 0 && editorRect.width > 0) {
                var ratioPercent = (imageRect.width / editorRect.width) * 100;
                return sanitizeImageWidthPercent(ratioPercent);
            }

            return IMAGE_DEFAULT_WIDTH_PERCENT;
        }

        function updateResizeInputs(percent) {
            var controls = ensureImageResizeControls();
            var sanitized = sanitizeImageWidthPercent(percent);
            updatingResizeInputs = true;
            controls.range.value = String(sanitized);
            controls.input.value = String(sanitized);
            updatingResizeInputs = false;
        }

        function setActiveImage(image) {
            if (activeImageElement && activeImageElement !== image) {
                activeImageElement.removeAttribute('data-editor-image-selected');
            }

            activeImageElement = image && image.isConnected ? image : null;

            if (activeImageElement) {
                activeImageElement.setAttribute('data-editor-image-selected', 'true');
            }
        }

        function showImageResizeControlsFor(image) {
            if (!image || !image.isConnected) {
                hideImageResizeControls();
                return;
            }

            var controls = ensureImageResizeControls();

            if (!controls.wrapper.isConnected) {
                document.body.appendChild(controls.wrapper);
            }

            setActiveImage(image);
            updateResizeInputs(readImageWidthPercent(image));

            controls.wrapper.classList.remove('d-none');

            if (!imageResizeListenersAttached) {
                window.addEventListener('scroll', queueImageResizeControlsPosition, true);
                window.addEventListener('resize', queueImageResizeControlsPosition);
                imageResizeListenersAttached = true;
            }

            queueImageResizeControlsPosition();
        }

        function hideImageResizeControls() {
            if (!imageResizeControls) {
                setActiveImage(null);
                return;
            }

            if (resizePositionFrame) {
                cancelAnimationFrame(resizePositionFrame);
                resizePositionFrame = null;
            }

            imageResizeControls.wrapper.classList.add('d-none');
            imageResizeControls.wrapper.removeAttribute('data-resizer-position');

            if (imageResizeListenersAttached) {
                window.removeEventListener('scroll', queueImageResizeControlsPosition, true);
                window.removeEventListener('resize', queueImageResizeControlsPosition);
                imageResizeListenersAttached = false;
            }

            setActiveImage(null);
        }

        function queueImageResizeControlsPosition() {
            if (!imageResizeControls || !activeImageElement || !activeImageElement.isConnected) {
                return;
            }

            if (imageResizeControls.wrapper.classList.contains('d-none')) {
                return;
            }

            if (resizePositionFrame) {
                cancelAnimationFrame(resizePositionFrame);
            }

            resizePositionFrame = window.requestAnimationFrame(positionImageResizeControls);
        }

        function positionImageResizeControls() {
            resizePositionFrame = null;

            if (!imageResizeControls || !activeImageElement || !activeImageElement.isConnected) {
                hideImageResizeControls();
                return;
            }

            var wrapper = imageResizeControls.wrapper;
            if (wrapper.classList.contains('d-none')) {
                return;
            }

            var imageRect = activeImageElement.getBoundingClientRect();
            var wrapperWidth = wrapper.offsetWidth;
            var wrapperHeight = wrapper.offsetHeight;
            var viewportWidth = document.documentElement.clientWidth || window.innerWidth || 0;
            var viewportHeight = document.documentElement.clientHeight || window.innerHeight || 0;
            var scrollX = window.scrollX || window.pageXOffset || 0;
            var scrollY = window.scrollY || window.pageYOffset || 0;

            var desiredLeft = scrollX + imageRect.left + (imageRect.width / 2) - (wrapperWidth / 2);
            var minLeft = scrollX + 12;
            var maxLeft = scrollX + Math.max(0, viewportWidth - wrapperWidth - 12);
            var left = Math.min(maxLeft, Math.max(minLeft, desiredLeft));

            var spaceBelow = viewportHeight - imageRect.bottom;
            var spaceAbove = imageRect.top;
            var useAbove = spaceBelow < wrapperHeight + 24 && spaceAbove > spaceBelow;

            var top;
            if (useAbove) {
                top = scrollY + imageRect.top - wrapperHeight - 16;
                wrapper.setAttribute('data-resizer-position', 'above');
            } else {
                top = scrollY + imageRect.bottom + 16;
                wrapper.setAttribute('data-resizer-position', 'below');
            }

            var minTop = scrollY + 12;
            var maxTop = scrollY + Math.max(0, viewportHeight - wrapperHeight - 12);
            top = Math.min(maxTop, Math.max(minTop, top));

            wrapper.style.left = String(Math.round(left)) + 'px';
            wrapper.style.top = String(Math.round(top)) + 'px';
        }

        function isImageInCurrentSelection(image) {
            if (!image || !image.isConnected) {
                return false;
            }

            var selection = window.getSelection();
            if (!selection || selection.rangeCount === 0) {
                return false;
            }

            var range = selection.getRangeAt(0);
            if (typeof range.intersectsNode === 'function') {
                try {
                    if (range.intersectsNode(image)) {
                        return true;
                    }
                } catch (error) {
                    // ignore
                }
            }

            var common = range.commonAncestorContainer;
            if (common === image || image.contains(common)) {
                return true;
            }

            return false;
        }

        function applyImageWidthPercent(percent, options) {
            if (!activeImageElement || !activeImageElement.isConnected) {
                return;
            }

            var sanitized = sanitizeImageWidthPercent(percent);
            updateResizeInputsFromSource(sanitized, options);

            activeImageElement.style.width = sanitized + '%';
            activeImageElement.style.height = 'auto';
            activeImageElement.setAttribute('data-editor-image-width', String(sanitized));

            syncContent();
            storeSelection();
            queueImageResizeControlsPosition();
        }

        function updateResizeInputsFromSource(percent, options) {
            var controls = ensureImageResizeControls();
            var sanitized = sanitizeImageWidthPercent(percent);
            updatingResizeInputs = true;
            if (!options || options.source !== 'range') {
                controls.range.value = String(sanitized);
            }
            if (!options || options.source !== 'number') {
                controls.input.value = String(sanitized);
            }
            updatingResizeInputs = false;
        }

        function resetImageWidth() {
            if (!activeImageElement || !activeImageElement.isConnected) {
                return;
            }

            activeImageElement.style.removeProperty('width');
            activeImageElement.style.removeProperty('height');
            activeImageElement.removeAttribute('width');
            activeImageElement.removeAttribute('height');
            activeImageElement.removeAttribute('data-editor-image-width');

            syncContent();
            storeSelection();

            window.requestAnimationFrame(function () {
                if (!activeImageElement || !activeImageElement.isConnected) {
                    hideImageResizeControls();
                    return;
                }

                updateResizeInputs(readImageWidthPercent(activeImageElement));
                queueImageResizeControlsPosition();
            });
        }

        function enableEditorFallback(wrapper, textarea, hint) {
            if (wrapper) {
                wrapper.classList.add('d-none');
            }
            textarea.classList.remove('d-none');
            textarea.classList.add('form-control');
            if (hint) {
                hint.classList.remove('d-none');
            }
        }

        function buildToolbar() {
            var toolbarElement = document.createElement('div');
            toolbarElement.className = 'blog-editor__toolbar';

            toolbarElement.appendChild(createGroup([
                createButton('چپ چین', 'bi-text-left', 'justifyLeft'),
                createButton('وسط چین', 'bi-text-center', 'justifyCenter'),
                createButton('راست چین', 'bi-text-right', 'justifyRight')
            ]));

            toolbarElement.appendChild(createGroup([
                createButton('بولد', 'bi-type-bold', 'bold'),
                createButton('ایتالیک', 'bi-type-italic', 'italic')
            ]));

            toolbarElement.appendChild(createGroup([
                createButton('فهرست شماره‌دار', 'bi-list-ol', 'insertOrderedList'),
                createButton('فهرست گلوله‌ای', 'bi-list-ul', 'insertUnorderedList')
            ]));

            toolbarElement.appendChild(createGroup([
                createSelect('سطح هدینگ', [
                    { value: 'P', label: 'پاراگراف', selected: true },
                    { value: 'H1', label: 'هدینگ 1' },
                    { value: 'H2', label: 'هدینگ 2' },
                    { value: 'H3', label: 'هدینگ 3' },
                    { value: 'H4', label: 'هدینگ 4' },
                    { value: 'H5', label: 'هدینگ 5' },
                    { value: 'H6', label: 'هدینگ 6' }
                ], 'formatBlock'),
                createSelect('اندازه فونت', [
                    { value: '', label: 'اندازه فونت' },
                    { value: '12px', label: '۱۲ px' },
                    { value: '14px', label: '۱۴ px' },
                    { value: '16px', label: '۱۶ px' },
                    { value: '18px', label: '۱۸ px' },
                    { value: '24px', label: '۲۴ px' },
                    { value: '32px', label: '۳۲ px' }
                ], 'fontSize'),
                createSelect('نوع فونت', [
                    { value: '', label: 'انتخاب فونت' },
                    { value: 'IRANSans, Vazirmatn, sans-serif', label: 'ایران سنس' },
                    { value: 'Tahoma, Geneva, sans-serif', label: 'Tahoma' },
                    { value: 'Arial, Helvetica, sans-serif', label: 'Arial' },
                    { value: 'Times New Roman, Times, serif', label: 'Times' },
                    { value: 'Courier New, Courier, monospace', label: 'Courier' }
                ], 'fontName')
            ]));

            toolbarElement.appendChild(createGroup([
                createColorPicker('رنگ متن', 'foreColor', '#1f2937'),
                createColorPicker('بک‌گراند', 'hiliteColor', '#fef3c7')
            ]));

            var tablePicker = createTablePicker();
            tablePickerControl = tablePicker;

            toolbarElement.appendChild(createGroup([
                tablePicker.wrapper,
                createButton('حذف سطر جدول', 'bi-x-circle', null, 'removeTableRow'),
                createButton('حذف ستون جدول', 'bi-x-octagon', null, 'removeTableColumn'),
                createButton('حذف جدول', 'bi-trash', null, 'removeTable')
            ]));

            toolbarElement.appendChild(createGroup([
                createButton('افزودن لینک', 'bi-link-45deg', null, 'createLink'),
                createButton('حذف لینک', 'bi-link-45deg', null, 'removeLink'),
                createButton('آپلود تصویر', 'bi-image', null, 'insertImage'),
                createButton('چاپ', 'bi-printer', null, 'print')
            ]));

            return toolbarElement;
        }

        function createGroup(children) {
            var group = document.createElement('div');
            group.className = 'blog-editor__group';
            children.forEach(function (child) {
                group.appendChild(child);
            });
            return group;
        }

        function createButton(title, iconClass, command, action) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'blog-editor__button';
            button.title = title;
            button.setAttribute('aria-label', title);

            if (command) {
                button.dataset.command = command;
            }

            if (action) {
                button.dataset.action = action;
            }

            if (iconClass) {
                var icon = document.createElement('i');
                icon.className = 'bi ' + iconClass;
                button.appendChild(icon);
            } else {
                button.textContent = title;
            }

            return button;
        }

        function createSelect(title, options, command) {
            var select = document.createElement('select');
            select.className = 'blog-editor__select';
            select.title = title;
            select.setAttribute('aria-label', title);
            if (command) {
                select.dataset.command = command;
            }

            options.forEach(function (option) {
                var optionElement = document.createElement('option');
                optionElement.value = option.value;
                optionElement.textContent = option.label;
                if (option.selected) {
                    optionElement.selected = true;
                }
                select.appendChild(optionElement);
            });

            return select;
        }

        function createColorPicker(labelText, command, defaultColor) {
            var wrapper = document.createElement('div');
            wrapper.className = 'blog-editor__color';

            var label = document.createElement('span');
            label.textContent = labelText;
            wrapper.appendChild(label);

            var input = document.createElement('input');
            input.type = 'color';
            input.value = defaultColor;
            input.dataset.command = command;
            wrapper.appendChild(input);

            return wrapper;
        }

        function createTablePicker() {
            var wrapper = document.createElement('div');
            wrapper.className = 'blog-editor__table-control';

            var trigger = createButton('افزودن جدول', 'bi-table');
            trigger.classList.add('blog-editor__table-trigger');

            var panel = document.createElement('div');
            panel.className = 'blog-editor__table-panel';
            panel.setAttribute('aria-hidden', 'true');

            var preview = document.createElement('div');
            preview.className = 'blog-editor__table-preview';
            preview.textContent = '0 × 0';

            var grid = document.createElement('div');
            grid.className = 'blog-editor__table-grid';

            var maxRows = 8;
            var maxColumns = 8;

            for (var row = 1; row <= maxRows; row += 1) {
                for (var column = 1; column <= maxColumns; column += 1) {
                    var cell = document.createElement('span');
                    cell.className = 'blog-editor__table-cell';
                    cell.dataset.row = String(row);
                    cell.dataset.column = String(column);
                    grid.appendChild(cell);
                }
            }

            var footer = document.createElement('div');
            footer.className = 'blog-editor__table-footer';

            var caption = document.createElement('span');
            caption.className = 'blog-editor__table-caption';
            caption.textContent = 'Create table';

            var wizard = document.createElement('button');
            wizard.type = 'button';
            wizard.className = 'blog-editor__table-wizard';
            wizard.textContent = 'Table Wizard';

            footer.appendChild(caption);
            footer.appendChild(wizard);

            panel.appendChild(preview);
            panel.appendChild(grid);
            panel.appendChild(footer);

            wrapper.appendChild(trigger);
            wrapper.appendChild(panel);

            var highlightedRows = 0;
            var highlightedColumns = 0;

            function updateHighlight(rows, columns) {
                highlightedRows = rows;
                highlightedColumns = columns;

                var cells = panel.querySelectorAll('.blog-editor__table-cell');
                cells.forEach(function (cell) {
                    var cellRow = parseInt(cell.dataset.row || '0', 10);
                    var cellColumn = parseInt(cell.dataset.column || '0', 10);
                    var shouldHighlight = rows > 0 && columns > 0 && cellRow <= rows && cellColumn <= columns;
                    cell.classList.toggle('is-active', shouldHighlight);
                });

                preview.textContent = rows > 0 && columns > 0
                    ? rows + ' × ' + columns
                    : '0 × 0';
            }

            function showPanel() {
                panel.classList.add('is-open');
                panel.setAttribute('aria-hidden', 'false');
                updateHighlight(highlightedRows, highlightedColumns);
            }

            function hidePanel() {
                panel.classList.remove('is-open');
                panel.setAttribute('aria-hidden', 'true');
                updateHighlight(0, 0);
            }

            function togglePanel() {
                if (panel.classList.contains('is-open')) {
                    hidePanel();
                } else {
                    showPanel();
                }
            }

            trigger.addEventListener('click', function (event) {
                event.preventDefault();
                togglePanel();
            });

            [trigger, panel].forEach(function (element) {
                element.addEventListener('pointerdown', function (event) {
                    event.stopPropagation();
                });
            });

            function handleGridHover(event) {
                var target = event.target.closest('.blog-editor__table-cell');
                if (!target) {
                    return;
                }

                var rows = parseInt(target.dataset.row || '0', 10);
                var columns = parseInt(target.dataset.column || '0', 10);
                updateHighlight(rows, columns);
            }

            grid.addEventListener('pointerover', handleGridHover);
            grid.addEventListener('pointermove', handleGridHover);
            grid.addEventListener('mousemove', handleGridHover);

            grid.addEventListener('mouseleave', function () {
                updateHighlight(0, 0);
            });

            grid.addEventListener('click', function (event) {
                var target = event.target.closest('.blog-editor__table-cell');
                if (!target) {
                    return;
                }

                var rows = parseInt(target.dataset.row || '0', 10);
                var columns = parseInt(target.dataset.column || '0', 10);
                if (!rows || !columns) {
                    return;
                }

                focusEditor();
                insertTable(rows, columns);
                syncContent();
                storeSelection();
                hidePanel();
            });

            wizard.addEventListener('click', function (event) {
                event.preventDefault();
                hidePanel();
                focusEditor();
                insertTable();
                syncContent();
                storeSelection();
            });

            return {
                wrapper: wrapper,
                hide: hidePanel,
                show: showPanel,
                toggle: togglePanel,
                isOpen: function () {
                    return panel.classList.contains('is-open');
                }
            };
        }

        function buildEditorArea(initialHtml) {
            var editor = document.createElement('div');
            editor.className = 'blog-editor__content';
            editor.contentEditable = 'true';
            editor.setAttribute('dir', 'rtl');
            editor.setAttribute('data-placeholder', 'محتوای بلاگ را اینجا بنویسید...');
            editor.innerHTML = initialHtml || '<p><br></p>';
            return editor;
        }

        function buildHiddenImageInput() {
            var input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';
            input.classList.add('d-none');
            return input;
        }

        function applyFontSize(value) {
            if (!value) {
                return;
            }

            var fontSizeMap = {
                '12px': '2',
                '14px': '3',
                '16px': '4',
                '18px': '5',
                '24px': '6',
                '32px': '7'
            };

            var sizeCommand = fontSizeMap[value];
            if (!sizeCommand) {
                return;
            }

            document.execCommand('fontSize', false, sizeCommand);
            normalizeFontTags(value, null);
        }

        function normalizeFontTags(preferredSize, preferredFace) {
            var sizeLookup = {
                '1': '10px',
                '2': '12px',
                '3': '14px',
                '4': '16px',
                '5': '18px',
                '6': '24px',
                '7': '32px'
            };

            var fonts = editorArea.querySelectorAll('font[size], font[face]');
            fonts.forEach(function (font) {
                var span = document.createElement('span');
                span.innerHTML = font.innerHTML;

                var size = font.getAttribute('size');
                if (size) {
                    var mapped = sizeLookup[size] || preferredSize;
                    if (mapped) {
                        span.style.fontSize = mapped;
                    }
                }

                var face = font.getAttribute('face');
                var resolvedFace = typeof preferredFace === 'string' ? preferredFace : face;
                if (resolvedFace) {
                    span.style.fontFamily = resolvedFace;
                }

                font.replaceWith(span);
            });

            if (typeof preferredFace === 'string' && preferredFace.trim() === '') {
                var styledSpans = editorArea.querySelectorAll('span');
                styledSpans.forEach(function (span) {
                    if (span.style && span.style.fontFamily) {
                        span.style.fontFamily = '';
                        if (span.style.length === 0) {
                            span.removeAttribute('style');
                        }
                    }
                });
            }
        }

        function applyHighlightColor(color) {
            var command = document.queryCommandSupported('hiliteColor') ? 'hiliteColor' : 'backColor';
            document.execCommand(command, false, color);
        }

        function handleLinkCreation() {
            var selection = window.getSelection();
            if (!selection || selection.rangeCount === 0 || selection.toString().trim().length === 0) {
                window.alert('برای افزودن لینک ابتدا متنی را انتخاب کنید.');
                return;
            }

            var url = window.prompt('آدرس لینک را وارد کنید:', 'https://');
            if (!url) {
                return;
            }

            var normalizedUrl = url.trim();
            if (normalizedUrl && !/^https?:\/\//i.test(normalizedUrl) && !/^mailto:/i.test(normalizedUrl)) {
                normalizedUrl = 'https://' + normalizedUrl;
            }

            document.execCommand('createLink', false, normalizedUrl);
        }

        function insertTable(rows, columns) {
            var rowCount = parseInt(rows, 10);
            var columnCount = parseInt(columns, 10);

            if (!rowCount || !columnCount || rowCount < 1 || columnCount < 1 || rowCount > 10 || columnCount > 10) {
                var requestedRows = window.prompt('تعداد سطرهای جدول را وارد کنید (۱ تا ۱۰):', rowCount && rowCount >= 1 ? String(rowCount) : '2');
                var requestedColumns = window.prompt('تعداد ستون‌های جدول را وارد کنید (۱ تا ۱۰):', columnCount && columnCount >= 1 ? String(columnCount) : '2');

                rowCount = parseInt(requestedRows || '0', 10);
                columnCount = parseInt(requestedColumns || '0', 10);
            }

            if (!rowCount || !columnCount || rowCount < 1 || columnCount < 1 || rowCount > 10 || columnCount > 10) {
                return;
            }

            var html = '<table><tbody>';
            for (var r = 0; r < rowCount; r += 1) {
                html += '<tr>';
                for (var c = 0; c < columnCount; c += 1) {
                    html += '<td>&nbsp;</td>';
                }
                html += '</tr>';
            }
            html += '</tbody></table><p><br></p>';

            document.execCommand('insertHTML', false, html);
        }

        function closestElement(node, selector) {
            var current = node instanceof Node ? node : null;
            while (current) {
                if (current.nodeType === Node.ELEMENT_NODE && current.matches(selector)) {
                    return current;
                }
                current = current.parentNode;
            }
            return null;
        }

        function ensureCellHasPlaceholder(cell) {
            if (!cell) {
                return;
            }

            var hasContent = Array.prototype.some.call(cell.childNodes || [], function (node) {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    return true;
                }
                if (node.nodeType === Node.TEXT_NODE) {
                    return node.textContent && node.textContent.trim().length > 0;
                }
                return false;
            });

            if (hasContent) {
                return;
            }

            cell.innerHTML = '&nbsp;';
        }

        function focusTableCell(cell) {
            if (!cell) {
                return;
            }

            ensureCellHasPlaceholder(cell);
            var range = document.createRange();
            range.selectNodeContents(cell);
            range.collapse(true);
            applyRange(range);
        }

        function focusParagraph(paragraph) {
            if (!paragraph) {
                return;
            }

            if (!paragraph.childNodes || paragraph.childNodes.length === 0) {
                paragraph.appendChild(document.createElement('br'));
            }

            var range = document.createRange();
            range.selectNodeContents(paragraph);
            range.collapse(false);
            applyRange(range);
        }

        function createFallbackParagraph(referenceNode) {
            if (referenceNode && referenceNode.parentNode) {
                var nextElement = referenceNode.nextElementSibling;
                if (nextElement && nextElement.tagName && nextElement.tagName.toLowerCase() === 'p') {
                    return nextElement;
                }
            }

            var paragraph = document.createElement('p');
            paragraph.appendChild(document.createElement('br'));

            if (referenceNode && referenceNode.parentNode) {
                if (referenceNode.nextSibling) {
                    referenceNode.parentNode.insertBefore(paragraph, referenceNode.nextSibling);
                } else {
                    referenceNode.parentNode.appendChild(paragraph);
                }
            } else {
                editorArea.appendChild(paragraph);
            }

            return paragraph;
        }

        function removeTableElement(table) {
            if (!table) {
                return false;
            }

            var targetParagraph = createFallbackParagraph(table);
            table.remove();
            focusParagraph(targetParagraph);
            return true;
        }

        function removeActiveTable() {
            var range = getActiveRange();
            if (!range) {
                window.alert('برای حذف جدول ابتدا داخل جدول کلیک کنید.');
                return false;
            }

            var table = closestElement(range.startContainer, 'table');
            if (!table) {
                window.alert('برای حذف جدول ابتدا داخل جدول کلیک کنید.');
                return false;
            }

            return removeTableElement(table);
        }

        function removeActiveTableRow() {
            var range = getActiveRange();
            if (!range) {
                window.alert('برای حذف سطر ابتدا داخل جدول قرار بگیرید.');
                return false;
            }

            var cell = closestElement(range.startContainer, 'td,th');
            if (!cell) {
                window.alert('برای حذف سطر ابتدا داخل جدول قرار بگیرید.');
                return false;
            }

            var row = closestElement(cell, 'tr');
            var table = closestElement(cell, 'table');

            if (!row || !table) {
                return false;
            }

            var tbody = row.parentNode;
            var nextFocusCell = row.nextElementSibling ? row.nextElementSibling.querySelector('td,th') : null;
            if (!nextFocusCell && row.previousElementSibling) {
                nextFocusCell = row.previousElementSibling.querySelector('td,th');
            }

            row.remove();

            if (!tbody || !tbody.querySelector('tr')) {
                return removeTableElement(table);
            }

            if (nextFocusCell) {
                focusTableCell(nextFocusCell);
            } else {
                focusParagraph(createFallbackParagraph(table));
            }

            return true;
        }

        function removeActiveTableColumn() {
            var range = getActiveRange();
            if (!range) {
                window.alert('برای حذف ستون ابتدا داخل جدول قرار بگیرید.');
                return false;
            }

            var cell = closestElement(range.startContainer, 'td,th');
            if (!cell) {
                window.alert('برای حذف ستون ابتدا داخل جدول قرار بگیرید.');
                return false;
            }

            var row = closestElement(cell, 'tr');
            var table = closestElement(cell, 'table');
            if (!row || !table) {
                return false;
            }

            var rowsBefore = Array.prototype.slice.call(table.querySelectorAll('tr'));
            var rowIndex = rowsBefore.indexOf(row);
            var cellIndex = Array.prototype.indexOf.call(row.children, cell);

            if (cellIndex < 0) {
                return false;
            }

            rowsBefore.forEach(function (currentRow) {
                if (currentRow.children[cellIndex]) {
                    currentRow.removeChild(currentRow.children[cellIndex]);
                }
            });

            var remainingRows = Array.prototype.slice.call(table.querySelectorAll('tr'));
            remainingRows.forEach(function (currentRow) {
                if (!currentRow.children.length) {
                    currentRow.remove();
                }
            });

            remainingRows = Array.prototype.slice.call(table.querySelectorAll('tr'));
            if (!remainingRows.length) {
                return removeTableElement(table);
            }

            var focusRow = row.isConnected ? row : null;
            if (!focusRow) {
                var candidateIndex = Math.min(Math.max(rowIndex, 0), remainingRows.length - 1);
                focusRow = remainingRows[candidateIndex] || remainingRows[remainingRows.length - 1];
            }

            if (!focusRow) {
                focusParagraph(createFallbackParagraph(table));
                return true;
            }

            var targetCell = focusRow.children[cellIndex] || focusRow.children[cellIndex - 1] || focusRow.querySelector('td,th');
            if (targetCell) {
                focusTableCell(targetCell);
            } else {
                focusParagraph(createFallbackParagraph(table));
            }

            return true;
        }

        function insertImageAtSelection(url, options) {
            if (!url) {
                return null;
            }

            var range = getActiveRange();
            if (!range) {
                range = document.createRange();
                range.selectNodeContents(editorArea);
                range.collapse(false);
            }

            applyRange(range);
            range = getActiveRange();
            if (!range) {
                return null;
            }

            var image = document.createElement('img');
            image.src = url;
            image.alt = '';
            image.setAttribute('data-uploaded-image', 'true');

            if (options && options.isTemporary) {
                image.setAttribute('data-editor-temp-image', 'true');
            }

            var workingRange = range.cloneRange();
            workingRange.collapse(false);
            workingRange.insertNode(image);

            var trailing = image.nextSibling;
            if (!trailing || trailing.nodeType !== Node.TEXT_NODE) {
                trailing = document.createTextNode(' ');
                if (image.parentNode) {
                    image.parentNode.insertBefore(trailing, image.nextSibling);
                }
            }

            var afterRange = document.createRange();
            afterRange.setStart(trailing, trailing.textContent.length);
            afterRange.setEnd(trailing, trailing.textContent.length);
            applyRange(afterRange);

            return image;
        }

        function removeImageElement(image) {
            if (!image) {
                return;
            }

            var trailingSibling = image.nextSibling;
            var parent = image.parentNode;
            image.remove();

            if (trailingSibling && trailingSibling.parentNode === parent && trailingSibling.nodeType === Node.TEXT_NODE) {
                var textContent = trailingSibling.textContent || '';
                var normalized = textContent.replace(/ /g, '').trim();
                if (normalized.length === 0) {
                    trailingSibling.remove();
                }
            }
        }

        function printEditorContent() {
            var printWindow = window.open('', '_blank', 'width=960,height=720');
            if (!printWindow) {
                window.alert('مرورگر امکان باز کردن پنجره چاپ را نداد.');
                return;
            }

            var html = '<html><head><title>چاپ محتوا</title>' +
                '<style>body{direction:rtl;font-family:Tahoma,Arial,sans-serif;padding:2rem;line-height:1.8;} table{width:100%;border-collapse:collapse;margin:1rem 0;} th,td{border:1px solid #94a3b8;padding:0.5rem;text-align:center;} img{max-width:100%;height:auto;display:block;margin:0.75rem auto;border-radius:0.5rem;}</style>' +
                '</head><body>' + editorArea.innerHTML + '</body></html>';

            printWindow.document.open();
            printWindow.document.write(html);
            printWindow.document.close();
            printWindow.focus();
            printWindow.print();
        }

        function uploadEditorImage(file) {
            if (!file) {
                return Promise.resolve(null);
            }

            if (!uploadUrl) {
                return readFileAsDataUrl(file);
            }

            var formData = new FormData();
            formData.append('file', file);
            var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
            if (tokenInput) {
                formData.append('__RequestVerificationToken', tokenInput.value);
            }

            return fetch(uploadUrl, {
                method: 'POST',
                body: formData,
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                }
            })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Upload failed');
                    }
                    return response.json();
                })
                .then(function (data) {
                    if (data && data.url) {
                        return data.url;
                    }
                    if (data && data.error) {
                        window.alert(data.error);
                    }
                    return null;
                });
        }

        function readFileAsDataUrl(file) {
            return new Promise(function (resolve, reject) {
                var reader = new FileReader();
                reader.onload = function (event) {
                    resolve(event.target && event.target.result ? String(event.target.result) : null);
                };
                reader.onerror = function (error) {
                    reject(error);
                };
                reader.readAsDataURL(file);
            });
        }
    }
    global.AdminRichEditor = {
        init: initRichEditor,
        initEditor: initEditor
    };
})(typeof window !== 'undefined' ? window : null);
