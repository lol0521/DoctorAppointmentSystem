// Patients AJAX functionality
function initializePatientsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'name';
    let currentSortOrder = 'asc';
    let currentSearch = '';

    // Load initial data
    loadPatients();

    // Search button click
    $('#searchButton').click(function () {
        currentSearch = $('#searchInput').val();
        currentPage = 1;
        loadPatients();
    });

    // Search on enter key
    $('#searchInput').keypress(function (e) {
        if (e.which === 13) {
            currentSearch = $(this).val();
            currentPage = 1;
            loadPatients();
        }
    });

    // Sort buttons
    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        // Toggle sort order if clicking the same column
        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            // Reset other buttons to asc order and default icons
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc')
                    .find('i')
                    .removeClass('bi-arrow-down')
                    .addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        // Update button icon
        $(this).find('i')
            .removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadPatients();
    });

    // Page size change
    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadPatients();
    });

    // Pagination click event
    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadPatients();

            // Scroll to top of table
            $('html, body').animate({
                scrollTop: $('#patientsTableContainer').offset().top - 100
            }, 300);
        }
    });

    function loadPatients() {
        $.ajax({
            url: $('#patientsTableContainer').data('url'),
            type: 'GET',
            data: {
                searchString: currentSearch,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: function () {
                $('#patientsTableContainer').html(`
                    <div class="text-center py-3">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                `);
            },
            success: function (response) {
                if (response.success) {
                    renderPatientsTable(response.patients);
                    renderPagination(response.totalPages, response.page);
                } else {
                    $('#patientsTableContainer').html(`
                        <div class="alert alert-danger">
                            ${response.message}
                        </div>
                    `);
                }
            },
            error: function (xhr, status, error) {
                $('#patientsTableContainer').html(`
                    <div class="alert alert-danger">
                        Error loading patients. Please try again.
                        ${xhr.statusText ? '<br>' + xhr.statusText : ''}
                    </div>
                `);
                console.error('AJAX Error:', error);
            }
        });
    }

    function calculateAge(dateOfBirth) {
        if (!dateOfBirth) return 0;
        const birthDate = new Date(dateOfBirth);
        const today = new Date();
        let age = today.getFullYear() - birthDate.getFullYear();
        const monthDiff = today.getMonth() - birthDate.getMonth();

        if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
            age--;
        }
        return age;
    }

    function renderPatientsTable(patients) {
        if (patients.length === 0) {
            $('#patientsTableContainer').html(`
                <div class="alert alert-info text-center">
                    No patients found.
                </div>
            `);
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Patient</th>
                            <th>Gender</th>
                            <th>Age</th>
                            <th>Email</th>
                            <th>Phone</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>`;

        patients.forEach(function (patient) {
            const age = calculateAge(patient.dateOfBirth);

            tableHtml += `
                <tr>
                    <td>${patient.id}</td>
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="avatar me-2">
                                <img src="${patient.profileImage || '/images/default-avatar.png'}"
                                     class="rounded-circle" width="32" height="32" alt="Patient">
                            </div>
                            ${patient.name}
                        </div>
                    </td>
                    <td>${patient.gender || 'N/A'}</td>
                    <td>${age}</td>
                    <td>${patient.email}</td>
                    <td>${patient.phoneNumber || 'N/A'}</td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <a href="/Admin/DetailsPatient/${patient.id}" class="btn btn-outline-info" title="Details">
                                <i class="bi bi-eye"></i> View
                            </a>
                            <a href="/Admin/EditPatient/${patient.id}" class="btn btn-outline-primary" title="Edit">
                                <i class="bi bi-pencil"></i> Edit
                            </a>
                            <a href="/Admin/DeletePatient/${patient.id}" class="btn btn-outline-danger" title="Delete">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </td>
                </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#patientsTableContainer').html(tableHtml);
    }

    function renderPagination(totalPages, currentPage) {
        if (totalPages <= 1) {
            $('#pagination').empty();
            return;
        }

        let paginationHtml = '';

        // Previous button
        paginationHtml += `
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">
                    <i class="bi bi-chevron-left"></i> Previous
                </a>
            </li>`;

        // Page numbers
        const maxVisiblePages = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }

        // First page and ellipsis
        if (startPage > 1) {
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="1">1</a>
                </li>`;
            if (startPage > 2) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>`;
        }

        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
                </li>`;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">
                    Next <i class="bi bi-chevron-right"></i>
                </a>
            </li>`;

        $('#pagination').html(paginationHtml);
    }
}

