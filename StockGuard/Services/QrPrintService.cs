using System.Net;
using System.Text;
using QRCoder;
using StockGuard.Models;

#if ANDROID
using Android.Content;
using Android.Print;
using Android.Webkit;

using AndroidWebView = Android.Webkit.WebView;
#endif

namespace StockGuard.Services
{
    public class QrPrintService
    {
        // ─────────────────────────────────────────────────────
        // PRINT QR LABELS
        // ─────────────────────────────────────────────────────

        public async Task PrintLabelsAsync(
            IEnumerable<Tool> tools,
            string catalogName)
        {
            var toolList = tools
                .Where(t =>
                    t != null &&
                    !t.IsDeleted &&
                    !string.IsNullOrWhiteSpace(t.ToolId))
                .OrderBy(t => t.ToolId)
                .ToList();


            if (toolList.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Equipment",
                    "There are no equipment QR labels to print.",
                    "OK");

                return;
            }


            try
            {
                var html =
                    BuildPrintHtml(
                        toolList,
                        catalogName);


#if ANDROID

                await PrintOnAndroidAsync(
                    html,
                    catalogName);

#else

                await Shell.Current.DisplayAlert(
                    "Printing",
                    "QR label printing is currently available " +
                    "on the Android version of StockGuard.",
                    "OK");

#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"QR Print error: {ex}");


                await Shell.Current.DisplayAlert(
                    "Print Error",
                    "Could not prepare the QR labels.\n\n" +
                    ex.Message,
                    "OK");
            }
        }


        // ─────────────────────────────────────────────────────
        // BUILD PRINTABLE HTML
        // ─────────────────────────────────────────────────────

        private static string BuildPrintHtml(
            List<Tool> tools,
            string catalogName)
        {
            var html =
                new StringBuilder();


            html.Append(
                """
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">

                    <style>

                        @page {
                            size: A4;
                            margin: 10mm;
                        }

                        * {
                            box-sizing: border-box;
                        }

                        body {
                            margin: 0;
                            padding: 0;
                            font-family: Arial, Helvetica, sans-serif;
                            color: #111111;
                            background: white;
                        }

                        .header {
                            margin-bottom: 12px;
                        }

                        .brand {
                            font-size: 20px;
                            font-weight: bold;
                            margin-bottom: 3px;
                        }

                        .subtitle {
                            font-size: 11px;
                            color: #555555;
                        }

                        .count {
                            font-size: 10px;
                            color: #777777;
                            margin-top: 2px;
                        }

                        .grid {
                            display: grid;
                            grid-template-columns: 1fr 1fr;
                            gap: 8px;
                        }

                        .label {
                            border: 1px solid #d5d5d5;
                            min-height: 72mm;

                            padding: 8px;

                            display: flex;
                            flex-direction: column;

                            justify-content: flex-start;
                            align-items: center;

                            text-align: center;

                            page-break-inside: avoid;
                            break-inside: avoid;
                        }

                        .label-brand {
                            font-size: 11px;
                            font-weight: bold;
                            margin-bottom: 8px;
                        }

                        .qr {
                            width: 36mm;
                            height: 36mm;
                            object-fit: contain;
                        }

                        .tool-id {
                            margin-top: 8px;

                            font-size: 14px;
                            font-weight: bold;
                        }

                        .tool-name {
                            margin-top: 4px;

                            font-size: 10px;
                            color: #444444;
                        }

                    </style>
                </head>

                <body>
                """);


            // ─────────────────────────────────────────────────
            // PAGE HEADER
            // ─────────────────────────────────────────────────

            html.Append(
                $"""
                <div class="header">

                    <div class="brand">
                        STOCKGUARD
                    </div>

                    <div class="subtitle">
                        {Encode(catalogName)} · QR Equipment Labels
                    </div>

                    <div class="count">
                        {tools.Count} physical equipment label(s)
                    </div>

                </div>

                <div class="grid">
                """);


            // ─────────────────────────────────────────────────
            // EQUIPMENT LABELS
            // ─────────────────────────────────────────────────

            foreach (var tool in tools)
            {
                var qrBytes =
                    GenerateQrCode(
                        tool.ToolId);


                var qrBase64 =
                    Convert.ToBase64String(
                        qrBytes);


                html.Append(
                    $"""
                    <div class="label">

                        <div class="label-brand">
                            STOCKGUARD
                        </div>

                        <img
                            class="qr"
                            src="data:image/png;base64,{qrBase64}" />

                        <div class="tool-id">
                            {Encode(tool.ToolId)}
                        </div>

                        <div class="tool-name">
                            {Encode(tool.ToolName)}
                        </div>

                    </div>
                    """);
            }


            html.Append(
                """
                </div>

                </body>
                </html>
                """);


            return html.ToString();
        }


