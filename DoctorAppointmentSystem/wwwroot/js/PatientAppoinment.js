function setFilter(filter) {
    window.location.href = '/PatientAppointments/Index?filter=' + filter;
}

function viewAppointment(appointmentId) {
    fetch('/PatientAppointments/GetAppointmentDetails?id=' + appointmentId)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            console.log('Appointment data:', data); // Debug log
            if (data.success) {
                const appointment = data.appointment;

                // Parse the full datetime string
                const appointmentDateTime = new Date(appointment.date);

                // Extract date and time components
                const dateFormatted = formatDate(appointmentDateTime);
                const startTimeFormatted = formatTime12Hour(appointmentDateTime);

                // Calculate end time based on duration (default to 30 if undefined)
                const duration = appointment.duration;
                const endTime = new Date(appointmentDateTime.getTime() + duration * 60000);
                const endTimeFormatted = formatTime12Hour(endTime);

                const modalContent = `
                    <div class="row">
                        <div class="col-12">
                            <strong>Doctor Information</strong>
                            <p><strong>Name:</strong> Dr. ${appointment.doctorName}</p>
                            <p><strong>Specialty:</strong> ${appointment.specialty}</p>
                            <strong class="mt-3">Appointment Details</strong>
                            <p><strong>Date:</strong> ${dateFormatted}</p>
                            <p><strong>Time:</strong> ${startTimeFormatted} - ${endTimeFormatted}</p>
                            <p><strong>Duration:</strong>  ${duration} minutes</p>
                            <p><strong>Status:</strong> <span class="badge ${getStatusBadgeClass(appointment.status)}">${appointment.status}</span></p>
                            <p><strong>Notes:</strong> ${appointment.notes || 'No notes available'}</p>
                        </div>
                    </div>
                `;
                document.getElementById('appointmentDetails').innerHTML = modalContent;
                new bootstrap.Modal(document.getElementById('appointmentModal')).show();
            } else {
                alert('Error: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Error loading appointment details: ' + error.message);
        });
}

function formatTime12Hour(date) {
    let hours = date.getHours();
    let minutes = date.getMinutes();
    const ampm = hours >= 12 ? 'pm' : 'am';

    hours = hours % 12;
    hours = hours ? hours : 12;

    if (minutes === 0) {
        return `${hours}${ampm}`;
    } else {
        return `${hours}:${minutes.toString().padStart(2, '0')}${ampm}`;
    }
}

function formatDate(date) {
    return date.toLocaleDateString('en-US', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

function cancelAppointment(appointmentId) {
    if (!confirm('Are you sure you want to cancel this appointment?')) {
        return;
    }

    console.log('Cancelling appointment:', appointmentId);

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('id', appointmentId);

    fetch('/PatientAppointments/CancelAppointment', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        body: formData
    })
        .then(response => {
            console.log('Response status:', response.status);
            return response.json();
        })
        .then(data => {
            console.log('Response data:', data);
            if (data.success) {
                alert(data.message);
                location.reload();
            } else {
                alert('Error: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Error cancelling appointment: ' + error.message);
        });
}

function getStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
        case 'confirmed': return 'bg-success';
        case 'completed': return 'bg-info';
        case 'cancelled': return 'bg-danger';
        case 'pending': return 'bg-warning';
        default: return 'bg-primary';
    }
}