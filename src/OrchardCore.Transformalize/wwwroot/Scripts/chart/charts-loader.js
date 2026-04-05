function setChartType(type) {
    document.getElementById('id_chart_type').value = type;
    document.getElementById('id_report').submit();
}
function toggleChartLegend() {
    const el = document.getElementById('id_chart_legend');
    el.value = el.value === 'true' ? 'false' : 'true';
    document.getElementById('id_report').submit();
}
function toggleChartPct() {
    const el = document.getElementById('id_chart_pct');
    el.value = el.value === 'true' ? 'false' : 'true';
    document.getElementById('id_report').submit();
}

(async () => {
    const charts = document.querySelectorAll("[id^='chart_']");

    const createChart = (chartElement) => {
        const ctx = chartElement;
        const elementDataset = ctx.dataset;
        const labels = JSON.parse(elementDataset.labels);
        const values = JSON.parse(elementDataset.values);
        const labelToHue = s => {
            let h = 0;
            for (const c of s) h = (Math.imul(h, 31) + c.charCodeAt(0)) | 0;
            return Math.round(((h >>> 0) * 137.508) % 360);
        };
        const colors = labels.map(label => `hsl(${labelToHue(label)}, 65%, 55%)`);
        const chartTitle = elementDataset.chartTitle;
        const rawType = elementDataset.chartType || 'doughnut';
        const isHorizontal = rawType === 'hbar';
        const chartType = isHorizontal ? 'bar' : rawType;
        const isPie = chartType === 'doughnut' || chartType === 'pie';
        const displayLegend = JSON.parse(elementDataset.displayLegend ?? 'true');
        const showPercentage = JSON.parse(elementDataset.showPercentage ?? 'false');
        const parameterName = elementDataset.parameterName;
        const isFilterable = parameterName && labels.length > 1;
        chartElement.style.cursor = isFilterable ? 'pointer' : 'default';

        const options = {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            onClick: (event, elements) => {
                if (!isFilterable || !elements.length) return;
                const clickedLabel = labels[elements[0].index];
                if (!clickedLabel) return;
                const url = new URL(window.location.href);
                url.searchParams.set(parameterName, clickedLabel);
                url.searchParams.delete('page');
                window.location.href = url.toString();
            },
            plugins: {
                title: {
                    display: false,
                },
                legend: {
                    display: displayLegend,
                    position: 'bottom',
                    labels: {
                        padding: 16,
                        usePointStyle: true,
                        pointStyle: 'circle',
                        color: '#141A1F',
                        font: {
                            size: 12,
                            weight: 600,
                            lineHeight: 1.2,
                            family: 'Nunito'
                        },
                        generateLabels: function(chart) {
                            if (!isPie) {
                                return Chart.defaults.plugins.legend.labels.generateLabels(chart);
                            }
                            const dataset = chart.data.datasets[0];
                            if (!dataset) return [];
                            return chart.data.labels.map((label, index) => ({
                                text: ' ' + label,
                                fontColor: '#141A1F',
                                fillStyle: dataset.backgroundColor[index],
                                strokeStyle: dataset.backgroundColor[index],
                                index,
                                hidden: dataset.data[index] == null,
                                datasetIndex: 0
                            }));
                        },
                    },
                    onClick: function(e, legendItem, legend) {
                        const chart = legend.chart;
                        const index = legendItem.index;
                        const dataset = chart.data.datasets[0];

                        if (!chart.config._originalData) {
                            chart.config._originalData = [...dataset.data];
                        }

                        dataset.data[index] =
                            dataset.data[index] == null
                                ? chart.config._originalData[index]
                                : null;

                        chart.update();
                    }
                },
                tooltip: {
                    cornerRadius: 15,
                    boxWidth: 16,
                    boxHeight: 16,
                    usePointStyle: true,
                    backgroundColor: 'rgba(20, 26, 31, 0.8)',
                    boxPadding: 8,
                    padding: 12,
                    caretSize: 9,
                    titleFont: {
                        size: 12,
                        weight: 500,
                        lineHeight: 1.2,
                        family: 'Nunito'
                    },
                    titleColor: '#ffffff',
                    callbacks: {
                        label: function(context) {
                            if (showPercentage && isPie) {
                                const dataArray = context.dataset.data;
                                const total = dataArray.reduce((sum, val) => sum + (val ?? 0), 0);
                                const pct = total === 0 ? 0 : (context.parsed / total * 100);
                                return `${Math.round(pct)}%`;
                            }
                            return context.formattedValue;
                        }
                    }
                },
            }
        };

        if (!isPie) {
            options.scales = {
                x: { ticks: { font: { color: '#3B4044', size: '10px', weight: 400, family: 'Nunito' } } },
                y: { ticks: { font: { color: '#3B4044', size: '10px', weight: 400, family: 'Nunito' } } }
            };
        }

        if (isHorizontal) {
            options.indexAxis = 'y';
        }

        let chart = new Chart(ctx, {
            type: chartType,
            data: {
                labels: labels,
                datasets: [
                    {
                        label: chartTitle,
                        data: values,
                        backgroundColor: colors,
                        borderColor: colors,
                        borderAlign: 'inner',
                        borderWidth: 0,
                    }
                ]
            },
            options: options
        });

        chart.config._originalData = [...chart.data.datasets[0].data];

        // Let the ResizeObserver settle, then trigger the entry animation
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                chart.options.animation = { duration: 800, easing: 'easeInOutQuart' };
                chart.reset();
                chart.update();
            });
        });
    };

    charts.forEach(chart => createChart(chart));
})();
