(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const trigger = document.getElementById('categoriesMenuTrigger');
        const panel = document.getElementById('categoriesMenuPanel');
        const menuItems = document.querySelectorAll('.categories-mega-menu__item[data-category-id]');
        const contentPanels = document.querySelectorAll('.categories-mega-menu__content-panel');

        if (!trigger || !panel) {
            return;
        }

        // Toggle menu on trigger click
        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            const isExpanded = trigger.getAttribute('aria-expanded') === 'true';
            trigger.setAttribute('aria-expanded', !isExpanded);
            panel.setAttribute('aria-hidden', isExpanded);
            
            if (!isExpanded) {
                // Show first category panel if available
                const firstItem = menuItems[0];
                if (firstItem) {
                    activateCategory(firstItem);
                }
            }
        });

        // Handle category item hover/click
        menuItems.forEach(function (item) {
            const link = item.querySelector('.categories-mega-menu__link');
            if (!link) return;

            // Desktop: hover
            if (window.innerWidth >= 992) {
                item.addEventListener('mouseenter', function () {
                    activateCategory(item);
                });
            }

            // Mobile: click
            item.addEventListener('click', function (e) {
                if (window.innerWidth < 992) {
                    e.preventDefault();
                    activateCategory(item);
                }
            });
        });

        // Close menu when clicking outside
        document.addEventListener('click', function (e) {
            if (panel && !panel.contains(e.target) && !trigger.contains(e.target)) {
                trigger.setAttribute('aria-expanded', 'false');
                panel.setAttribute('aria-hidden', 'true');
            }
        });

        // Prevent panel click from closing menu
        panel.addEventListener('click', function (e) {
            e.stopPropagation();
        });

        function activateCategory(item) {
            const categoryId = item.getAttribute('data-category-id');
            if (!categoryId) return;

            // Remove active class from all items
            menuItems.forEach(function (i) {
                i.classList.remove('active');
            });

            // Hide all content panels
            contentPanels.forEach(function (p) {
                p.classList.remove('active');
                p.style.display = 'none';
            });

            // Activate current item
            item.classList.add('active');

            // Show corresponding content panel
            const targetPanel = document.querySelector(`[data-category-panel="${categoryId}"]`);
            if (targetPanel) {
                targetPanel.classList.add('active');
                targetPanel.style.display = 'block';
            }
        }

        // Handle window resize
        let resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                // Close menu on mobile when resizing
                if (window.innerWidth < 992) {
                    trigger.setAttribute('aria-expanded', 'false');
                    panel.setAttribute('aria-hidden', 'true');
                }
            }, 250);
        });
    });
})();
