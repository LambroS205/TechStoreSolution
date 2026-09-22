/**
 * TechStore Toast Notification System
 * Tự động tạo container và hiển thị popup thông báo góc màn hình với hiệu ứng mượt mà
 */
(function () {
    function getContainer() {
        let container = document.getElementById('techstore-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'techstore-toast-container';
            container.className = 'fixed top-5 right-5 z-[9999] flex flex-col gap-2.5 max-w-sm w-full pointer-events-none px-4 sm:px-0';
            document.body.appendChild(container);
        }
        return container;
    }

    window.showToast = function (message, type = 'success', duration = 4000) {
        if (!message) return;

        const container = getContainer();

        const toast = document.createElement('div');
        toast.className = 'pointer-events-auto transform transition-all duration-300 ease-out translate-x-full opacity-0 flex items-start gap-3 p-4 rounded-xl shadow-lg border text-xs font-semibold backdrop-blur-md';

        let iconSvg = '';
        let colorClasses = '';

        switch (type.toLowerCase()) {
            case 'success':
                colorClasses = 'bg-emerald-50/95 border-emerald-300 text-emerald-900';
                iconSvg = `<svg class="w-5 h-5 text-emerald-600 shrink-0" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>`;
                break;
            case 'error':
            case 'danger':
                colorClasses = 'bg-rose-50/95 border-rose-300 text-rose-900';
                iconSvg = `<svg class="w-5 h-5 text-rose-600 shrink-0" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>`;
                break;
            case 'warning':
                colorClasses = 'bg-amber-50/95 border-amber-300 text-amber-900';
                iconSvg = `<svg class="w-5 h-5 text-amber-600 shrink-0" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>`;
                break;
            default: // info
                colorClasses = 'bg-blue-50/95 border-blue-300 text-blue-900';
                iconSvg = `<svg class="w-5 h-5 text-[#0046be] shrink-0" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/></svg>`;
                break;
        }

        toast.className += ' ' + colorClasses;
        toast.innerHTML = `
            ${iconSvg}
            <div class="flex-1 leading-relaxed pt-0.5">
                ${message}
            </div>
            <button type="button" class="text-slate-400 hover:text-slate-600 transition shrink-0 ml-1 p-0.5" onclick="this.parentElement.remove()">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
        `;

        container.appendChild(toast);

        // Slide-in animation
        requestAnimationFrame(() => {
            toast.classList.remove('translate-x-full', 'opacity-0');
            toast.classList.add('translate-x-0', 'opacity-100');
        });

        // Auto remove after duration
        setTimeout(() => {
            toast.classList.remove('translate-x-0', 'opacity-100');
            toast.classList.add('translate-x-full', 'opacity-0');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    };

    // Auto-display server TempData messages on load if present
    document.addEventListener('DOMContentLoaded', () => {
        const serverToast = document.getElementById('techstore-server-toast');
        if (serverToast) {
            const msg = serverToast.getAttribute('data-message');
            const type = serverToast.getAttribute('data-type') || 'success';
            if (msg) {
                window.showToast(msg, type);
            }
        }
    });
})();
