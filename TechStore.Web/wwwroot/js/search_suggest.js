// Live Autocomplete Search for Header (Desktop & Mobile)
(function () {
    const trendingKeywords = [
        'iPhone 16 Pro Max',
        'MacBook Pro M4',
        'Galaxy S25 Ultra',
        'iPad Pro M4',
        'Sony WH-1000XM5',
        'Apple Watch Ultra 2'
    ];

    function setupSearch(inputId, containerId) {
        const input = document.getElementById(inputId);
        const container = document.getElementById(containerId);
        if (!input || !container) return;

        let debounceTimer = null;
        let activeIndex = -1;

        function showTrending() {
            let html = `
                <div class="p-4">
                    <div class="flex items-center gap-1.5 text-xs font-bold text-slate-500 mb-2.5">
                        <svg class="w-3.5 h-3.5 text-amber-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M12.395 2.553a1 1 0 00-1.45-.385c-.345.23-.614.558-.822.88-.316.492-.474.966-.567 1.402a5.986 5.986 0 00-2.308.878c-.52.336-1.026.79-1.393 1.34A5.96 5.96 0 005 10c0 1.93.92 3.65 2.35 4.75A5.992 5.992 0 0011 16a6 6 0 006-6c0-1.657-.672-3.157-1.757-4.243a8.955 8.955 0 00-2.848-3.204zM10 14a4 4 0 100-8 4 4 0 000 8z" clip-rule="evenodd" /></svg>
                        <span>TÌM KIẾM XU HƯỚNG</span>
                    </div>
                    <div class="flex flex-wrap gap-2">
            `;
            trendingKeywords.forEach(kw => {
                html += `
                    <button type="button" class="trending-tag px-3 py-1.5 bg-slate-100 hover:bg-blue-50 hover:text-blue-600 text-slate-700 text-xs font-semibold rounded-full border border-slate-200 transition-colors">
                        ${escapeHtml(kw)}
                    </button>
                `;
            });
            html += `
                    </div>
                </div>
            `;
            container.innerHTML = html;
            container.classList.remove('hidden');

            // Wire clicks
            container.querySelectorAll('.trending-tag').forEach(tag => {
                tag.addEventListener('click', () => {
                    input.value = tag.innerText.trim();
                    input.focus();
                    fetchSuggestions(input.value.trim());
                });
            });
        }

        input.addEventListener('focus', function () {
            const query = input.value.trim();
            if (query.length < 2) {
                showTrending();
            } else if (container.children.length > 0) {
                container.classList.remove('hidden');
            }
        });

        input.addEventListener('input', function () {
            const query = input.value.trim();
            clearTimeout(debounceTimer);

            if (query.length < 2) {
                showTrending();
                return;
            }

            debounceTimer = setTimeout(() => {
                fetchSuggestions(query);
            }, 250);
        });

        input.addEventListener('keydown', function (e) {
            const items = container.querySelectorAll('.suggestion-item');
            if (!items.length || container.classList.contains('hidden')) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                activeIndex = (activeIndex + 1) % items.length;
                updateActiveItem(items);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                activeIndex = (activeIndex - 1 + items.length) % items.length;
                updateActiveItem(items);
            } else if (e.key === 'Enter') {
                if (activeIndex >= 0 && items[activeIndex]) {
                    e.preventDefault();
                    items[activeIndex].click();
                }
            } else if (e.key === 'Escape') {
                container.classList.add('hidden');
            }
        });

        function updateActiveItem(items) {
            items.forEach((item, idx) => {
                if (idx === activeIndex) {
                    item.classList.add('bg-blue-50');
                    item.scrollIntoView({ block: 'nearest' });
                } else {
                    item.classList.remove('bg-blue-50');
                }
            });
        }

        async function fetchSuggestions(query) {
            try {
                const res = await fetch(`/api/product/suggest?q=${encodeURIComponent(query)}`);
                if (!res.ok) return;
                const data = await res.json();
                renderSuggestions(data, query);
            } catch (err) {
                console.error('Error fetching suggestions:', err);
            }
        }

        function renderSuggestions(products, query) {
            activeIndex = -1;
            if (!products || products.length === 0) {
                container.innerHTML = `
                    <div class="p-5 text-center text-slate-500 text-xs">
                        <p class="font-bold text-slate-700">Không tìm thấy sản phẩm nào khớp với "${escapeHtml(query)}"</p>
                        <p class="text-[11px] text-slate-400 mt-1">Thử tìm kiếm với từ khóa khác như iPhone, MacBook, Galaxy...</p>
                    </div>
                `;
                container.classList.remove('hidden');
                return;
            }

            let html = '<div class="py-1">';
            products.forEach(p => {
                const img = p.image ? p.image : '/assets/images/placeholder.png';
                html += `
                    <a href="/product/${p.slug}" class="suggestion-item flex items-center gap-3 px-4 py-2.5 hover:bg-slate-50 transition-colors">
                        <div class="w-11 h-11 rounded-lg bg-slate-100 border border-slate-200 overflow-hidden shrink-0 flex items-center justify-center">
                            <img src="${img}" alt="${escapeHtml(p.name)}" class="w-full h-full object-contain p-1" onerror="this.src='/assets/images/placeholder.png'">
                        </div>
                        <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 mb-0.5">
                                <span class="text-[10px] font-bold uppercase tracking-wider text-blue-600 bg-blue-50 px-1.5 py-0.5 rounded">${escapeHtml(p.brand || '')}</span>
                                <span class="text-[10px] text-slate-400 truncate">${escapeHtml(p.category || '')}</span>
                            </div>
                            <h4 class="text-xs font-semibold text-slate-900 truncate">${highlightKeyword(p.name, query)}</h4>
                            <div class="text-xs font-bold text-red-600 mt-0.5">${p.formattedPrice || 'Liên hệ'}</div>
                        </div>
                        <svg class="w-4 h-4 text-slate-300 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
                    </a>
                `;
            });
            html += '</div>';

            html += `
                <a href="/products?q=${encodeURIComponent(query)}" class="block bg-slate-50 hover:bg-blue-50 px-4 py-2.5 text-center text-xs font-bold text-blue-600 border-t border-slate-100 transition-colors">
                    Xem tất cả kết quả cho "${escapeHtml(query)}" &rarr;
                </a>
            `;

            container.innerHTML = html;
            container.classList.remove('hidden');
        }

        // Close when clicking outside
        document.addEventListener('click', function (e) {
            if (!input.contains(e.target) && !container.contains(e.target)) {
                container.classList.add('hidden');
            }
        });
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function highlightKeyword(text, keyword) {
        if (!text || !keyword) return escapeHtml(text);
        const escaped = escapeHtml(text);
        const escapedKw = escapeHtml(keyword);
        const regex = new RegExp(`(${escapedKw.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return escaped.replace(regex, '<span class="text-blue-600 font-bold bg-blue-50">$1</span>');
    }

    // Initialize both Desktop and Mobile
    document.addEventListener('DOMContentLoaded', () => {
        setupSearch('headerSearchInput', 'headerSearchSuggestions');
        setupSearch('mobileSearchInput', 'mobileSearchSuggestions');
    });
})();
