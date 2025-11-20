(function () {
    document.addEventListener('DOMContentLoaded', function () {
        setupCategoryCreateModal();
        setupCategoryEditModal();
        setupCategoryDeleteModal();
    });

    function setupCategoryCreateModal() {
        var modalElement = document.getElementById('createCategoryModal');
        if (!modalElement) {
            return;
        }

        var nameInput = modalElement.querySelector('input[name="Name"]');
        var slugInput = modalElement.querySelector('input[name="Slug"]');
        var descriptionInput = modalElement.querySelector('textarea[name="Description"]');
        var parentSelect = modalElement.querySelector('select[name="ParentId"]');

        modalElement.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (nameInput) {
                nameInput.value = '';
                nameInput.focus();
            }

            if (slugInput) {
                slugInput.value = '';
            }

            if (descriptionInput) {
                descriptionInput.value = '';
            }

            if (parentSelect) {
                var parentId = trigger ? trigger.getAttribute('data-category-parent-id') : '';
                parentSelect.value = parentId || '';
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (nameInput) {
                nameInput.value = '';
            }

            if (slugInput) {
                slugInput.value = '';
            }

            if (descriptionInput) {
                descriptionInput.value = '';
            }

            if (parentSelect) {
                parentSelect.value = '';
            }
        });
    }

    function setupCategoryEditModal() {
        var modalElement = document.getElementById('editCategoryModal');
        if (!modalElement) {
            return;
        }

        var idInput = modalElement.querySelector('input[name="Id"]');
        var nameInput = modalElement.querySelector('input[name="Name"]');
        var slugInput = modalElement.querySelector('input[name="Slug"]');
        var descriptionInput = modalElement.querySelector('textarea[name="Description"]');
        var parentSelect = modalElement.querySelector('select[name="ParentId"]');

        modalElement.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var categoryId = trigger.getAttribute('data-category-id') || '';
            var categoryName = trigger.getAttribute('data-category-name') || '';
            var categorySlug = trigger.getAttribute('data-category-slug') || '';
            var categoryDescription = trigger.getAttribute('data-category-description') || '';
            var parentId = trigger.getAttribute('data-category-parent-id') || '';
            var descendantsRaw = trigger.getAttribute('data-category-descendants') || '';

            var blockedIds = [];
            if (categoryId) {
                blockedIds.push(categoryId.toLowerCase());
            }

            if (descendantsRaw) {
                var descendantList = descendantsRaw.split('|');
                descendantList.forEach(function (item) {
                    var normalized = item.trim().toLowerCase();
                    if (normalized) {
                        blockedIds.push(normalized);
                    }
                });
            }

            if (idInput) {
                idInput.value = categoryId;
            }

            if (nameInput) {
                nameInput.value = categoryName;
                nameInput.focus();
            }

            if (slugInput) {
                slugInput.value = categorySlug;
            }

            if (descriptionInput) {
                descriptionInput.value = categoryDescription;
            }

            if (parentSelect) {
                var options = Array.prototype.slice.call(parentSelect.options);
                options.forEach(function (option) {
                    option.disabled = false;
                });

                options.forEach(function (option) {
                    var value = option.value ? option.value.toLowerCase() : '';
                    if (blockedIds.indexOf(value) !== -1) {
                        option.disabled = true;
                    }
                });

                parentSelect.value = parentId;

                if (blockedIds.indexOf((parentSelect.value || '').toLowerCase()) !== -1) {
                    parentSelect.value = '';
                }
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (idInput) {
                idInput.value = '';
            }

            if (nameInput) {
                nameInput.value = '';
            }

            if (slugInput) {
                slugInput.value = '';
            }

            if (descriptionInput) {
                descriptionInput.value = '';
            }

            if (parentSelect) {
                var options = Array.prototype.slice.call(parentSelect.options);
                options.forEach(function (option) {
                    option.disabled = false;
                });
                parentSelect.value = '';
            }
        });
    }

    function setupCategoryDeleteModal() {
        var modalElement = document.getElementById('deleteCategoryModal');
        if (!modalElement) {
            return;
        }

        var namePlaceholder = modalElement.querySelector('[data-category-delete-name]');
        var form = modalElement.querySelector('[data-category-delete-form]');
        var idInput = form ? form.querySelector('input[name="id"]') : null;

        modalElement.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var categoryName = trigger.getAttribute('data-category-name') || '';
            var categoryId = trigger.getAttribute('data-category-id') || '';

            if (namePlaceholder) {
                namePlaceholder.textContent = categoryName;
            }

            if (idInput) {
                idInput.value = categoryId;
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (namePlaceholder) {
                namePlaceholder.textContent = '';
            }

            if (idInput) {
                idInput.value = '';
            }
        });
    }
})();
