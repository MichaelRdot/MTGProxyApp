using MTGProxyApp.Containers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Infrastructure.Unit;

namespace MTGProxyApp.Services;

public class QuestPdfService
{
    private List<byte[]> _cardsPrints = new();
    private bool _blackCorners = true;
    private bool _borders = true;
    private int _cardsDone;
    
    private readonly float _cardHeight = UnitExtensions.ToPoints(88f, Millimetre);
    private readonly float _cardWidth = UnitExtensions.ToPoints(63f, Millimetre);
    private const float CrossLength = 8f;
    private const float CrossThickness = 1f;

    public Task<byte[]> CreatePdf(List<byte[]> cardsPrints, bool blackCorners, bool borders)
    {
        _cardsPrints = cardsPrints;
        _blackCorners = blackCorners;
        _borders = borders;
        _cardsDone = 0;
        var doc = Document.Create(doc =>
        {
            var paddingTop = (PageSizes.Letter.Height - _cardHeight * 3) / 2;
            while (_cardsDone != _cardsPrints.Count)
                doc.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(0);
                    page.Background().AlignCenter().AlignMiddle().CutLine(1, PageSizes.Letter.Width,
                        PageSizes.Letter.Height, _cardWidth, _cardHeight);

                    page.Content().AlignCenter().PaddingTop(paddingTop).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(_cardWidth);
                            columns.ConstantColumn(_cardWidth);
                            columns.ConstantColumn(_cardWidth);
                        });
                        for (var i = 0; i < 9 && _cardsDone < _cardsPrints.Count; i++)
                        {
                            table.Cell().Element(card =>
                            {
                                card
                                    .Height(_cardHeight)
                                    .Width(_cardWidth)
                                    .Layers(layers =>
                                    {
                                        layers.PrimaryLayer()
                                            .Border(_borders ? 1 : 0, Colors.Black)
                                            .Background(_blackCorners ? Colors.Black : Colors.White)
                                            .Image(_cardsPrints[_cardsDone])
                                            .WithCompressionQuality(ImageCompressionQuality.Best)
                                            .WithRasterDpi(300);
                                        if (!_blackCorners) return;
                                        layers.Layer().Element(e =>
                                            e.AlignLeft().AlignTop().CornerCross(CrossLength, CrossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignRight().AlignTop().CornerCross(CrossLength, CrossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignLeft().AlignBottom().CornerCross(CrossLength, CrossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignRight().AlignBottom().CornerCross(CrossLength, CrossThickness));
                                    });
                            });
                            _cardsDone++;
                        }
                    });
                });
        });
        return Task.FromResult(doc.GeneratePdf());
    }

    private static class UnitExtensions
    {
        private const float InchToCentimetre = 2.54f;
        private const float InchToPoints = 72;

        public static float ToPoints(float value, Unit unit)
        {
            return value * GetConversionFactor();

            float GetConversionFactor()
            {
                return unit switch
                {
                    Point => 1,
                    Meter => 100 / InchToCentimetre * InchToPoints,
                    Centimetre => 1 / InchToCentimetre * InchToPoints,
                    Millimetre => 0.1f / InchToCentimetre * InchToPoints,
                    Feet => 12 * InchToPoints,
                    Inch => InchToPoints,
                    Mil => InchToPoints / 1000f,
                    _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
                };
            }
        }
    }
}