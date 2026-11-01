// Quản lý Giỏ hàng LocalStorage & Giao tiếp Web API cho TechStore
const CART_STORAGE_KEY = 'techstore_cart';
const COUPON_STORAGE_KEY = 'techstore_coupon';
const FREE_SHIPPING_THRESHOLD = 1000000; // 1.000.000đ

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

// 3. Thêm mặt hàng vào giỏ
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

    if (window.showToast) {
        showToast('Đã thêm sản phẩm vào giỏ hàng thành công!', 'success');
    }

    if (openDrawerImmediately) {
        toggleCartDrawer(true);
    }
}
window.addToCart = addToCart;

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
        document.body.classList.add('overflow-hidden');
        renderCartDrawerItems();
        loadAvailableCoupons();
    } else {
        drawer.classList.add('translate-x-full');
        backdrop.classList.add('hidden');
        document.body.classList.remove('overflow-hidden');
    }
}
window.toggleCartDrawer = toggleCartDrawer;

let drawerSubTotalValue = 0;
let drawerDiscountValue = 0;

// Cập nhật thanh tiến trình Miễn phí vận chuyển
function updateShippingProgress(subTotal) {
    const progressEl = document.getElementById('shippingProgressBar');
    const textEl = document.getElementById('shippingProgressText');
    const badgeEl = document.getElementById('shippingStatusBadge');
    if (!progressEl || !textEl) return;

    if (subTotal >= FREE_SHIPPING_THRESHOLD) {
        progressEl.style.width = '100%';
        progressEl.className = 'bg-emerald-500 h-2 rounded-full transition-all duration-500';
        textEl.innerHTML = `<span class="text-emerald-700 font-bold flex items-center gap-1.5"><svg class="w-4 h-4 text-emerald-600" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" /></svg> Đơn hàng của bạn đã đủ điều kiện <strong>MIỄN PHÍ VẬN CHUYỂN</strong>!</span>`;
        if (badgeEl) badgeEl.innerText = 'ĐÃ ĐẠT FREE SHIP';
    } else {
        const remaining = FREE_SHIPPING_THRESHOLD - subTotal;
        const pct = Math.min(100, Math.max(5, (subTotal / FREE_SHIPPING_THRESHOLD) * 100));
        progressEl.style.width = `${pct}%`;
        progressEl.className = 'bg-gradient-to-r from-blue-500 to-indigo-600 h-2 rounded-full transition-all duration-500';
        textEl.innerHTML = `<span>Mua thêm <strong class="text-blue-600 font-bold">${remaining.toLocaleString('vi-VN')}đ</strong> để nhận <strong>Freeship</strong></span>`;
        if (badgeEl) badgeEl.innerText = `CÒN THIẾU ${remaining.toLocaleString('vi-VN')}đ`;
    }
}

// 6. Tải danh sách Coupon khả dụng để click 1 chạm
let cachedCoupons = null;
async function loadAvailableCoupons() {
    const container = document.getElementById('drawerAvailableCoupons');
    if (!container) return;

    if (cachedCoupons) {
        renderCoupons(cachedCoupons);
        return;
    }

    try {
        const res = await fetch('/api/cart/available-coupons');
        if (res.ok) {
            cachedCoupons = await res.json();
            renderCoupons(cachedCoupons);
        }
    } catch (e) {
        console.error('Lỗi tải danh sách mã giảm giá', e);
    }
}

function renderCoupons(coupons) {
    const container = document.getElementById('drawerAvailableCoupons');
    if (!container || !Array.isArray(coupons) || coupons.length === 0) return;

    const savedCoupon = sessionStorage.getItem(COUPON_STORAGE_KEY) || '';
    let html = '';
    coupons.forEach(c => {
        const isSelected = savedCoupon.toUpperCase() === c.code.toUpperCase();
        html += `
            <button type="button" onclick="selectCouponCode('${c.code}')" 
                    class="px-2.5 py-1 text-[11px] font-bold rounded-lg border transition-all flex items-center gap-1.5 ${
                        isSelected 
                        ? 'bg-blue-600 text-white border-blue-600 shadow-sm' 
                        : 'bg-white hover:bg-blue-50 text-slate-700 border-slate-200'
                    }">
                <span class="text-amber-500">🏷️</span>
                <span>${c.code}</span>
                <span class="text-[10px] opacity-75 font-normal">(${c.discountText})</span>
            </button>
        `;
    });
    container.innerHTML = html;
}

