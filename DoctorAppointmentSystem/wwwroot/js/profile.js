document.addEventListener('DOMContentLoaded', function () {
    initializeImageUpload();
    initializeRemoveButton();
    checkExistingImage(); // Check if we should show remove button on page load
});

document.addEventListener('DOMContentLoaded', function () {
    const dobInput = document.querySelector('input[name="DateOfBirth"]');
    if (dobInput) {
        // Set max date to today
        const today = new Date().toISOString().split('T')[0];
        dobInput.setAttribute('max', today);

        // Validate on input
        dobInput.addEventListener('change', function () {
            const selectedDate = new Date(this.value);
            const today = new Date();

            if (selectedDate > today) {
                this.setCustomValidity('Date of birth cannot be in the future');
            } else {
                this.setCustomValidity('');
            }
        });
    }
});

function checkExistingImage() {
    const currentProfileImage = document.getElementById('current-profile-image');
    const removeImageBtn = document.getElementById('remove-image-btn');
    const defaultImagePath = '/images/profile-icon.png';

    if (currentProfileImage && removeImageBtn) {
        // Check if current image is not the default one
        const currentImageSrc = currentProfileImage.src;
        const isDefaultImage = currentImageSrc.includes(defaultImagePath) ||
            currentImageSrc.endsWith('/images/profile-icon.png');

        if (!isDefaultImage) {
            removeImageBtn.style.display = 'block';
        } else {
            removeImageBtn.style.display = 'none';
        }
    }
}

function initializeImageUpload() {
    const dropArea = document.getElementById('drop-area');
    const fileInput = document.getElementById('file-input');
    const imagePreview = document.getElementById('image-preview');
    const removeImageBtn = document.getElementById('remove-image-btn');
    const currentProfileImage = document.getElementById('current-profile-image');
    const dropAreaText = document.querySelector('.drop-area-text');
    const defaultImagePath = '/images/profile-icon.png';

    if (!dropArea) return;

    // Show remove button if current image is not default
    checkExistingImage();

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropArea.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop area when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        dropArea.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropArea.addEventListener(eventName, unhighlight, false);
    });

    // Handle dropped files
    dropArea.addEventListener('drop', handleDrop, false);

    // Handle click on drop area
    dropArea.addEventListener('click', function (e) {
        // Don't trigger file input if clicking the remove button
        if (!e.target.closest('#remove-image-btn')) {
            fileInput.click();
        }
    });

    // Handle file selection
    fileInput.addEventListener('change', function () {
        handleFiles(this.files);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight() {
        dropArea.classList.add('highlight');
    }

    function unhighlight() {
        dropArea.classList.remove('highlight');
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        handleFiles(files);
    }

    function handleFiles(files) {
        if (files.length === 0) return;

        const file = files[0];

        // Validate file type
        const validImageTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
        if (!validImageTypes.includes(file.type)) {
            showAlert('Please upload a valid image file (JPEG, PNG, GIF, BMP, WEBP).', 'danger');
            return;
        }

        // Validate file size (max 2MB)
        if (file.size > 2 * 1024 * 1024) {
            showAlert('File size too large. Maximum allowed size is 2MB.', 'danger');
            return;
        }

        // Preview image
        const reader = new FileReader();
        reader.onload = function (e) {
            if (imagePreview) {
                imagePreview.src = e.target.result;
                imagePreview.style.display = 'block';
            }

            // Hide current profile image
            if (currentProfileImage) {
                currentProfileImage.style.display = 'none';
            }

            // Hide drop area text
            if (dropAreaText) {
                dropAreaText.style.display = 'none';
            }

            // Show remove button (user is uploading a new image)
            if (removeImageBtn) {
                removeImageBtn.style.display = 'block';
            }
        };
        reader.readAsDataURL(file);

        // Set the file to the hidden input
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        fileInput.files = dataTransfer.files;
    }
}

function initializeRemoveButton() {
    const removeImageBtn = document.getElementById('remove-image-btn');

    if (removeImageBtn) {
        removeImageBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            // Determine user type from the current URL or page context
            const currentPath = window.location.pathname;
            let userType = 'patient';

            if (currentPath.includes('/Profile/Doctor') || currentPath.includes('/doctor')) {
                userType = 'doctor';
            } else if (currentPath.includes('/Profile/Patient') || currentPath.includes('/patient')) {
                userType = 'patient';
            }

            removeProfileImage(userType);
        });
    }
}

