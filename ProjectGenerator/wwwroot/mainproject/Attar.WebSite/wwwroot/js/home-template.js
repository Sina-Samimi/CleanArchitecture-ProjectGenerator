// Note: searchProducts function is defined inline in views using Razor
// This allows it to use @Url.Action for proper routing

// Smooth scrolling for navigation links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Bottom Navigation Active State
document.addEventListener('DOMContentLoaded', function() {
    const bottomNavItems = document.querySelectorAll('.bottom-nav-item');
    const sections = document.querySelectorAll('section[id]');
    
    function updateActiveNav() {
        let current = '';
        const scrollY = window.pageYOffset;
        
        sections.forEach(section => {
            const sectionTop = section.offsetTop - 100;
            const sectionHeight = section.offsetHeight;
            const sectionId = section.getAttribute('id');
            
            if (scrollY >= sectionTop && scrollY < sectionTop + sectionHeight) {
                current = sectionId;
            }
        });
        
        bottomNavItems.forEach(item => {
            item.classList.remove('active');
            const href = item.getAttribute('href');
            if (href === `#${current}` || (current === 'home' && href === '#home')) {
                item.classList.add('active');
            }
        });
    }
    
    // Update on scroll
    window.addEventListener('scroll', updateActiveNav);
    
    // Update on load
    updateActiveNav();
});

// Form submission (only for contact forms without server-side handling)
document.addEventListener('DOMContentLoaded', function() {
    const contactForms = document.querySelectorAll('form:not([method="post"]):not([action])');
    contactForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            alert('پیام شما با موفقیت ارسال شد! در اسرع وقت با شما تماس خواهیم گرفت.');
            this.reset();
        });
    });
});

// Add to cart functionality
document.querySelectorAll('.btn-small').forEach(button => {
    button.addEventListener('click', function() {
        const productName = this.closest('.product-card').querySelector('.product-name').textContent;
        alert(`محصول "${productName}" به سبد خرید اضافه شد!`);
    });
});

// Desktop Nested Menu Management - Prevent cascade opening
document.addEventListener('DOMContentLoaded', function() {
    // Only apply to desktop (not mobile)
    if (window.innerWidth > 1024) {
        const nestedMenuItems = document.querySelectorAll('.nav-submenu-item--has-children');
        
        nestedMenuItems.forEach(item => {
            const nestedMenu = item.querySelector('.nav-submenu--nested');
            if (nestedMenu) {
                // Close nested menu when mouse leaves the item
                item.addEventListener('mouseleave', function() {
                    nestedMenu.style.opacity = '0';
                    nestedMenu.style.visibility = 'hidden';
                    nestedMenu.style.transform = 'translateX(8px) scale(0.95)';
                    nestedMenu.style.pointerEvents = 'none';
                });
                
                // Open nested menu when mouse enters the item
                item.addEventListener('mouseenter', function() {
                    nestedMenu.style.opacity = '1';
                    nestedMenu.style.visibility = 'visible';
                    nestedMenu.style.transform = 'translateX(0) scale(1)';
                    nestedMenu.style.pointerEvents = 'auto';
                });
            }
        });
    }
});

// Mobile Menu Toggle
document.addEventListener('DOMContentLoaded', function() {
    const mobileMenuToggle = document.getElementById('mobile-menu-toggle');
    const mobileSidebar = document.getElementById('mobile-sidebar');
    const mobileSidebarClose = document.getElementById('mobile-sidebar-close');
    const mobileSidebarOverlay = document.getElementById('mobile-sidebar-overlay');
    const body = document.body;
    
    function openMobileMenu() {
        if (mobileSidebar) {
            mobileSidebar.classList.add('active');
            if (mobileSidebarOverlay) {
                mobileSidebarOverlay.classList.add('active');
            }
            if (mobileMenuToggle) {
                mobileMenuToggle.classList.add('active');
            }
            body.classList.add('mobile-menu-open');
            body.style.overflow = 'hidden';
        }
    }
    
    function closeMobileMenu() {
        if (mobileSidebar) {
            mobileSidebar.classList.remove('active');
            if (mobileSidebarOverlay) {
                mobileSidebarOverlay.classList.remove('active');
            }
            if (mobileMenuToggle) {
                mobileMenuToggle.classList.remove('active');
            }
            body.classList.remove('mobile-menu-open');
            body.style.overflow = '';
        }
    }
    
    if (mobileMenuToggle && mobileSidebar) {
        mobileMenuToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            if (mobileSidebar.classList.contains('active')) {
                closeMobileMenu();
            } else {
                openMobileMenu();
            }
        });
    }
    
    if (mobileSidebarClose) {
        mobileSidebarClose.addEventListener('click', closeMobileMenu);
    }
    
    if (mobileSidebarOverlay) {
        mobileSidebarOverlay.addEventListener('click', closeMobileMenu);
    }
    
    // Toggle submenu in mobile sidebar (supports nested menus)
    if (mobileSidebar) {
        // Ensure ALL submenus (including nested) are closed by default - CRITICAL FIX
        const allSubmenus = mobileSidebar.querySelectorAll('.mobile-sidebar__menu-item--has-children');
        allSubmenus.forEach(item => {
            item.classList.remove('active');
        });
        
        // Also ensure nested submenus are closed
        const nestedSubmenus = mobileSidebar.querySelectorAll('.mobile-sidebar__submenu-item.mobile-sidebar__menu-item--has-children');
        nestedSubmenus.forEach(item => {
            item.classList.remove('active');
        });
        
        // Use event delegation to handle nested submenus
        mobileSidebar.addEventListener('click', function(e) {
            const toggle = e.target.closest('[data-toggle-submenu]');
            if (toggle) {
                e.preventDefault();
                e.stopPropagation();
                const menuItem = toggle.closest('.mobile-sidebar__menu-item--has-children, .mobile-sidebar__submenu-item.mobile-sidebar__menu-item--has-children');
                if (menuItem) {
                    const isActive = menuItem.classList.contains('active');
                    // Only close sibling submenus, not parent submenus
                    const parentSubmenu = menuItem.closest('.mobile-sidebar__submenu');
                    if (parentSubmenu) {
                        const siblings = parentSubmenu.querySelectorAll('.mobile-sidebar__menu-item--has-children');
                        siblings.forEach(item => {
                            if (item !== menuItem) {
                                item.classList.remove('active');
                            }
                        });
                    } else {
                        // Top level: close all other top-level submenus
                        const topLevelItems = mobileSidebar.querySelectorAll('.mobile-sidebar__menu > .mobile-sidebar__menu-item--has-children');
                        topLevelItems.forEach(item => {
                            if (item !== menuItem) {
                                item.classList.remove('active');
                            }
                        });
                    }
                    // Toggle current submenu
                    if (isActive) {
                        menuItem.classList.remove('active');
                    } else {
                        menuItem.classList.add('active');
                    }
                }
            }
        });
        
        // Close menu when clicking on a regular link (not submenu toggle)
        mobileSidebar.addEventListener('click', function(e) {
            const link = e.target.closest('.mobile-sidebar__link:not([data-toggle-submenu]), .mobile-sidebar__submenu-link:not([data-toggle-submenu])');
            if (link && !link.hasAttribute('data-toggle-submenu')) {
                // Close menu after a short delay to allow navigation
                setTimeout(closeMobileMenu, 300);
            }
        });
    }
    
    // Close menu on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && mobileSidebar && mobileSidebar.classList.contains('active')) {
            closeMobileMenu();
        }
    });
});