// Doctors AJAX functionality
function initializeDoctorsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'id';
    let currentSortOrder = 'asc';
    let currentSearch = '';
    let currentSpecialty = '';

    // Load initial data
    loadDoctors();

    // Search button click
    $('#searchButton').click(function () {
        currentSearch = $('#searchInput').val();
        currentPage = 1;
        loadDoctors();
    });

    // Search on enter key
    $('#searchInput').keypress(function (e) {
        if (e.which === 13) {
            currentSearch = $(this).val();
            currentPage = 1;
            loadDoctors();
        }
    });

    // Specialty filter
    $('#filterButton').click(function () {
        currentSpecialty = $('#specialtyFilter').val();
        currentPage = 1;
        loadDoctors();
    });

    // Sort buttons
    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        // Toggle sort order if clicking the same column
        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            // Reset other buttons to asc order and default icons
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc')
                    .find('i')
                    .removeClass('bi-arrow-down')
                    .addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        // Update button icon
        $(this).find('i')
            .removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadDoctors();
    });

    // Page size change
    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadDoctors();
    });

    // Pagination click event
    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadDoctors();

            // Scroll to top of table
            $('html, body').animate({
                scrollTop: $('#doctorsTableContainer').offset().top - 100
            }, 300);
        }
    });

    function loadDoctors() {
        $.ajax({
            url: $('#doctorsTableContainer').data('url'),
            type: 'GET',
            data: {
                searchString: currentSearch,
                specialtyFilter: currentSpecialty,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: function () {
                $('#doctorsTableContainer').html(`
                    <div class="text-center py-3">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                `);
            },
            success: function (response) {
                if (response.success) {
                    renderDoctorsTable(response.doctors);
                    renderPagination(response.totalPages, response.page);
                    updateSpecialtyFilter(response.specialties);
                } else {
                    $('#doctorsTableContainer').html(`
                        <div class="alert alert-danger">
                            ${response.message}
                        </div>
                    `);
                }
            },
            error: function (xhr, status, error) {
                $('#doctorsTableContainer').html(`
                    <div class="alert alert-danger">
                        Error loading doctors. Please try again.
                        ${xhr.statusText ? '<br>' + xhr.statusText : ''}
                    </div>
                `);
                console.error('AJAX Error:', error);
            }
        });
    }

    function renderDoctorsTable(doctors) {
        if (doctors.length === 0) {
            $('#doctorsTableContainer').html(`
                <div class="alert alert-info text-center">
                    No doctors found.
                </div>
            `);
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover table-striped">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Profile</th>
                            <th>Specialty</th>
                            <th>Email</th>
                            <th>Phone</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>`;

        doctors.forEach(function (doctor) {
            tableHtml += `
                <tr>
                    <td>${doctor.id}</td>
                    <td>
                        <div class="doctor-profile">
                            <img src="${doctor.profileImage || '/images/default-avatar.png'}"
                                 class="profile-img" alt="Doctor" />
                            <span>${doctor.name}</span>
                        </div>
                    </td>
                    <td>${doctor.specialty}</td>
                    <td>${doctor.email}</td>
                    <td>${doctor.phoneNumber}</td>
                    <td>
                        <span class="badge bg-success">Active</span>
                    </td>
                    <td>
                        <div class="action-buttons">
                            <a href="/Admin/DetailsDoctor/${doctor.id}" class="btn btn-outline-info btn-sm" title="Details">
                                <i class="bi bi-eye"></i> View
                            </a>
                            <a href="/Admin/EditDoctor/${doctor.id}" class="btn btn-outline-primary btn-sm" title="Edit">
                                <i class="bi bi-pencil-square"></i> Edit
                            </a>
                            <a href="/Admin/DeleteDoctor/${doctor.id}" class="btn btn-outline-danger btn-sm" title="Delete">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </td>
                </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#doctorsTableContainer').html(tableHtml);
    }

    function renderPagination(totalPages, currentPage) {
        // Same pagination function as patients
        if (totalPages <= 1) {
            $('#pagination').empty();
            return;
        }

        let paginationHtml = '';

        // Previous button
        paginationHtml += `
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">
                    <i class="bi bi-chevron-left"></i> Previous
                </a>
            </li>`;

        // Page numbers
        const maxVisiblePages = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }

        // First page and ellipsis
        if (startPage > 1) {
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="1">1</a>
                </li>`;
            if (startPage > 2) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>`;
        }

        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
                </li>`;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">
                    Next <i class="bi bi-chevron-right"></i>
                </a>
            </li>`;

        $('#pagination').html(paginationHtml);
    }

    function updateSpecialtyFilter(specialties) {
        if (specialties && specialties.length > 0) {
            let options = '<option value="">All Specialties</option>';
            specialties.forEach(function (specialty) {
                const selected = specialty === currentSpecialty ? 'selected' : '';
                options += `<option value="${specialty}" ${selected}>${specialty}</option>`;
            });
            $('#specialtyFilter').html(options);
        }
    }
}

