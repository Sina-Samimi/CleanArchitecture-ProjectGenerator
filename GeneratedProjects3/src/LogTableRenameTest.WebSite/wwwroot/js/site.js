(function () {
    const body = document.body;

    const initSiteMenu = () => {
        const menu = document.getElementById('siteMenu');
        if (!menu) {
            return;
        }

        const panel = document.getElementById('siteMenuPanel');
        const overlay = menu.querySelector('.site-menu__overlay');
        const toggles = document.querySelectorAll('[data-site-menu-toggle]');
        const dismissors = menu.querySelectorAll('[data-site-menu-dismiss]');
        const actionableDismissors = Array.from(dismissors).filter((el) => el !== overlay);
        const desktopQuery = window.matchMedia('(min-width: 992px)');
        let lastFocusedElement = null;

        if (panel && !panel.hasAttribute('tabindex')) {
            panel.setAttribute('tabindex', '-1');
        }

        const setToggleState = (expanded) => {
            toggles.forEach((btn) => {
                btn.classList.toggle('is-active', expanded);
                btn.setAttribute('aria-expanded', expanded ? 'true' : 'false');
                btn.setAttribute('aria-label', expanded ? 'بستن منو' : 'باز کردن منو');
            });
        };

        const closeMenu = () => {
            if (!menu.classList.contains('is-active')) {
                return;
            }

            menu.classList.remove('is-active');
            menu.setAttribute('aria-hidden', 'true');
            body?.classList.remove('site-body--menu-open');
            setToggleState(false);

            if (lastFocusedElement && typeof lastFocusedElement.focus === 'function') {
                lastFocusedElement.focus({ preventScroll: true });
            }

            lastFocusedElement = null;
        };

        const openMenu = () => {
            if (menu.classList.contains('is-active')) {
                return;
            }

            lastFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
            menu.classList.add('is-active');
            menu.setAttribute('aria-hidden', 'false');
            body?.classList.add('site-body--menu-open');
            setToggleState(true);

            panel?.focus({ preventScroll: true });
        };

        const toggleMenu = (event) => {
            event.preventDefault();
            if (menu.classList.contains('is-active')) {
                closeMenu();
            } else {
                openMenu();
            }
        };

        toggles.forEach((btn) => btn.addEventListener('click', toggleMenu));

        overlay?.addEventListener('click', closeMenu);

        actionableDismissors.forEach((el) => {
            el.addEventListener('click', () => {
                window.requestAnimationFrame(() => closeMenu());
            });
        });

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape' && menu.classList.contains('is-active')) {
                closeMenu();
            }
        });

        desktopQuery.addEventListener('change', (event) => {
            if (event.matches) {
                closeMenu();
            }
        });
    };

    const initAdminShell = () => {
        const shell = document.getElementById('appShell');
        const aside = document.getElementById('appAside');

        if (!shell || !aside) {
            return;
        }

        const overlay = document.getElementById('appAsideOverlay');
        const toolbarToggle = document.getElementById('appAsideToggle');
        const edgeToggle = document.getElementById('appAsideEdgeToggle');
        const mobileQuery = window.matchMedia('(max-width: 991.98px)');
        const toggleButtons = [toolbarToggle, edgeToggle].filter(Boolean);
        const closeTriggers = document.querySelectorAll('[data-app-aside-close]');

        const setToggleState = (isOpen) => {
            toggleButtons.forEach((btn) => {
                btn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
                btn.classList.toggle('is-active', isOpen);
                btn.setAttribute('aria-label', isOpen ? 'بستن منو' : 'باز کردن منو');
            });
        };

        const openAside = () => {
            aside.classList.add('is-open');
            shell.classList.add('app-shell--aside-open');
            body.classList.add('app-body--aside-open');
            if (overlay) {
                overlay.classList.add('is-active');
            }

            setToggleState(true);
        };

        const closeAside = () => {
            aside.classList.remove('is-open');
            shell.classList.remove('app-shell--aside-open');
            body.classList.remove('app-body--aside-open');
            if (overlay) {
                overlay.classList.remove('is-active');
            }

            setToggleState(false);
        };

        const toggleAside = () => {
            if (aside.classList.contains('is-open')) {
                closeAside();
            } else {
                openAside();
            }
        };

        toggleButtons.forEach((btn) => btn.addEventListener('click', toggleAside));
        closeTriggers.forEach((trigger) => trigger.addEventListener('click', closeAside));

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape' && aside.classList.contains('is-open')) {
                closeAside();
            }
        });

        const handleBreakpointChange = () => {
            closeAside();
        };

        handleBreakpointChange();
        mobileQuery.addEventListener('change', handleBreakpointChange);
    };

    const initContentSliders = () => {
        const uniqueSliders = new Set(
            Array.from(document.querySelectorAll('[data-slider], [data-blog-slider]'))
        );
        const sliders = Array.from(uniqueSliders);
        if (sliders.length === 0) {
            return;
        }

        const parseGap = (track) => {
            const styles = window.getComputedStyle(track);
            const gap = styles.columnGap || styles.gap || '0';
            const parsed = parseFloat(gap);
            return Number.isNaN(parsed) ? 0 : parsed;
        };

        sliders.forEach((slider) => {
            const track = slider.querySelector('[data-slider-track]');
            const slides = Array.from(slider.querySelectorAll('[data-slider-slide]'));
            const prev = slider.querySelector('[data-slider-prev]');
            const next = slider.querySelector('[data-slider-next]');

            if (!track || slides.length === 0) {
                if (prev) {
                    prev.disabled = true;
                }
                if (next) {
                    next.disabled = true;
                }
                return;
            }

            const getScrollAmount = () => {
                if (slides.length === 0) {
                    return track.clientWidth;
                }

                const slideWidth = slides[0].getBoundingClientRect().width;
                return slideWidth + parseGap(track);
            };

            const updateControls = () => {
                const maxScrollLeft = track.scrollWidth - track.clientWidth;
                if (prev) {
                    prev.disabled = track.scrollLeft <= 2;
                }
                if (next) {
                    next.disabled = track.scrollLeft >= (maxScrollLeft - 2);
                }
            };

            const scrollToDirection = (direction) => {
                const amount = getScrollAmount();
                track.scrollBy({ left: direction * amount, behavior: 'smooth' });
            };

            prev?.addEventListener('click', () => scrollToDirection(-1));
            next?.addEventListener('click', () => scrollToDirection(1));

            track.addEventListener('scroll', updateControls, { passive: true });
            window.addEventListener('resize', updateControls);

            updateControls();
        });
    };

    const ensureSlidersInitialized = () => {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initContentSliders, { once: true });
        } else {
            initContentSliders();
        }
    };

    initSiteMenu();
    initAdminShell();
    ensureSlidersInitialized();

    if (body && body.classList.contains('site-body')) {
        const mobileQuery = window.matchMedia('(max-width: 768px)');
        const updateMode = () => {
            if (mobileQuery.matches) {
                body.classList.add('is-mobile');
            } else {
                body.classList.remove('is-mobile');
            }
        };

        updateMode();
        mobileQuery.addEventListener('change', updateMode);
    }

    const alertEl = document.getElementById('appAlert');
    if (!alertEl) {
        window.AppAlert = window.AppAlert || {
            show: (options) => console.warn('Alert component is not available on this page.', options),
            hide: () => undefined,
            isOpen: () => false
        };
        return;
    }

    const dialog = alertEl.querySelector('.app-alert__dialog');
    const titleEl = alertEl.querySelector('[data-alert-title]');
    const messageEl = alertEl.querySelector('[data-alert-message]');
    const iconEl = alertEl.querySelector('[data-alert-icon]');
    const confirmBtn = alertEl.querySelector('[data-alert-confirm]');
    const cancelBtn = alertEl.querySelector('[data-alert-cancel]');
    const dismissors = alertEl.querySelectorAll('[data-alert-dismiss]');

    const iconVariants = {
        info: '<span class="app-alert__icon-mark">ℹ</span>',
        success: '<span class="app-alert__icon-mark">✓</span>',
        warning: '<span class="app-alert__icon-mark">!</span>',
        danger: '<span class="app-alert__icon-mark">!</span>'
    };

    const classes = ['app-alert--info', 'app-alert--success', 'app-alert--warning', 'app-alert--danger'];
    const state = {
        onConfirm: null,
        onCancel: null
    };

    const closeAlert = () => {
        alertEl.classList.remove('is-active', 'app-alert--info', 'app-alert--success', 'app-alert--warning', 'app-alert--danger');
        alertEl.setAttribute('aria-hidden', 'true');
        state.onConfirm = null;
        state.onCancel = null;
        confirmBtn?.removeAttribute('disabled');
        cancelBtn?.removeAttribute('disabled');
    };

    const openAlert = () => {
        alertEl.classList.add('is-active');
        alertEl.setAttribute('aria-hidden', 'false');
        dialog.focus({ preventScroll: true });
    };

    confirmBtn?.addEventListener('click', () => {
        const callback = state.onConfirm;
        closeAlert();
        if (typeof callback === 'function') {
            callback();
        }
    });

    cancelBtn?.addEventListener('click', () => {
        const callback = state.onCancel;
        closeAlert();
        if (typeof callback === 'function') {
            callback();
        }
    });

    dismissors.forEach((el) => el.addEventListener('click', closeAlert));

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && alertEl.classList.contains('is-active')) {
            closeAlert();
        }
    });

    const setAlertType = (type) => {
        alertEl.classList.remove(...classes);
        switch (type) {
            case 'success':
                alertEl.classList.add('app-alert--success');
                break;
            case 'warning':
                alertEl.classList.add('app-alert--warning');
                break;
            case 'danger':
            case 'error':
                alertEl.classList.add('app-alert--danger');
                type = 'danger';
                break;
            default:
                alertEl.classList.add('app-alert--info');
                break;
        }

        const iconTemplate = iconVariants[type] || iconVariants.info;
        if (iconEl) {
            iconEl.innerHTML = iconTemplate;
        }
    };

    window.AppAlert = {
        show: (options) => {
            const config = {
                title: 'اعلان سیستم',
                message: '',
                type: 'info',
                confirmText: 'باشه',
                cancelText: 'خیر، انصراف',
                showCancel: false,
                onConfirm: null,
                onCancel: null,
                ...options
            };

            if (titleEl) {
                titleEl.textContent = config.title;
            }

            if (messageEl) {
                if (typeof config.message === 'string') {
                    messageEl.textContent = config.message;
                } else if (config.message instanceof Node) {
                    messageEl.innerHTML = '';
                    messageEl.appendChild(config.message);
                }
            }

            if (confirmBtn) {
                confirmBtn.textContent = config.confirmText || 'باشه';
            }

            if (cancelBtn) {
                if (config.showCancel) {
                    cancelBtn.textContent = config.cancelText || 'خیر، انصراف';
                    cancelBtn.hidden = false;
                } else {
                    cancelBtn.hidden = true;
                }
            }

            state.onConfirm = typeof config.onConfirm === 'function' ? config.onConfirm : null;
            state.onCancel = typeof config.onCancel === 'function' ? config.onCancel : null;
            setAlertType(config.type);
            openAlert();
        },
        hide: closeAlert,
        isOpen: () => alertEl.classList.contains('is-active')
    };

    const toastStack = document.querySelector('[data-app-toast-stack]');
    if (toastStack) {
        const toasts = Array.from(toastStack.querySelectorAll('[data-app-toast]'));

        const hideToast = (toast) => {
            if (!toast || toast.classList.contains('is-hiding')) {
                return;
            }

            toast.classList.remove('is-visible');
            toast.classList.add('is-hiding');
            const remove = () => {
                toast.removeEventListener('transitionend', remove);
                toast.remove();
                if (!toastStack.querySelector('[data-app-toast]')) {
                    toastStack.remove();
                }
            };

            toast.addEventListener('transitionend', remove);
        };

        toasts.forEach((toast) => {
            requestAnimationFrame(() => {
                toast.classList.add('is-visible');
            });

            const dismiss = toast.querySelector('[data-app-toast-dismiss]');
            if (dismiss) {
                dismiss.addEventListener('click', () => hideToast(toast));
            }

            const lifetimeAttr = toast.getAttribute('data-toast-lifetime');
            const lifetime = lifetimeAttr ? parseInt(lifetimeAttr, 10) : NaN;
            if (!Number.isNaN(lifetime) && lifetime > 0) {
                setTimeout(() => hideToast(toast), lifetime);
            }
        });
    }

    const initPhoneLogin = () => {
        const form = document.querySelector('[data-phone-login-form]');
        if (!form) {
            return;
        }

        const termsCheckbox = form.querySelector('input[type="checkbox"][data-phone-login-terms]');
        const submitButton = form.querySelector('[data-phone-login-submit]');
        const errorContainer = form.querySelector('[data-phone-login-terms-error]');
        const fallbackMessage = errorContainer?.dataset?.errorMessage?.trim() || 'پذیرش قوانین و مقررات الزامی است.';
        let hasServerError = Boolean(errorContainer && errorContainer.textContent.trim().length);
        let hasInteracted = hasServerError;

        const showError = () => {
            if (!errorContainer) {
                return;
            }

            errorContainer.textContent = fallbackMessage;
        };

        const clearError = () => {
            if (!errorContainer) {
                return;
            }

            errorContainer.textContent = '';
            hasServerError = false;
        };

        const updateSubmitState = () => {
            const accepted = Boolean(termsCheckbox?.checked);
            if (submitButton) {
                submitButton.disabled = !accepted;
                submitButton.setAttribute('aria-disabled', accepted ? 'false' : 'true');
            }

            if (accepted) {
                clearError();
                hasInteracted = false;
                return;
            }

            if (hasServerError || hasInteracted) {
                showError();
            }
        };

        termsCheckbox?.addEventListener('change', () => {
            hasInteracted = !termsCheckbox.checked ? true : false;
            updateSubmitState();
        });

        form.addEventListener('submit', (event) => {
            if (termsCheckbox && !termsCheckbox.checked) {
                event.preventDefault();
                event.stopImmediatePropagation();
                hasInteracted = true;
                showError();
                updateSubmitState();
                termsCheckbox.focus();
            }
        });

        updateSubmitState();
    };

    initPhoneLogin();
})();