// Desktop Search Toggle
document.addEventListener('DOMContentLoaded', function() {
    const desktopSearchToggle = document.getElementById('desktop-search-toggle');
    const desktopSearchBox = document.getElementById('desktop-search-box');
    const desktopSearchInput = document.getElementById('desktop-search-input');
    
    if (desktopSearchToggle && desktopSearchBox) {
        desktopSearchToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            desktopSearchBox.classList.toggle('active');
            if (desktopSearchBox.classList.contains('active')) {
                setTimeout(() => {
                    if (desktopSearchInput) {
                        desktopSearchInput.focus();
                    }
                }, 100);
            }
        });
        
        // Close search box when clicking outside
        document.addEventListener('click', function(e) {
            if (!desktopSearchBox.contains(e.target) && !desktopSearchToggle.contains(e.target)) {
                desktopSearchBox.classList.remove('active');
            }
        });
        
        // Close on Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && desktopSearchBox.classList.contains('active')) {
                desktopSearchBox.classList.remove('active');
            }
        });
    }
    
    // Mobile Search Toggle
    const mobileSearchToggle = document.getElementById('mobile-search-toggle');
    const mobileSearchBox = document.getElementById('mobile-search-box');
    const mobileSearchClose = document.getElementById('mobile-search-close');
    const mobileSearchInput = document.getElementById('mobile-search-input');
    
    if (mobileSearchToggle && mobileSearchBox) {
        mobileSearchToggle.addEventListener('click', function(e) {
            e.preventDefault();
            mobileSearchBox.classList.add('active');
            setTimeout(() => {
                if (mobileSearchInput) {
                    mobileSearchInput.focus();
                }
            }, 300);
        });
        
        if (mobileSearchClose) {
            mobileSearchClose.addEventListener('click', function() {
                mobileSearchBox.classList.remove('active');
            });
        }
        
        // Close on Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && mobileSearchBox.classList.contains('active')) {
                mobileSearchBox.classList.remove('active');
            }
        });
    }
    
    // Hide header on scroll down in mobile, show on scroll up
    const header = document.querySelector('header');
    let lastScrollTop = 0;
    const scrollThreshold = 100; // Hide header after scrolling 100px down
    
    function handleHeaderScroll() {
        // Only apply on mobile devices (max-width: 768px)
        if (window.innerWidth > 768) {
            // On desktop/tablet, always show header
            if (header) {
                header.classList.remove('header-scrolled');
            }
            return;
        }
        
        // Mobile behavior
        if (header) {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            if (scrollTop > scrollThreshold) {
                // Scrolled down - hide header
                if (scrollTop > lastScrollTop) {
                    header.classList.add('header-scrolled');
                } else {
                    // Scrolled up - show header
                    header.classList.remove('header-scrolled');
                }
            } else {
                // Near top - always show header
                header.classList.remove('header-scrolled');
            }
            
            lastScrollTop = scrollTop <= 0 ? 0 : scrollTop;
        }
    }
    
    // Throttle scroll event for better performance
    let scrollTimeout;
    window.addEventListener('scroll', function() {
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }
        scrollTimeout = setTimeout(handleHeaderScroll, 10);
    }, { passive: true });
    
    // Handle window resize
    window.addEventListener('resize', function() {
        handleHeaderScroll();
    });
    
    // Initial check
    handleHeaderScroll();
});