function selectCouponCode(code) {
    const input = document.getElementById('drawerCouponInput');
    if (input) {
        input.value = code;
        applyDrawerCoupon();
    }
}
window.selectCouponCode = selectCouponCode;

// 7. Gửi danh sách variantId lên Web API `/api/cart/details` và render giao diện
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
        updateShippingProgress(0);
        if (discountRow) discountRow.classList.add('hidden');
        if (checkoutBtn) {
            checkoutBtn.classList.add('pointer-events-none', 'opacity-50');
            checkoutBtn.removeAttribute('href');
        }

        container.innerHTML = `
            <div class="py-16 text-center space-y-3">
                <div class="w-16 h-16 mx-auto bg-slate-100 rounded-2xl flex items-center justify-center text-slate-400">
                    <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" /></svg>
                </div>
                <p class="text-sm font-bold text-slate-800">Giỏ hàng của bạn đang trống</p>
                <p class="text-xs text-slate-400">Hãy lựa chọn sản phẩm yêu thích và thêm vào giỏ nhé!</p>
                <a href="/products" onclick="toggleCartDrawer(false)" class="inline-block px-5 py-2.5 bg-blue-600 text-white rounded-xl text-xs font-bold hover:bg-blue-700 shadow-sm transition">Khám phá sản phẩm ngay</a>
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
        <div class="flex items-center justify-center py-12">
            <svg class="animate-spin w-7 h-7 text-blue-600" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path></svg>
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
        updateShippingProgress(drawerSubTotalValue);

        // Check if there is a saved coupon to re-evaluate
        const savedCoupon = sessionStorage.getItem(COUPON_STORAGE_KEY);
        if (savedCoupon && !drawerDiscountValue) {
            const input = document.getElementById('drawerCouponInput');
            if (input) input.value = savedCoupon;
            await reevaluateSavedCoupon(savedCoupon);
        }

        const finalTotal = Math.max(0, drawerSubTotalValue - drawerDiscountValue);

        if (drawerItemCount) drawerItemCount.innerText = `${data.totalCount} sản phẩm`;
        if (subTotalEl) subTotalEl.innerText = `${drawerSubTotalValue.toLocaleString('vi-VN')} đ`;
        if (finalTotalEl) finalTotalEl.innerText = `${finalTotal.toLocaleString('vi-VN')} đ`;

        let html = '';
        data.items.forEach(item => {
            const maxStock = item.stockQuantity || 50;
            html += `
                <div class="pt-3.5 first:pt-0 flex gap-3 items-center group">
                    <img src="${item.thumbnailImage}" alt="${item.productName}" class="w-16 h-16 object-contain rounded-xl border border-slate-200 p-1 shrink-0 bg-white" onerror="this.src='/assets/images/placeholder.png'" />
                    <div class="flex-1 min-w-0">
                        <a href="/product/${item.productSlug}" class="font-bold text-slate-900 text-xs line-clamp-1 hover:text-blue-600 transition-colors">${item.productName}</a>
                        <p class="text-[11px] text-slate-500 font-medium">${item.variantName}</p>
                        <div class="flex items-center justify-between mt-1.5">
                            <span class="text-xs font-black text-blue-600">${item.salePrice.toLocaleString('vi-VN')} đ</span>
                            <div class="flex items-center border border-slate-200 rounded-lg bg-slate-50 overflow-hidden">
                                <button onclick="changeQuantity(${item.variantId}, -1)" class="px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-200 font-bold transition-colors">-</button>
                                <span class="px-2.5 text-xs font-bold text-slate-800">${item.quantity}</span>
                                <button onclick="changeQuantity(${item.variantId}, 1, ${maxStock})" class="px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-200 font-bold transition-colors">+</button>
                            </div>
                        </div>
                    </div>
                    <button onclick="removeCartItem(${item.variantId})" class="text-slate-300 hover:text-rose-600 p-1.5 rounded-lg transition-colors" title="Xóa khỏi giỏ">
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
window.refreshCartDrawer = renderCartDrawerItems;

