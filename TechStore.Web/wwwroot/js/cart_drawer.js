// Quản lý Giỏ hàng LocalStorage & Giao tiếp Web API cho TechStore
const CART_STORAGE_KEY = 'techstore_cart';
const COUPON_STORAGE_KEY = 'techstore_coupon';

// 1. Lấy dữ liệu giỏ hàng từ LocalStorage
function getStoredCart() {
    try {
        const raw = localStorage.getItem(CART_STORAGE_KEY);
        return raw ? JSON.parse(raw) : [];
    } catch {
        return [];
    }
}

// 2. Lưu giỏ hàng vào LocalStorage
function saveStoredCart(cart) {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(cart));
    updateCartBadge();
}

// 3. Thêm mặt hàng vào giỏ (gọi từ nút Mua Ngay hoặc Thêm Vào Giỏ)
function addToCart(variantId, quantity = 1, openDrawerImmediately = true) {
    let cart = getStoredCart();
    quantity = Math.max(1, Math.min(50, quantity));
    const existingIndex = cart.findIndex(item => item.variantId === variantId);

    if (existingIndex > -1) {
        cart[existingIndex].quantity = Math.min(50, cart[existingIndex].quantity + quantity);
    } else {
        cart.push({ variantId: variantId, quantity: quantity });
    }

    saveStoredCart(cart);

    if (openDrawerImmediately) {
        toggleCartDrawer(true);
    }
}

// 4. Cập nhật số lượng hiển thị trên Badge icon giỏ hàng ở Header
function updateCartBadge() {
    const cart = getStoredCart();
    const count = cart.reduce((total, item) => total + item.quantity, 0);
    const badge = document.getElementById('cartBadgeCount');
    if (badge) badge.innerText = count;
}

// 5. Mở hoặc đóng Cart Drawer trượt
function toggleCartDrawer(forceOpen = null) {
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartBackdrop');
    if (!drawer || !backdrop) return;

    const isOpen = !drawer.classList.contains('translate-x-full');
    const willOpen = forceOpen !== null ? forceOpen : !isOpen;

    if (willOpen) {
        backdrop.classList.remove('hidden');
        drawer.classList.remove('translate-x-full');
        renderCartDrawerItems();
    } else {
        drawer.classList.add('translate-x-full');
        backdrop.classList.add('hidden');
    }
}

let drawerSubTotalValue = 0;
let drawerDiscountValue = 0;

// 6. Gửi danh sách variantId lên Web API `/api/cart/details` và render giao diện
async function renderCartDrawerItems() {
    const container = document.getElementById('drawerCartItems');
    const drawerItemCount = document.getElementById('drawerItemCount');
    const subTotalEl = document.getElementById('drawerSubTotal');
    const finalTotalEl = document.getElementById('drawerFinalTotal');
    const checkoutBtn = document.getElementById('drawerCheckoutBtn');
    const discountRow = document.getElementById('drawerDiscountRow');
    const cart = getStoredCart();

    if (!container) return;

    if (cart.length === 0) {
        drawerSubTotalValue = 0;
        drawerDiscountValue = 0;
        if (discountRow) discountRow.classList.add('hidden');
        if (checkoutBtn) {
            checkoutBtn.classList.add('pointer-events-none', 'opacity-50');
            checkoutBtn.removeAttribute('href');
        }

        container.innerHTML = `
            <div class="py-16 text-center space-y-3">
                <div class="w-16 h-16 mx-auto bg-slate-100 rounded-full flex items-center justify-center text-slate-400">
                    <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" /></svg>
                </div>
                <p class="text-sm font-semibold text-slate-700">Giỏ hàng của bạn đang trống</p>
                <p class="text-xs text-slate-400">Hãy lựa chọn sản phẩm yêu thích và thêm vào giỏ nhé!</p>
                <a href="/products" onclick="toggleCartDrawer(false)" class="inline-block px-5 py-2.5 bg-blue-600 text-white rounded-xl text-xs font-bold hover:bg-blue-700 shadow-sm transition">Khám phá sản phẩm</a>
            </div>
        `;
        if (drawerItemCount) drawerItemCount.innerText = '0 sản phẩm';
        if (subTotalEl) subTotalEl.innerText = '0 đ';
        if (finalTotalEl) finalTotalEl.innerText = '0 đ';
        return;
    }

    if (checkoutBtn) {
        checkoutBtn.classList.remove('pointer-events-none', 'opacity-50');
        checkoutBtn.setAttribute('href', '/checkout');
    }

    container.innerHTML = `
        <div class="flex items-center justify-center py-10">
            <svg class="animate-spin w-6 h-6 text-blue-600" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path></svg>
        </div>
    `;

    try {
        const response = await fetch('/api/cart/details', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(cart)
        });
        const data = await response.json();

        drawerSubTotalValue = data.subTotal || 0;
        const finalTotal = Math.max(0, drawerSubTotalValue - drawerDiscountValue);

        if (drawerItemCount) drawerItemCount.innerText = `${data.totalCount} sản phẩm`;
        if (subTotalEl) subTotalEl.innerText = `${drawerSubTotalValue.toLocaleString('vi-VN')} đ`;
        if (finalTotalEl) finalTotalEl.innerText = `${finalTotal.toLocaleString('vi-VN')} đ`;

        let html = '';
        data.items.forEach(item => {
            html += `
                <div class="pt-3 first:pt-0 flex gap-3 items-center">
                    <img src="${item.thumbnailImage}" alt="${item.productName}" class="w-16 h-16 object-contain rounded-lg border border-slate-200 p-1 shrink-0" onerror="this.src='/assets/images/placeholder.png'" />
                    <div class="flex-1 min-w-0">
                        <a href="/product/${item.productSlug}" class="font-bold text-slate-900 text-xs line-clamp-1 hover:text-blue-600">${item.productName}</a>
                        <p class="text-[11px] text-slate-500 font-medium">${item.variantName}</p>
                        <div class="flex items-center justify-between mt-1">
                            <span class="text-xs font-extrabold text-blue-600">${item.salePrice.toLocaleString('vi-VN')} đ</span>
                            <div class="flex items-center border border-slate-200 rounded-md">
                                <button onclick="changeQuantity(${item.variantId}, -1)" class="px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100 font-bold">-</button>
                                <span class="px-2 text-xs font-bold text-slate-800">${item.quantity}</span>
                                <button onclick="changeQuantity(${item.variantId}, 1)" class="px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100 font-bold">+</button>
                            </div>
                        </div>
                    </div>
                    <button onclick="removeCartItem(${item.variantId})" class="text-slate-300 hover:text-rose-600 p-1" title="Xóa">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                    </button>
                </div>
            `;
        });
        container.innerHTML = html;
    } catch (err) {
        console.error('Lỗi khi tải giỏ hàng:', err);
    }
}

