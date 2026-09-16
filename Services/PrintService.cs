using System.Net.Sockets;
using System.Text;
using control_lavanderia.Components.Data;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Models;

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
                e.PrintLine("DESCRIPCION".PadRight(22) + "CANTIDAD".PadRight(18) + "SUBTOTAL"), //22 18 8

            };

            request.Order.OrderDetails?.ToList().ForEach(od =>
            {
                ticketParts.Add(ByteSplicer.Combine(
                    e.LeftAlign(),
                    e.Print(od.Type?.PadRight(22) ?? "N/A".PadRight(22)),
                    e.CenterAlign(),
                    e.Print(od.Quantity.ToString().PadRight(18) ?? "N/A".PadRight(22)),
                    e.RightAlign(),
                    e.PrintLine($"{od.Total:C}".PadLeft(8))
                ));
            });

            ticketParts.Add(e.PrintLine($"Total: {Total:C}"));
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