// Appointments AJAX functionality
function initializeAppointmentsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'date';
    let currentSortOrder = 'asc';
    let currentStatusFilter = '';
    let currentDateRangeFilter = '';
    let currentDoctorId = '';
    let currentPatientId = '';

    // Load initial data
    loadAppointments();

    // Apply filters button
    $('#applyFilters').click(function () {
        currentStatusFilter = $('#statusFilter').val();
        currentDateRangeFilter = $('#dateRangeFilter').val();
        currentDoctorId = $('#doctorFilter').val();
        currentPatientId = $('#patientFilter').val();
        currentPage = 1;
        loadAppointments();
    });

    // Clear filters button
    $('#clearFilters').click(function () {
        $('#statusFilter').val('');
        $('#dateRangeFilter').val('');
        $('#doctorFilter').val('');
        $('#patientFilter').val('');
        currentStatusFilter = '';
        currentDateRangeFilter = '';
        currentDoctorId = '';
        currentPatientId = '';
        currentPage = 1;
        loadAppointments();
    });

    // Sort buttons
    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        // Toggle sort order if clicking the same column
        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            // Reset other buttons to asc order and default icons
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc')
                    .find('i')
                    .removeClass('bi-arrow-down')
                    .addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        // Update button icon
        $(this).find('i')
            .removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadAppointments();
    });

    // Page size change
    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadAppointments();
    });

    // Pagination click event
    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadAppointments();

            // Scroll to top of table
            $('html, body').animate({
                scrollTop: $('#appointmentsTableContainer').offset().top - 100
            }, 300);
        }
    });

    function loadAppointments() {
        $.ajax({
            url: $('#appointmentsTableContainer').data('url'),
            type: 'GET',
            data: {
                statusFilter: currentStatusFilter,
                dateRangeFilter: currentDateRangeFilter,
                doctorId: currentDoctorId,
                patientId: currentPatientId,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: function () {
                $('#appointmentsTableContainer').html(`
                    <div class="text-center py-3">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                `);
            },
            success: function (response) {
                if (response.success) {
                    renderAppointmentsTable(response.appointments);
                    renderPagination(response.totalPages, response.page);
                    updateFilterDropdowns(response.doctors, response.patients, response.statuses);
                } else {
                    $('#appointmentsTableContainer').html(`
                        <div class="alert alert-danger">
                            ${response.message}
                        </div>
                    `);
                }
            },
            error: function (xhr, status, error) {
                $('#appointmentsTableContainer').html(`
                    <div class="alert alert-danger">
                        Error loading appointments. Please try again.
                        ${xhr.statusText ? '<br>' + xhr.statusText : ''}
                    </div>
                `);
                console.error('AJAX Error:', error);
            }
        });
    }

    function renderAppointmentsTable(appointments) {
        if (appointments.length === 0) {
            $('#appointmentsTableContainer').html(`
                <div class="alert alert-info text-center">
                    No appointments found.
                </div>
            `);
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>Appt. ID</th>
                            <th>Patient</th>
                            <th>Doctor</th>
                            <th>Date & Time</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>`;

        appointments.forEach(function (appointment) {
            const statusClass = appointment.status === "Completed" ? "bg-success" :
                appointment.status === "Confirmed" ? "bg-primary" :
                    appointment.status === "Pending" ? "bg-warning" : "bg-danger";

            tableHtml += `
                <tr>
                    <td>#AP-${appointment.id.toString().padStart(4, '0')}</td>
                    <td>${appointment.patient?.name || 'N/A'}</td>
                    <td>${appointment.doctor?.name || 'N/A'}</td>
                    <td>${formatDateTime(appointment.appointmentDate)}</td>
                    <td>
                        <span class="badge ${statusClass}">${appointment.status}</span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <a href="/Admin/AppointmentDetails/${appointment.id}" class="btn btn-outline-info">
                                <i class="bi bi-eye"></i> View
                            </a>
                            <a href="/Admin/EditAppointment/${appointment.id}" class="btn btn-outline-primary">
                                <i class="bi bi-pencil"></i> Edit
                            </a>
                            <a href="/Admin/DeleteAppointment/${appointment.id}" class="btn btn-outline-danger">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </td>
                </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#appointmentsTableContainer').html(tableHtml);
    }

    function formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
    }

    function renderPagination(totalPages, currentPage) {
        if (totalPages <= 1) {
            $('#pagination').empty();
            return;
        }

        let paginationHtml = '';

        // Previous button
        paginationHtml += `
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">
                    <i class="bi bi-chevron-left"></i> Previous
                </a>
            </li>`;

        // Page numbers
        const maxVisiblePages = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }

        // First page and ellipsis
        if (startPage > 1) {
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="1">1</a>
                </li>`;
            if (startPage > 2) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>`;
        }

        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                paginationHtml += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>`;
            }
            paginationHtml += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
                </li>`;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">
                    Next <i class="bi bi-chevron-right"></i>
                </a>
            </li>`;

        $('#pagination').html(paginationHtml);
    }

    function updateFilterDropdowns(doctors, patients, statuses) {
        // Update doctors dropdown
        if (doctors && doctors.length > 0) {
            let doctorOptions = '<option value="">All Doctors</option>';
            doctors.forEach(function (doctor) {
                const selected = doctor.id == currentDoctorId ? 'selected' : '';
                doctorOptions += `<option value="${doctor.id}" ${selected}>${doctor.name}</option>`;
            });
            $('#doctorFilter').html(doctorOptions);
        }

        // Update patients dropdown
        if (patients && patients.length > 0) {
            let patientOptions = '<option value="">All Patients</option>';
            patients.forEach(function (patient) {
                const selected = patient.id == currentPatientId ? 'selected' : '';
                patientOptions += `<option value="${patient.id}" ${selected}>${patient.name}</option>`;
            });
            $('#patientFilter').html(patientOptions);
        }

        // Update status dropdown
        if (statuses && statuses.length > 0) {
            let statusOptions = '<option value="">All Statuses</option>';
            statuses.forEach(function (status) {
                const selected = status === currentStatusFilter ? 'selected' : '';
                statusOptions += `<option value="${status}" ${selected}>${status}</option>`;
            });
            $('#statusFilter').html(statusOptions);
        }
    }
}

// Medical Records AJAX functionality
function initializeMedicalRecordsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'created';
    let currentSortOrder = 'desc';
    let currentSearch = '';
    let currentBloodType = '';

    loadMedicalRecords();

    $('#searchButton').click(function () {
        currentSearch = $('#searchInput').val();
        currentPage = 1;
        loadMedicalRecords();
    });

    $('#searchInput').keypress(function (e) {
        if (e.which === 13) {
            currentSearch = $(this).val();
            currentPage = 1;
            loadMedicalRecords();
        }
    });

    $('#bloodTypeFilter').change(function () {
        currentBloodType = $(this).val();
        currentPage = 1;
        loadMedicalRecords();
    });

    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc').find('i').removeClass('bi-arrow-down').addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        $(this).find('i').removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadMedicalRecords();
    });

    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadMedicalRecords();
    });

    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadMedicalRecords();
            $('html, body').animate({ scrollTop: $('#medicalRecordsTableContainer').offset().top - 100 }, 300);
        }
    });

    function loadMedicalRecords() {
        $.ajax({
            url: $('#medicalRecordsTableContainer').data('url'),
            type: 'GET',
            data: {
                searchString: currentSearch,
                bloodTypeFilter: currentBloodType,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: showLoading('#medicalRecordsTableContainer'),
            success: function (response) {
                if (response.success) {
                    renderMedicalRecordsTable(response.medicalRecords);
                    renderPagination(response.totalPages, response.page, '#pagination');
                    updateBloodTypeFilter(response.bloodTypes);
                } else {
                    showError('#medicalRecordsTableContainer', response.message);
                }
            },
            error: function (xhr) {
                showError('#medicalRecordsTableContainer', 'Error loading medical records: ' + xhr.statusText);
            }
        });
    }

    function renderMedicalRecordsTable(medicalRecords) {
        if (medicalRecords.length === 0) {
            $('#medicalRecordsTableContainer').html(noDataMessage('No medical records found.'));
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>Record ID</th>
                            <th>Patient Name</th>
                            <th>Blood Type</th>
                            <th>Height (cm)</th>
                            <th>Weight (kg)</th>
                            <th>Allergies</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>`;

        medicalRecords.forEach(function (record) {
            tableHtml += `
                <tr>
                    <td>#MR-${record.id.toString().padStart(4, '0')}</td>
                    <td>${record.patient?.name || 'N/A'}</td>
                    <td>${record.bloodType || 'N/A'}</td>
                    <td>${record.height || 'N/A'}</td>
                    <td>${record.weight || 'N/A'}</td>
                    <td>${record.allergies ? (record.allergies.length > 20 ? record.allergies.substring(0, 20) + '...' : record.allergies) : 'N/A'}</td>
                    <td>${new Date(record.createdDate).toLocaleDateString()}</td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <a href="/Admin/MedicalRecordDetails/${record.id}" class="btn btn-outline-info">
                                <i class="bi bi-eye"></i> View
                            </a>
                            <a href="/Admin/EditMedicalRecord/${record.id}" class="btn btn-outline-primary">
                                <i class="bi bi-pencil"></i> Edit
                            </a>
                            <a href="/Admin/DeleteMedicalRecord/${record.id}" class="btn btn-outline-danger">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </td>
                </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#medicalRecordsTableContainer').html(tableHtml);
    }

    function updateBloodTypeFilter(bloodTypes) {
        if (bloodTypes && bloodTypes.length > 0) {
            let options = '<option value="">All Blood Types</option>';
            bloodTypes.forEach(function (bloodType) {
                const selected = bloodType === currentBloodType ? 'selected' : '';
                options += `<option value="${bloodType}" ${selected}>${bloodType}</option>`;
            });
            $('#bloodTypeFilter').html(options);
        }
    }
}

// Doctor Schedules AJAX functionality
// Add calendar view functionality to the Doctor Schedules AJAX
function initializeDoctorSchedulesAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'date';
    let currentSortOrder = 'asc';
    let currentDoctorId = '';
    let currentDateRange = '';
    let currentAvailability = '';
    let currentViewType = 'list';

    loadSchedules();

    $('#applyFilters').click(function () {
        currentDoctorId = $('#doctorFilter').val();
        currentDateRange = $('#dateRangeFilter').val();
        currentAvailability = $('#availabilityFilter').val();
        currentViewType = $('#viewTypeSelect').val();
        currentPage = 1;
        loadSchedules();
    });

    $('#clearFilters').click(function () {
        $('#doctorFilter').val('');
        $('#dateRangeFilter').val('');
        $('#availabilityFilter').val('');
        $('#viewTypeSelect').val('list');
        currentDoctorId = '';
        currentDateRange = '';
        currentAvailability = '';
        currentViewType = 'list';
        currentPage = 1;
        loadSchedules();
    });

    // View type toggle buttons
    $('.view-type-btn').click(function () {
        currentViewType = $(this).data('viewtype');
        $('.view-type-btn').removeClass('active');
        $(this).addClass('active');
        $('#viewTypeSelect').val(currentViewType);
        loadSchedules();
    });

    // Sort buttons
    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc').find('i').removeClass('bi-arrow-down').addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        $(this).find('i').removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadSchedules();
    });

    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadSchedules();
    });

    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadSchedules();
            $('html, body').animate({ scrollTop: $('#schedulesTableContainer').offset().top - 100 }, 300);
        }
    });

    function loadSchedules() {
        $.ajax({
            url: $('#schedulesTableContainer').data('url'),
            type: 'GET',
            data: {
                doctorId: currentDoctorId,
                dateRangeFilter: currentDateRange,
                isAvailable: currentAvailability,
                viewType: currentViewType,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: showLoading('#schedulesTableContainer'),
            success: function (response) {
                if (response.success) {
                    if (response.viewType === 'calendar') {
                        renderCalendarView(response.schedules, response.appointments, response.doctors);
                    } else {
                        renderSchedulesTable(response.schedules);
                    }
                    renderPagination(response.totalPages, response.page, '#pagination');
                    updateDoctorFilter(response.doctors);
                } else {
                    showError('#schedulesTableContainer', response.message);
                }
            },
            error: function (xhr) {
                showError('#schedulesTableContainer', 'Error loading schedules: ' + xhr.statusText);
            }
        });
    }

    function renderSchedulesTable(schedules) {
        if (schedules.length === 0) {
            $('#schedulesTableContainer').html(noDataMessage('No schedules found.'));
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>Schedule ID</th>
                            <th>Doctor</th>
                            <th>Date</th>
                            <th>Start Time</th>
                            <th>End Time</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>`;

        schedules.forEach(function (schedule) {
            tableHtml += `
                <tr>
                    <td>#SC-${schedule.id.toString().padStart(4, '0')}</td>
                    <td>${schedule.doctor?.name || 'N/A'}</td>
                    <td>${new Date(schedule.date).toLocaleDateString()}</td>
                    <td>${schedule.startTime}</td>
                    <td>${schedule.endTime}</td>
                    <td><span class="badge ${schedule.isAvailable ? 'bg-success' : 'bg-secondary'}">${schedule.isAvailable ? 'Available' : 'Unavailable'}</span></td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <a href="/Admin/EditSchedule/${schedule.id}" class="btn btn-outline-primary">
                                <i class="bi bi-pencil"></i> Edit
                            </a>
                            <a href="/Admin/DetailsSchedule/${schedule.id}" class="btn btn-outline-info">
                                <i class="bi bi-eye"></i> View
                            </a>
                            <a href="/Admin/DeleteSchedule/${schedule.id}" class="btn btn-outline-danger">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </td>
                </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#schedulesTableContainer').html(tableHtml);
    }

    function renderCalendarView(schedules, appointments, doctors) {
        if (schedules.length === 0) {
            $('#schedulesTableContainer').html(noDataMessage('No schedules found for the selected criteria.'));
            return;
        }

        const selectedDoctor = doctors.find(d => d.id == currentDoctorId);

        let calendarHtml = `
            <div class="calendar-view">
                ${selectedDoctor ? `<h5>Schedule for Dr. ${selectedDoctor.name}</h5>` : '<h5>All Doctors Schedule</h5>'}
                
                <div class="weekly-calendar">`;

        // Group schedules by date
        const schedulesByDate = {};
        schedules.forEach(schedule => {
            const dateKey = new Date(schedule.date).toDateString();
            if (!schedulesByDate[dateKey]) {
                schedulesByDate[dateKey] = [];
            }
            schedulesByDate[dateKey].push(schedule);
        });

        // Create calendar days
        Object.keys(schedulesByDate).sort().forEach(dateKey => {
            const date = new Date(dateKey);
            const daySchedules = schedulesByDate[dateKey];

            calendarHtml += `
                <div class="calendar-day-card">
                    <div class="day-header">
                        <strong>${date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })}</strong>
                    </div>
                    <div class="time-slots">`;

            daySchedules.sort((a, b) => a.startTime.localeCompare(b.startTime)).forEach(schedule => {
                const slotAppointments = appointments ? appointments.filter(a =>
                    new Date(a.appointmentDate).toDateString() === dateKey &&
                    a.appointmentDate.includes(schedule.startTime)
                ) : [];

                calendarHtml += `
                    <div class="time-slot ${!schedule.isAvailable ? 'unavailable' : ''}">
                        <div class="slot-time">
                            ${schedule.startTime} - ${schedule.endTime}
                        </div>
                        <div class="slot-content">`;

                if (!schedule.isAvailable) {
                    calendarHtml += `<span class="text-muted">Not Available</span>`;
                } else if (slotAppointments.length > 0) {
                    slotAppointments.forEach(appt => {
                        calendarHtml += `
                            <div class="appointment-badge ${appt.status.toLowerCase()}">
                                <small>
                                    ${new Date(appt.appointmentDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}<br>
                                    ${appt.patient?.name || 'Unknown Patient'}<br>
                                    <span class="badge bg-secondary">${appt.status}</span>
                                </small>
                            </div>`;
                    });
                } else {
                    calendarHtml += `<span class="text-success">Available</span>`;
                }

                calendarHtml += `</div></div>`;
            });

            calendarHtml += `</div></div>`;
        });

        calendarHtml += `</div>`;

        // Add summary
        const availableSlots = schedules.filter(s => s.isAvailable).length;
        const unavailableSlots = schedules.filter(s => !s.isAvailable).length;
        const totalAppointments = appointments ? appointments.length : 0;

        calendarHtml += `
            <div class="row mt-4">
                <div class="col-md-3">
                    <div class="card bg-success text-white">
                        <div class="card-body text-center">
                            <h6>Available Slots</h6>
                            <h4>${availableSlots}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-info text-white">
                        <div class="card-body text-center">
                            <h6>Booked Appointments</h6>
                            <h4>${totalAppointments}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-warning text-white">
                        <div class="card-body text-center">
                            <h6>Unavailable Slots</h6>
                            <h4>${unavailableSlots}</h4>
                        </div>
                    </div>
                </div>
            </div>
        </div>`;

        $('#schedulesTableContainer').html(calendarHtml);
    }

    function updateDoctorFilter(doctors) {
        if (doctors && doctors.length > 0) {
            let options = '<option value="">All Doctors</option>';
            doctors.forEach(function (doctor) {
                const selected = doctor.id == currentDoctorId ? 'selected' : '';
                options += `<option value="${doctor.id}" ${selected}>${doctor.name}</option>`;
            });
            $('#doctorFilter').html(options);
        }
    }
}

// Payments AJAX functionality
function initializePaymentsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'date';
    let currentSortOrder = 'desc';
    let currentStatus = '';
    let currentMethod = '';
    let currentDateRange = '';

    loadPayments();

    $('#applyFilters').click(function () {
        currentStatus = $('#statusFilter').val();
        currentMethod = $('#methodFilter').val();
        currentDateRange = $('#dateRangeFilter').val();
        currentPage = 1;
        loadPayments();
    });

    $('#clearFilters').click(function () {
        $('#statusFilter').val('');
        $('#methodFilter').val('');
        $('#dateRangeFilter').val('');
        currentStatus = '';
        currentMethod = '';
        currentDateRange = '';
        currentPage = 1;
        loadPayments();
    });

    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc').find('i').removeClass('bi-arrow-down').addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        $(this).find('i').removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadPayments();
    });

    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadPayments();
    });

    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadPayments();
            $('html, body').animate({ scrollTop: $('#paymentsTableContainer').offset().top - 100 }, 300);
        }
    });

    function loadPayments() {
        $.ajax({
            url: $('#paymentsTableContainer').data('url'),
            type: 'GET',
            data: {
                statusFilter: currentStatus,
                paymentMethodFilter: currentMethod,
                dateRangeFilter: currentDateRange,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: showLoading('#paymentsTableContainer'),
            success: function (response) {
                if (response.success) {
                    renderPaymentsTable(response.payments);
                    renderPagination(response.totalPages, response.page, '#pagination');
                    updatePaymentFilters(response.statuses, response.paymentMethods);
                } else {
                    showError('#paymentsTableContainer', response.message);
                }
            },
            error: function (xhr) {
                showError('#paymentsTableContainer', 'Error loading payments: ' + xhr.statusText);
            }
        });
    }

    function renderPaymentsTable(payments) {
        if (payments.length === 0) {
            $('#paymentsTableContainer').html(noDataMessage('No payments found.'));
            return;
        }

        let tableHtml = `
        <div class="table-responsive">
            <table class="table table-hover mb-0">
                <thead>
                    <tr>
                        <th>Payment ID</th>
                        <th>Appointment</th>
                        <th>Patient</th>
                        <th>Date</th>
                        <th>Amount</th>
                        <th>Method</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>`;

        payments.forEach(function (payment) {
            // Safe handling of TransactionId - generate one if missing
            const transactionId = payment.transactionId || `PY-${payment.id.toString().padStart(4, '0')}`;
            const patientName = payment.appointment?.patient?.name || 'N/A';
            const paymentDate = payment.paymentDate ? new Date(payment.paymentDate).toLocaleDateString() : 'N/A';
            const amount = payment.amount ? `$${payment.amount.toFixed(2)}` : '$0.00';
            const paymentMethod = payment.paymentMethod || 'N/A';
            const status = payment.status || 'Unknown';

            tableHtml += `
            <tr>
                <td>${transactionId}</td>
                <td>#AP-${payment.appointmentId.toString().padStart(4, '0')}</td>
                <td>${patientName}</td>
                <td>${paymentDate}</td>
                <td>${amount}</td>
                <td>${paymentMethod}</td>
                <td><span class="badge ${status === 'Paid' ? 'bg-success' : 'bg-warning'}">${status}</span></td>
                <td>
                    <div class="btn-group btn-group-sm">
                        <button onclick="viewInvoice(${payment.id})" class="btn btn-outline-info" title="View Invoice">
                            <i class="bi bi-receipt"></i> Invoice
                        </button>
                        <button onclick="printInvoice(${payment.id})" class="btn btn-outline-secondary" title="Print">
                            <i class="bi bi-printer"></i> Print
                        </button>
                    </div>
                </td>
            </tr>`;
        });

        tableHtml += `</tbody></table></div>`;
        $('#paymentsTableContainer').html(tableHtml);
    }

    function updatePaymentFilters(statuses, methods) {
        if (statuses && statuses.length > 0) {
            let options = '<option value="">All Statuses</option>';
            statuses.forEach(function (status) {
                const selected = status === currentStatus ? 'selected' : '';
                options += `<option value="${status}" ${selected}>${status}</option>`;
            });
            $('#statusFilter').html(options);
        }

        if (methods && methods.length > 0) {
            let options = '<option value="">All Methods</option>';
            methods.forEach(function (method) {
                const selected = method === currentMethod ? 'selected' : '';
                options += `<option value="${method}" ${selected}>${method}</option>`;
            });
            $('#methodFilter').html(options);
        }
    }
}

// Reviews AJAX functionality
function initializeReviewsAjax() {
    let currentPage = 1;
    let currentPageSize = 10;
    let currentSortBy = 'date';
    let currentSortOrder = 'desc';
    let currentRating = '';
    let currentDoctorId = '';
    let currentDateRange = '';

    loadReviews();

    $('#applyFilters').click(function () {
        currentRating = $('#ratingFilter').val();
        currentDoctorId = $('#doctorFilter').val();
        currentDateRange = $('#dateRangeFilter').val();
        currentPage = 1;
        loadReviews();
    });

    $('#clearFilters').click(function () {
        $('#ratingFilter').val('');
        $('#doctorFilter').val('');
        $('#dateRangeFilter').val('');
        currentRating = '';
        currentDoctorId = '';
        currentDateRange = '';
        currentPage = 1;
        loadReviews();
    });

    $('.sort-btn').click(function () {
        const sortBy = $(this).data('sortby');
        let sortOrder = $(this).data('sortorder');

        if (currentSortBy === sortBy) {
            sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
            $(this).data('sortorder', sortOrder);
        } else {
            $('.sort-btn').not(this).each(function () {
                $(this).data('sortorder', 'asc').find('i').removeClass('bi-arrow-down').addClass('bi-arrow-up');
            });
        }

        currentSortBy = sortBy;
        currentSortOrder = sortOrder;

        $(this).find('i').removeClass('bi-arrow-up bi-arrow-down')
            .addClass(sortOrder === 'asc' ? 'bi-arrow-up' : 'bi-arrow-down');

        loadReviews();
    });

    $('#pageSize').change(function () {
        currentPageSize = parseInt($(this).val());
        currentPage = 1;
        loadReviews();
    });

    $(document).on('click', '#pagination .page-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        if (page && page !== currentPage) {
            currentPage = page;
            loadReviews();
            $('html, body').animate({ scrollTop: $('#reviewsContainer').offset().top - 100 }, 300);
        }
    });

    function loadReviews() {
        $.ajax({
            url: $('#reviewsContainer').data('url'),
            type: 'GET',
            data: {
                ratingFilter: currentRating,
                doctorId: currentDoctorId,
                dateRangeFilter: currentDateRange,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
                page: currentPage,
                pageSize: currentPageSize
            },
            beforeSend: showLoading('#reviewsContainer'),
            success: function (response) {
                if (response.success) {
                    renderReviews(response.reviews);
                    renderPagination(response.totalPages, response.page, '#pagination');
                    updateReviewFilters(response.doctors);
                } else {
                    showError('#reviewsContainer', response.message);
                }
            },
            error: function (xhr) {
                showError('#reviewsContainer', 'Error loading reviews: ' + xhr.statusText);
            }
        });
    }

    function renderReviews(reviews) {
        if (reviews.length === 0) {
            $('#reviewsContainer').html(noDataMessage('No reviews found.'));
            return;
        }

        let reviewsHtml = '<div class="row">';

        reviews.forEach(function (review) {
            reviewsHtml += `
                <div class="col-md-6 col-lg-4 mb-4">
                    <div class="review-card h-100">
                        <div class="review-header">
                            <div class="review-patient">
                                <img src="${review.patient?.profileImage || '/images/default-avatar.png'}"
                                     class="rounded-circle me-2" width="40" height="40">
                                <div>
                                    <h6 class="mb-0">${review.patient?.name || 'N/A'}</h6>
                                    <small class="text-muted">${new Date(review.date).toLocaleDateString()}</small>
                                </div>
                            </div>
                            <div class="review-rating">`;

            for (let i = 1; i <= 5; i++) {
                reviewsHtml += `<i class="bi ${i <= review.rating ? 'bi-star-fill text-warning' : 'bi-star text-muted'}"></i>`;
            }

            reviewsHtml += `
                            </div>
                        </div>
                        <div class="review-body">
                            <h5>${review.doctor?.name || 'N/A'}</h5>
                            <p class="review-comment">${review.comment || 'No comment'}</p>
                        </div>
                        <div class="review-actions">                            
                            <a href="/Admin/DeleteReview/${review.id}" class="btn btn-sm btn-outline-danger">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    </div>
                </div>`;
        });

        reviewsHtml += '</div>';
        $('#reviewsContainer').html(reviewsHtml);
    }

    function updateReviewFilters(doctors) {
        if (doctors && doctors.length > 0) {
            let options = '<option value="">All Doctors</option>';
            doctors.forEach(function (doctor) {
                const selected = doctor.id == currentDoctorId ? 'selected' : '';
                options += `<option value="${doctor.id}" ${selected}>${doctor.name}</option>`;
            });
            $('#doctorFilter').html(options);
        }
    }
}

// Utility functions
function showLoading(container) {
    $(container).html(`
        <div class="text-center py-3">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `);
}

function showError(container, message) {
    $(container).html(`
        <div class="alert alert-danger">
            ${message}
        </div>
    `);
}

function noDataMessage(message) {
    return `
        <div class="alert alert-info text-center">
            ${message}
        </div>
    `;
}

function renderPagination(totalPages, currentPage, paginationId) {
    if (totalPages <= 1) {
        $(paginationId).empty();
        return;
    }

    let paginationHtml = '';

    // Previous button
    paginationHtml += `
        <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${currentPage - 1}">
                <i class="bi bi-chevron-left"></i> Previous
            </a>
        </li>`;

    // Page numbers
    const maxVisiblePages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

    if (endPage - startPage + 1 < maxVisiblePages) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    // First page and ellipsis
    if (startPage > 1) {
        paginationHtml += `
            <li class="page-item">
                <a class="page-link" href="#" data-page="1">1</a>
            </li>`;
        if (startPage > 2) {
            paginationHtml += `
                <li class="page-item disabled">
                    <span class="page-link">...</span>
                </li>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        paginationHtml += `
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">${i}</a>
            </li>`;
    }

    // Last page and ellipsis
    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHtml += `
                <li class="page-item disabled">
                    <span class="page-link">...</span>
                </li>`;
        }
        paginationHtml += `
            <li class="page-item">
                <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
            </li>`;
    }

    // Next button
    paginationHtml += `
        <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${currentPage + 1}">
                Next <i class="bi bi-chevron-right"></i>
            </a>
        </li>`;

    $(paginationId).html(paginationHtml);
}

// Update the initialization function
$(document).ready(function () {
    if ($('#patientsTableContainer').length) initializePatientsAjax();
    if ($('#doctorsTableContainer').length) initializeDoctorsAjax();
    if ($('#appointmentsTableContainer').length) initializeAppointmentsAjax();
    if ($('#medicalRecordsTableContainer').length) initializeMedicalRecordsAjax();
    if ($('#schedulesTableContainer').length) initializeDoctorSchedulesAjax();
    if ($('#paymentsTableContainer').length) initializePaymentsAjax();
    if ($('#reviewsContainer').length) initializeReviewsAjax();
});

// Function to view invoice
function viewInvoice(paymentId) {
    window.location.href = `/Admin/Invoice/${paymentId}`;
}

function printInvoice(paymentId) {
    window.location.href = `/Admin/Print/${paymentId}?print=true`;
}

// Function to download PDF
function downloadPdf(paymentId) {
    window.open(`/Admin/DownloadPdf/${paymentId}`, '_blank');
}