function removeProfileImage(userType) {
    if (!confirm('Are you sure you want to remove your profile image?')) {
        return;
    }

    fetch(`/Profile/Remove${userType.charAt(0).toUpperCase() + userType.slice(1)}Image`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value,
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Update the UI
                const imagePreview = document.getElementById('image-preview');
                const currentProfileImage = document.getElementById('current-profile-image');
                const dropAreaText = document.querySelector('.drop-area-text');
                const removeImageBtn = document.getElementById('remove-image-btn');
                const fileInput = document.getElementById('file-input');

                if (imagePreview) {
                    imagePreview.style.display = 'none';
                    imagePreview.src = '';
                }

                if (currentProfileImage) {
                    currentProfileImage.src = data.imageUrl;
                    currentProfileImage.style.display = 'block';
                }

                if (dropAreaText) {
                    dropAreaText.style.display = 'block';
                }

                // Hide remove button after successful removal (since it's now default image)
                if (removeImageBtn) {
                    removeImageBtn.style.display = 'none';
                }

                if (fileInput) {
                    fileInput.value = '';
                }

                // Update navigation
                updateProfileImageInNavigation(data.imageUrl);

                showAlert('Profile image removed successfully!', 'success');
            } else {
                showAlert(data.message || 'Failed to remove image', 'danger');
            }
        })
        .catch(error => {
            console.error('Error removing profile image:', error);
            showAlert('An error occurred while removing the image.', 'danger');
        });
}

