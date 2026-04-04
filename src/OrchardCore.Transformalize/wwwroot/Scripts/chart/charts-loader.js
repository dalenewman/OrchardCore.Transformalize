(async () => {
    const charts = document.querySelectorAll("[id^='chart_']");

    const createChart = (chartElement) => {
        const ctx = chartElement;
        const elementDataset = ctx.dataset;
        const inputDatasets = JSON.parse(elementDataset.inputDatasets);
        const labelList = JSON.parse(elementDataset.labelList);
        const indexAxeLabels = JSON.parse(elementDataset.indexAxeLabels);
        const displayLegend = JSON.parse(elementDataset.displayLegend);
        const backgroundColors = JSON.parse(elementDataset.backgroundColors);
        const showPercentage = JSON.parse(elementDataset.showPercentage);
        const isStacked = JSON.parse(elementDataset.stacked);
        const isCombined = JSON.parse(elementDataset.combined);
        const isHorizontal = JSON.parse(elementDataset.horizontal);
        const fullChartType = elementDataset.fullChartType;
        const chartType = elementDataset.chartType;
        const chartTitle = elementDataset.chartTitle;
        const hasMoreDatasets = inputDatasets.length > 1;
        const isOnlyLine = fullChartType === 'line';
        const isPie = fullChartType.includes('pie');
        const isMultiSeriesPie = fullChartType === 'multi pie';
        const chartTypeIsLine = chartType === 'line';
        const firstTypeInCombined = fullChartType.split(' ').splice(1, 1).pop();
        const secondTypeInCombined = fullChartType.split(' ').splice(2, 1).pop();
        const backgroundColorAlpha = 'b3'; // 70% opacity in hex

        const options = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: false,
                    align: 'start',
                    text: chartTitle,
                    color: '#141A1F',
                    padding: {
                        bottom: 20
                    },
                    font: {
                        size: 18,
                        weight: 700,
                        lineHeight: 1.2,
                        family: 'Nunito'
                    }
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
                            const dataset = chart.data.datasets[chart.data.datasets.length -1];

                            if (dataset && chart.data.datasets.length === 1) {
                                return chart.data.labels.map((label, index) => ({
                                    // This is the only way to add space between the circle and label.
                                    text: ' ' + label,
                                    fontColor: '#141A1F',
                                    fillStyle: dataset.backgroundColor[index],
                                    strokeStyle: dataset.backgroundColor[index],
                                    index,
                                    hidden: dataset.data[index] == null,
                                    datasetIndex: 0
                                }));
                            }

                            if (isMultiSeriesPie) {
                                // Get the default label list.
                                const original = Chart.overrides.pie.plugins.legend.labels.generateLabels;
                                const labelsOriginal = original.call(this, chart);

                                // Build an array of colors used in the datasets of the chart.
                                let datasetColors = chart.data.datasets.map(function(e) {
                                    return e.backgroundColor;
                                });
                                datasetColors = datasetColors.flat();

                                // Modify the color and hide state of each label.
                                labelsOriginal.forEach(label => {
                                    // There are twice as many labels as there are datasets. This converts the label index into the corresponding dataset index.
                                    label.datasetIndex = (label.index - label.index % 2) / 2;

                                    // The hidden state must match the dataset's hidden state.
                                    label.hidden = !chart.isDatasetVisible(label.datasetIndex);

                                    // Change the color to match the dataset.
                                    label.fillStyle = datasetColors[label.index];
                                    label.strokeStyle = datasetColors[label.index];
                                });

                                return labelsOriginal;
                            }

                            return Chart.defaults.plugins.legend.labels.generateLabels(chart);
                        },
                    },
                    onClick: function(e, legendItem, legend) {
                        const chart = legend.chart;

                        if (chart.data.datasets.length > 1) {
                            return Chart.defaults.plugins.legend.onClick(e, legendItem, legend);
                        }

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
                            const hasMoreDatasets = context.chart.data.datasets.length > 1;

                            if (hasMoreDatasets && !isMultiSeriesPie) {
                                const dataArray = context.dataset.data;
                                const currentValue = dataArray[context.dataIndex];
                                return `${context.dataset.label}: ${currentValue}`;
                            }
                            if (showPercentage)
                            {
                                const dataArray = context.dataset.data;
                                const total = dataArray.reduce((sum, val) => sum + val, 0);

                                const currentValue = dataArray[context.dataIndex];
                                const percentage = total === 0 ? 0 : (currentValue / total * 100);

                                return `${Math.round(percentage)}%`;
                            }

                            return context.formattedValue;
                        },
                        title: function(context) {
                            if (isMultiSeriesPie)
                            {
                                const labelIndex = (context[0].datasetIndex * 2) + context[0].dataIndex;
                                return context[0].chart.data.labels[labelIndex];
                            }

                            return Chart.defaults.plugins.tooltip.callbacks.title(context);
                        }
                    }
                },
            }
        };

        if (!isPie)
        {
            options.scales = {
                x: {
                    ticks: {
                        font: {
                            color: '#3B4044',
                            size: '10px',
                            weight: 400,
                            lineHeight: '12px',
                            family: 'Nunito'
                        },
                    }
                },
                y: {
                    ticks: {
                        font: {
                            color: '#3B4044',
                            size: '10px',
                            weight: 400,
                            lineHeight: '12px',
                            family: 'Nunito'
                        },
                    }
                }
            };
        }

        if (isStacked) {
            options.scales.x.stacked = true;
            options.scales.y.stacked = true;
        }

        if (isHorizontal) {
            options.indexAxis = 'y';
        }

        let borderWith = 0;
        let pointRadius = 3;
        if (isOnlyLine)
        {
            borderWith = 3;
            pointRadius = 6;
        }
        else if (chartTypeIsLine)
        {
            borderWith = 2;
        }

        let firstBackgroundColor;
        let firstDatasetBorderColor;
        if (hasMoreDatasets)
        {
            firstDatasetBorderColor = backgroundColors[0];
            if (chartTypeIsLine)
            {
                firstBackgroundColor = backgroundColors[0] + backgroundColorAlpha;
            }
            else
            {
                firstBackgroundColor = backgroundColors[0];
            }
        }
        else
        {
            firstBackgroundColor = backgroundColors;
            firstDatasetBorderColor = backgroundColors;
        }

        const datasets = [
            {
                label: labelList[0],
                data: inputDatasets[0],
                backgroundColor: firstBackgroundColor,
                borderColor: firstDatasetBorderColor,
                borderAlign: 'inner',
                borderWith,
                pointRadius,
                grouped: false,
            }
        ];

        for (let index = 0; index < inputDatasets.length; index++)
        {
            if (index === 0) {
                if (isCombined)
                {
                    datasets[0].type = firstTypeInCombined;
                    datasets[0].stack = 'combined';
                }

                if (isMultiSeriesPie){
                    datasets[0].backgroundColor = backgroundColors.slice(0,2);
                    datasets[0].borderColor = backgroundColors.slice(0,2);
                }

                continue;
            }

            let backgroundColor = backgroundColors[index];
            let borderColor = backgroundColors[index];
            if (chartTypeIsLine || (isCombined && firstTypeInCombined === 'line'))
            {
                // For line charts, add some transparency to the background color.
                backgroundColor += backgroundColorAlpha;
            }

            if (isMultiSeriesPie)
            {
                backgroundColor = backgroundColors.slice(index * 2, index * 2 + 2);
                borderColor = backgroundColors.slice(index * 2, index * 2 + 2);
            }


            let datasetElement = {
                label: labelList.length >= index ? labelList[index] : '',
                data: inputDatasets[index],
                backgroundColor: backgroundColor,
                borderColor: borderColor,
                borderAlign: 'inner',
                borderWith,
                pointRadius,
                grouped: false,
            };

            if (isCombined)
            {
                // For combined charts, all but the last dataset are of the first type, the last is of the second type.
                // Currently, this means all but the last are bars, the last is a line when the type is "Stacked bar/line".
                datasetElement.type = index === inputDatasets.length - 1
                    ? secondTypeInCombined
                    : firstTypeInCombined;
                datasetElement.stack = 'combined';
            }
            datasets.push(datasetElement);
        }

        let chart = new Chart(ctx, {
            type: chartType,
            fullChartType,
            data: {
                labels: isMultiSeriesPie ? labelList : indexAxeLabels,
                datasets:  datasets
            },
            options: options
        });

        chart.config._originalData = [...chart.data.datasets[0].data];
    };

    charts.forEach(chart => createChart(chart));
})();
