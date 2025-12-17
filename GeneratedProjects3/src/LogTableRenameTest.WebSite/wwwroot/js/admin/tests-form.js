(() => {
    'use strict';

    const forms = document.querySelectorAll('[data-tests-form]');
    if (!forms.length) {
        return;
    }

    const persianDigits = '۰۱۲۳۴۵۶۷۸۹';
    const arabicDigits = '٠١٢٣٤٥٦٧٨٩';

    const pad = value => value.toString().padStart(2, '0');

    function normaliseDigits(input) {
        if (!input) {
            return '';
        }

        let result = '';
        for (const ch of input) {
            const persianIndex = persianDigits.indexOf(ch);
            if (persianIndex >= 0) {
                result += persianIndex.toString();
                continue;
            }

            const arabicIndex = arabicDigits.indexOf(ch);
            if (arabicIndex >= 0) {
                result += arabicIndex.toString();
                continue;
            }

            result += ch;
        }

        return result;
    }

    function jalaliToGregorian(jy, jm, jd) {
        jy = parseInt(jy, 10);
        jm = parseInt(jm, 10);
        jd = parseInt(jd, 10);

        if (Number.isNaN(jy) || Number.isNaN(jm) || Number.isNaN(jd)) {
            return null;
        }

        jy += 1595;
        let days = -355668 + (365 * jy) + Math.floor(jy / 33) * 8 + Math.floor(((jy % 33) + 3) / 4) + jd;
        days += (jm < 7) ? ((jm - 1) * 31) : (((jm - 7) * 30) + 186);

        let gy = 400 * Math.floor(days / 146097);
        days %= 146097;

        if (days > 36524) {
            gy += 100 * Math.floor(--days / 36524);
            days %= 36524;
            if (days >= 365) {
                days++;
            }
        }

        gy += 4 * Math.floor(days / 1461);
        days %= 1461;

        if (days > 365) {
            gy += Math.floor((days - 1) / 365);
            days = (days - 1) % 365;
        }

        const gd = days + 1;
        const monthDays = [0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

        if ((gy % 4 === 0 && gy % 100 !== 0) || (gy % 400 === 0)) {
            monthDays[2] = 29;
        }

        let gm = 1;
        let accumulatedDays = 0;

        while (gm <= 12 && gd > accumulatedDays + monthDays[gm]) {
            accumulatedDays += monthDays[gm];
            gm += 1;
        }

        const finalDay = gd - accumulatedDays;

        return {
            gy,
            gm,
            gd: finalDay
        };
    }

    function updateHiddenValue(displayInput) {
        const targetKey = displayInput.getAttribute('data-jalali-display');
        if (!targetKey) {
            return;
        }

        const form = displayInput.closest('form');
        if (!form) {
            return;
        }

        const hidden = form.querySelector(`[data-jalali-target="${targetKey}"]`);
        if (!hidden) {
            return;
        }

        const rawValue = normaliseDigits(displayInput.value.trim());
        if (!rawValue) {
            hidden.value = '';
            return;
        }

        const parts = rawValue.split(' ');
        let datePart = parts.length > 1 ? parts[parts.length - 1] : parts[0];
        let timePart = parts.length > 1 ? parts[0] : '';

        const datePieces = datePart.split(/[\/\-]/);
        if (datePieces.length !== 3) {
            hidden.value = '';
            return;
        }

        const conversion = jalaliToGregorian(datePieces[0], datePieces[1], datePieces[2]);
        if (!conversion) {
            hidden.value = '';
            return;
        }

        let hours = 0;
        let minutes = 0;

        if (timePart && timePart.includes(':')) {
            const timePieces = timePart.split(':');
            hours = parseInt(normaliseDigits(timePieces[0]), 10) || 0;
            minutes = parseInt(normaliseDigits(timePieces[1]), 10) || 0;
        }

        const candidate = new Date(conversion.gy, conversion.gm - 1, conversion.gd, hours, minutes, 0);

        if (Number.isNaN(candidate.getTime())) {
            hidden.value = '';
            return;
        }

        const offsetMinutes = -candidate.getTimezoneOffset();
        const offsetSign = offsetMinutes >= 0 ? '+' : '-';
        const absoluteOffset = Math.abs(offsetMinutes);
        const offsetHours = pad(Math.floor(absoluteOffset / 60));
        const offsetMins = pad(absoluteOffset % 60);

        hidden.value = `${candidate.getFullYear()}-${pad(candidate.getMonth() + 1)}-${pad(candidate.getDate())}T${pad(candidate.getHours())}:${pad(candidate.getMinutes())}:00${offsetSign}${offsetHours}:${offsetMins}`;
    }

    forms.forEach(form => {
        const displays = form.querySelectorAll('[data-jalali-display]');

        displays.forEach(display => {
            display.addEventListener('change', () => updateHiddenValue(display));
            display.addEventListener('blur', () => updateHiddenValue(display));
        });

        form.querySelectorAll('[data-jalali-clear]').forEach(button => {
            const key = button.getAttribute('data-jalali-clear');
            if (!key) {
                return;
            }

            const display = form.querySelector(`[data-jalali-display="${key}"]`);
            const hidden = form.querySelector(`[data-jalali-target="${key}"]`);
            if (!display || !hidden) {
                return;
            }

            button.addEventListener('click', () => {
                display.value = '';
                hidden.value = '';
            });
        });

        form.querySelectorAll('[data-jalali-open]').forEach(button => {
            const key = button.getAttribute('data-jalali-open');
            if (!key) {
                return;
            }

            const display = form.querySelector(`[data-jalali-display="${key}"]`);
            if (!display) {
                return;
            }

            button.addEventListener('click', () => {
                if (window.jalaliDatepicker && typeof window.jalaliDatepicker.show === 'function') {
                    window.jalaliDatepicker.show(display);
                } else {
                    display.focus();
                }
            });
        });

        form.addEventListener('submit', () => {
            displays.forEach(display => updateHiddenValue(display));
        });

        displays.forEach(display => {
            if (display.value) {
                updateHiddenValue(display);
            }
        });
    });

    if (window.jalaliDatepicker && typeof window.jalaliDatepicker.startWatch === 'function') {
        window.jalaliDatepicker.startWatch({
            time: true,
            showTodayBtn: true,
            showEmptyBtn: true
        });
    }
})();
