using System.Globalization;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Payroll;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ate.Infrastructure.Services;

/// <summary>PDF de los cortes de nómina. Los de RRHH solo muestran cédula, nombre y total; nunca los pedidos.</summary>
public class PayrollPdfRenderer : IPayrollPdfRenderer, ISingletonDependency
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");
    private const string Brand = "#1F7A4D";

    private readonly IDateTimeProvider _dateTimeProvider;

    public PayrollPdfRenderer(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public byte[] RenderEmployeeSettlement(PayrollSettlement settlement, string companyName) =>
        Render(
            $"Detalle de consumo · {TypeLabel(settlement.Type)}",
            [("Empresa", companyName), ("Colaborador", settlement.EmployeeName), ("Cédula", settlement.EmployeeDocument), ("Período", Period(settlement))],
            table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(70); c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(3); c.ConstantColumn(70); });
                Header(table, "Fecha", "Restaurante", "Comida", "Detalle", "Monto");
                foreach (var line in settlement.Lines)
                {
                    Cell(table, ShortDate(line.ServiceDate));
                    Cell(table, line.RestaurantName);
                    Cell(table, MealLabel(line.MealType));
                    Cell(table, string.Join(", ", line.Items.Select(i => i.Name)));
                    Cell(table, Money(line.Amount), right: true);
                }
            },
            settlement.TotalAmount,
            $"{settlement.OrderCount} pedidos. Este monto se descuenta de tu {(settlement.Type == SettlementType.Termination ? "liquidación" : "sueldo")}.");

    public byte[] RenderCompanyMonthly(string companyName, string month, IReadOnlyList<PayrollSettlement> settlements) =>
        Render(
            "Descuentos de nómina · Mensual",
            [("Empresa", companyName), ("Mes", MonthLabel(month)), ("Colaboradores", settlements.Count.ToString(Es))],
            table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(90); c.RelativeColumn(); c.ConstantColumn(90); });
                Header(table, "Cédula", "Colaborador", "Total a descontar");
                foreach (var s in settlements.OrderBy(s => s.EmployeeName, StringComparer.Create(Es, true)))
                {
                    Cell(table, s.EmployeeDocument);
                    Cell(table, s.EmployeeName);
                    Cell(table, Money(s.TotalAmount), right: true);
                }
            },
            settlements.Sum(s => s.TotalAmount),
            "Montos a descontar del sueldo de cada colaborador por su consumo en Ate.");

    public byte[] RenderCompanyTermination(string companyName, PayrollSettlement settlement) =>
        Render(
            "Descuento de nómina · Liquidación",
            [("Empresa", companyName), ("Colaborador", settlement.EmployeeName), ("Cédula", settlement.EmployeeDocument), ("Período", Period(settlement)), ("Fecha de salida", ShortDate(settlement.PeriodEnd))],
            null,
            settlement.TotalAmount,
            "Monto a descontar de la liquidación del colaborador por su consumo en Ate.");

    private byte[] Render(
        string title,
        IReadOnlyList<(string Label, string Value)> facts,
        Action<TableDescriptor>? table,
        decimal total,
        string note)
    {
        var generatedAt = _dateTimeProvider.BusinessNow.ToString("dd/MM/yyyy HH:mm", Es);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

            page.Header().Column(col =>
            {
                col.Item().Text("Ate").FontSize(20).Bold().FontColor(Brand);
                col.Item().PaddingTop(4).Text(title).FontSize(14).SemiBold();
            });

            page.Content().PaddingVertical(16).Column(col =>
            {
                col.Spacing(12);
                col.Item().Column(f =>
                {
                    foreach (var (label, value) in facts)
                    {
                        f.Item().Text(t => { t.Span($"{label}: ").SemiBold(); t.Span(value); });
                    }
                });

                if (table != null)
                {
                    col.Item().Table(table);
                }

                col.Item().AlignRight().Text(t =>
                {
                    t.Span("Total: ").FontSize(12).SemiBold();
                    t.Span(Money(total)).FontSize(12).Bold().FontColor(Brand);
                });
                col.Item().Text(note).FontColor(Colors.Grey.Darken1);
            });

            page.Footer().Text($"Generado el {generatedAt}").FontSize(8).FontColor(Colors.Grey.Medium);
        })).GeneratePdf();
    }

    private static void Header(TableDescriptor table, params string[] titles) =>
        table.Header(h =>
        {
            for (var i = 0; i < titles.Length; i++)
            {
                var cell = h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4);
                if (i == titles.Length - 1) cell.AlignRight().Text(titles[i]).SemiBold();
                else cell.Text(titles[i]).SemiBold();
            }
        });

    private static void Cell(TableDescriptor table, string text, bool right = false)
    {
        var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4);
        if (right) cell.AlignRight().Text(text);
        else cell.Text(text);
    }

    private static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

    private static string ShortDate(string serviceDate) => SettlementRules.Parse(serviceDate).ToString("dd/MM/yyyy", Es);

    private static string Period(PayrollSettlement s) => $"{ShortDate(s.PeriodStart)} al {ShortDate(s.PeriodEnd)}";

    private static string MonthLabel(string month) =>
        DateOnly.ParseExact(month + "-01", SettlementRules.DateFormat, CultureInfo.InvariantCulture).ToString("MMMM yyyy", Es);

    private static string TypeLabel(SettlementType type) => type == SettlementType.Termination ? "Liquidación" : "Mensual";

    private static string MealLabel(MealType mealType) => mealType switch
    {
        MealType.Breakfast => "Desayuno",
        MealType.Lunch => "Almuerzo",
        MealType.Dinner => "Merienda",
        _ => mealType.ToString()
    };
}
