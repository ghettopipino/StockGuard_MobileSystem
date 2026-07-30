using QRCoder;

namespace StockGuard.Web.Services
{
    public class QrCodeService
    {
        public string GenerateQrCodeBase64(
            string content)
        {
            try
            {
                var qrGenerator =
                    new QRCodeGenerator();

                var qrCodeData =
                    qrGenerator.CreateQrCode(
                        content,
                        QRCodeGenerator.ECCLevel.Q);

                // ✅ QRCoder 1.7.0 uses this
                var qrCode =
                    new PngByteQRCode(qrCodeData);

                var qrCodeBytes =
                    qrCode.GetGraphic(
                        pixelsPerModule: 10,
                        darkColorRgba:
                            new byte[] { 0, 0, 0, 255 },
                        lightColorRgba:
                            new byte[] { 255, 255, 255, 255 });

                return Convert.ToBase64String(
                    qrCodeBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"QR error: {ex.Message}");
                return string.Empty;
            }
        }

        public byte[] GenerateQrCodeBytes(
            string content)
        {
            try
            {
                var qrGenerator =
                    new QRCodeGenerator();

                var qrCodeData =
                    qrGenerator.CreateQrCode(
                        content,
                        QRCodeGenerator.ECCLevel.Q);

                var qrCode =
                    new PngByteQRCode(qrCodeData);

                return qrCode.GetGraphic(
                    pixelsPerModule: 10,
                    darkColorRgba:
                        new byte[] { 0, 0, 0, 255 },
                    lightColorRgba:
                        new byte[] { 255, 255, 255, 255 });
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}