window.mafaliScroll = {
    attach: function (element, dotNetRef) {
        if (!element) return;
        const handler = () => {
            const nearBottom = element.scrollTop + element.clientHeight >= element.scrollHeight - 400;
            if (nearBottom) {
                dotNetRef.invokeMethodAsync('OnScrolledNearBottom');
            }
        };
        element.addEventListener('scroll', handler, { passive: true });
        element._mafaliScrollHandler = handler;
    },
    detach: function (element) {
        if (element && element._mafaliScrollHandler) {
            element.removeEventListener('scroll', element._mafaliScrollHandler);
            delete element._mafaliScrollHandler;
        }
    },
    toTop: function (element) {
        if (element) element.scrollTop = 0;
    }
};

window.mafaliFileDownload = {
    downloadFileFromStream: async function (fileName, contentType, streamRef) {
        const arrayBuffer = await streamRef.arrayBuffer();
        const blob = new Blob([arrayBuffer], { type: contentType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    }
};

window.mafaliColumnResize = {
    attach: function (headerRow, dotNetRef, resizeMethod, autoFitMethod) {
        if (!headerRow) return;
        resizeMethod = resizeMethod || 'OnColumnResized';
        autoFitMethod = autoFitMethod || 'GetColumnAutoFitData';

        let drag = null;

        const widthAt = (clientX) => Math.max(40, Math.round(drag.startWidth + (clientX - drag.startX)));

        const onMouseDown = (e) => {
            const handle = e.target.closest('.col-resize-handle');
            if (!handle) return;
            e.preventDefault();
            e.stopPropagation();

            const gridHeight = headerRow.closest('.table-wrap')?.getBoundingClientRect().height
                ?? headerRow.getBoundingClientRect().height;

            const ghost = document.createElement('div');
            ghost.className = 'col-resize-ghost';
            ghost.style.left = e.clientX + 'px';
            ghost.style.top = headerRow.getBoundingClientRect().top + 'px';
            ghost.style.height = gridHeight + 'px';
            document.body.appendChild(ghost);

            drag = {
                key: handle.dataset.colKey,
                startX: e.clientX,
                startWidth: handle.closest('.cell').getBoundingClientRect().width,
                ghost: ghost,
            };
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        };

        const onMouseMove = (e) => {
            if (!drag) return;
            const newWidth = widthAt(e.clientX);
            drag.ghost.style.left = (drag.startX + (newWidth - drag.startWidth)) + 'px';
        };

        const onMouseUp = (e) => {
            if (!drag) return;
            const newWidth = widthAt(e.clientX);
            document.body.removeChild(drag.ghost);
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            const key = drag.key;
            drag = null;
            dotNetRef.invokeMethodAsync(resizeMethod, key, newWidth);
        };

        const measureAutoFitWidth = (header, values, sampleCell, sampleHeaderCell) => {
            const canvas = measureAutoFitWidth._canvas || (measureAutoFitWidth._canvas = document.createElement('canvas'));
            const ctx = canvas.getContext('2d');

            const cellStyle = getComputedStyle(sampleCell);
            const headerStyle = getComputedStyle(sampleHeaderCell);
            const hPad = parseFloat(cellStyle.paddingLeft) + parseFloat(cellStyle.paddingRight);

            ctx.font = cellStyle.font;
            let maxContentWidth = 0;
            for (const v of values) {
                const w = ctx.measureText(v ?? '').width;
                if (w > maxContentWidth) maxContentWidth = w;
            }

            ctx.font = headerStyle.font;
            const headerWidth = ctx.measureText((header ?? '').toUpperCase()).width;

            return Math.max(40, Math.ceil(Math.max(maxContentWidth, headerWidth) + hPad + 4));
        };

        const onDblClick = async (e) => {
            const handle = e.target.closest('.col-resize-handle');
            if (!handle) return;
            e.preventDefault();
            e.stopPropagation();

            const key = handle.dataset.colKey;
            const headerCell = handle.closest('.cell');
            const sampleBodyCell = headerRow.parentElement?.querySelector('.row .cell') || headerCell;

            const data = await dotNetRef.invokeMethodAsync(autoFitMethod, key);
            const newWidth = measureAutoFitWidth(data.header, data.values, sampleBodyCell, headerCell);
            dotNetRef.invokeMethodAsync(resizeMethod, key, newWidth);
        };

        headerRow.addEventListener('mousedown', onMouseDown);
        headerRow.addEventListener('dblclick', onDblClick);
        headerRow._mafaliResizeMouseDownHandler = onMouseDown;
        headerRow._mafaliResizeDblClickHandler = onDblClick;
    },
    detach: function (headerRow) {
        if (!headerRow) return;
        if (headerRow._mafaliResizeMouseDownHandler) {
            headerRow.removeEventListener('mousedown', headerRow._mafaliResizeMouseDownHandler);
            delete headerRow._mafaliResizeMouseDownHandler;
        }
        if (headerRow._mafaliResizeDblClickHandler) {
            headerRow.removeEventListener('dblclick', headerRow._mafaliResizeDblClickHandler);
            delete headerRow._mafaliResizeDblClickHandler;
        }
    }
};

window.mafaliRowResize = {
    _inited: false,
    init: function () {
        if (window.mafaliRowResize._inited) return;
        window.mafaliRowResize._inited = true;

        const TOPBAR_HEIGHT = 64;
        const MIN_PANEL_HEIGHT = 120;
        const HANDLE_CLEARANCE = 6;
        let nextGridId = 0;

        const overages = new Map();
        let scrollSpacer = null;
        let scrollPage = null;

        const PAGE_ROOT_SELECTOR = '.page, .pc-page, .rc-page';

        function updateScrollSpacer(pageEl) {
            if (!scrollPage) {
                scrollPage = pageEl;
                if (getComputedStyle(scrollPage).position === 'static') {
                    scrollPage.style.position = 'relative';
                }
            }
            if (!scrollSpacer) {
                scrollSpacer = document.createElement('div');
                scrollSpacer.className = 'resize-scroll-spacer';
                scrollPage.appendChild(scrollSpacer);
            }
            let total = 0;
            overages.forEach((v) => { total += v; });
            scrollSpacer.style.height = total + 'px';
            scrollPage.style.overflowY = total > 0 ? 'auto' : '';
        }

        function splitterWrapper(tableWrap) {
            const parent = tableWrap.parentElement;
            return parent && parent.classList.contains('grid-group') ? parent : null;
        }

        function splitterPanel(wrapper) {
            const formPanel = wrapper.parentElement.querySelector('.pc-body-wrap, .rc-body');
            if (formPanel) return formPanel;
            let el = wrapper.previousElementSibling;
            while (el) {
                if (el.classList.contains('grid-group')) return el;
                el = el.previousElementSibling;
            }
            return null;
        }

        function splitterSides(wrapper) {
            const panel = splitterPanel(wrapper);
            if (!panel) return null;
            return parseFloat(getComputedStyle(wrapper).flexGrow) === 0
                ? { resized: wrapper, sign: 1 }
                : { resized: panel, sign: -1 };
        }

        function collectGroup(tableWrap) {
            const wrapper = splitterWrapper(tableWrap);
            if (wrapper) return [wrapper];
            const group = [tableWrap];
            let el = tableWrap.previousElementSibling;
            while (el) {
                if (el.classList.contains('table-wrap-resize-handle') || el.classList.contains('table-wrap-placeholder')) {
                    el = el.previousElementSibling;
                    continue;
                }
                if (el.classList.contains('table-wrap')) break;
                group.unshift(el);
                if (el.classList.contains('grid-group-start')) break;
                el = el.previousElementSibling;
            }
            return group;
        }

        const ensureHandles = () => {
            document.querySelectorAll('.table-wrap-resize-handle, .table-wrap-placeholder').forEach((el) => {
                const id = el.dataset.gridId;
                if (!id || !document.querySelector('.table-wrap[data-grid-id="' + id + '"]')) {
                    el.remove();
                }
            });
            overages.forEach((_, id) => {
                if (!document.querySelector('.table-wrap[data-grid-id="' + id + '"]')) {
                    overages.delete(id);
                }
            });
            if (scrollPage && !document.body.contains(scrollPage)) {
                overages.clear();
                scrollSpacer = null;
                scrollPage = null;
            }
            document.querySelectorAll('.table-wrap').forEach((tableWrap) => {
                if (tableWrap.dataset.gridId) return;
                const id = 'g' + (nextGridId++);
                tableWrap.dataset.gridId = id;

                const group = collectGroup(tableWrap);

                const handle = document.createElement('div');
                handle.className = 'table-wrap-resize-handle';
                handle.dataset.gridId = id;
                group[0].parentElement.insertBefore(handle, group[0]);

                const wrapper = splitterWrapper(tableWrap);
                if (wrapper && wrapper.classList.contains('show-grip') && !wrapper.querySelector(':scope > .grid-grip')) {
                    const grip = document.createElement('span');
                    grip.className = 'grid-grip';
                    grip.setAttribute('aria-hidden', 'true');
                    wrapper.insertBefore(grip, wrapper.firstChild);
                }

                if (wrapper && splitterPanel(wrapper)) return;
                const flowEl = wrapper || tableWrap;

                const placeholder = document.createElement('div');
                placeholder.className = 'table-wrap-placeholder';
                placeholder.dataset.gridId = id;
                const computed = getComputedStyle(flowEl);
                placeholder.style.flex = computed.flex;
                if (parseFloat(computed.flexGrow) === 0) {
                    placeholder.style.height = flowEl.getBoundingClientRect().height + 'px';
                }
                flowEl.parentElement.insertBefore(placeholder, flowEl);
            });
        };

        let scheduled = false;
        const scheduleEnsure = () => {
            if (scheduled) return;
            scheduled = true;
            requestAnimationFrame(() => {
                scheduled = false;
                ensureHandles();
            });
        };

        ensureHandles();
        new MutationObserver(scheduleEnsure).observe(document.body, { childList: true, subtree: true });

        let drag = null;

        document.addEventListener('mousedown', (e) => {
            const handle = e.target.closest('.table-wrap-resize-handle');
            if (!handle) return;
            const id = handle.dataset.gridId;
            const target = document.querySelector('.table-wrap[data-grid-id="' + id + '"]');
            if (!target) return;

            const wrapper = splitterWrapper(target);
            const split = wrapper ? splitterSides(wrapper) : null;
            if (split) {
                e.preventDefault();
                handle.classList.add('dragging');
                const { resized, sign } = split;
                if (!resized.dataset.naturalBasis) {
                    resized.dataset.naturalBasis = resized.getBoundingClientRect().height;
                }
                const natural = parseFloat(resized.dataset.naturalBasis);
                const startBasis = resized.getBoundingClientRect().height;
                const rcs = getComputedStyle(resized);
                const boxDelta = rcs.boxSizing === 'border-box' ? 0 : (
                    parseFloat(rcs.paddingTop) + parseFloat(rcs.paddingBottom) +
                    parseFloat(rcs.borderTopWidth) + parseFloat(rcs.borderBottomWidth)
                );
                const panel = splitterPanel(wrapper);
                const slack = panel ? Math.max(0, panel.getBoundingClientRect().height - MIN_PANEL_HEIGHT) : 0;
                drag = {
                    mode: 'splitter',
                    handle,
                    resized,
                    sign,
                    boxDelta,
                    startY: e.clientY,
                    startBasis,
                    minBasis: sign > 0 ? natural : Math.max(MIN_PANEL_HEIGHT, startBasis - slack),
                    maxBasis: sign > 0 ? startBasis + slack : natural,
                };
                document.addEventListener('mousemove', onMouseMove);
                document.addEventListener('mouseup', onMouseUp);
                return;
            }

            const placeholder = document.querySelector('.table-wrap-placeholder[data-grid-id="' + id + '"]');
            if (!placeholder) return;

            if (wrapper) {
                e.preventDefault();
                handle.classList.add('dragging');
                const standardRect = wrapper.dataset.expanded === '1'
                    ? placeholder.getBoundingClientRect()
                    : wrapper.getBoundingClientRect();
                drag = {
                    mode: 'float',
                    handle,
                    wrapper,
                    placeholder,
                    gridId: id,
                    startY: e.clientY,
                    standard: {
                        top: standardRect.top, bottom: standardRect.bottom,
                        left: standardRect.left, width: standardRect.width,
                    },
                };
                document.addEventListener('mousemove', onMouseMove);
                document.addEventListener('mouseup', onMouseUp);
                return;
            }

            e.preventDefault();
            handle.classList.add('dragging');

            const group = collectGroup(target);
            const topEl = group[0];
            const topRect = topEl.getBoundingClientRect();
            const standardTargetRect = target.dataset.expanded === '1' ? placeholder.getBoundingClientRect() : target.getBoundingClientRect();

            drag = {
                handle,
                placeholder,
                target,
                group,
                startY: e.clientY,
                groupTop: topRect.top,
                standardTarget: { top: standardTargetRect.top, bottom: standardTargetRect.bottom, left: standardTargetRect.left, width: standardTargetRect.width },
            };
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });

        function positionHandle(handle, standardTarget, top) {
            const handleHeight = handle.getBoundingClientRect().height || 8;
            handle.style.position = 'fixed';
            handle.style.left = standardTarget.left + 'px';
            handle.style.width = standardTarget.width + 'px';
            handle.style.top = (top - handleHeight / 2) + 'px';
            handle.style.margin = '0';
            handle.style.zIndex = '501';
        }

        function resetFloat(wrapper, placeholder, handle) {
            ['position', 'left', 'width', 'top', 'height', 'zIndex'].forEach((p) => { wrapper.style[p] = ''; });
            wrapper.dataset.expanded = '0';
            placeholder.style.display = 'none';
            resetHandle(handle);
        }

        function resetHandle(handle) {
            handle.style.position = '';
            handle.style.left = '';
            handle.style.width = '';
            handle.style.top = '';
            handle.style.margin = '';
            handle.style.zIndex = '';
        }

        function onMouseMove(e) {
            if (!drag) return;
            if (drag.mode === 'splitter') {
                const { resized, sign, boxDelta, startY, startBasis, minBasis, maxBasis } = drag;
                const basis = Math.min(maxBasis, Math.max(minBasis, startBasis + sign * (startY - e.clientY)));
                resized.style.flex = '0 1 ' + (basis - boxDelta) + 'px';
                return;
            }
            if (drag.mode === 'float') {
                const { wrapper, placeholder, handle, gridId, startY, standard } = drag;
                const ceiling = TOPBAR_HEIGHT + HANDLE_CLEARANCE;
                const newTop = Math.max(ceiling, Math.min(standard.top, standard.top - (startY - e.clientY)));
                const offset = newTop - standard.top; // <= 0
                if (offset < -0.5) {
                    wrapper.style.position = 'fixed';
                    wrapper.style.left = standard.left + 'px';
                    wrapper.style.width = standard.width + 'px';
                    wrapper.style.top = newTop + 'px';
                    wrapper.style.height = (standard.bottom - newTop) + 'px';
                    wrapper.style.zIndex = '500';
                    wrapper.dataset.expanded = '1';
                    wrapper.dataset.standardTop = standard.top;
                    placeholder.style.display = 'block';
                    positionHandle(handle, standard, newTop);
                    overages.set(gridId, -offset);
                } else {
                    resetFloat(wrapper, placeholder, handle);
                    overages.delete(gridId);
                }
                updateScrollSpacer(wrapper.closest(PAGE_ROOT_SELECTOR) || document.querySelector('.app-body'));
                return;
            }
            const { target, placeholder, handle, group, groupTop, standardTarget } = drag;
            const delta = drag.startY - e.clientY;
            let newGroupTop = groupTop - delta;
            newGroupTop = Math.max(TOPBAR_HEIGHT, Math.min(groupTop, newGroupTop));
            const offset = newGroupTop - groupTop; // <= 0

            if (offset < -0.5) {
                group.forEach((el) => {
                    if (el === target) return;
                    el.style.transform = 'translateY(' + offset + 'px)';
                });
                const newTop = standardTarget.top + offset;
                target.style.position = 'fixed';
                target.style.left = standardTarget.left + 'px';
                target.style.width = standardTarget.width + 'px';
                target.style.top = newTop + 'px';
                target.style.height = (standardTarget.bottom - newTop) + 'px';
                target.style.zIndex = '500';
                target.dataset.expanded = '1';
                target.dataset.standardTop = standardTarget.top;
                placeholder.style.display = 'block';
                positionHandle(handle, standardTarget, newGroupTop);
                overages.set(target.dataset.gridId, -offset);
                updateScrollSpacer(target.closest(PAGE_ROOT_SELECTOR) || document.querySelector('.app-body'));
            } else {
                group.forEach((el) => {
                    if (el === target) return;
                    el.style.transform = '';
                });
                target.style.position = '';
                target.style.left = '';
                target.style.width = '';
                target.style.top = '';
                target.style.height = '';
                target.style.zIndex = '';
                target.dataset.expanded = '0';
                placeholder.style.display = 'none';
                resetHandle(handle);
                overages.delete(target.dataset.gridId);
                updateScrollSpacer(target.closest(PAGE_ROOT_SELECTOR) || document.querySelector('.app-body'));
            }
        }

        document.addEventListener('dblclick', (e) => {
            const handle = e.target.closest('.table-wrap-resize-handle');
            if (!handle) return;
            const target = document.querySelector('.table-wrap[data-grid-id="' + handle.dataset.gridId + '"]');
            if (!target) return;
            const wrapper = splitterWrapper(target);
            if (!wrapper) return;
            e.preventDefault();
            const split = splitterSides(wrapper);
            if (split) {
                split.resized.style.flex = '';
                return;
            }
            const placeholder = document.querySelector('.table-wrap-placeholder[data-grid-id="' + handle.dataset.gridId + '"]');
            if (!placeholder) return;
            resetFloat(wrapper, placeholder, handle);
            overages.delete(handle.dataset.gridId);
            updateScrollSpacer(wrapper.closest(PAGE_ROOT_SELECTOR) || document.querySelector('.app-body'));
        });

        function onMouseUp() {
            if (!drag) return;
            drag.handle.classList.remove('dragging');
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            drag = null;
        }

        function findScrollable(el, wantX, wantY) {
            let node = el;
            while (node && node !== document.documentElement) {
                const style = getComputedStyle(node);
                const canY = wantY && (style.overflowY === 'auto' || style.overflowY === 'scroll') && node.scrollHeight > node.clientHeight;
                const canX = wantX && (style.overflowX === 'auto' || style.overflowX === 'scroll') && node.scrollWidth > node.clientWidth;
                if (canY || canX) return node;
                node = node.parentElement;
            }
            return null;
        }

        document.addEventListener('wheel', (e) => {
            const overGrid = e.target.closest('.grid-group[data-expanded="1"], .table-wrap[data-expanded="1"]');
            if (!overGrid) return;
            const standardTop = parseFloat(overGrid.dataset.standardTop);
            if (e.clientY >= standardTop) return;

            overGrid.style.pointerEvents = 'none';
            const under = document.elementFromPoint(e.clientX, e.clientY);
            overGrid.style.pointerEvents = '';
            if (!under) return;

            const scrollable = findScrollable(under, e.deltaX !== 0, e.deltaY !== 0);
            if (!scrollable) return;

            e.preventDefault();
            scrollable.scrollTop += e.deltaY;
            scrollable.scrollLeft += e.deltaX;
        }, { passive: false });
    }
};
window.mafaliRowResize.init();

window.mafaliBonCommandeDestination = {
    isSupported: function () {
        return 'showDirectoryPicker' in window;
    },

    pickFolder: async function () {
        let handle;
        try {
            handle = await window.showDirectoryPicker({ mode: 'readwrite' });
        } catch (e) {
            if (e.name === 'AbortError') return null;
            throw e;
        }
        await this._storeHandle(handle);
        return handle.name;
    },

    getCurrentFolderName: async function () {
        const handle = await this._getStoredHandle();
        return handle ? handle.name : null;
    },

    writeFile: async function (fileName, streamRef) {
        const handle = await this._getStoredHandle();
        if (!handle) return 'no-folder';

        let permission;
        try {
            permission = await handle.requestPermission({ mode: 'readwrite' });
        } catch (e) {
            return 'permission-error';
        }
        if (permission !== 'granted') return 'permission-denied';

        try {
            const arrayBuffer = await streamRef.arrayBuffer();
            const fileHandle = await handle.getFileHandle(fileName, { create: true });
            const writable = await fileHandle.createWritable();
            await writable.write(arrayBuffer);
            await writable.close();
            return 'ok';
        } catch (e) {
            return 'write-error';
        }
    },

    _dbPromise: null,
    _openDb: function () {
        if (this._dbPromise) return this._dbPromise;
        this._dbPromise = new Promise((resolve, reject) => {
            const req = indexedDB.open('mafali-bon-commande', 1);
            req.onupgradeneeded = () => req.result.createObjectStore('handles');
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
        return this._dbPromise;
    },
    _storeHandle: async function (handle) {
        const db = await this._openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('handles', 'readwrite');
            tx.objectStore('handles').put(handle, 'destinationFolder');
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
        });
    },
    _getStoredHandle: async function () {
        const db = await this._openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('handles', 'readonly');
            const req = tx.objectStore('handles').get('destinationFolder');
            req.onsuccess = () => resolve(req.result || null);
            req.onerror = () => reject(req.error);
        });
    }
};

window.mafaliClock = {
    read: function () {
        var zone = null;
        try {
            zone = Intl.DateTimeFormat().resolvedOptions().timeZone || null;
        } catch (e) {
            zone = null;
        }
        return {
            timeZoneId: zone,
            offsetMinutes: -new Date().getTimezoneOffset()
        };
    }
};
