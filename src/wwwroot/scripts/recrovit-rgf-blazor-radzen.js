/*!
* recrovit-rgf-blazor-radzen.js v1.0.0
*/

window.Recrovit = window.Recrovit || { RGF: {} };
window.Recrovit.RGF.Blazor = window.Recrovit.RGF.Blazor || {};
var Blazor = window.Recrovit.RGF.Blazor;

Blazor.RadzenUI = {
    Grid: {
        initializeTable: function (gridRef) {
            const dataGridTable = gridRef.querySelector('table.rz-grid-table');
            if (dataGridTable) {
                dataGridTable.classList.add('recro-grid');
                gridRef.classList.remove('recro-grid');
            }
        }
    }
};