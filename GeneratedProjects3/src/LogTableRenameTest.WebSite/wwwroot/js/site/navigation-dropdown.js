(function() {
    'use strict';
    
    // Calculate nesting level of a dropdown menu
    function getNestingLevel(element) {
        let level = 0;
        let current = element;
        // Count how many dropdown-menu ancestors this element has
        while (current) {
            const parent = current.parentElement;
            if (!parent) break;
            
            // Check if parent is a dropdown-menu
            if (parent.classList.contains('dropdown-menu')) {
                level++;
                current = parent;
            } else {
                // Check if there's a dropdown-menu ancestor
                const ancestor = parent.closest('.dropdown-menu');
                if (ancestor) {
                    level++;
                    current = ancestor;
                } else {
                    break;
                }
            }
        }
        return level;
    }
    
    // Handle nested dropdown menus to prevent clipping by parent overflow
    function handleNestedDropdowns() {
        // Use event delegation for all dropdown menus
        document.addEventListener('shown.bs.dropdown', function(event) {
            // event.target is the toggle button
            const toggleButton = event.target;
            if (!toggleButton || !toggleButton.classList.contains('dropdown-toggle')) {
                return;
            }
            
            // Find the dropdown menu associated with this toggle
            const menuId = toggleButton.getAttribute('id');
            if (!menuId) return;
            
            const dropdownMenu = document.querySelector(`[aria-labelledby="${menuId}"]`);
            if (!dropdownMenu || !dropdownMenu.classList.contains('dropdown-menu')) {
                return;
            }
            
            // Check if this dropdown is nested (inside another dropdown-menu)
            const parentDropdown = dropdownMenu.closest('.dropdown-menu');
            if (!parentDropdown) {
                return; // Not a nested dropdown
            }
            
            // Calculate nesting level for z-index
            const nestingLevel = getNestingLevel(dropdownMenu);
            const zIndex = 1000 + nestingLevel; // Increment z-index for deeper levels
            
            // Get position of toggle button
            const rect = toggleButton.getBoundingClientRect();
            
            // Position nested dropdown to the left of the toggle button (RTL)
            dropdownMenu.style.position = 'fixed';
            dropdownMenu.style.top = rect.top + 'px';
            dropdownMenu.style.right = (window.innerWidth - rect.left) + 'px';
            dropdownMenu.style.left = 'auto';
            dropdownMenu.style.zIndex = zIndex.toString();
        });
        
        // Reset position when hidden
        document.addEventListener('hidden.bs.dropdown', function(event) {
            const toggleButton = event.target;
            if (!toggleButton || !toggleButton.classList.contains('dropdown-toggle')) {
                return;
            }
            
            const menuId = toggleButton.getAttribute('id');
            if (!menuId) return;
            
            const dropdownMenu = document.querySelector(`[aria-labelledby="${menuId}"]`);
            if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                const parentDropdown = dropdownMenu.closest('.dropdown-menu');
                if (parentDropdown) {
                    // Reset to default positioning
                    dropdownMenu.style.position = '';
                    dropdownMenu.style.top = '';
                    dropdownMenu.style.right = '';
                    dropdownMenu.style.left = '';
                    dropdownMenu.style.zIndex = '';
                }
            }
        });
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', handleNestedDropdowns);
    } else {
        handleNestedDropdowns();
    }
})();
