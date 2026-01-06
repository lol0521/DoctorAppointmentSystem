document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');
    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        events: '/DoctorSchedule/GetCalendarEvents',
        eventClick: function (info) {
            viewAppointment(info.event.extendedProps.appointmentId);
        },
        eventContent: function (info) {
            return {
                html: `
                        <div class="fc-event-content">
                            <strong>${info.event.extendedProps.patientName}</strong>
                            <br>
                            <small>${info.event.start.toLocaleTimeString()}</small>
                            <br>
                            <span class="badge">${info.event.extendedProps.status}</span>
                        </div>
                    `
            };
        }
    });
    calendar.render();
});

function viewAppointment(appointmentId) {
    // Implement view appointment details
    alert('View appointment: ' + appointmentId);
    // You can implement a modal or redirect to appointment details
}

function updateAppointmentStatus(appointmentId, status) {
    // Create a custom confirmation dialog
    const statusText = status === 'Confirmed' ? 'confirm' : 'cancel';
    const isConfirmed = confirm(`Are you sure you want to ${statusText} this appointment?`);

    if (!isConfirmed) {
        return;
    }

    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Show loading indicator
    showAlert('Updating appointment status...', 'info');

    fetch('/DoctorSchedule/UpdateAppointmentStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            appointmentId: appointmentId,
            status: status
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                showAlert(data.message, 'success');
                // Refresh the page after a short delay
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showAlert('Error: ' + data.message, 'danger');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showAlert('Error updating status: ' + error.message, 'danger');
        });
}

// Custom alert function
function showAlert(message, type) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.custom-alert');
    existingAlerts.forEach(alert => alert.remove());

    // Create alert styles based on type
    const alertStyles = {
        'success': {
            background: '#d4edda',
            color: '#155724',
            border: '1px solid #c3e6cb'
        },
        'danger': {
            background: '#f8d7da',
            color: '#721c24',
            border: '1px solid #f5c6cb'
        },
        'info': {
            background: '#d1ecf1',
            color: '#0c5460',
            border: '1px solid #bee5eb'
        }
    };

    // Create new alert
    const alertDiv = document.createElement('div');
    alertDiv.className = 'custom-alert';
    alertDiv.style.position = 'fixed';
    alertDiv.style.top = '20px';
    alertDiv.style.right = '20px';
    alertDiv.style.zIndex = '1050';
    alertDiv.style.padding = '15px 20px';
    alertDiv.style.borderRadius = '5px';
    alertDiv.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
    alertDiv.style.minWidth = '300px';
    alertDiv.style.maxWidth = '500px';
    alertDiv.style.fontWeight = '500';

    // Apply styles based on type
    Object.assign(alertDiv.style, alertStyles[type] || alertStyles.info);

    alertDiv.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <span>${message}</span>
                <button type="button" onclick="this.parentElement.parentElement.remove()" 
                        style="background: none; border: none; font-size: 1.2rem; cursor: pointer; margin-left: 15px;">
                    &times;
                </button>
            </div>
        `;

    document.body.appendChild(alertDiv);

    // Auto remove after 5 seconds (except for info alerts which might be loading indicators)
    if (type !== 'info') {
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 5000);
    }
}