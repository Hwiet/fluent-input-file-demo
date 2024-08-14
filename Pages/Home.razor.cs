using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace FluentInputFileDemo.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private IJSObjectReference? Module { get; set; }

    private FileData? File { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/Home.razor.js");
        }
    }

    private void OnUpload(FluentInputFileEventArgs args)
    {
        var data = new byte[args.Buffer.BytesRead];
        Array.Copy(args.Buffer.Data, data, args.Buffer.BytesRead);

        File = new FileData(Name: args.Name, Data: data);
    }

    // It would be better to show a thumbnail preview for files, but
    // allow users to download the uploaded file as an alternative to
    // previewing the file on the same webpage.
    private async Task DownloadFileAsync()
    {
        if (File != null && Module != null)
        {
            var fileStream = new MemoryStream(File.Data);
            using var streamRef = new DotNetStreamReference(fileStream);

            await Module.InvokeVoidAsync("downloadFileFromStream", File.Name, streamRef);
        }
    }

    private void ClearFile()
    {
        File = null;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (Module != null)
        {
            await Module.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    private record FileData(string Name, byte[] Data);
}
