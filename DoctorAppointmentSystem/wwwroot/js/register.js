$(document).ready(function () {
    // Handle doctor registration toggle
    $('#registerAsDoctor').change(function () {
        if (this.checked) {
            $('#specialtyField').show();
            $('#dateOfBirthField').hide();
        } else {
            $('#specialtyField').hide();
            $('#dateOfBirthField').show();
        }
    });

    // Trigger change event on page load
    $('#registerAsDoctor').trigger('change');

    // Handle file input label
    $('#profileImageInput').change(function () {
        var fileName = $(this).val().split('\\').pop();
        $(this).next('.custom-file-label').html(fileName || 'Choose profile image');
    });
});

function checkUsernameAvailability() {
    var username = $('#usernameInput').val();
    if (username.length < 3) return;

    $.get('@Url.Action("CheckUsernameAvailability", "Account")', { username: username })
        .done(function (data) {
            if (data.available) {
                $('#usernameAvailability').html('<span class="text-success">✓ Username available</span>');
            } else {
                $('#usernameAvailability').html('<span class="text-danger">✗ Username already taken</span>');
            }
        });
}

function checkEmailAvailability() {
    var email = $('#emailInput').val();
    if (!email.includes('@')) return;

    $.get('@Url.Action("CheckEmailAvailability", "Account")', { email: email })
        .done(function (data) {
            if (data.available) {
                $('#emailAvailability').html('<span class="text-success">✓ Email available</span>');
            } else {
                $('#emailAvailability').html('<span class="text-danger">✗ Email already registered</span>');
            }
        });
}

function checkPhoneAvailability() {
    var phone = $('#phoneInput').val();
    if (phone.length < 5) return;

    $.get('@Url.Action("CheckPhoneAvailability", "Account")', { phoneNumber: phone })
        .done(function (data) {
            if (data.available) {
                $('#phoneAvailability').html('<span class="text-success">✓ Phone number available</span>');
            } else {
                $('#phoneAvailability').html('<span class="text-danger">✗ Phone number already registered</span>');
            }
        });
}

// Date of birth validation
function validateDateOfBirth() {
    var dob = new Date($('#DateOfBirth').val());
    var today = new Date();

    if (dob > today) {
        $('#dateOfBirthError').html('<span class="text-danger">Date of birth cannot be in the future</span>');
        return false;
    } else {
        $('#dateOfBirthError').html('');
        return true;
    }
}

// Form submission validation
$(document).ready(function () {
    $('#registerForm').on('submit', function (e) {
        if (!validateDateOfBirth()) {
            e.preventDefault();
            return false;
        }
        return true;
    });
});

function checkPasswordStrength() {
    var password = $('#Password').val();
    var hasUpper = /[A-Z]/.test(password);
    var hasLower = /[a-z]/.test(password);
    var hasNumber = /[0-9]/.test(password); a
    var hasSpecial = /[^A-Za-z0-9]/.test(password);

    var messages = [];
    if (!hasUpper) messages.push('uppercase letter');
    if (!hasLower) messages.push('lowercase letter');
    if (!hasNumber) messages.push('number');
    if (!hasSpecial) messages.push('special character');

    if (messages.length === 0) {
        $('#passwordStrength').html('<span class="text-success">✓ Strong password</span>');
    } else {
        $('#passwordStrength').html('<span class="text-warning">Missing: ' + messages.join(', ') + '</span>');
    }
}