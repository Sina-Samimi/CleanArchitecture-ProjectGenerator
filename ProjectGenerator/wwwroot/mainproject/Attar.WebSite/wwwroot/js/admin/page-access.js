(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const modalContainer = document.querySelector('[data-page-access-modal-container]');
        if (!modalContainer) {
            return;
        }

        function bindForm(root) {
            const form = root.querySelector('[data-page-access-form]');
            if (!form) {
                return;
            }

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
                    showModal(html, action);
                } catch (error) {
                    console.error('خطا در ذخیره تنظیمات دسترسی صفحه', error);
                } finally {
                    submitButton?.removeAttribute('disabled');
                }
            });
        }

        function showModal(html, sourceUrl) {
            const template = document.createElement('template');
            template.innerHTML = html.trim();

            const modalElement = template.content.querySelector('[data-page-access-modal]');
            if (!modalElement) {
                console.warn('Page access modal markup بارگذاری نشد.');
                if (sourceUrl) {
                    window.location.href = sourceUrl;
                }
                return;
            }

            modalContainer.innerHTML = '';
            modalContainer.appendChild(modalElement);

            bindForm(modalElement);

            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                const existingInstance = bootstrap.Modal.getInstance(modalElement);
                existingInstance?.dispose();

                const modalInstance = new bootstrap.Modal(modalElement);
                modalElement.addEventListener('hidden.bs.modal', () => {
                    modalContainer.innerHTML = '';
                }, { once: true });
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
                    throw new Error(`Failed to load page access modal: ${response.status}`);
                }

                const html = await response.text();
                showModal(html, url);
            } catch (error) {
                console.error('خطا در بارگذاری مودال دسترسی صفحه', error);
            }
        }

        document.addEventListener('click', event => {
            const trigger = event.target.closest('[data-page-access-modal-trigger]');
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