        // ─────────────────────────────────────────────────────
        // GENERATE QR IMAGE
        // ─────────────────────────────────────────────────────

        private static byte[] GenerateQrCode(
            string toolId)
        {
            using var qrGenerator =
                new QRCodeGenerator();


            using var qrData =
                qrGenerator.CreateQrCode(
                    toolId,
                    QRCodeGenerator.ECCLevel.Q);


            var qrCode =
                new PngByteQRCode(
                    qrData);


            return qrCode.GetGraphic(
                pixelsPerModule: 20);
        }


        // ─────────────────────────────────────────────────────
        // HTML ENCODING
        // ─────────────────────────────────────────────────────

        private static string Encode(
            string? value)
        {
            return WebUtility.HtmlEncode(
                value ?? string.Empty);
        }


#if ANDROID

        // ─────────────────────────────────────────────────────
        // ANDROID PRINTING
        // ─────────────────────────────────────────────────────

        private static async Task PrintOnAndroidAsync(
            string html,
            string catalogName)
        {
            var activity =
                Platform.CurrentActivity;


            if (activity == null)
            {
                throw new InvalidOperationException(
                    "Android activity is not available.");
            }


            var taskSource =
                new TaskCompletionSource<bool>();


            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    var webView =
                         new AndroidWebView(activity);


                    webView.Settings.JavaScriptEnabled = false;


                    webView.SetWebViewClient(
                        new PrintWebViewClient(
                            activity,
                            webView,
                            catalogName,
                            taskSource));


                    webView.LoadDataWithBaseURL(
                        null,
                        html,
                        "text/html",
                        "UTF-8",
                        null);
                });


            await taskSource.Task;
        }


        // ─────────────────────────────────────────────────────
        // WAIT UNTIL HTML IS READY, THEN OPEN PRINT DIALOG
        // ─────────────────────────────────────────────────────

        private sealed class PrintWebViewClient :
            WebViewClient
        {
            private readonly Android.App.Activity _activity;

            private readonly AndroidWebView _webView;

            private readonly string _catalogName;

            private readonly TaskCompletionSource<bool>
                _taskSource;

            private bool _printStarted;


            
                public PrintWebViewClient(
                    Android.App.Activity activity,
                    AndroidWebView webView,
                    string catalogName,
                    TaskCompletionSource<bool> taskSource)
            {
                _activity = activity;
                _webView = webView;
                _catalogName = catalogName;
                _taskSource = taskSource;
            }


            public override void OnPageFinished(
                AndroidWebView? view,
                string? url)
            {
                base.OnPageFinished(
                    view,
                    url);


                if (_printStarted)
                    return;


                _printStarted = true;


                try
                {
                    var printManager =
                        _activity.GetSystemService(
                            Context.PrintService)
                        as PrintManager;


                    if (printManager == null)
                    {
                        throw new InvalidOperationException(
                            "Android printing service " +
                            "is not available.");
                    }


                    var safeCatalogName =
                        string.IsNullOrWhiteSpace(
                            _catalogName)
                            ? "Equipment"
                            : _catalogName;


                    var jobName =
                        $"StockGuard - " +
                        $"{safeCatalogName} QR Labels";


                    var printAdapter =
                        _webView
                            .CreatePrintDocumentAdapter(
                                jobName);


                    var printAttributes =
                        new PrintAttributes
                            .Builder()
                            .SetMediaSize(
                                PrintAttributes
                                    .MediaSize
                                    .IsoA4)
                            .Build();


                    printManager.Print(
                        jobName,
                        printAdapter,
                        printAttributes);


                    _taskSource.TrySetResult(
                        true);
                }
                catch (Exception ex)
                {
                    _taskSource.TrySetException(
                        ex);
                }
            }
        }

#endif
    }
}