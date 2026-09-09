using System.Net.Sockets;
using System.Text;
using control_lavanderia.Components.Data;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using Microsoft.EntityFrameworkCore;
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

            _printerIp = "http://" + printerIpConfig.Value;
            return true;
        }

        public async Task<bool> PrintOrder(PrintOrderRequest request)
        {
            if (!await IsPrinterConfigured())
                return false;
            using var client = new TcpClient();
            await client.ConnectAsync(_printerIp!, 9100); // Assuming the printer listens on port 9100
            var e = new EPSON();
            using var stream = client.GetStream();
            var Total = request.Order.OrderDetails?.Sum(od => od.Total) ?? 0;
            var ticketParts = new List<byte[]>{
                e.CenterAlign(),
                e.SetStyles(PrintStyle.DoubleHeight | PrintStyle.DoubleWidth),
                e.PrintLine(request.BusinessTittle),
                e.SetStyles(PrintStyle.None),
                e.LeftAlign(),
                e.PrintLine($"RFC: {request.RFC}"),
                e.PrintLine($"{request.BusinessRegimen}"),
                e.PrintLine(request.BusinessAddress),
                e.PrintLine("Telefono: " + request.BusinessPhone),
                e.PrintLine(request.BusinessDays),
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
                    e.Print(od.Description?.PadRight(22) ?? "N/A"),
                    e.CenterAlign(),
                    e.Print(od.Quantity.ToString().PadRight(18) ?? "N/A"),
                    e.RightAlign(),
                    e.PrintLine($"${od.Total:C}".PadLeft(8))
                ));
            });

            ticketParts.Add(e.PrintLine($"Total: ${Total:C}"));
            ticketParts.Add(e.PrintLine("------------------------------"));
            ticketParts.Add(e.CenterAlign());
            ticketParts.Add(e.PrintLine(request.BusinessMessage));


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
        public required string BusinessTittle { get; set; }
        public required string BusinessAddress { get; set; }
        public required string BusinessPhone { get; set; }
        public required string BusinessDays { get; set; }
        public required string RFC { get; set; }
        public required string BusinessRegimen { get; set; }

        // bottom of the receipt

        public required string BusinessMessage { get; set; }
    }
}