// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Accessible, overflow-safe action menus for table rows.
(function () {
    function closeActionMenus(except) {
        document.querySelectorAll('.hms-action-menu.is-open').forEach(function (menu) {
            if (menu === except) return;
            menu.classList.remove('is-open');
            menu.closest('.hms-action-wrap')?.querySelector('.hms-action-trigger')
                ?.setAttribute('aria-expanded', 'false');
        });
    }

    function placeActionMenu(trigger, menu) {
        menu.style.visibility = 'hidden';
        menu.classList.add('is-open');
        var triggerRect = trigger.getBoundingClientRect();
        var menuRect = menu.getBoundingClientRect();
        var gutter = 8;
        var left = Math.min(triggerRect.right - menuRect.width, window.innerWidth - menuRect.width - gutter);
        left = Math.max(gutter, left);
        var top = triggerRect.bottom + 5;
        if (top + menuRect.height > window.innerHeight - gutter) {
            top = Math.max(gutter, triggerRect.top - menuRect.height - 5);
        }
        menu.style.left = left + 'px';
        menu.style.top = top + 'px';
        menu.style.visibility = 'visible';
    }

    document.addEventListener('click', function (event) {
        var trigger = event.target.closest('.hms-action-trigger');
        if (trigger) {
            event.preventDefault();
            event.stopPropagation();
            var menu = trigger.closest('.hms-action-wrap')?.querySelector('.hms-action-menu');
            if (!menu) return;
            var shouldOpen = !menu.classList.contains('is-open');
            closeActionMenus(menu);
            if (shouldOpen) {
                placeActionMenu(trigger, menu);
                trigger.setAttribute('aria-expanded', 'true');
            } else {
                menu.classList.remove('is-open');
                trigger.setAttribute('aria-expanded', 'false');
            }
            return;
        }
        if (!event.target.closest('.hms-action-menu')) closeActionMenus();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closeActionMenus();
    });
    window.addEventListener('resize', function () { closeActionMenus(); });
    document.addEventListener('scroll', function () { closeActionMenus(); }, true);
})();
