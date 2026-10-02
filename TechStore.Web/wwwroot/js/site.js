/**
 * TechStore - Global Client-Side Enhancements
 * Provides anti-double submit protection, form helpers, and UI polish.
 */

document.addEventListener('DOMContentLoaded', () => {
    // 1. Anti-double submit handler on POST forms (except checkout which has custom handler)
    const forms = document.querySelectorAll('form[method="post"]:not(#checkoutForm)');
    forms.forEach(form => {
        form.addEventListener('submit', (e) => {
            if (form.dataset.submitting === 'true') {
                e.preventDefault();
                return false;
            }

            // Let native validation do its job first
            if (form.checkValidity && !form.checkValidity()) {
                return;
            }

            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                form.dataset.submitting = 'true';
                submitBtn.dataset.originalText = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.classList.add('opacity-75', 'cursor-not-allowed');

                const labelSpan = submitBtn.querySelector('span');
                if (labelSpan) {
                    labelSpan.textContent = 'Đang xử lý...';
                } else {
                    submitBtn.textContent = 'Đang xử lý...';
                }

                // Safety release after 8 seconds in case response is a file download or doesn't navigate
                setTimeout(() => {
                    form.dataset.submitting = 'false';
                    submitBtn.disabled = false;
                    submitBtn.classList.remove('opacity-75', 'cursor-not-allowed');
                    if (submitBtn.dataset.originalText) {
                        submitBtn.innerHTML = submitBtn.dataset.originalText;
                    }
                }, 8000);
            }
        });
    });
});