// ===============================================
// Sticky Navbar & Scroll to Top Functionality
// ===============================================
(function () {
    'use strict';

    // Sticky Navbar Logic
    const initStickyNavbar = () => {
        const navbar = document.querySelector('.site-navbar');
        if (!navbar) {
            return;
        }

        let ticking = false;

        const updateNavbar = (scrollTop) => {
            // Add scrolled class for enhanced shadow when scrolled
            if (scrollTop > 50) {
                navbar.classList.add('scrolled', 'navbar-visible');
            } else {
                navbar.classList.remove('scrolled', 'navbar-visible');
            }
        };

        const handleScroll = () => {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            if (!ticking) {
                window.requestAnimationFrame(() => {
                    updateNavbar(scrollTop);
                    ticking = false;
                });
                ticking = true;
            }
        };

        window.addEventListener('scroll', handleScroll, { passive: true });

        // Initialize navbar state
        updateNavbar(window.pageYOffset || document.documentElement.scrollTop);
    };

    // Scroll to Top Button Logic
    const initScrollToTop = () => {
        const scrollBtn = document.getElementById('scrollToTopBtn');
        if (!scrollBtn) {
            return;
        }

        const scrollThreshold = 300;
        let isVisible = false;
        let ticking = false;

        const updateButtonVisibility = (scrollTop) => {
            const shouldShow = scrollTop > scrollThreshold;

            if (shouldShow && !isVisible) {
                scrollBtn.classList.add('visible', 'animate-in');
                isVisible = true;

                // Remove animate-in class after animation completes
                setTimeout(() => {
                    scrollBtn.classList.remove('animate-in');
                }, 600);
            } else if (!shouldShow && isVisible) {
                scrollBtn.classList.remove('visible');
                isVisible = false;
            }
        };

        const handleScroll = () => {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            if (!ticking) {
                window.requestAnimationFrame(() => {
                    updateButtonVisibility(scrollTop);
                    ticking = false;
                });
                ticking = true;
            }
        };

        const scrollToTop = () => {
            // Use smooth scroll if supported
            if ('scrollBehavior' in document.documentElement.style) {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            } else {
                // Fallback for browsers that don't support smooth scroll
                const scrollStep = -window.scrollY / (500 / 15);
                const scrollInterval = setInterval(() => {
                    if (window.scrollY !== 0) {
                        window.scrollBy(0, scrollStep);
                    } else {
                        clearInterval(scrollInterval);
                    }
                }, 15);
            }
        };

        scrollBtn.addEventListener('click', scrollToTop);
        window.addEventListener('scroll', handleScroll, { passive: true });

        // Initialize button state
        updateButtonVisibility(window.pageYOffset || document.documentElement.scrollTop);
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            initStickyNavbar();
            initScrollToTop();
        });
    } else {
        initStickyNavbar();
        initScrollToTop();
    }
})();