// 8. Thay đổi số lượng mặt hàng (clamp 1..maxStock)
function changeQuantity(variantId, delta, maxStock = 50) {
    let cart = getStoredCart();
    const idx = cart.findIndex(item => item.variantId === variantId);
    if (idx > -1) {
        const nextQty = cart[idx].quantity + delta;
        if (nextQty <= 0) {
            cart.splice(idx, 1);
        } else if (nextQty > maxStock) {
            if (window.showToast) showToast(`Kho chỉ còn tối đa ${maxStock} sản phẩm cho phiên bản này`, 'warning');
            return;
        } else {
            cart[idx].quantity = nextQty;
        }
        saveStoredCart(cart);
        renderCartDrawerItems();
    }
}
window.changeQuantity = changeQuantity;

// 9. Xóa hẳn sản phẩm khỏi giỏ hàng
function removeCartItem(variantId) {
    let cart = getStoredCart();
    cart = cart.filter(item => item.variantId !== variantId);
    saveStoredCart(cart);
    renderCartDrawerItems();
}
window.removeCartItem = removeCartItem;

// 10. Áp dụng mã giảm giá trong Drawer
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
            msgEl.innerText = `✓ ${data.message}`;
            if (discountRow) discountRow.classList.remove('hidden');
            if (discountAmountEl) discountAmountEl.innerText = `-${drawerDiscountValue.toLocaleString('vi-VN')} đ`;
            if (finalTotalEl) {
                const finalTotal = Math.max(0, drawerSubTotalValue - drawerDiscountValue);
                finalTotalEl.innerText = `${finalTotal.toLocaleString('vi-VN')} đ`;
            }
            if (cachedCoupons) renderCoupons(cachedCoupons);
        } else {
            drawerDiscountValue = 0;
            sessionStorage.removeItem(COUPON_STORAGE_KEY);
            msgEl.className = 'text-[11px] font-bold text-rose-600';
            msgEl.innerText = `✕ ${data.message}`;
            if (discountRow) discountRow.classList.add('hidden');
            if (finalTotalEl) finalTotalEl.innerText = `${drawerSubTotalValue.toLocaleString('vi-VN')} đ`;
            if (cachedCoupons) renderCoupons(cachedCoupons);
        }
    } catch (err) {
        console.error('Lỗi kiểm tra mã giảm giá:', err);
        msgEl.className = 'text-[11px] font-bold text-rose-600';
        msgEl.innerText = 'Lỗi kết nối kiểm tra mã giảm giá';
        msgEl.classList.remove('hidden');
    }
}
window.applyDrawerCoupon = applyDrawerCoupon;

async function reevaluateSavedCoupon(code) {
    try {
        const response = await fetch('/api/cart/apply-coupon', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ couponCode: code, subTotal: drawerSubTotalValue })
        });
        const data = await response.json();
        const discountRow = document.getElementById('drawerDiscountRow');
        const discountAmountEl = document.getElementById('drawerDiscountAmount');
        const msgEl = document.getElementById('couponMsg');

        if (data.success) {
            drawerDiscountValue = data.discountAmount || 0;
            if (discountRow) discountRow.classList.remove('hidden');
            if (discountAmountEl) discountAmountEl.innerText = `-${drawerDiscountValue.toLocaleString('vi-VN')} đ`;
            if (msgEl) {
                msgEl.className = 'text-[11px] font-bold text-emerald-600';
                msgEl.innerText = `✓ Đã áp dụng mã: ${code}`;
                msgEl.classList.remove('hidden');
            }
        } else {
            sessionStorage.removeItem(COUPON_STORAGE_KEY);
            drawerDiscountValue = 0;
            if (discountRow) discountRow.classList.add('hidden');
        }
    } catch (e) {}
}

function removeDrawerCoupon() {
    sessionStorage.removeItem(COUPON_STORAGE_KEY);
    drawerDiscountValue = 0;
    const input = document.getElementById('drawerCouponInput');
    const msgEl = document.getElementById('couponMsg');
    const discountRow = document.getElementById('drawerDiscountRow');
    const finalTotalEl = document.getElementById('drawerFinalTotal');
    if (input) input.value = '';
    if (msgEl) msgEl.classList.add('hidden');
    if (discountRow) discountRow.classList.add('hidden');
    if (finalTotalEl) finalTotalEl.innerText = `${drawerSubTotalValue.toLocaleString('vi-VN')} đ`;
    if (cachedCoupons) renderCoupons(cachedCoupons);
}
window.removeDrawerCoupon = removeDrawerCoupon;

// Khởi chạy khi load trang
document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});