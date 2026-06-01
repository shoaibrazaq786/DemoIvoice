/* Pak Accounting ERP - JavaScript Utilities */

const PakERP = {
    currencySymbol: 'Rs.',

    formatCurrency: function (amount) {
        return this.currencySymbol + ' ' + parseFloat(amount || 0).toLocaleString('en-PK', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    },

    showToast: function (message, type = 'success') {
        const toast = $(`<div class="alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed top-0 end-0 m-3" style="z-index:9999">${message}</div>`);
        $('body').append(toast);
        setTimeout(() => toast.fadeOut(() => toast.remove()), 3000);
    },

    ajaxPost: function (url, data, onSuccess, onError) {
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    if (onSuccess) onSuccess(response);
                    else PakERP.showToast(response.message || 'Success');
                } else {
                    if (onError) onError(response);
                    else PakERP.showToast(response.message || 'Error', 'error');
                }
            },
            error: function (xhr) {
                const msg = xhr.responseJSON?.message || xhr.responseJSON?.error || 'Request failed';
                if (onError) onError({ message: msg });
                else PakERP.showToast(msg, 'error');
            }
        });
    },

    initDataTable: function (selector, options = {}) {
        const defaults = {
            responsive: true,
            pageLength: 25,
            order: [[0, 'desc']],
            language: {
                search: 'Search:',
                lengthMenu: 'Show _MENU_ entries',
                info: 'Showing _START_ to _END_ of _TOTAL_ entries'
            }
        };
        return $(selector).DataTable({ ...defaults, ...options });
    },

    confirmDelete: function (url, id, table) {
        if (!confirm('Are you sure you want to delete this record?')) return;
        $.post(url, { id: id, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() })
            .done(function (response) {
                if (response.success) {
                    PakERP.showToast('Deleted successfully');
                    if (table) table.row($(`.delete-btn[data-id="${id}"]`).closest('tr')).remove().draw();
                    else location.reload();
                } else {
                    PakERP.showToast(response.message, 'error');
                }
            });
    }
};

// Sidebar toggle for mobile
$(document).ready(function () {
    $('#sidebarToggle').on('click', function () {
        $('.sidebar').toggleClass('show');
    });

    // Highlight active nav
    const path = window.location.pathname.toLowerCase();
    $('.sidebar-nav .nav-link').each(function () {
        const href = $(this).attr('href')?.toLowerCase();
        if (href && path.startsWith(href) && href !== '/') {
            $(this).addClass('active');
        }
    });

    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();
});

// Invoice calculation engine
const InvoiceCalc = {
    calculateLine: function (row) {
        const qty = parseFloat($(row).find('.qty').val()) || 0;
        const price = parseFloat($(row).find('.price').val()) || 0;
        const taxRate = parseFloat($(row).find('.tax-rate').val()) || 0;
        const discount = parseFloat($(row).find('.discount').val()) || 0;

        const subtotal = qty * price;
        const taxAmount = Math.round(subtotal * taxRate / 100 * 100) / 100;
        const lineTotal = Math.round((subtotal - discount + taxAmount) * 100) / 100;

        $(row).find('.tax-amount').val(taxAmount.toFixed(2));
        $(row).find('.line-total').val(lineTotal.toFixed(2));
        return { subtotal, taxAmount, discount, lineTotal };
    },

    calculateTotals: function () {
        let subTotal = 0, taxAmount = 0, discountAmount = 0, lineTotal = 0;

        $('#invoiceLines tbody tr').each(function () {
            const calc = InvoiceCalc.calculateLine(this);
            subTotal += calc.subtotal;
            taxAmount += calc.taxAmount;
            discountAmount += calc.discount;
            lineTotal += calc.lineTotal;
        });

        const furtherTax = parseFloat($('#FurtherTax').val()) || 0;
        const fed = parseFloat($('#FED').val()) || 0;
        const extraTax = parseFloat($('#ExtraTax').val()) || 0;
        const wht = parseFloat($('#WithholdingTax').val()) || 0;
        const netTotal = lineTotal + furtherTax + fed + extraTax - wht;

        $('#SubTotal').text(PakERP.formatCurrency(subTotal));
        $('#TaxAmount').text(PakERP.formatCurrency(taxAmount));
        $('#DiscountAmount').text(PakERP.formatCurrency(discountAmount));
        $('#NetTotal').text(PakERP.formatCurrency(netTotal));
        $('#NetTotalHidden').val(netTotal.toFixed(2));

        return { subTotal, taxAmount, discountAmount, netTotal };
    },

    addRow: function () {
        const template = $('#lineTemplate tr').clone();
        $('#invoiceLines tbody').append(template);
        InvoiceCalc.bindRowEvents(template);
    },

    bindRowEvents: function (row) {
        $(row).find('input').on('input change', () => InvoiceCalc.calculateTotals());
        $(row).find('.remove-row').on('click', function () {
            $(this).closest('tr').remove();
            InvoiceCalc.calculateTotals();
        });
        $(row).find('.item-select').on('change', function () {
            const option = $(this).find(':selected');
            const row = $(this).closest('tr');
            row.find('.hs-code').val(option.data('hs'));
            row.find('.price').val(option.data('price'));
            row.find('.description').val(option.data('name'));
            InvoiceCalc.calculateTotals();
        });
    }
};

// Bill calculation
const BillCalc = {
    calculateLine: function (row) {
        const qty = parseFloat($(row).find('.qty').val()) || 0;
        const rate = parseFloat($(row).find('.rate').val()) || 0;
        const taxRate = parseFloat($(row).find('.tax-rate').val()) || 18;
        const amount = qty * rate;
        const tax = Math.round(amount * taxRate / 100 * 100) / 100;
        $(row).find('.amount').val(amount.toFixed(2));
        $(row).find('.tax-amount').val(tax.toFixed(2));
    },

    calculateTotals: function () {
        let subTotal = 0, taxAmount = 0;
        $('#billLines tbody tr').each(function () {
            BillCalc.calculateLine(this);
            subTotal += parseFloat($(this).find('.amount').val()) || 0;
            taxAmount += parseFloat($(this).find('.tax-amount').val()) || 0;
        });
        $('#BillSubTotal').text(PakERP.formatCurrency(subTotal));
        $('#BillTaxAmount').text(PakERP.formatCurrency(taxAmount));
        $('#BillNetAmount').text(PakERP.formatCurrency(subTotal + taxAmount));
    }
};
