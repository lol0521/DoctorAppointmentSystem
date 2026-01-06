// Sidebar toggle functionality
document.addEventListener('DOMContentLoaded', function () {
    // Toggle sidebar on mobile
    const sidebarToggle = document.createElement('button');
    sidebarToggle.className = 'btn btn-sm btn-primary sidebar-toggle d-lg-none';
    sidebarToggle.innerHTML = '<i class="bi bi-list"></i>';
    document.querySelector('.admin-header').prepend(sidebarToggle);

    sidebarToggle.addEventListener('click', function () {
        document.querySelector('.admin-nav').classList.toggle('mobile-show');
    });

    // Active link highlighting
    const currentPath = window.location.pathname;
    document.querySelectorAll('.nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});