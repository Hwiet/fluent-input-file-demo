using System.Diagnostics.CodeAnalysis;
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

    private async Task OnProgressChangeAsync(FluentInputFileEventArgs args)
    {
        if (File == null)
        {
            var bytes = new byte[args.Buffer.BytesRead];
            Array.Copy(args.Buffer.Data, bytes, args.Buffer.BytesRead);

            File = new FileData(name: args.Name, size: args.Size, data: bytes);
            await Task.Delay(1000);
        }
        else
        {
            var bytes = File.Data.Concat(args.Buffer.Data[..args.Buffer.BytesRead]).ToArray();

            File.Data = bytes;
            await Task.Delay(1000);
        }
    }

    private void OnCompleted(IEnumerable<FluentInputFileEventArgs> args)
    {
        ArgumentNullException.ThrowIfNull(File);

        File.UploadComplete = true;
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

    private record FileData
    {
        public required string Name { get; init; }

        public required long Size { get; set; }

        public byte[] Data { get; set; } = [];

        public bool UploadComplete { get; set; } = false;

        [SetsRequiredMembers]
        public FileData(string name, long size, byte[] data)
        {
            Name = name; Size = size; Data = data;
        }
    }
}
