/**
 * pagination.js — Reusable client-side pagination utility
 *
 * Usage:
 *   const pg = new Paginator({
 *     containerId: 'myTable',   // tbody or container id to inject rows
 *     pageSize: 10,             // rows per page (default: 10)
 *     infoId: 'pgInfo',        // optional span to show "1–10 của 25"
 *     wrapId: 'pgWrap',        // id of the .pagination-wrap div
 *   });
 *
 *   pg.setData(allItems, renderFn);
 *   // renderFn(items) should return HTML string for that page's items
 */
class Paginator {
    constructor({ containerId, pageSize = 10, infoId = null, wrapId = null }) {
        this.containerId = containerId;
        this.pageSize    = pageSize;
        this.infoId      = infoId;
        this.wrapId      = wrapId;
        this._data       = [];
        this._renderFn   = null;
        this._page       = 1;
    }

    /** Load new data set and re-render from page 1 */
    setData(data, renderFn) {
        this._data     = data;
        this._renderFn = renderFn;
        this._page     = 1;
        this._render();
    }

    get totalPages() {
        return Math.max(1, Math.ceil(this._data.length / this.pageSize));
    }

    _pageData() {
        const start = (this._page - 1) * this.pageSize;
        return this._data.slice(start, start + this.pageSize);
    }

    _render() {
        const container = document.getElementById(this.containerId);
        if (!container) return;

        // Render rows
        if (this._data.length === 0) {
            container.innerHTML = this._renderFn([]);
        } else {
            container.innerHTML = this._renderFn(this._pageData());
        }

        // Info text
        if (this.infoId) {
            const el = document.getElementById(this.infoId);
            if (el) {
                const start = Math.min((this._page - 1) * this.pageSize + 1, this._data.length);
                const end   = Math.min(this._page * this.pageSize, this._data.length);
                el.textContent = this._data.length
                    ? `${start}–${end} |Tổng:  ${this._data.length}`
                    : '0 mục';
            }
        }

        // Pagination buttons
        if (this.wrapId) {
            const wrap = document.getElementById(this.wrapId);
            if (wrap) this._renderButtons(wrap);
        }
    }

    _renderButtons(wrap) {
        const tp = this.totalPages;
        const cp = this._page;

        let btns = '';

        // Prev
        btns += `<button class="pg-btn" ${cp === 1 ? 'disabled' : ''} data-pg="${cp - 1}" aria-label="Trang trước">
                    <i class="bi bi-chevron-left"></i>
                 </button>`;

        // Page numbers — show max 5 buttons with ellipsis
        const pages = this._pageRange(cp, tp);
        for (const p of pages) {
            if (p === '...') {
                btns += `<button class="pg-btn" disabled>…</button>`;
            } else {
                btns += `<button class="pg-btn ${p === cp ? 'active' : ''}" data-pg="${p}">${p}</button>`;
            }
        }

        // Next
        btns += `<button class="pg-btn" ${cp === tp ? 'disabled' : ''} data-pg="${cp + 1}" aria-label="Trang sau">
                    <i class="bi bi-chevron-right"></i>
                 </button>`;

        // Find or create buttons container inside wrap
        let btnDiv = wrap.querySelector('.pagination-btns');
        if (!btnDiv) { btnDiv = document.createElement('div'); btnDiv.className = 'pagination-btns'; wrap.appendChild(btnDiv); }
        btnDiv.innerHTML = btns;

        // Event delegation
        btnDiv.querySelectorAll('.pg-btn[data-pg]').forEach(b => {
            b.addEventListener('click', () => {
                const target = parseInt(b.getAttribute('data-pg'));
                if (target >= 1 && target <= tp) {
                    this._page = target;
                    this._render();
                }
            });
        });
    }

    _pageRange(current, total) {
        if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
        const pages = [];
        if (current <= 4) {
            pages.push(1, 2, 3, 4, 5, '...', total);
        } else if (current >= total - 3) {
            pages.push(1, '...', total - 4, total - 3, total - 2, total - 1, total);
        } else {
            pages.push(1, '...', current - 1, current, current + 1, '...', total);
        }
        return pages;
    }
}
