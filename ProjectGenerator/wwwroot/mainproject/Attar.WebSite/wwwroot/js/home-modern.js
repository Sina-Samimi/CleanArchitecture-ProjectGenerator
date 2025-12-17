/**
 * Modern Home Page JavaScript
 * Handles animations, interactions, and smooth scrolling
 */

(function() {
    'use strict';

    // Smooth scroll for anchor links
    function initSmoothScroll() {
        const links = document.querySelectorAll('a[href^="#"]');
        
        links.forEach(link => {
            link.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                
                if (href === '#') return;
                
                const target = document.querySelector(href);
                
                if (target) {
                    e.preventDefault();
                    
                    const headerOffset = 80;
                    const elementPosition = target.getBoundingClientRect().top;
                    const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                    window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                    });
                }
            });
        });
    }

    // Add scroll effects to header
    function initScrollEffects() {
        const header = document.querySelector('header');
        let lastScroll = 0;

        window.addEventListener('scroll', () => {
            const currentScroll = window.pageYOffset;

            if (currentScroll > 100) {
                header?.classList.add('scrolled');
            } else {
                header?.classList.remove('scrolled');
            }

            lastScroll = currentScroll;
        });
    }

    // Animate numbers (count up effect)
    function animateNumbers() {
        const stats = document.querySelectorAll('.stat-item__value, .card-value');
        
        const observerOptions = {
            threshold: 0.5,
            rootMargin: '0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !entry.target.classList.contains('animated')) {
                    entry.target.classList.add('animated');
                    animateValue(entry.target);
                }
            });
        }, observerOptions);

        stats.forEach(stat => observer.observe(stat));
    }

    function animateValue(element) {
        const text = element.textContent;
        const match = text.match(/[\d,۰-۹]+/);
        
        if (!match) return;

        const numberStr = match[0];
        const isPersian = /[۰-۹]/.test(numberStr);
        
        // Convert Persian/Arabic numbers to English for calculation
        const englishNumber = isPersian 
            ? numberStr.replace(/[۰-۹]/g, d => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d))
            : numberStr;
        
        const cleanNumber = parseInt(englishNumber.replace(/,/g, ''));
        
        if (isNaN(cleanNumber)) return;

        const duration = 2000;
        const steps = 60;
        const increment = cleanNumber / steps;
        let current = 0;
        let step = 0;

        const timer = setInterval(() => {
            current += increment;
            step++;

            if (step >= steps) {
                current = cleanNumber;
                clearInterval(timer);
            }

            const displayNumber = Math.floor(current);
            const formattedNumber = isPersian 
                ? displayNumber.toString().replace(/\d/g, d => '۰۱۲۳۴۵۶۷۸۹'[d])
                : displayNumber.toString();
            
            element.textContent = text.replace(/[\d,۰-۹]+/, formattedNumber);
        }, duration / steps);
    }

    // Add hover effect to cards
    function initCardEffects() {
        const cards = document.querySelectorAll('.feature-card, .talent-category, .testimonial-card, .product-card-modern');
        
        cards.forEach(card => {
            card.addEventListener('mouseenter', function() {
                this.style.transform = 'translateY(-10px)';
            });
            
            card.addEventListener('mouseleave', function() {
                this.style.transform = 'translateY(0)';
            });
        });
    }

    // Parallax effect for hero section
    function initParallax() {
        const hero = document.querySelector('.hero-modern');
        
        if (!hero) return;

        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            const parallaxElements = hero.querySelectorAll('.hero-visual__card');
            
            parallaxElements.forEach((element, index) => {
                const speed = 0.1 + (index * 0.05);
                const yPos = -(scrolled * speed);
                element.style.transform = `translateY(${yPos}px)`;
            });
        });
    }

    // Add floating animation to visual elements
    function initFloatingAnimation() {
        const floatingElements = document.querySelectorAll('.hero-visual__card');
        
        floatingElements.forEach((element, index) => {
            element.style.animationDelay = `${index * 0.5}s`;
        });
    }

    // Handle form submissions with animation
    function initFormAnimations() {
        const forms = document.querySelectorAll('form');
        
        forms.forEach(form => {
            form.addEventListener('submit', function(e) {
                const button = this.querySelector('button[type="submit"]');
                
                if (button && !button.disabled) {
                    button.classList.add('loading');
                    button.disabled = true;
                    
                    // Re-enable after 3 seconds (fallback)
                    setTimeout(() => {
                        button.classList.remove('loading');
                        button.disabled = false;
                    }, 3000);
                }
            });
        });
    }

    // Initialize stats counter on scroll
    function initStatsCounter() {
        const stats = document.querySelectorAll('.stat-item');
        
        const observerOptions = {
            threshold: 0.5,
            rootMargin: '0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                }
            });
        }, observerOptions);

        stats.forEach(stat => observer.observe(stat));
    }

    // Add loading states to CTA buttons
    function initCTAButtons() {
        const ctaButtons = document.querySelectorAll('.btn-modern--primary, .btn-modern--white');
        
        ctaButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                // Don't prevent default for links
                if (this.tagName === 'A') return;
                
                this.classList.add('loading');
                
                setTimeout(() => {
                    this.classList.remove('loading');
                }, 2000);
            });
        });
    }

    // Intersection Observer for fade-in animations
    function initFadeInAnimations() {
        const elements = document.querySelectorAll('.section-header, .feature-card, .timeline-item, .talent-category, .testimonial-card, .product-card-modern');
        
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -100px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        elements.forEach(element => {
            element.style.opacity = '0';
            element.style.transform = 'translateY(30px)';
            element.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            observer.observe(element);
        });
    }

    // Add ripple effect to buttons
    function createRipple(event) {
        const button = event.currentTarget;
        const circle = document.createElement('span');
        const diameter = Math.max(button.clientWidth, button.clientHeight);
        const radius = diameter / 2;

        circle.style.width = circle.style.height = `${diameter}px`;
        circle.style.left = `${event.clientX - button.offsetLeft - radius}px`;
        circle.style.top = `${event.clientY - button.offsetTop - radius}px`;
        circle.classList.add('ripple');

        const ripple = button.getElementsByClassName('ripple')[0];

        if (ripple) {
            ripple.remove();
        }

        button.appendChild(circle);
    }

    function initRippleEffect() {
        const buttons = document.querySelectorAll('.btn-modern');
        
        buttons.forEach(button => {
            button.addEventListener('click', createRipple);
        });

        // Add ripple styles
        const style = document.createElement('style');
        style.textContent = `
            .btn-modern {
                position: relative;
                overflow: hidden;
            }
            .ripple {
                position: absolute;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.6);
                transform: scale(0);
                animation: ripple-animation 0.6s ease-out;
                pointer-events: none;
            }
            @keyframes ripple-animation {
                to {
                    transform: scale(4);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }

    // Initialize all functions when DOM is ready
    function init() {
        initSmoothScroll();
        initScrollEffects();
        initCardEffects();
        initParallax();
        initFloatingAnimation();
        initFormAnimations();
        initStatsCounter();
        initCTAButtons();
        initRippleEffect();
        
        // Add loading complete class to body
        document.body.classList.add('loaded');

        // Initialize number animations after a short delay
        setTimeout(() => {
            animateNumbers();
        }, 500);
    }

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose public API
    window.HomeModern = {
        animateNumbers: animateNumbers,
        initSmoothScroll: initSmoothScroll
    };

})();
