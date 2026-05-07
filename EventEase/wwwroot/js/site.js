// EventEase - Site JavaScript

// Image preview for file inputs
document.addEventListener('DOMContentLoaded', function () {
    const fileInputs = document.querySelectorAll('input[type="file"][accept*="image"]');
    fileInputs.forEach(function (input) {
        input.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (!file) return;
            const previewId = input.dataset.preview || 'imagePreview';
            const preview = document.getElementById(previewId);
            if (preview) {
                const reader = new FileReader();
                reader.onload = function (ev) {
                    preview.src = ev.target.result;
                    preview.style.display = 'block';
                };
                reader.readAsDataURL(file);
            }
        });
    });

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });

    // Confirm delete forms
    const deleteForms = document.querySelectorAll('form[data-confirm]');
    deleteForms.forEach(function (form) {
        form.addEventListener('submit', function (e) {
            const msg = form.dataset.confirm || 'Are you sure you want to delete this item?';
            if (!confirm(msg)) {
                e.preventDefault();
            }
        });
    });

    // Booking date validation — EndDate must be after StartDate
    const startDateInput = document.getElementById('StartDate');
    const endDateInput = document.getElementById('EndDate');
    if (startDateInput && endDateInput) {
        startDateInput.addEventListener('change', function () {
            if (endDateInput.value && endDateInput.value <= startDateInput.value) {
                endDateInput.value = '';
                endDateInput.setCustomValidity('End date must be after start date.');
            } else {
                endDateInput.setCustomValidity('');
            }
        });
        endDateInput.addEventListener('change', function () {
            if (startDateInput.value && endDateInput.value <= startDateInput.value) {
                endDateInput.setCustomValidity('End date must be after start date.');
            } else {
                endDateInput.setCustomValidity('');
            }
        });
    }

    // Tooltip initialization (Bootstrap 5)
    const tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipEls.forEach(function (el) {
        new bootstrap.Tooltip(el);
    });
});
