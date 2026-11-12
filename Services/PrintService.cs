using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using control_lavanderia.Components.Data;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Models;
using Radzen.Blazor.Rendering;

namespace Services
{
    public class PrintService(IDbContextFactory<LavanderiaDbContext> context)
    {
        private string? _printerIp;

        public async Task<bool> IsPrinterConfigured()
        {
            if (!string.IsNullOrWhiteSpace(_printerIp) && Uri.IsWellFormedUriString(_printerIp, UriKind.Absolute))
                return true;
            await using var _context = await context.CreateDbContextAsync();
            var printerIpConfig = await _context.Configurations
                .FirstOrDefaultAsync(config => config.Key == "PrinterIp");

            if (printerIpConfig is null || string.IsNullOrWhiteSpace(printerIpConfig.Value))
                return false;

            _printerIp = printerIpConfig.Value;
            return true;
        }

        public async Task<bool> PrintOrder(PrintOrderRequest request)
        {

            if (!await IsPrinterConfigured())
                return false;
            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(_printerIp!, 9100); // Assuming the printer listens on port 9100
            }
            catch (SocketException)
            {
                Console.WriteLine("Error en la conexión con la impresora: " + _printerIp);
                return false;
            }
            var e = new EPSON();
            using var stream = client.GetStream();
            string[] businessKeys =
            [
                "BusinessTittle",
                "BusinessRFC",
                "BusinessRegimen",
                "BusinessAddress",
                "BusinessPhone",
                "BusinessDays",
                "BusinessMessage"
            ];
            await using var _context = await context.CreateDbContextAsync();
            Dictionary<string, string> configurations = await _context.Configurations
                .Where(conf => businessKeys.Contains(conf.Key))
                .ToDictionaryAsync(conf => conf.Key, conf => conf.Value);

            // Devuelve el valor de la configuración o "" si la key no existe.
            string GetConfig(string key) => configurations.GetValueOrDefault(key) ?? string.Empty;

            var Total = request.Order.OrderDetails?.Sum(od => od.Total) ?? 0;
            var ticketParts = new List<byte[]>{
                e.CenterAlign(),
                e.SetStyles(PrintStyle.DoubleHeight | PrintStyle.DoubleWidth),
                e.PrintLine(GetConfig("BusinessTittle")),
                e.SetStyles(PrintStyle.None),
                e.LeftAlign(),
                e.PrintLine($"RFC: {GetConfig("BusinessRFC")}"),
                e.PrintLine($"{GetConfig("BusinessRegimen")}"),
                e.PrintLine(GetConfig("BusinessAddress")),
                e.PrintLine("Telefono: " + GetConfig("BusinessPhone")),
                e.PrintLine(GetConfig("BusinessDays")),
                e.PrintLine("------------------------------"),
                e.PrintLine($"Folio: {request.Order.Folio}"),
                e.PrintLine($"Recibido: {request.Order.ReceivedDate:dd/MM/yyyy}"),
                request.Order.DeliveryDate.HasValue ? e.PrintLine($"Entrega: {request.Order.DeliveryDate:dd/MM/yyyy}") : [],
                request.Order.CancelDate.HasValue ? e.PrintLine($"Cancelado: {request.Order.CancelDate:dd/MM/yyyy}") : [],
                e.SetStyles(PrintStyle.DoubleHeight | PrintStyle.DoubleWidth),
                e.PrintLine($"Cliente: {request.Order.Client?.Name ?? "N/A"}"),
                e.SetStyles(PrintStyle.None),
                e.PrintLine("------------------------------"),
                e.LeftAlign(),
                e.PrintLine("DESCRIPCION".PadRight(20) + "CANTIDAD".PadLeft(9) + "UNITARIO".PadLeft(10) + "SUBTOTAL".PadLeft(9)), //22 18 8 //nuevo 20, 9, 10, 9

            };
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            request.Order.OrderDetails?.ToList().ForEach(od =>
            {
                ticketParts.Add(ByteSplicer.Combine(
                    e.LeftAlign(),
                    GetServicioStringBytes(od, e, textInfo, 20),
                    GetCantidadStringBytes(od, e, textInfo, 9),
                    e.Print(od.Price.ToString().PadLeft(10) ?? "N/A".PadRight(10)),
                    e.PrintLine($"{od.Total:C}".PadLeft(9))
                ));
            });

            ticketParts.Add(e.PrintLine($"Total: {Total:C}".PadLeft(48)));
            ticketParts.Add(e.PrintLine("------------------------------"));
            ticketParts.Add(e.CenterAlign());
            ticketParts.Add(e.PrintLine(GetConfig("BusinessMessage")));
            ticketParts.Add(e.FullCutAfterFeed(2));


            Console.WriteLine("Impresión lista");
            var ticket = ByteSplicer.Combine([.. ticketParts]);
            await stream.WriteAsync(ticket);
            await stream.FlushAsync();

            return true; // Return true if printing was successful
        }

        public async void RecheckPrinterConfiguration()
        {
            _printerIp = null;
            await IsPrinterConfigured();
        }

        private byte[] GetServicioStringBytes(OrderDetail od, EPSON e, TextInfo textInfo, int pad = 20)
        {
            if (od.Type.Equals("individual") && !string.IsNullOrWhiteSpace(od.Description))
            {
                return e.Print(od.Description.PadRight(pad) ?? "N/A".PadRight(pad));
            }
            else if (od.Type.Equals("lavado"))
            {
                return e.Print("Ropa".PadRight(pad) ?? "N/A".PadRight(pad));
            }
            return e.Print(textInfo.ToTitleCase(od.Type?.PadRight(pad) ?? "N/A".PadRight(pad)));
        }

        private byte[] GetCantidadStringBytes(OrderDetail od, EPSON e, TextInfo textInfo, int pad = 9)
        {
            if (od.Quantity == 0)
            {
                return e.Print("Pendiente".PadLeft(9));
            }
            return e.Print(od.Quantity.ToString().PadLeft(9) ?? "N/A".PadRight(9));
        }
    }

    public class PrintOrderRequest
    {
        public required Order Order { get; set; }

        // top of the receipt
        /*         public required string BusinessTittle { get; set; }
                public required string BusinessAddress { get; set; }
                public required string BusinessPhone { get; set; }
                public required string BusinessDays { get; set; }
                public required string RFC { get; set; }
                public required string BusinessRegimen { get; set; }

                // bottom of the receipt

                public required string BusinessMessage { get; set; } */
    }
}