using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Invoice.Tests;

public class PdfArabicSmokeTest
{
    private readonly ITestOutputHelper _output;

    public PdfArabicSmokeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Generate_Arabic_Pdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            "Invoice_Arabic_Smoke.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(24).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Item().Text("مرحبا بالعالم");
                    col.Item().Text("هذا اختبار للغة العربية");
                    col.Item().Text("Hello World");
                    col.Item().Text("أرقام: 1234567890");
                });
            });
        })
        .GeneratePdf(outputPath);

        _output.WriteLine("PDF generated at: " + outputPath);
        Assert.True(File.Exists(outputPath));
    }
}