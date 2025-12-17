(function () {
    'use strict';

    function initializeDatePickers() {
        if (window.jalaliDatepicker && typeof window.jalaliDatepicker.startWatch === 'function') {
            window.jalaliDatepicker.startWatch({
                date: true,
                time: false,
                autoHide: true,
                persianDigits: true,
                showCloseBtn: 'dynamic',
                topSpace: 10,
                bottomSpace: 30,
                overflowSpace: 10
            });
        }

        const formWrapper = document.querySelector('[data-visits-filter-form]');
        if (!formWrapper) return;

        formWrapper.querySelectorAll('[data-jalali-picker]').forEach(container => {
            const jalaliInput = container.querySelector('[data-jalali-input]');
            const jalaliTarget = container.querySelector('[data-jalali-target]');

            const applyNormalizedDate = () => {
                if (!jalaliInput || !jalaliTarget) {
                    return;
                }

                const value = jalaliInput.value || '';
                const normalized = value.replace(/\//g, '-');
                jalaliTarget.value = normalized || '';
            };

            if (jalaliTarget && jalaliInput && jalaliTarget.value && !jalaliInput.value) {
                jalaliInput.value = jalaliTarget.value.replace(/-/g, '/');
            }

            jalaliInput?.addEventListener('change', applyNormalizedDate);
            jalaliInput?.addEventListener('input', applyNormalizedDate);
        });
    }

    function initializeChart() {
        if (typeof Chart === 'undefined' || !window.dailyVisitsData) {
            return;
        }

        const ctx = document.getElementById('dailyVisitsChart');
        if (!ctx) {
            return;
        }

        const data = window.dailyVisitsData || [];
        const labels = data.map(d => d.date);
        const visitCounts = data.map(d => d.visitCount);
        const uniqueVisitorCounts = data.map(d => d.uniqueVisitorCount);

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'کل بازدیدها',
                        data: visitCounts,
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.1)',
                        tension: 0.4,
                        fill: true
                    },
                    {
                        label: 'بازدیدکنندگان منحصر به فرد',
                        data: uniqueVisitorCounts,
                        borderColor: 'rgb(255, 99, 132)',
                        backgroundColor: 'rgba(255, 99, 132, 0.1)',
                        tension: 0.4,
                        fill: true
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                plugins: {
                    legend: {
                        position: 'top',
                        rtl: true
                    },
                    tooltip: {
                        rtl: true,
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + context.parsed.y.toLocaleString('fa-IR');
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return value.toLocaleString('fa-IR');
                            }
                        }
                    }
                }
            }
        });
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initializeDatePickers();
            initializeChart();
        });
    } else {
        initializeDatePickers();
        initializeChart();
    }
})();