// 7. Thay đổi số lượng mặt hàng (clamp 1..50)
function changeQuantity(variantId, delta) {
    let cart = getStoredCart();
    const idx = cart.findIndex(item => item.variantId === variantId);
    if (idx > -1) {
        cart[idx].quantity += delta;
        if (cart[idx].quantity <= 0) {
            cart.splice(idx, 1);
        } else if (cart[idx].quantity > 50) {
            cart[idx].quantity = 50;
        }
        saveStoredCart(cart);
        renderCartDrawerItems();
    }
}

// 8. Xóa hẳn sản phẩm khỏi giỏ hàng
function removeCartItem(variantId) {
    let cart = getStoredCart();
    cart = cart.filter(item => item.variantId !== variantId);
    saveStoredCart(cart);
    renderCartDrawerItems();
}

// 9. Áp dụng mã giảm giá trong Drawer
async function applyDrawerCoupon() {
    const input = document.getElementById('drawerCouponInput');
    const msgEl = document.getElementById('couponMsg');
    const discountRow = document.getElementById('drawerDiscountRow');
    const discountAmountEl = document.getElementById('drawerDiscountAmount');
    const finalTotalEl = document.getElementById('drawerFinalTotal');

    if (!input || !msgEl) return;
    const code = input.value.trim().toUpperCase();
    if (!code) {
        msgEl.className = 'text-[11px] font-bold text-amber-600';
        msgEl.innerText = 'Vui lòng nhập mã giảm giá';
        msgEl.classList.remove('hidden');
        return;
    }

    try {
        const response = await fetch('/api/cart/apply-coupon', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ couponCode: code, subTotal: drawerSubTotalValue })
        });
        const data = await response.json();
        msgEl.classList.remove('hidden');

        if (data.success) {
            drawerDiscountValue = data.discountAmount || 0;
            sessionStorage.setItem(COUPON_STORAGE_KEY, code);
            msgEl.className = 'text-[11px] font-bold text-emerald-600';
            msgEl.innerText = data.message;
            if (discountRow) discountRow.classList.remove('hidden');
            if (discountAmountEl) discountAmountEl.innerText = `-${drawerDiscountValue.toLocaleString('vi-VN')} đ`;
            if (finalTotalEl) {
                const finalTotal = Math.max(0, drawerSubTotalValue - drawerDiscountValue);
                finalTotalEl.innerText = `${finalTotal.toLocaleString('vi-VN')} đ`;
            }
        } else {
            drawerDiscountValue = 0;
            sessionStorage.removeItem(COUPON_STORAGE_KEY);
            msgEl.className = 'text-[11px] font-bold text-rose-600';
            msgEl.innerText = data.message;
            if (discountRow) discountRow.classList.add('hidden');
            if (finalTotalEl) finalTotalEl.innerText = `${drawerSubTotalValue.toLocaleString('vi-VN')} đ`;
        }
    } catch (err) {
        console.error('Lỗi kiểm tra mã giảm giá:', err);
        msgEl.className = 'text-[11px] font-bold text-rose-600';
        msgEl.innerText = 'Lỗi kết nối kiểm tra mã giảm giá';
        msgEl.classList.remove('hidden');
    }
}

// Khởi chạy khi load trang
document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});