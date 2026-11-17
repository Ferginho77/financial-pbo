(function () {
    'use strict';

    var fmt = new Intl.NumberFormat('id-ID');

    function rupiah(n) {
        return 'Rp ' + fmt.format(Math.round(n));
    }

    function digitsOnly(value) {
        return (value || '').replace(/\D/g, '');
    }

    function formatRupiahInput(input) {
        var digits = digitsOnly(input.value).replace(/^0+(?=\d)/, '');
        input.value = digits ? fmt.format(parseInt(digits, 10)) : '';
    }

    function populateEditTxDialog(dlg, trigger) {
        var id = trigger.getAttribute('data-tx-id') || '';
        var type = trigger.getAttribute('data-tx-type') || '2';
        var title = trigger.getAttribute('data-tx-title') || '';
        var amount = trigger.getAttribute('data-tx-amount') || '';
        var date = trigger.getAttribute('data-tx-date') || '';
        var note = trigger.getAttribute('data-tx-note') || '';
        var catId = trigger.getAttribute('data-tx-category-id') || '';
        var catLabel = trigger.getAttribute('data-tx-category-label') || '';

        dlg.querySelector('[name="EditTransaction.TransactionId"]').value = id;
        dlg.querySelector('[name="EditTransaction.Type"]').value = type;
        dlg.querySelector('[name="EditTransaction.Title"]').value = title;
        dlg.querySelector('[name="EditTransaction.Date"]').value = date;
        dlg.querySelector('[name="EditTransaction.Note"]').value = note;

        var amountInput = dlg.querySelector('[name="EditTransaction.Amount"]');
        amountInput.value = amount;
        formatRupiahInput(amountInput);

        var catWrap = dlg.querySelector('[data-edit-category-wrap]');
        var catSelect = dlg.querySelector('[name="EditTransaction.CategoryId"]');
        if (catWrap && catSelect) {
            var isExpense = type === '2';
            catWrap.style.display = isExpense ? '' : 'none';

            // Hapus opsi sementara sebelumnya jika ada.
            catSelect.querySelectorAll('option[data-temp-cat]').forEach(function (o) { o.remove(); });

            if (isExpense && catId) {
                var exists = Array.prototype.some.call(catSelect.options, function (o) { return o.value === catId; });
                if (!exists && catLabel) {
                    var opt = document.createElement('option');
                    opt.value = catId;
                    opt.textContent = catLabel + ' (Alokasi sebelumnya)';
                    opt.setAttribute('data-temp-cat', '1');
                    catSelect.appendChild(opt);
                }
                catSelect.value = catId;
            } else {
                catSelect.value = '';
            }
        }
    }

    // ---------------- Dialog ----------------
    function openDialog(id, trigger) {
        var dlg = document.getElementById(id);
        if (!dlg || typeof dlg.showModal !== 'function') { return; }

        if (trigger) {
            if (id === 'dlgEditTx') {
                populateEditTxDialog(dlg, trigger);
            } else {
                // data-fill-xxx="nilai" mengisi elemen [name="xxx"] di dalam dialog.
                Array.prototype.forEach.call(trigger.attributes, function (attr) {
                    if (attr.name.indexOf('data-fill-') !== 0) { return; }
                    var key = attr.name.substring('data-fill-'.length);
                    var targets = dlg.querySelectorAll('[name="' + key + '"], [name="' + key.replace(/-([a-z])/g, function (m, c) { return c.toUpperCase(); }) + '"]');
                    Array.prototype.forEach.call(targets, function (el) { el.value = attr.value; });
                });
            }
        }

        if (!dlg.open) { dlg.showModal(); }
        var first = dlg.querySelector('input:not([type=hidden]), select, textarea');
        if (first) { setTimeout(function () { first.focus(); if (first.select) { first.select(); } }, 30); }
        dlg.dispatchEvent(new Event('dialog-opened'));
    }

    document.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-open-dialog]');
        if (opener) {
            e.preventDefault();
            openDialog(opener.getAttribute('data-open-dialog'), opener);
            return;
        }

        var closer = e.target.closest('[data-close-dialog]');
        if (closer) {
            var dlg = closer.closest('dialog');
            if (dlg) { dlg.close(); }
            return;
        }

        var toastClose = e.target.closest('[data-toast-close]');
        if (toastClose) {
            var toast = toastClose.closest('[data-toast]');
            if (toast) { toast.remove(); }
            return;
        }

        // Klik pada backdrop menutup dialog.
        if (e.target.tagName === 'DIALOG' && e.target.open) {
            var rect = e.target.getBoundingClientRect();
            var inside = e.clientX >= rect.left && e.clientX <= rect.right && e.clientY >= rect.top && e.clientY <= rect.bottom;
            if (!inside) { e.target.close(); }
        }
    });

    // Dialog yang harus terbuka otomatis (mis. setelah validasi server gagal).
    document.querySelectorAll('dialog[data-auto-open="true"]').forEach(function (dlg) {
        openDialog(dlg.id, null);
    });

    // ---------------- Konfirmasi form ----------------
    document.addEventListener('submit', function (e) {
        var form = e.target;
        var message = form.getAttribute && form.getAttribute('data-confirm');
        if (message && !window.confirm(message)) {
            e.preventDefault();
            return;
        }

        // Kirim angka murni (tanpa titik ribuan) ke server.
        form.querySelectorAll('input[data-rupiah]').forEach(function (input) {
            input.value = digitsOnly(input.value);
        });
    });

    // ---------------- Auto-submit select & input ----------------
    document.querySelectorAll('select[data-autosubmit], input[data-autosubmit]').forEach(function (el) {
        el.addEventListener('change', function () {
            if (el.form) {
                if (typeof el.form.requestSubmit === 'function') { el.form.requestSubmit(); }
                else { el.form.submit(); }
            }
        });
    });

    // ---------------- Input Rupiah ----------------
    function updateBalanceHint(input) {
        var balanceAttr = input.getAttribute('data-balance');
        if (balanceAttr === null) { return; }
        var field = input.closest('.field');
        var hint = field ? field.querySelector('[data-balance-hint]') : null;
        if (!hint) { return; }

        var balance = parseInt(balanceAttr, 10) || 0;
        var amount = parseInt(digitsOnly(input.value) || '0', 10);
        hint.classList.remove('is-error', 'is-ok');

        if (amount <= 0) {
            hint.textContent = '';
            return;
        }

        if (amount > balance) {
            hint.textContent = 'Melebihi sisa saldo (' + rupiah(balance) + ').';
            hint.classList.add('is-error');
        } else {
            hint.textContent = 'Sisa saldo setelah transaksi: ' + rupiah(balance - amount);
            hint.classList.add('is-ok');
        }
    }

    document.querySelectorAll('input[data-rupiah]').forEach(function (input) {
        formatRupiahInput(input);
        updateBalanceHint(input);
        input.addEventListener('input', function () {
            formatRupiahInput(input);
            updateBalanceHint(input);
        });
    });

    // ---------------- Toast ----------------
    document.querySelectorAll('[data-toast]').forEach(function (toast) {
        setTimeout(function () {
            toast.classList.add('is-leaving');
            setTimeout(function () { toast.remove(); }, 400);
        }, 6000);
    });
})();
