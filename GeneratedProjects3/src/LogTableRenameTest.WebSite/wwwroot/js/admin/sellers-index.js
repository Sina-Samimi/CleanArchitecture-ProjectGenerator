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

            const sellerName = trigger.getAttribute('data-seller-name') ?? '';
            const sellerId = trigger.getAttribute('data-seller-id') ?? '';

            if (namePlaceholder) {
                namePlaceholder.textContent = sellerName;
            }

            if (idInput) {
                idInput.value = sellerId;
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

    setupModal('deactivateSellerModal', '[data-seller-deactivate-name]', '[data-seller-deactivate-id]');
    setupModal('activateSellerModal', '[data-seller-activate-name]', '[data-seller-activate-id]');
})();
