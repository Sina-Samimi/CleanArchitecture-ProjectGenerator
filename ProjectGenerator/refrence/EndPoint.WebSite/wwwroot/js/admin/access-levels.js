(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const modalHost = document.querySelector('[data-access-level-modal]');
        if (!modalHost) {
            return;
        }

        function bindPermissionGroups(root) {
            const groups = root.querySelectorAll('[data-permission-group]');
            groups.forEach(group => {
                const inputs = Array.from(group.querySelectorAll('[data-permission-option]'));
                const refresh = () => {
                    inputs.forEach(input => {
                        const option = input.closest('.permission-option');
                        if (option) {
                            option.classList.toggle('is-selected', input.checked);
                        }
                    });
                };

                inputs.forEach(input => {
                    input.addEventListener('change', refresh);
                });

                const selectAllButton = group.querySelector('[data-permission-select-all]');
                if (selectAllButton) {
                    selectAllButton.addEventListener('click', event => {
                        event.preventDefault();
                        const shouldSelectAll = inputs.some(input => !input.checked);
                        inputs.forEach(input => {
                            input.checked = shouldSelectAll;
                        });
                        refresh();
                    });
                }

                refresh();
            });
        }

        function bindAccessLevelForm(root) {
            const form = root.querySelector('[data-access-level-form]');
            if (!form) {
                return;
            }

            bindPermissionGroups(form);

            const submitButton = form.querySelector('[type="submit"]');

            form.addEventListener('submit', async event => {
                event.preventDefault();

                submitButton?.setAttribute('disabled', 'disabled');

                try {
                    const action = form.getAttribute('action') ?? window.location.href;
                    const method = (form.getAttribute('method') ?? 'post').toUpperCase();
                    const formData = new FormData(form);

                    const response = await fetch(action, {
                        method,
                        body: formData,
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });

                    const contentType = response.headers.get('content-type') ?? '';

                    if (contentType.includes('application/json')) {
                        const payload = await response.json();
                        if (payload.success && payload.redirectUrl) {
                            window.location.href = payload.redirectUrl;
                            return;
                        }
                    }

                    const html = await response.text();
                    showModal(html);
                } catch (error) {
                    console.error('خطا در ذخیره نقش', error);
                } finally {
                    submitButton?.removeAttribute('disabled');
                }
            });
        }

        function showModal(html) {
            modalHost.innerHTML = html;
            bindAccessLevelForm(modalHost);

            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const modalInstance = bootstrap.Modal.getOrCreateInstance(modalHost);
                modalInstance.show();
            }
        }

        async function loadModal(url) {
            try {
                const response = await fetch(url, {
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error(`Failed to load modal: ${response.status}`);
                }

                const html = await response.text();
                showModal(html);
            } catch (error) {
                console.error('خطا در بارگذاری مودال سطح دسترسی', error);
            }
        }

        modalHost.addEventListener('hidden.bs.modal', () => {
            modalHost.innerHTML = '';
        });

        document.addEventListener('click', event => {
            const trigger = event.target.closest('[data-access-level-modal-trigger]');
            if (!trigger) {
                return;
            }

            event.preventDefault();

            const url = trigger.getAttribute('data-modal-url') || trigger.getAttribute('href');
            if (!url) {
                return;
            }

            loadModal(url);
        });
    });
})();
