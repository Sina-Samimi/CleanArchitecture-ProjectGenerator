// Search Products Function
function searchProducts() {
    const searchInput = document.getElementById('hero-search-input');
    const searchTerm = searchInput ? searchInput.value.trim() : '';
    
    if (searchTerm) {
        window.location.href = `products-archive.html?search=${encodeURIComponent(searchTerm)}`;
    } else {
        window.location.href = 'products-archive.html';
    }
}

// Enter key search in hero
document.addEventListener('DOMContentLoaded', function() {
    const heroSearchInput = document.getElementById('hero-search-input');
    if (heroSearchInput) {
        heroSearchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchProducts();
            }
        });
    }
});

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

// Form submission
document.querySelector('form').addEventListener('submit', function(e) {
    e.preventDefault();
    alert('پیام شما با موفقیت ارسال شد! در اسرع وقت با شما تماس خواهیم گرفت.');
    this.reset();
});

// Add to cart functionality
document.querySelectorAll('.btn-small').forEach(button => {
    button.addEventListener('click', function() {
        const productName = this.closest('.product-card').querySelector('.product-name').textContent;
        alert(`محصول "${productName}" به سبد خرید اضافه شد!`);
    });
});

