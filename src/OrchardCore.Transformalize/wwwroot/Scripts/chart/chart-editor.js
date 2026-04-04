(() => {
    const elementById = (id) => document.getElementById(id);

    const chartTypeSelect = elementById('TransformalizeReportPart_ChartType_Text');

    const showPercentageFieldWrapper = elementById('TransformalizeReportPart_ChartShowPercentage_Value_FieldWrapper');
    const showPercentageCheckbox = elementById('TransformalizeReportPart_ChartShowPercentage_Value');

    const datasetOneInput = elementById('TransformalizeReportPart_ChartField1_Text');

    const datasetTwoFieldWrapper = elementById('TransformalizeReportPart_ChartField2_Text_FieldWrapper');
    const datasetTwoInput = elementById('TransformalizeReportPart_ChartField2_Text');

    const datasetThreeFieldWrapper = elementById('TransformalizeReportPart_ChartField3_Text_FieldWrapper');
    const datasetThreeInput = elementById('TransformalizeReportPart_ChartField3_Text');

    const useRawDataCheckbox = elementById('TransformalizeReportPart_ChartUseRawData_Value');
    const rawDataLabelFieldWrapper = elementById('TransformalizeReportPart_ChartRawDataLabelField_Text_FieldWrapper');

    const requiredElements = [
        chartTypeSelect,
        showPercentageFieldWrapper,
        showPercentageCheckbox,
        datasetOneInput,
        datasetTwoFieldWrapper,
        datasetTwoInput,
        datasetThreeFieldWrapper,
        datasetThreeInput,
        useRawDataCheckbox,
        rawDataLabelFieldWrapper
    ];

    if (requiredElements.some((element) => !element)) {
        return;
    }

    const helpPanelId = 'chart-config-help';
    if (!elementById(helpPanelId)) {
        const fieldWrapper = datasetOneInput.closest('.mb-3');
        if (fieldWrapper) {
            const help = document.createElement('details');
            help.id = helpPanelId;
            help.className = 'alert alert-info mb-3';
            help.style.fontSize = '0.875rem';
            help.innerHTML = `
<summary style="cursor:pointer;font-weight:600;">How to configure charts</summary>
<div style="margin-top:0.75rem;">

  <p>The examples below use a Northwind order-details report with these fields:</p>
  <table style="border-collapse:collapse;font-size:0.8rem;margin-bottom:0.75rem;">
    <thead><tr style="background:#d1ecf1">
      <th style="padding:3px 8px;border:1px solid #bee5eb">Field name</th>
      <th style="padding:3px 8px;border:1px solid #bee5eb">Type</th>
      <th style="padding:3px 8px;border:1px solid #bee5eb">Description</th>
    </tr></thead>
    <tbody>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>OrderYear</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">long</td><td style="padding:3px 8px;border:1px solid #bee5eb">Year the order was placed</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>OrderMonthSortable</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">string</td><td style="padding:3px 8px;border:1px solid #bee5eb">Month in sortable format (e.g. 01-January)</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>CompanyName</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">string</td><td style="padding:3px 8px;border:1px solid #bee5eb">Customer company (Sold To)</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>ProductName</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">string</td><td style="padding:3px 8px;border:1px solid #bee5eb">Product name</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>CategoryName</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">string</td><td style="padding:3px 8px;border:1px solid #bee5eb">Product category (Beverages, Condiments, …)</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>SuppliersCompanyName</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">string</td><td style="padding:3px 8px;border:1px solid #bee5eb">Supplier company name</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>Quantity</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">int</td><td style="padding:3px 8px;border:1px solid #bee5eb">Units ordered</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>UnitPrice</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">decimal</td><td style="padding:3px 8px;border:1px solid #bee5eb">Price per unit</td></tr>
      <tr><td style="padding:3px 8px;border:1px solid #bee5eb"><code>Revenue</code></td><td style="padding:3px 8px;border:1px solid #bee5eb">decimal</td><td style="padding:3px 8px;border:1px solid #bee5eb">Quantity × UnitPrice</td></tr>
    </tbody>
  </table>

  <p><strong>Standard mode</strong> (Use Raw Data unchecked) — the chart <em>counts rows</em> per distinct field value.</p>
  <ul>
    <li><strong>Dataset 1 only</strong> — one bar or slice per distinct value, showing the row count for that value.
      <br>Example using Northwind order details:
      <br>Dataset 1 = <code>CategoryName</code>, Type = <code>doughnut</code>, Show Percentage = on
      <br>→ one slice per category (Beverages, Condiments, …) sized by number of order lines.</li>
    <li style="margin-top:0.5rem"><strong>Dataset 1 + Dataset 2</strong> — Dataset 1 is the X-axis grouping; Dataset 2 splits each group into series (one series per distinct value of that field).
      <br>Example:
      <br>Dataset 1 = <code>CategoryName</code>, Dataset 2 = <code>OrderYear</code>, Type = <code>stacked bar</code>
      <br>→ stacked bars per category, each stack segment is a year, height is order-line count.</li>
    <li style="margin-top:0.5rem"><strong>Dataset 1 + Dataset 2 + Dataset 3</strong> — a third cross-tabulation on top of the first two.
      <br>Example:
      <br>Dataset 1 = <code>CategoryName</code>, Dataset 2 = <code>OrderYear</code>, Dataset 3 = <code>SuppliersCompanyName</code>, Type = <code>bar</code>
      <br>→ one bar series per supplier-year combination, grouped along the category axis.</li>
  </ul>

  <p><strong>Raw mode</strong> (Use Raw Data checked) — values are plotted <em>as-is</em> in row order, no counting or grouping.</p>
  <ul>
    <li><strong>Dataset 1</strong> must be a <strong>numeric field</strong> (<code>int</code>, <code>decimal</code>, etc.). Each row becomes one data point in order.
      <br>Example:
      <br>Dataset 1 = <code>Revenue</code>, Raw Label Field = <code>ProductName</code>, Type = <code>bar</code>
      <br>→ one bar per row, height is the Revenue value, label is the product name.</li>
    <li><strong>Raw Label Field</strong> — any field to use as the X-axis or slice label. If blank, Dataset 1 values double as labels (useful when the numeric values are themselves meaningful labels, e.g. <code>OrderYear</code>).</li>
    <li>Dataset 2, Dataset 3, and Show Percentage are unavailable in raw mode.</li>
  </ul>

  <p><strong>Chart types:</strong></p>
  <ul>
    <li><code>doughnut</code> / <code>pie</code> — proportion charts; support Show Percentage. Best with Dataset 1 only.</li>
    <li><code>multi pie</code> — side-by-side pie series; requires Dataset 2. Example: Dataset 1 = <code>CategoryName</code>, Dataset 2 = <code>OrderYear</code> → one pie per year.</li>
    <li><code>bar</code> / <code>horizontal bar</code> — grouped bars per X-axis value. Add Dataset 2 for multiple series.</li>
    <li><code>stacked bar</code> — bars stacked to show both total and per-series composition. Requires Dataset 2 to be meaningful.</li>
    <li><code>line</code> — line chart. Works well with raw numeric data (<code>Revenue</code>, <code>Quantity</code>, <code>UnitPrice</code>) plotted in row order.</li>
    <li><code>stacked bar line</code> — combined: all datasets except the last render as bars, the last renders as a line overlay. Example: bars for order count per category (Dataset 1 + 2), line for average revenue (Dataset 3).</li>
  </ul>

</div>`;
            fieldWrapper.insertAdjacentElement('beforebegin', help);
        }
    }

    const allowedChartTypesForPercentage = new Set(['doughnut', 'pie', 'multi pie']);

    const setVisible = (element, isVisible) => {
        element.style.display = isVisible ? '' : 'none';
    };

    const ensureHelpMessage = (id, wrapperElement) => {
        let messageElement = elementById(id);
        if (!messageElement) {
            messageElement = document.createElement('span');
            messageElement.id = id;
            messageElement.className = 'hint dashed';
            wrapperElement.appendChild(messageElement);
        }

        return messageElement;
    };

    const normalizedValue = (inputElement) => (inputElement.value ?? '').trim().toLowerCase();
    const hasValue = (inputElement) => normalizedValue(inputElement).length > 0;

    const validationMessageElementForPart = (() => {
        const validationMessageElementId = 'TransformalizeReportPart__DatasetsUniqueValidationMessage';
        let validationMessageElement = elementById(validationMessageElementId);

        if (!validationMessageElement) {
            validationMessageElement = document.createElement('div');
            validationMessageElement.id = validationMessageElementId;
            validationMessageElement.className = 'text-danger mb-2';
            validationMessageElement.style.display = 'none';

            const insertionPointElement =
                datasetOneInput.closest('.mb-3') ?? datasetOneInput.parentElement;

            if (insertionPointElement) {
                insertionPointElement.insertAdjacentElement('afterend', validationMessageElement);
            }
        }

        return validationMessageElement;
    })();

    const setPartValidationMessage = (message) => {
        validationMessageElementForPart.textContent = message;
        validationMessageElementForPart.style.display = message ? '' : 'none';
    };

    const clearAllInputCustomValidity = () => {
        datasetOneInput.setCustomValidity('');
        datasetTwoInput.setCustomValidity('');
        datasetThreeInput.setCustomValidity('');
    };

    const validateDatasetsAreUnique = () => {
        clearAllInputCustomValidity();
        setPartValidationMessage('');

        const datasetInputs = [
            datasetOneInput,
            datasetTwoFieldWrapper.style.display === 'none' ? null : datasetTwoInput,
            datasetThreeFieldWrapper.style.display === 'none' ? null : datasetThreeInput
        ].filter((inputElement) => inputElement && hasValue(inputElement));

        const datasetValueToInputsMap = new Map();

        for (const inputElement of datasetInputs) {
            const value = normalizedValue(inputElement);

            if (!datasetValueToInputsMap.has(value)) {
                datasetValueToInputsMap.set(value, []);
            }

            datasetValueToInputsMap.get(value).push(inputElement);
        }

        const conflictingInputIds = [];

        for (const [, inputElements] of datasetValueToInputsMap) {
            if (inputElements.length > 1) {
                conflictingInputIds.push(...inputElements.map((inputElement) => inputElement.id));
            }
        }

        if (conflictingInputIds.length === 0) {
            return true;
        }

        const datasetLabels = conflictingInputIds
            .map((id) => {
                if (id === datasetOneInput.id) {
                    return 'Dataset 1';
                }

                if (id === datasetTwoInput.id) {
                    return 'Dataset 2';
                }

                return 'Dataset 3';
            })
            .filter((value, index, array) => array.indexOf(value) === index);

        const message =
            `Datasets must be unique. Please change: ${datasetLabels.join(', ')}.`;

        for (const inputElement of [datasetOneInput, datasetTwoInput, datasetThreeInput]) {
            inputElement.setCustomValidity(message);
        }

        setPartValidationMessage(message);

        return false;
    };

    const updateVisibilityAndValidation = () => {
        const selectedChartType = (chartTypeSelect.value ?? '').trim().toLowerCase();
        const useRawData = useRawDataCheckbox.checked;

        const showPercentageLabel = showPercentageFieldWrapper.querySelector('label');
        const showPercentageHintParent = showPercentageLabel ?? showPercentageFieldWrapper;
        const showPercentageHelp = ensureHelpMessage(
            'TransformalizeReportPart_ChartShowPercentage_Value_DisabledHint',
            showPercentageHintParent
        );

        const canUsePercentage = allowedChartTypesForPercentage.has(selectedChartType) && !useRawData;
        showPercentageCheckbox.disabled = !canUsePercentage;

        if (!canUsePercentage) {
            showPercentageCheckbox.checked = false;
        }

        if (useRawData) {
            showPercentageHelp.textContent = ' Disabled for raw data charts (values are not proportions).';
            showPercentageHelp.style.display = '';
        } else if (!allowedChartTypesForPercentage.has(selectedChartType)) {
            showPercentageHelp.textContent = ' Only available for doughnut, pie, and multi pie charts.';
            showPercentageHelp.style.display = '';
        } else {
            showPercentageHelp.textContent = '';
            showPercentageHelp.style.display = 'none';
        }

        const shouldShowDatasetTwo = !useRawData && hasValue(datasetOneInput);
        setVisible(datasetTwoFieldWrapper, shouldShowDatasetTwo);

        if (!shouldShowDatasetTwo) {
            datasetTwoInput.value = '';
        }

        const shouldShowDatasetThree =
            shouldShowDatasetTwo && hasValue(datasetTwoInput);

        setVisible(datasetThreeFieldWrapper, shouldShowDatasetThree);

        if (!shouldShowDatasetThree) {
            datasetThreeInput.value = '';
        }

        setVisible(rawDataLabelFieldWrapper, useRawData);
        if (!useRawData) {
            const rawDataLabelInput = elementById('TransformalizeReportPart_ChartRawDataLabelField_Text');
            if (rawDataLabelInput) {
                rawDataLabelInput.value = '';
            }
        }

        if (rawDataLabelFieldWrapper) {
            const rawLabelHint = ensureHelpMessage(
                'TransformalizeReportPart_ChartRawDataLabelField_Text_Hint',
                rawDataLabelFieldWrapper
            );
            rawLabelHint.textContent = '';
            rawLabelHint.style.display = 'none';
        }

        validateDatasetsAreUnique();
    };

    updateVisibilityAndValidation();

    chartTypeSelect.addEventListener('change', updateVisibilityAndValidation);
    datasetOneInput.addEventListener('input', updateVisibilityAndValidation);
    datasetTwoInput.addEventListener('input', updateVisibilityAndValidation);
    datasetThreeInput.addEventListener('input', validateDatasetsAreUnique);
    useRawDataCheckbox.addEventListener('change', updateVisibilityAndValidation);

    const containingFormElement = datasetOneInput.closest('form');

    if (containingFormElement) {
        containingFormElement.addEventListener('submit', (submitEvent) => {
            if (!validateDatasetsAreUnique()) {
                submitEvent.preventDefault();
                submitEvent.stopPropagation();
                containingFormElement.reportValidity();
            }
        });
    }
})();
