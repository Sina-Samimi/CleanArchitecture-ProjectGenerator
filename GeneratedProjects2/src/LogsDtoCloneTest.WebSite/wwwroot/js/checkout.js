// Checkout Page JavaScript
(function() {
    'use strict';

    // Ensure jQuery is available globally
    if (typeof jQuery !== 'undefined') {
        window.$ = window.jQuery = jQuery;
    }

    // Wait for DOM
    document.addEventListener('DOMContentLoaded', function() {
        initializeCheckout();
    });

    function initializeCheckout() {
        // Ensure Bootstrap Modal is available
        if (typeof bootstrap === 'undefined' && typeof $ !== 'undefined') {
            window.bootstrap = {
                Modal: function(element) {
                    return $(element).modal();
                }
            };
            bootstrap.Modal.getInstance = function(element) {
                return $(element).data('bs.modal') ? {
                    hide: function() { $(element).modal('hide'); }
                } : null;
            };
        }

        initializeAddressSelection();
        initializeCheckoutForm();
        initializeAddAddressModal();
        initializeOptionalFields();
    }

    // Local toast helper (keeps behavior consistent with other pages)
    function showToast(message, type = 'info') {
        let toastStack = document.querySelector('[data-app-toast-stack]');
        if (!toastStack) {
            toastStack = document.createElement('div');
            toastStack.className = 'app-toast-stack';
            toastStack.setAttribute('data-app-toast-stack', '');
            document.body.appendChild(toastStack);
        }

        const toastClass = type === 'danger' ? 'app-toast--danger' :
                          type === 'success' ? 'app-toast--success' : 'app-toast';
        const iconMark = type === 'danger' ? '!' :
                        type === 'success' ? '✓' : 'i';

        const toast = document.createElement('div');
        toast.className = `app-toast ${toastClass}`;
        toast.setAttribute('role', type === 'danger' ? 'alert' : 'status');
        toast.setAttribute('aria-live', type === 'danger' ? 'assertive' : 'polite');
        toast.setAttribute('data-app-toast', '');
        toast.setAttribute('data-toast-lifetime', type === 'danger' ? '6000' : '3000');

        toast.innerHTML = `
            <span class="app-toast__icon" aria-hidden="true">
                <span class="app-toast__icon-mark">${iconMark}</span>
            </span>
            <div class="app-toast__body">${message}</div>
            <button type="button" class="app-toast__close" aria-label="بستن اعلان" data-app-toast-dismiss>
                <span aria-hidden="true">×</span>
            </button>
        `;

        toastStack.appendChild(toast);

        requestAnimationFrame(() => {
            toast.classList.add('is-visible');
        });

        const dismissBtn = toast.querySelector('[data-app-toast-dismiss]');
        if (dismissBtn) {
            dismissBtn.addEventListener('click', () => hideToast(toast));
        }

        const lifetime = parseInt(toast.getAttribute('data-toast-lifetime') || '3000', 10);
        if (lifetime > 0) {
            setTimeout(() => hideToast(toast), lifetime);
        }
    }

    function hideToast(toast) {
        if (!toast || toast.classList.contains('is-hiding')) {
            return;
        }

        toast.classList.remove('is-visible');
        toast.classList.add('is-hiding');
        const remove = () => {
            toast.removeEventListener('transitionend', remove);
            toast.remove();
            const toastStack = document.querySelector('[data-app-toast-stack]');
            if (toastStack && !toastStack.querySelector('[data-app-toast]')) {
                toastStack.remove();
            }
        };

        toast.addEventListener('transitionend', remove);
    }

    function initializeAddressSelection() {
        // Handle address selection
        document.querySelectorAll('input[name="SelectedAddressId"]').forEach(function(radio) {
            radio.addEventListener('change', function() {
                document.querySelectorAll('.address-card').forEach(function(card) {
                    card.classList.remove('selected');
                });
                if (this.checked) {
                    this.closest('.address-card').classList.add('selected');
                    const selectedInput = document.getElementById('selected-address-input');
                    if (selectedInput) {
                        selectedInput.value = this.value;
                    }
                }
            });
        });

        // Set initial selected address
        const checkedRadio = document.querySelector('input[name="SelectedAddressId"]:checked');
        if (checkedRadio) {
            const selectedInput = document.getElementById('selected-address-input');
            if (selectedInput) {
                selectedInput.value = checkedRadio.value;
            }
        }
    }

    function initializeCheckoutForm() {
        const checkoutForm = document.getElementById('checkout-form');
        if (!checkoutForm) return;

        checkoutForm.addEventListener('submit', function(e) {
            const selectedAddress = document.querySelector('input[name="SelectedAddressId"]:checked');
            if (!selectedAddress) {
                e.preventDefault();
                showToast('لطفاً یک آدرس ارسال انتخاب کنید.', 'danger');
                return false;
            }
            const selectedInput = document.getElementById('selected-address-input');
            if (selectedInput) {
                selectedInput.value = selectedAddress.value;
            }
        });
    }

    function initializeAddAddressModal() {
        const modalElement = document.getElementById('addAddressModal');
        if (!modalElement) return;

        // Handle add address button click
        const addAddressBtn = document.querySelector('.btn-add-address');
        if (addAddressBtn) {
            addAddressBtn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                openModal();
            });
        }

        // Handle add address form submission
        const addAddressForm = document.getElementById('add-address-form');
        if (addAddressForm) {
            addAddressForm.addEventListener('submit', function(e) {
                e.preventDefault();
                handleAddAddressSubmit(this);
            });
        }

        // Reset modal on close
        modalElement.addEventListener('hidden.bs.modal', function() {
            const form = document.getElementById('add-address-form');
            if (form) form.reset();
        });

        if (typeof $ !== 'undefined') {
            $(modalElement).on('hidden.bs.modal', function() {
                const form = document.getElementById('add-address-form');
                if (form) form.reset();
            });
        }

        // Handle close buttons
        const closeButtons = modalElement.querySelectorAll('[data-bs-dismiss="modal"], .btn-close, .btn-secondary');
        closeButtons.forEach(function(btn) {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                closeModal();
            });
        });

        // Handle backdrop click
        modalElement.addEventListener('click', function(e) {
            if (e.target === modalElement) {
                closeModal();
            }
        });
    }

    function openModal() {
        const modalElement = document.getElementById('addAddressModal');
        if (!modalElement) return;

        // Try Bootstrap 5 first
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            try {
                let modal = bootstrap.Modal.getInstance(modalElement);
                if (!modal) {
                    modal = new bootstrap.Modal(modalElement, {
                        backdrop: true,
                        keyboard: true,
                        focus: true
                    });
                }
                modal.show();
                return;
            } catch (err) {
                console.log('Bootstrap 5 modal error:', err);
            }
        }

        // Try jQuery Bootstrap
        if (typeof $ !== 'undefined' && $.fn.modal) {
            try {
                $(modalElement).modal({
                    backdrop: true,
                    keyboard: true,
                    show: true
                });
                return;
            } catch (err) {
                console.log('jQuery modal error:', err);
            }
        }

        // Manual fallback
        modalElement.style.position = 'fixed';
        modalElement.style.top = '0';
        modalElement.style.left = '0';
        modalElement.style.right = '0';
        modalElement.style.bottom = '0';
        modalElement.style.width = '100%';
        modalElement.style.height = '100%';
        modalElement.style.display = 'flex';
        modalElement.style.alignItems = 'center';
        modalElement.style.justifyContent = 'center';
        modalElement.style.padding = '1.5rem';
        modalElement.style.margin = '0';
        modalElement.classList.add('show');
        document.body.classList.add('modal-open');
        document.body.style.overflow = 'hidden';

        // Ensure modal-dialog is centered
        const modalDialog = modalElement.querySelector('.modal-dialog');
        if (modalDialog) {
            modalDialog.style.margin = '0 auto';
            modalDialog.style.maxWidth = '600px';
            modalDialog.style.width = '100%';
            modalDialog.style.position = 'relative';
        }

        // Add backdrop if not exists
        let backdrop = document.querySelector('.modal-backdrop');
        if (!backdrop) {
            backdrop = document.createElement('div');
            backdrop.className = 'modal-backdrop fade show';
            backdrop.style.zIndex = '1050';
            backdrop.style.position = 'fixed';
            backdrop.style.top = '0';
            backdrop.style.left = '0';
            backdrop.style.right = '0';
            backdrop.style.bottom = '0';
            backdrop.style.width = '100%';
            backdrop.style.height = '100%';
            backdrop.style.backgroundColor = 'rgba(0, 0, 0, 0.5)';
            backdrop.style.backdropFilter = 'blur(2px)';
            document.body.appendChild(backdrop);

            // Close on backdrop click
            backdrop.addEventListener('click', function() {
                closeModal();
            });
        }
    }

    function closeModal() {
        const modalElement = document.getElementById('addAddressModal');
        if (!modalElement) return;

        // Try Bootstrap 5
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            try {
                const modal = bootstrap.Modal.getInstance(modalElement);
                if (modal) {
                    modal.hide();
                    return;
                }
            } catch (err) {
                console.log('Bootstrap close error:', err);
            }
        }

        // Try jQuery
        if (typeof $ !== 'undefined' && $.fn.modal) {
            try {
                $(modalElement).modal('hide');
                return;
            } catch (err) {
                console.log('jQuery close error:', err);
            }
        }

        // Manual close
        modalElement.style.display = 'none';
        modalElement.classList.remove('show');
        modalElement.style.position = '';
        modalElement.style.top = '';
        modalElement.style.left = '';
        modalElement.style.right = '';
        modalElement.style.bottom = '';
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        const backdrop = document.querySelector('.modal-backdrop');
        if (backdrop) backdrop.remove();
        const form = document.getElementById('add-address-form');
        if (form) form.reset();
    }

    function handleAddAddressSubmit(form) {
        const formData = new FormData(form);
        const data = {
            Title: formData.get('title'),
            RecipientName: formData.get('recipientName'),
            RecipientPhone: formData.get('recipientPhone'),
            Province: formData.get('province'),
            City: formData.get('city'),
            PostalCode: formData.get('postalCode'),
            AddressLine: formData.get('addressLine'),
            Plaque: formData.get('plaque') || null,
            Unit: formData.get('unit') || null,
            IsDefault: (formData.get('isDefault') === 'true')
        };

        // Get URL from data attribute or use default
        const addAddressUrl = form.getAttribute('data-action-url') || window.location.origin + '/Checkout/AddAddress';

        fetch(addAddressUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                closeModal();

                // Show success toast
                showToast('آدرس با موفقیت ثبت شد', 'success');

                // Remove "no address" message if exists
                const noAddressMsg = document.querySelector('.no-address-message');
                if (noAddressMsg) {
                    noAddressMsg.remove();
                }

                // Add new address to the list
                const container = document.getElementById('addresses-container');
                if (container && result.addresses) {
                    const address = result.addresses.find(a => a.id === result.addressId);
                    if (address) {
                        const addressCard = createAddressCard(address);
                        const addButton = container.querySelector('.btn-add-address');
                        if (addButton) {
                            container.insertBefore(addressCard, addButton);
                        } else {
                            container.appendChild(addressCard);
                        }

                        // Select the new address
                        const radio = addressCard.querySelector('input[type="radio"]');
                        if (radio) {
                            radio.checked = true;
                            radio.dispatchEvent(new Event('change'));
                        }
                    }
                }
            } else {
                showToast(result.error || 'خطا در افزودن آدرس.', 'danger');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('خطا در ارتباط با سرور.', 'danger');
        });
    }

    function createAddressCard(address) {
        const card = document.createElement('div');
        card.className = 'address-card' + (address.isDefault ? ' selected' : '');
        card.setAttribute('data-address-id', address.id);

        const plaqueText = address.plaque ? `، پلاک ${address.plaque}` : '';
        const unitText = address.unit ? `، واحد ${address.unit}` : '';
        const badgeHtml = address.isDefault ? '<span class="address-badge">پیش‌فرض</span>' : '';

        card.innerHTML = `
            <label style="cursor: pointer; display: block; margin: 0;">
                <input type="radio" name="SelectedAddressId" value="${address.id}" ${address.isDefault ? 'checked' : ''} style="margin-left: 0.5rem;" />
                <div class="address-title">
                    ${address.title}
                    ${badgeHtml}
                </div>
                <div class="address-details">
                    <div>${address.recipientName} - ${address.recipientPhone}</div>
                    <div>${address.province}، ${address.city}</div>
                    <div>${address.addressLine}${plaqueText}${unitText}</div>
                    <div>کد پستی: ${address.postalCode}</div>
                </div>
            </label>
        `;

        // Add event listener for radio change
        const radio = card.querySelector('input[type="radio"]');
        if (radio) {
            radio.addEventListener('change', function() {
                document.querySelectorAll('.address-card').forEach(function(c) {
                    c.classList.remove('selected');
                });
                if (this.checked) {
                    card.classList.add('selected');
                    const selectedInput = document.getElementById('selected-address-input');
                    if (selectedInput) {
                        selectedInput.value = this.value;
                    }
                }
            });
        }

        return card;
    }

    function initializeOptionalFields() {
        // Handle optional field buttons
        document.querySelectorAll('.btn-optional').forEach(function(btn) {
            btn.addEventListener('click', function() {
                const fieldId = this.getAttribute('data-toggle-field');
                const input = document.getElementById(fieldId);
                if (input) {
                    input.style.display = 'block';
                    input.focus();
                    this.style.display = 'none';
                }
            });
        });

        // Hide optional button if input has value
        document.querySelectorAll('#plaqueInput, #unitInput').forEach(function(input) {
            input.addEventListener('blur', function() {
                if (!this.value || this.value.trim() === '') {
                    this.style.display = 'none';
                    const btn = document.querySelector(`[data-toggle-field="${this.id}"]`);
                    if (btn) {
                        btn.style.display = 'flex';
                    }
                }
            });
        });
    }
})();
