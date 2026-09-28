/**
 * TechStore - Module Quản Lý So Sánh Sản Phẩm Client-Side (Product Comparison)
 * Lưu trữ danh sách sản phẩm so sánh trong localStorage (tối đa 4 sản phẩm)
 */

const TechStoreCompare = (function () {
    const STORAGE_KEY = 'techstore_compare_list';
    const MAX_COMPARE_ITEMS = 4;

    function getList() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            console.error('Lỗi đọc dữ liệu so sánh từ localStorage', e);
            return [];
        }
    }

    function saveList(list) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(list));
            window.dispatchEvent(new CustomEvent('techstore:compare:changed', { detail: { list } }));
            updateUI();
        } catch (e) {
            console.error('Lỗi lưu dữ liệu so sánh', e);
        }
    }

    function isComparing(productId) {
        const list = getList();
        return list.some(item => String(item.id) === String(productId));
    }

    function toggleCompare(product) {
        if (!product || !product.id) return;

        let list = getList();
        const existingIndex = list.findIndex(item => String(item.id) === String(product.id));

        if (existingIndex >= 0) {
            // Đã có trong danh sách -> Xóa
            list.splice(existingIndex, 1);
            saveList(list);
            if (typeof window.showToast === 'function') {
                window.showToast('Đã xóa sản phẩm khỏi danh sách so sánh', 'info');
            }
            return false;
        } else {
            // Kiểm tra giới hạn 4 sản phẩm
            if (list.length >= MAX_COMPARE_ITEMS) {
                if (typeof window.showToast === 'function') {
                    window.showToast(`Bạn chỉ có thể so sánh tối đa ${MAX_COMPARE_ITEMS} sản phẩm cùng lúc!`, 'warning');
                } else {
                    alert(`Bạn chỉ có thể so sánh tối đa ${MAX_COMPARE_ITEMS} sản phẩm cùng lúc!`);
                }
                return false;
            }

            // Thêm mới
            list.push({
                id: product.id,
                name: product.name || '',
                slug: product.slug || '',
                price: product.price || 0,
                originalPrice: product.originalPrice || 0,
                image: product.image || '/assets/images/placeholder.png',
                brand: product.brand || '',
                category: product.category || ''
            });

            saveList(list);
            if (typeof window.showToast === 'function') {
                window.showToast(`Đã thêm "${product.name}" vào danh sách so sánh!`, 'success');
            }
            return true;
        }
    }

    function remove(productId) {
        let list = getList();
        list = list.filter(item => String(item.id) !== String(productId));
        saveList(list);
        if (typeof window.showToast === 'function') {
            window.showToast('Đã xóa khỏi danh sách so sánh', 'info');
        }
    }

    function clearAll() {
        localStorage.removeItem(STORAGE_KEY);
        saveList([]);
        if (typeof window.showToast === 'function') {
            window.showToast('Đã làm trống danh sách so sánh', 'info');
        }
    }

    function goToComparePage() {
        const list = getList();
        if (list.length < 2) {
            if (typeof window.showToast === 'function') {
                window.showToast('Vui lòng chọn ít nhất 2 sản phẩm để tiến hành so sánh!', 'warning');
            } else {
                alert('Vui lòng chọn ít nhất 2 sản phẩm để tiến hành so sánh!');
            }
            return;
        }
        const ids = list.map(item => item.id).join(',');
        window.location.href = `/compare?ids=${ids}`;
    }

    function updateUI() {
        const list = getList();
        const bar = document.getElementById('floatingCompareBar');
        const container = document.getElementById('compareThumbnailsContainer');
        const countBadge = document.getElementById('compareCountBadge');
        const actionBtn = document.getElementById('compareActionBtn');

        // Cập nhật trạng thái các nút So Sánh trên trang
        document.querySelectorAll('.compare-toggle-btn').forEach(btn => {
            const pId = btn.getAttribute('data-product-id');
            const inList = isComparing(pId);
            if (inList) {
                btn.classList.add('is-active', 'bg-blue-600', 'text-white', 'border-blue-600');
                btn.classList.remove('bg-white', 'text-slate-600', 'border-slate-200');
                const textSpan = btn.querySelector('.compare-btn-text');
                if (textSpan) textSpan.textContent = 'Đã chọn so sánh';
            } else {
                btn.classList.remove('is-active', 'bg-blue-600', 'text-white', 'border-blue-600');
                btn.classList.add('bg-white', 'text-slate-600', 'border-slate-200');
                const textSpan = btn.querySelector('.compare-btn-text');
                if (textSpan) textSpan.textContent = 'So sánh';
            }
        });

        if (!bar) return;

        if (list.length === 0) {
            bar.classList.add('translate-y-full', 'opacity-0', 'pointer-events-none');
            bar.classList.remove('translate-y-0', 'opacity-100');
            return;
        }

        bar.classList.remove('translate-y-full', 'opacity-0', 'pointer-events-none');
        bar.classList.add('translate-y-0', 'opacity-100');

        if (countBadge) countBadge.textContent = list.length;
        if (actionBtn) {
            if (list.length < 2) {
                actionBtn.classList.add('opacity-50', 'cursor-not-allowed');
                actionBtn.title = 'Cần chọn ít nhất 2 sản phẩm';
            } else {
                actionBtn.classList.remove('opacity-50', 'cursor-not-allowed');
                actionBtn.title = 'Xem bảng so sánh';
            }
        }

        if (container) {
            let html = '';
            for (let i = 0; i < MAX_COMPARE_ITEMS; i++) {
                if (i < list.length) {
                    const item = list[i];
                    html += `
                        <div class="relative w-12 h-12 md:w-14 md:h-14 bg-white rounded-xl border border-slate-200 p-1 flex items-center justify-center shadow-sm group">
                            <img src="${item.image}" alt="${item.name}" class="max-h-full max-w-full object-contain" />
                            <button type="button" onclick="TechStoreCompare.remove(${item.id})" class="absolute -top-1.5 -right-1.5 w-5 h-5 bg-rose-500 hover:bg-rose-600 text-white rounded-full flex items-center justify-center text-xs shadow-sm font-bold transition">
                                ×
                            </button>
                        </div>
                    `;
                } else {
                    html += `
                        <div class="w-12 h-12 md:w-14 md:h-14 rounded-xl border-2 border-dashed border-slate-200 flex items-center justify-center text-slate-300 font-bold text-xs bg-slate-50/50">
                            +
                        </div>
                    `;
                }
            }
            container.innerHTML = html;
        }
    }

    function toggleFromButton(button) {
        if (!button) return;
        const ds = button.dataset;
        const product = {
            id: ds.productId,
            name: ds.productName,
            slug: ds.productSlug,
            price: parseFloat(ds.productPrice) || 0,
            originalPrice: parseFloat(ds.productOrigPrice) || 0,
            image: ds.productImage,
            brand: ds.productBrand,
            category: ds.productCategory
        };
        toggleCompare(product);
    }

    // Tự động khởi chạy khi tải trang
    document.addEventListener('DOMContentLoaded', function () {
        updateUI();
    });

    return {
        getList,
        isComparing,
        toggleCompare,
        toggleFromButton,
        remove,
        clearAll,
        goToComparePage,
        updateUI
    };
})();

// Gắn toàn cục window để gọi trực tiếp từ Razor view onclick
window.TechStoreCompare = TechStoreCompare;
