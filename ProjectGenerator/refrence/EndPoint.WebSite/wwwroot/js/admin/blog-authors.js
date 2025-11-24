(function () {
    document.addEventListener('DOMContentLoaded', function () {
        setupAuthorCreateModal();
        setupAuthorEditModal();
        setupAuthorDeleteModal();
    });

    function setupAuthorCreateModal() {
        var modalElement = document.getElementById('createAuthorModal');
        if (!modalElement) {
            return;
        }

        var form = modalElement.querySelector('form');
        var userSelect = modalElement.querySelector('[data-author-user-select]');
        var nameInput = modalElement.querySelector('input[name="DisplayName"]');
        var bioInput = modalElement.querySelector('textarea[name="Bio"]');
        var avatarInput = modalElement.querySelector('input[name="AvatarUrl"]');
        var activeCheckbox = modalElement.querySelector('input[name="IsActive"][type="checkbox"]');

        modalElement.addEventListener('show.bs.modal', function () {
            if (form) {
                form.reset();
            }

            if (userSelect) {
                resetUserOptions(userSelect, null);
                userSelect.value = '';
            }

            if (nameInput) {
                nameInput.value = '';
                nameInput.dataset.autofill = '';
                nameInput.focus();
            }

            if (bioInput) {
                bioInput.value = '';
            }

            if (avatarInput) {
                avatarInput.value = '';
            }

            if (activeCheckbox) {
                activeCheckbox.checked = true;
            }
        });

        if (userSelect && nameInput) {
            userSelect.addEventListener('change', function () {
                handleNameAutofill(userSelect, nameInput);
            });
        }
    }

    function setupAuthorEditModal() {
        var modalElement = document.getElementById('editAuthorModal');
        if (!modalElement) {
            return;
        }

        var idInput = modalElement.querySelector('input[name="Id"]');
        var userSelect = modalElement.querySelector('[data-author-user-select]');
        var nameInput = modalElement.querySelector('input[name="DisplayName"]');
        var bioInput = modalElement.querySelector('textarea[name="Bio"]');
        var avatarInput = modalElement.querySelector('input[name="AvatarUrl"]');
        var activeCheckbox = modalElement.querySelector('input[name="IsActive"][type="checkbox"]');

        modalElement.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var authorId = trigger.getAttribute('data-author-id') || '';
            var authorName = trigger.getAttribute('data-author-name') || '';
            var authorBio = trigger.getAttribute('data-author-bio') || '';
            var authorAvatar = trigger.getAttribute('data-author-avatar') || '';
            var authorActive = trigger.getAttribute('data-author-active') === 'true';
            var authorUserId = trigger.getAttribute('data-author-user-id') || '';

            if (idInput) {
                idInput.value = authorId;
            }

            if (nameInput) {
                nameInput.value = authorName;
                nameInput.dataset.autofill = trigger.getAttribute('data-author-user-name') || '';
                nameInput.focus();
            }

            if (bioInput) {
                bioInput.value = authorBio;
            }

            if (avatarInput) {
                avatarInput.value = authorAvatar;
            }

            if (activeCheckbox) {
                activeCheckbox.checked = authorActive;
            }

            if (userSelect) {
                resetUserOptions(userSelect, authorUserId);
                userSelect.value = authorUserId;
            }
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (idInput) {
                idInput.value = '';
            }

            if (nameInput) {
                nameInput.value = '';
                nameInput.dataset.autofill = '';
            }

            if (bioInput) {
                bioInput.value = '';
            }

            if (avatarInput) {
                avatarInput.value = '';
            }

            if (activeCheckbox) {
                activeCheckbox.checked = false;
            }

            if (userSelect) {
                resetUserOptions(userSelect, null);
                userSelect.value = '';
            }
        });

        if (userSelect && nameInput) {
            userSelect.addEventListener('change', function () {
                handleNameAutofill(userSelect, nameInput);
            });
        }
    }

    function setupAuthorDeleteModal() {
        var modalElement = document.getElementById('deleteAuthorModal');
        if (!modalElement) {
            return;
        }

        var namePlaceholder = modalElement.querySelector('[data-author-delete-name]');
        var form = modalElement.querySelector('[data-author-delete-form]');
        var idInput = form ? form.querySelector('input[name="id"]') : null;

        modalElement.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var authorName = trigger.getAttribute('data-author-name') || '';
            var authorId = trigger.getAttribute('data-author-id') || '';

            if (namePlaceholder) {
                namePlaceholder.textContent = authorName;
            }

            if (idInput) {
                idInput.value = authorId;
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

    function resetUserOptions(select, currentUserId) {
        var options = select ? Array.prototype.slice.call(select.options) : [];
        options.forEach(function (option) {
            if (!option.value) {
                option.disabled = false;
                return;
            }

            var isAssigned = option.getAttribute('data-user-assigned') === 'true';
            option.disabled = isAssigned && option.value !== (currentUserId || '');
        });
    }

    function handleNameAutofill(select, nameInput) {
        if (!select || !nameInput) {
            return;
        }

        var selectedOption = select.options[select.selectedIndex];
        var newName = selectedOption ? selectedOption.getAttribute('data-user-name') || '' : '';
        var previousAutoFill = nameInput.dataset.autofill || '';

        if (!nameInput.value || nameInput.value === previousAutoFill) {
            nameInput.value = newName;
        }

        nameInput.dataset.autofill = newName;
    }
})();