function showAlert(message, type) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.custom-alert');
    existingAlerts.forEach(alert => alert.remove());

    // Create new alert
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show custom-alert`;
    alertDiv.style.position = 'fixed';
    alertDiv.style.top = '20px';
    alertDiv.style.right = '20px';
    alertDiv.style.zIndex = '9999';
    alertDiv.style.minWidth = '300px';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(alertDiv);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}

function updateProfileImageInNavigation(imageUrl) {
    const profileImages = document.querySelectorAll('.profile-image-nav');
    profileImages.forEach(img => {
        img.src = imageUrl;
    });

    // Also update the avatar in the dropdown
    const navAvatar = document.querySelector('.navbar .rounded-circle');
    if (navAvatar) {
        navAvatar.src = imageUrl;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Username availability check
    const usernameInput = document.querySelector('input[name="Username"]');
    if (usernameInput) {
        usernameInput.addEventListener('blur', function () {
            checkProfileUsernameAvailability(this.value);
        });
    }

    // Phone availability check
    const phoneInput = document.querySelector('input[name="PhoneNumber"]');
    if (phoneInput) {
        phoneInput.addEventListener('blur', function () {
            checkProfilePhoneAvailability(this.value);
        });
    }

    const emailInput = document.querySelector('input[name="Email"]');
    if (emailInput) {
        emailInput.addEventListener('blur', function () {
            checkProfileEmailAvailability(this.value);
        });
    }



});
function checkProfileEmailAvailability(email) {
    if (!email.includes('@')) return;

    fetch(`/Profile/CheckProfileEmailAvailability?email=${encodeURIComponent(email)}`)
        .then(response => response.json())
        .then(data => {
            const element = document.getElementById('emailAvailability') || createValidationElement('Email');
            if (data.available) {
                element.innerHTML = '<span class="text-success">✓ Email available</span>';
            } else {
                element.innerHTML = '<span class="text-danger">✗ Email already registered</span>';
            }
        });
}
function checkProfileUsernameAvailability(username) {
    if (username.length < 3) return;

    fetch(`/Profile/CheckProfileUsernameAvailability?username=${encodeURIComponent(username)}`)
        .then(response => response.json())
        .then(data => {
            const element = document.getElementById('usernameAvailability') || createValidationElement('username');
            if (data.available) {
                element.innerHTML = '<span class="text-success">✓ Username available</span>';
            } else {
                element.innerHTML = '<span class="text-danger">✗ Username already taken</span>';
            }
        });
}

function checkProfilePhoneAvailability(phoneNumber) {
    if (phoneNumber.length < 5) return;

    fetch(`/Profile/CheckProfilePhoneAvailability?phoneNumber=${encodeURIComponent(phoneNumber)}`)
        .then(response => response.json())
        .then(data => {
            const element = document.getElementById('phoneAvailability') || createValidationElement('PhoneNumber');
            if (data.available) {
                element.innerHTML = '<span class="text-success">✓ Phone number available</span>';
            } else {
                element.innerHTML = '<span class="text-danger">✗ Phone number already registered</span>';
            }
        });
}

function createValidationElement(forField) {
    const element = document.createElement('div');
    element.id = `${forField}Availability`;
    element.className = 'small mt-1';

    const inputField = document.querySelector(`[name="${forField}"]`);
    if (inputField) {
        inputField.parentNode.appendChild(element);
    }

    return element;
}

// Helper: debounce function
function debounce(func, wait) {
    let timeout;
    return function (...args) {
        const later = () => {
            clearTimeout(timeout);
            func.apply(this, args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

document.addEventListener('DOMContentLoaded', function () {
    // Real-time username check
    const usernameInput = document.querySelector('input[name="Username"]');
    if (usernameInput) {
        usernameInput.addEventListener('input', debounce(function () {
            checkProfileUsernameAvailability(this.value);
        }, 500)); // 500ms debounce
    }

    // Real-time phone check
    const phoneInput = document.querySelector('input[name="PhoneNumber"]');
    if (phoneInput) {
        phoneInput.addEventListener('input', debounce(function () {
            checkProfilePhoneAvailability(this.value);
        }, 500));
    }

    // Real-time email check
    const emailInput = document.querySelector('input[name="Email"]');
    if (emailInput) {
        emailInput.addEventListener('input', debounce(function () {
            checkProfileEmailAvailability(this.value);
        }, 500));
    }
});

document.addEventListener('DOMContentLoaded', function () {
    const phoneInput = document.getElementById('phoneNumberInput');

    phoneInput.addEventListener('blur', function () {
        const value = phoneInput.value.trim();
        const original = phoneInput.dataset.original;

        // Simple client-side validation: 10-15 digits
        const phoneRegex = /^\d{10,15}$/;

        if (value && !phoneRegex.test(value)) {
            alert("Invalid phone number! It must be 10-15 digits.");
            phoneInput.value = original; // restore original valid value
        }
    });
});

document.addEventListener("DOMContentLoaded", function () {

    const usernameInput = document.querySelector('input[name="Username"]');
    const emailInput = document.querySelector('input[name="Email"]');
    const phoneInput = document.querySelector('input[name="PhoneNumber"]');

    const usernameMsg = document.getElementById("usernameAvailability");
    const emailMsg = document.getElementById("emailAvailability");
    const phoneMsg = document.getElementById("phoneAvailability");

    // Helper function to perform AJAX GET request
    function checkAvailability(url, param, input, messageElement) {
        if (!input.value.trim()) {
            messageElement.textContent = "";
            return;
        }

        fetch(`${url}?${param}=${encodeURIComponent(input.value.trim())}`)
            .then(res => res.json())
            .then(data => {
                if (data.available) {
                    messageElement.textContent = "Available";
                    messageElement.style.color = "green";
                } else {
                    messageElement.textContent = "Already taken";
                    messageElement.style.color = "red";
                }
            })
            .catch(err => console.error(err));
    }

    // Username AJAX check
    usernameInput?.addEventListener("blur", () => {
        checkAvailability("/Profile/CheckProfileUsernameAvailability", "username", usernameInput, usernameMsg);
    });

    // Email AJAX check
    emailInput?.addEventListener("blur", () => {
        checkAvailability("/Profile/CheckProfileEmailAvailability", "email", emailInput, emailMsg);
    });

    // Phone AJAX check
    phoneInput?.addEventListener("blur", () => {
        checkAvailability("/Profile/CheckProfilePhoneAvailability", "phoneNumber", phoneInput, phoneMsg);
    });
});