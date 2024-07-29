using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Recrovit.RecroGridFramework.Abstraction.Contracts.Services;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Recrovit.RecroGridFramework.Client.Events;
using Recrovit.RecroGridFramework.Client.Handlers;

namespace Recrovit.RecroGridFramework.Client.Blazor.RadzenUI.Components;

public partial class GridComponent : ComponentBase, IDisposable
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
        GridParameters.EventDispatcher.Subscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
        GridParameters.EventDispatcher.Subscribe(RgfListEventKind.ColumnSettingsChanged, OnColumnSettingsChanged);
        _initialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_initialized)
        {
            if (EntityDesc.SortColumns.Any() && !_radzenGridRef.Sorts.Any())
            {
                foreach (var item in EntityDesc.SortColumns)
                {
                    _radzenGridRef.Sorts.Add(new SortDescriptor() { Property = item.Alias, SortOrder = item.Sort > 0 ? SortOrder.Ascending : SortOrder.Descending });
                }
            }
            await _jsRuntime.InvokeVoidAsync(RGFClientBlazorRadzenConfiguration.JsBlazorRadzenUiNamespace + ".Grid.initializeTable", _radzenGridRef.Element);
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

    private void OnColumnSettingsChanged(IRgfEventArgs<RgfListEventArgs> args) => Recreate();

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

    protected virtual Task OnCreateAttributes(IRgfEventArgs<RgfListEventArgs> ars)
    {
        _logger.LogDebug("CreateAttributes");
        var rowData = ars.Args.Data ?? throw new ArgumentException();
        foreach (var prop in EntityDesc.SortedVisibleColumns)
        {
            string? propClass = null;
            if (prop.FormType == PropertyFormType.CheckBox)
            {
                propClass = "rz-text-align-center";
            }
            else if (prop.ListType == PropertyListType.Numeric)
            {
                propClass = "rz-text-align-end";
            }
            if (propClass != null)
            {
                var attributes = rowData.GetOrNew<RgfDynamicDictionary>("__attributes");
                var propAttributes = attributes.GetOrNew<RgfDynamicDictionary>(prop.Alias);
                propAttributes.Set<string>("class", (old) => string.IsNullOrEmpty(old) ? propClass : $"{old.Trim()} {propClass}");
            }
        }
        return Task.CompletedTask;
    }

    private void OnRowRender(RowRenderEventArgs<RgfDynamicDictionary> args)
    {
        var rowData = args.Data;
        var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
        if (attributes != null)
        {
            var val = attributes.Get<string>("class");
            if (val != null)
            {
                args.Attributes.Add("class", $"rz-data-row {val}");
            }
            val = attributes.Get<string>("style");
            if (val != null)
            {
                args.Attributes.Add("style", val);
            }
        }
    }

    private void OnCellRender(DataGridCellRenderEventArgs<RgfDynamicDictionary> args)
    {
        var prop = EntityDesc.Properties.SingleOrDefault(e => e.Alias == args.Column.Property);
        if (prop?.ColPos > 0)
        {
            var rowData = args.Data;
            var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
            if (attributes != null)
            {
                var propAttributes = attributes.Get<RgfDynamicDictionary>(prop.Alias);
                if (propAttributes != null)
                {
                    var val = propAttributes.Get<string>("class");
                    if (val != null)
                    {
                        args.Attributes.Add("class", val);
                    }
                    val = propAttributes.Get<string>("style");
                    if (val != null)
                    {
                        args.Attributes.Add("style", val);
                    }
                }
            }
        }
    }

    private Task OnRowSelect(RgfDynamicDictionary arg) => _rgfGridRef.RowSelectHandlerAsync(arg);

    private Task OnRowDeselect(RgfDynamicDictionary arg) => _rgfGridRef.RowDeselectHandlerAsync(arg);

    private Task OnRowDoubleClick(DataGridRowMouseEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.OnRecordDoubleClickAsync(args.Data);

    public void Dispose()
    {
        GridParameters.EventDispatcher.Unsubscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
        GridParameters.EventDispatcher.Unsubscribe(RgfListEventKind.ColumnSettingsChanged, OnColumnSettingsChanged);
    }
}