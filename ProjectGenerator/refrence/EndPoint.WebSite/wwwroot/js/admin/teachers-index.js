(function () {
    const doc = document;

    function setupModal(modalId, nameSelector, inputSelector) {
        const modal = doc.getElementById(modalId);
        if (!modal) {
            return;
        }

        const namePlaceholder = modal.querySelector(nameSelector);
        const idInput = modal.querySelector(inputSelector);

        modal.addEventListener('show.bs.modal', event => {
            const trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            const teacherName = trigger.getAttribute('data-teacher-name') ?? '';
            const teacherId = trigger.getAttribute('data-teacher-id') ?? '';

            if (namePlaceholder) {
                namePlaceholder.textContent = teacherName;
            }

            if (idInput) {
                idInput.value = teacherId;
            }
        });

        modal.addEventListener('hidden.bs.modal', () => {
            if (namePlaceholder) {
                namePlaceholder.textContent = '';
            }

            if (idInput) {
                idInput.value = '';
            }
        });
    }

    setupModal('deactivateTeacherModal', '[data-teacher-deactivate-name]', '[data-teacher-deactivate-id]');
    setupModal('activateTeacherModal', '[data-teacher-activate-name]', '[data-teacher-activate-id]');
    setupModal('removeTeacherModal', '[data-teacher-remove-name]', '[data-teacher-remove-id]');
})();
