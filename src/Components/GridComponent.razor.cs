using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Recrovit.RecroGridFramework.Abstraction.Infrastructure.Events;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Recrovit.RecroGridFramework.Client.Handlers;
using System;
using System.Linq;

namespace Recrovit.RecroGridFramework.Client.Blazor.RadzenUI.Components;

public partial class GridComponent : ComponentBase
{
    [Inject]
    private ILogger<GridComponent> _logger { get; set; } = default!;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    private RgfGridComponent _rgfGridRef { get; set; } = default!;

    private RadzenDataGrid<RgfDynamicDictionary> _radzenGridRef { get; set; } = default!;

    private bool _initialized { get; set; }

    private RgfEntity EntityDesc => Manager.EntityDesc;

    public IRgListHandler ListHandler => Manager.ListHandler;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GridParameters.Events.CreateAttributes.Subscribe(OnCreateAttributes);
        GridParameters.Events.ColumnSettingsChanged.Subscribe((arg) => Recreate());
        _initialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
        }
        if (_initialized)
        {
            if (EntityDesc.SortColumns.Any() && !_radzenGridRef.Sorts.Any())
            {
                foreach (var item in EntityDesc.SortColumns)
                {
                    _radzenGridRef.Sorts.Add(new SortDescriptor() { Property = item.Alias, SortOrder = item.Sort > 0 ? SortOrder.Ascending : SortOrder.Descending });
                }
            }
            await _jsRuntime.InvokeVoidAsync(Configuration.JsBlazorRadzenUiNamespace + ".Grid.initializeTable", _radzenGridRef.Element);
        }
    }

    void OnLoadData(LoadDataArgs args)
    {
        args.Sorts = EntityDesc.SortColumns.Select(e => new SortDescriptor() { Property = e.Alias, SortOrder = e.Sort > 0 ? SortOrder.Ascending : SortOrder.Descending }).ToArray();
    }

    private async Task OnSort(DataGridColumnSortEventArgs<RgfDynamicDictionary> args)
    {
        var dict = new Dictionary<string, int>();
        int idx = 1;
        bool add = true;
        foreach (var item in EntityDesc.SortColumns)
        {
            int sort = item.Sort;
            if (args.Column.Property == item.Alias)
            {
                add = false;
                if (args.SortOrder == null)
                {
                    continue;
                }
                dict.Add(args.Column.Property, args.SortOrder == SortOrder.Ascending ? idx : -idx);
            }
            else
            {
                dict.Add(item.Alias, item.Sort > 0 ? idx : -idx);
            }
            idx++;
        }
        if (add)
        {
            dict.Add(args.Column.Property, args.SortOrder == SortOrder.Ascending ? idx : -idx);
        }
        await ListHandler.SetSortAsync(dict);
    }

    private async Task OnColumnReordered(DataGridColumnReorderedEventArgs<RgfDynamicDictionary> args)
    {
        await ListHandler.MoveColumnAsync(args.OldIndex + 1, args.NewIndex + (args.OldIndex < args.NewIndex ? 1 : 2), false);
        //_radzenGridRef.Reset();
        Recreate();
    }

    private void OnColumnResized(DataGridColumnResizedEventArgs<RgfDynamicDictionary> args) => ListHandler.ReplaceColumnWidth(args.Column.Property, Convert.ToInt32(args.Width));

    private void Recreate()
    {
        _initialized = false;
        StateHasChanged();
        _ = Task.Run(() =>
        {
            _initialized = true;
            StateHasChanged();
        });
    }

    protected virtual Task OnCreateAttributes(DataEventArgs<RgfDynamicDictionary> rowData)
    {
        foreach (var prop in EntityDesc.SortedVisibleColumns)
        {
            var attr = rowData.Value["__attributes"] as RgfDynamicDictionary;
            if (attr != null)
            {
                string? propAttr = null;
                if (prop.FormType == PropertyFormType.CheckBox)
                {
                    propAttr = " rz-text-align-center";
                }
                else if (prop.ListType == PropertyListType.Numeric)
                {
                    propAttr = " rz-text-align-end";
                }
                if (propAttr != null)
                {
                    attr[$"class-{prop.Alias}"] += propAttr;
                }
            }
        }
        return Task.CompletedTask;
    }

    private void OnRowRender(RowRenderEventArgs<RgfDynamicDictionary> args)
    {
        var rowData = args.Data;
        var attributes = (RgfDynamicDictionary)rowData["__attributes"];
        var attr = attributes.GetItemData("class").StringValue;
        if (attr != null)
        {
            args.Attributes.Add("class", $"rz-data-row {attr}");
        }
        attr = attributes.GetItemData("style").StringValue;
        if (attr != null)
        {
            args.Attributes.Add("style", attr);
        }
    }

    private void OnCellRender(DataGridCellRenderEventArgs<RgfDynamicDictionary> args)
    {
        var prop = EntityDesc.Properties.SingleOrDefault(e => e.Alias == args.Column.Property);
        if (prop?.ColPos > 0)
        {
            var rowData = args.Data;
            var attributes = (RgfDynamicDictionary)rowData["__attributes"];
            var attr = attributes.GetItemData($"class-{prop.Alias}").StringValue;
            if (attr != null)
            {
                args.Attributes.Add("class", attr);
            }
            attr = attributes.GetItemData($"style-{prop.Alias}").StringValue;
            if (attr != null)
            {
                args.Attributes.Add("style", attr);
            }
        }
    }

    private Task OnRowSelect(RgfDynamicDictionary arg) => _rgfGridRef.RowSelectHandlerAsync(arg);

    private Task OnRowDeselect(RgfDynamicDictionary arg) => _rgfGridRef.RowDeselectHandlerAsync(arg);

    private Task OnRowDoubleClick(DataGridRowMouseEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.OnRecordDoubleClickAsync(args.Data);
}
