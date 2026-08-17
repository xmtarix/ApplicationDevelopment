// Lebanon Basketball Reservation — progressive enhancement only.
// Every feature here degrades to a working server-rendered form if JS is off.
(function () {
    'use strict';

    // ---------------------------------------------------------------------
    // Cascading location dropdowns: Governorate -> District -> Area
    // ---------------------------------------------------------------------
    function fillSelect(select, items, placeholder, selectedValue) {
        select.innerHTML = '';
        var blank = document.createElement('option');
        blank.value = '';
        blank.textContent = placeholder;
        select.appendChild(blank);

        items.forEach(function (item) {
            var opt = document.createElement('option');
            opt.value = item.id;
            opt.textContent = item.name;
            if (selectedValue && String(item.id) === String(selectedValue)) opt.selected = true;
            select.appendChild(opt);
        });
    }

    function loadInto(url, select, placeholder, selectedValue) {
        select.disabled = true;
        return fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (items) {
                fillSelect(select, items, placeholder, selectedValue);
                select.disabled = false;
            })
            .catch(function () {
                fillSelect(select, [], placeholder, null);
                select.disabled = false;
            });
    }

    function initLocationCascade() {
        var gov = document.querySelector('[data-location="governorate"]');
        var dist = document.querySelector('[data-location="district"]');
        var area = document.querySelector('[data-location="area"]');
        if (!gov || !dist || !area) return;

        gov.addEventListener('change', function () {
            fillSelect(area, [], 'Select an area', null);
            if (!gov.value) {
                fillSelect(dist, [], 'Select a district', null);
                return;
            }
            loadInto('/Locations/Districts?governorateId=' + encodeURIComponent(gov.value), dist, 'Select a district', null);
        });

        dist.addEventListener('change', function () {
            if (!dist.value) {
                fillSelect(area, [], 'Select an area', null);
                return;
            }
            loadInto('/Locations/Areas?districtId=' + encodeURIComponent(dist.value), area, 'Select an area', null);
        });
    }

    // ---------------------------------------------------------------------
    // Stadium details: load available slots for the chosen date
    // ---------------------------------------------------------------------
    function initSlotPickers() {
        document.querySelectorAll('.date-picker').forEach(function (input) {
            input.addEventListener('change', function () {
                var courtId = this.dataset.courtId;
                var container = document.querySelector('.slot-container-' + courtId);
                if (!container) return;

                if (!this.value) {
                    container.innerHTML = '';
                    return;
                }

                container.innerHTML = '<div class="spinner-border spinner-border-sm text-warning" role="status">' +
                    '<span class="visually-hidden">Loading…</span></div>';

                fetch('/Stadiums/Availability?courtId=' + encodeURIComponent(courtId) +
                      '&date=' + encodeURIComponent(this.value))
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .then(function (slots) {
                        if (!slots.length) {
                            container.innerHTML = '<p class="text-muted small mb-0">' +
                                'No open slots on this date. Try another day.</p>';
                            return;
                        }

                        var wrap = document.createElement('div');
                        wrap.className = 'slot-grid mt-2';

                        slots.forEach(function (s) {
                            var a = document.createElement('a');
                            a.className = 'btn btn-sm btn-outline-brand';
                            a.href = '/Customer/Reservations/Create?courtId=' + encodeURIComponent(courtId) +
                                     '&timeSlotId=' + encodeURIComponent(s.id);
                            a.textContent = s.label || (s.start + ' – ' + s.end);
                            wrap.appendChild(a);
                        });

                        container.innerHTML = '';
                        container.appendChild(wrap);
                    })
                    .catch(function () {
                        container.innerHTML = '<p class="text-danger small mb-0">' +
                            'Could not load slots. Please try again.</p>';
                    });
            });
        });
    }

    // ---------------------------------------------------------------------
    // Booking form: show the running price as slots are picked
    // ---------------------------------------------------------------------
    function initPricePreview() {
        var form = document.querySelector('[data-price-form]');
        if (!form) return;

        var output = form.querySelector('[data-price-output]');
        var submit = form.querySelector('[data-price-submit]');
        var hourly = parseFloat(form.dataset.hourlyPrice || '0');
        if (!output) return;

        function update() {
            var checked = form.querySelector('input[name="TimeSlotId"]:checked');
            if (!checked) {
                output.textContent = '—';
                if (submit) submit.disabled = true;
                return;
            }
            var hours = parseFloat(checked.dataset.hours || '1');
            output.textContent = '$' + (hourly * hours).toFixed(2);
            if (submit) submit.disabled = false;
        }

        form.addEventListener('change', function (e) {
            if (e.target && e.target.name === 'TimeSlotId') update();
        });

        update();
    }

    // ---------------------------------------------------------------------
    // Confirm destructive actions before they submit
    // ---------------------------------------------------------------------
    function initConfirms() {
        document.querySelectorAll('form[data-confirm]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (!window.confirm(form.dataset.confirm)) e.preventDefault();
            });
        });
    }

    // Auto-dismiss success banners so they do not pile up on the page.
    function initAutoDismiss() {
        document.querySelectorAll('.alert-success[data-bs-dismiss-after]').forEach(function (el) {
            setTimeout(function () {
                if (window.bootstrap && window.bootstrap.Alert) {
                    window.bootstrap.Alert.getOrCreateInstance(el).close();
                }
            }, parseInt(el.dataset.bsDismissAfter, 10) || 5000);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initLocationCascade();
        initSlotPickers();
        initPricePreview();
        initConfirms();
        initAutoDismiss();
    });
})();
