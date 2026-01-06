function toggleSlideLogin() {
    document.getElementById('slideLogin').classList.toggle('show');
}

function showLogin() {
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('loginTab').classList.add('active');
    document.getElementById('registerTab').classList.remove('active');
}

function showRegister() {
    document.getElementById('loginForm').style.display = 'none';
    document.getElementById('registerForm').style.display = 'block';
    document.getElementById('loginTab').classList.remove('active');
    document.getElementById('registerTab').classList.add('active');
}

// Notifications
document.addEventListener('DOMContentLoaded', function () {
    loadAppointments();
    setInterval(loadAppointments, 30000); // Check every 30 seconds
});

function loadAppointments() {
    fetch('/Notifications/GetAppointments')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateAppointmentNotifications(data.appointments, data.unreadCount);
            }
        })
        .catch(error => console.error('Error loading appointments:', error));
}

function updateAppointmentNotifications(appointments, unreadCount) {
    const badge = document.getElementById('notificationCount');
    const dropdown = document.querySelector('[aria-labelledby="notificationsDropdown"]');

    // Update badge count
    if (badge) {
        if (unreadCount > 0) {
            badge.textContent = unreadCount;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }
    }

    // Update dropdown content
    if (dropdown) {
        if (appointments.length === 0) {
            dropdown.innerHTML = `
                <li><h6 class="dropdown-header">My Appointments</h6></li>
                <li><span class="dropdown-item-text">No upcoming appointments</span></li>
            `;
        } else {
            let html = '<li><h6 class="dropdown-header">My Appointments</h6></li>';

            appointments.forEach(appt => {
                // Add null checks and fallbacks
                const doctorName = appt.doctorName || appt.DoctorName || 'Unknown Doctor';
                const doctorSpecialty = appt.doctorSpecialty || appt.DoctorSpecialty || '';
                const formattedDate = appt.formattedDate || appt.FormattedDate || 'Date not available';
                const formattedTime = appt.formattedTime || appt.FormattedTime || 'Time not available';
                const formattedEndTime = appt.formattedEndTime || appt.FormattedEndTime || '';
                const duration = appt.duration || appt.Duration || 30;
                const status = appt.status || appt.Status || 'Unknown';
                const notes = appt.notes || appt.Notes || '';

                const icon = appt.isToday || appt.IsToday ? 'fa-calendar-day text-warning' : 'fa-calendar-check text-primary';
                const isNew = appt.isNew || appt.IsNew ? ' border-start border-3 border-primary' : '';
                const statusClass = getStatusBadgeClass(status);

                // Format duration display
                const durationText = duration === 30 ? '30 min' :
                    duration === 60 ? '1 hour' :
                        durationMinutes === 90 ? '1.5 hours' :
                            `${duration} min`;

                html += `
                    <li>
                        <div class="dropdown-item${isNew}" onclick="markAsRead(${appt.id || appt.Id})" style="cursor: pointer;">
                            <div class="d-flex align-items-start">
                                <i class="fas ${icon} me-2 mt-1"></i>
                                <div class="flex-grow-1">
                                    <div class="fw-bold">Dr. ${doctorName}</div>
                                    <small class="text-muted">${doctorSpecialty}</small>
                                    
                                    <div class="text-muted small mt-1">
                                        <i class="fas fa-clock me-1"></i>
                                        ${formattedDate} at ${formattedTime}
                                        ${formattedEndTime ? ` - ${formattedEndTime}` : ''}
                                        <span class="text-info ms-2">(${durationText})</span>
                                        ${appt.isToday || appt.IsToday ? '<span class="text-warning ms-1">(Today)</span>' : ''}
                                    </div>
                                    
                                    <div class="mt-1">
                                        <span class="badge ${statusClass}">${status}</span>
                                    </div>
                                    
                                    ${notes ? `
                                        <div class="mt-1 small text-truncate">
                                            <i class="fas fa-sticky-note me-1"></i>
                                            ${notes}
                                        </div>
                                    ` : ''}
                                </div>
                                ${appt.isNew || appt.IsNew ? '<span class="badge bg-primary badge-sm ms-2">New</span>' : ''}
                            </div>
                        </div>
                    </li>
                `;
            });

            html += '<li><hr class="dropdown-divider"></li>';
            html += `
                <li>
                    <button class="dropdown-item text-center" onclick="markAllAsRead()">
                        <i class="fas fa-check-double me-1"></i>Mark all as read
                    </button>
                </li>
                <li>
                    <a class="dropdown-item text-center" href="/PatientAppointments">
                        <i class="fas fa-list me-1"></i>View All Appointments
                    </a>
                </li>
            `;

            dropdown.innerHTML = html;
        }
    }
}

function markAsRead(appointmentId) {
    fetch('/Notifications/MarkAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ appointmentId: appointmentId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Reload to update the UI
                loadAppointments();
            }
        })
        .catch(error => console.error('Error marking as read:', error));
}

function markAllAsRead() {
    fetch('/Notifications/MarkAllAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Hide the badge immediately
                const badge = document.getElementById('notificationCount');
                if (badge) {
                    badge.style.display = 'none';
                }

                // Remove "New" badges from all items
                document.querySelectorAll('.border-primary, .badge-sm.bg-primary').forEach(el => {
                    el.classList.remove('border-primary', 'bg-primary');
                });
            }
        })
        .catch(error => console.error('Error marking all as read:', error));
}

function getStatusBadgeClass(status) {
    switch (status?.toLowerCase()) {
        case 'confirmed':
            return 'bg-success';
        case 'pending':
            return 'bg-warning';
        case 'cancelled':
            return 'bg-danger';
        case 'completed':
            return 'bg-info';
        default:
            return 'bg-secondary';
    }
}