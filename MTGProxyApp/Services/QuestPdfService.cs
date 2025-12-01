using static QuestPDF.Infrastructure.Unit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MTGProxyApp.Dtos;
using QuestPDFPlayground;

namespace MTGProxyApp.Services;

public class QuestPdfService()
{
    List<CardDto> _cards;
    float _cardWidth = UnitExtensions.ToPoints(63f, Millimetre);
    float _cardHeight = UnitExtensions.ToPoints(88f, Millimetre);
    float _crossThickness = 1f;
    float _crosslength = 8f;
    bool _blackCorners = true;
    bool _borders = true;

    int _cardsDone;

    public void CreatePdf(List<CardDto?> cards, bool blackCorners, bool borders)
    {
        _cards = cards;
        _blackCorners = blackCorners;
        _borders = borders;
        Document.Create(doc =>
        {
            var paddingTop = (PageSizes.Letter.Height - _cardHeight * 3) / 2;
            while (_cardsDone != _cards.Count)
            {
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
                        for (int i = 0; i < 9 && _cardsDone < _cards.Count; i++)
                        {
                            var countOuter = i;
                            table.Cell().Element(card =>
                            {
                                var countInner = countOuter;
                                card
                                    .Height(_cardHeight)
                                    .Width(_cardWidth)
                                    .Layers(layers =>
                                    {
                                        layers.PrimaryLayer()
                                            .Border(_borders ? 1 : 0, Colors.Black)
                                            .Background(_blackCorners ? Colors.Black : Colors.White)
                                            .Image(_cards[_cardsDone].ImageUris.Png.ToString())
                                            .WithCompressionQuality(ImageCompressionQuality.Best)
                                            .WithRasterDpi(300);
                                        if (!_blackCorners) return;
                                        layers.Layer().Element(e =>
                                            e.AlignLeft().AlignTop().CornerCross(_crosslength, _crossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignRight().AlignTop().CornerCross(_crosslength, _crossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignLeft().AlignBottom().CornerCross(_crosslength, _crossThickness));
                                        layers.Layer().Element(e =>
                                            e.AlignRight().AlignBottom().CornerCross(_crosslength, _crossThickness));
                                    });
                            });
                            _cardsDone++;
                        }
                    });
                });
            }
        }).GeneratePdf();
    }

    internal static class UnitExtensions
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