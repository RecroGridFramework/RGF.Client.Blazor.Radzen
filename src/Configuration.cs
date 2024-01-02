using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Client.Blazor.RadzenUI.Components;
using System.Reflection;

namespace Recrovit.RecroGridFramework.Client.Blazor.RadzenUI;

public static class RGFClientBlazorRadzenConfiguration
{
    public static async Task InitializeRgfRadzenUIAsync(this IServiceProvider serviceProvider, string themeName = "default", bool loadResources = true)
    {
        RgfBlazorConfiguration.RegisterComponent<MenuComponent>(RgfBlazorConfiguration.ComponentType.Menu);
        RgfBlazorConfiguration.RegisterComponent<DialogComponent>(RgfBlazorConfiguration.ComponentType.Dialog);
        RgfBlazorConfiguration.RegisterEntityComponent<EntityComponent>(string.Empty);

        if (loadResources)
        {
            var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
            await LoadResourcesAsync(jsRuntime, themeName);
        }
    }

    public static async Task LoadResourcesAsync(IJSRuntime jsRuntime, string themeName)
    {
        var libName = Assembly.GetExecutingAssembly().GetName().Name;

        await jsRuntime.InvokeVoidAsync("import", $"{RgfClientConfiguration.AppRootUrl}_content/Radzen.Blazor/Radzen.Blazor.js");
        await jsRuntime.InvokeVoidAsync("Recrovit.LPUtils.AddStyleSheetLink", $"{RgfClientConfiguration.AppRootUrl}_content/Radzen.Blazor/css/{themeName}.css", false, RadzenThemeId);
        await jsRuntime.InvokeVoidAsync("import", $"{RgfClientConfiguration.AppRootUrl}_content/{libName}/scripts/recrovit-rgf-blazor-radzen.js");
    }

    public static async Task UnloadResourcesAsync(IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{RadzenThemeId}')?.remove();");
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementsByTagName('body')[0].removeAttribute('class');");
    }

    public static readonly string RadzenThemeId = "radzen-theme";
    public static readonly string JsBlazorRadzenUiNamespace = "Recrovit.RGF.Blazor.RadzenUI";
}
