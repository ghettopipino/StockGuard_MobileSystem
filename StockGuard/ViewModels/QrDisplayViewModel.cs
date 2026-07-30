using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using StockGuard.Services;

namespace StockGuard.ViewModels
{
    [QueryProperty(nameof(ToolId), "toolId")]
    [QueryProperty(nameof(ToolName), "toolName")]
    [QueryProperty(nameof(Status), "status")]
    [QueryProperty(nameof(CatalogName), "catalogName")]
    public class QrDisplayViewModel : BaseViewModel
    {
        private readonly ThemeService _theme;

        // ── Query Properties ──────────────────────────────────────
        private string _toolId = string.Empty;
        public string ToolId
        {
            get => _toolId;
            set => SetProperty(ref _toolId, value);
        }

        private string _toolName = string.Empty;
        public string ToolName
        {
            get => _toolName;
            set => SetProperty(ref _toolName, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _catalogName = string.Empty;
        public string CatalogName
        {
            get => _catalogName;
            set => SetProperty(ref _catalogName, value);
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand ShareCommand { get; }

        // ── Constructor ───────────────────────────────────────────
        public QrDisplayViewModel(ThemeService theme)
        {
            _theme = theme;

            GoBackCommand = new Command(async () =>
                await Shell.Current.GoToAsync(".."));

            ShareCommand = new Command(
                async () => await ShareQrAsync());
        }

        // ── Share QR ──────────────────────────────────────────────
        private async Task ShareQrAsync()
        {
            await Share.Default.RequestAsync(
                new ShareTextRequest
                {
                    Title = $"QR Code — {ToolName} ({ToolId})",
                    Text =
                        $"StockGuard Tool QR Code\n\n" +
                        $"Tool Name: {ToolName}\n" +
                        $"Tool ID:   {ToolId}\n" +
                        $"Catalog:   {CatalogName}\n\n" +
                        $"Scan this ID in the StockGuard app: {ToolId}"
                });
        }
    }
}
