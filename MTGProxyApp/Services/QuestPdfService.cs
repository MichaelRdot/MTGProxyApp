using MTGProxyApp.Containers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Infrastructure.Unit;

namespace MTGProxyApp.Services;

public class QuestPdfService(IWebHostEnvironment env)
{
    private readonly float _cardHeight = UnitExtensions.ToPoints(88f, Millimetre);
    private readonly float _cardWidth = UnitExtensions.ToPoints(63f, Millimetre);
    private readonly float _paddingTop = (PageSizes.Letter.Height - UnitExtensions.ToPoints(88f, Millimetre) * 3) / 2;

    private const float CrossLength = 8f;
    private const float CrossThickness = 1f; 

    public Task<byte[]> CreatePdf(List<List<byte[]>> cardsPrints, bool blackCorners, bool borders, bool printFlipCardsSeparate)
    {
        var doc = Document.Create(doc =>
        {
            var cardPages = cardsPrints[0].Chunk(9).Select(chunk => chunk.ToList()).ToList();
            foreach (var page in cardPages)
            {
                doc.Page(MakePage(page, blackCorners, borders));
            }
            if (printFlipCardsSeparate && cardsPrints[1].Count > 0)
            {
                cardPages = cardsPrints[1].Chunk(9).Select(chunk => chunk.ToList()).ToList();
                foreach (var page in cardPages)
                {
                    doc.Page(MakePage(page, blackCorners, borders));
                }
                cardPages = cardsPrints[2].Chunk(9).Select(chunk => chunk.ToList()).ToList();
                foreach (var page in cardPages)
                {
                    var pageLineChunkList = page.Chunk(3).Select(chunk => chunk.ToList()).ToList();
                    for (var chunkIndex = 0; chunkIndex < pageLineChunkList.Count; chunkIndex++)
                    {
                        var pageLineChunk = pageLineChunkList[chunkIndex];
                        var tempPageLineChunk = new List<byte[]>();
                        for (var i = 0; i < 3 - pageLineChunk.Count; i++)
                            tempPageLineChunk.
                                Add(File.ReadAllBytes(Path.Combine(env.WebRootPath, "Images", "Transparent.png")));
                        for (var i = pageLineChunk.Count - 1; i >= 0; i--)
                        {
                            tempPageLineChunk.Add(pageLineChunk[i]);
                        }
                        pageLineChunkList[chunkIndex] = tempPageLineChunk;
                    }
                    var newPage = pageLineChunkList.SelectMany(x => x).ToList();
                    doc.Page(MakePage(newPage, blackCorners, borders));
                }
            }
        });
        return Task.FromResult(doc.GeneratePdf());
    }

    private Action<PageDescriptor> MakePage(List<byte[]> cards, bool blackCorners, bool borders)
    {  
        var cardsDone = 0;
        return page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(0);
            page.Background().AlignCenter().AlignMiddle().CutLine(1, PageSizes.Letter.Width,
                PageSizes.Letter.Height, _cardWidth, _cardHeight);

            page.Content().AlignCenter().PaddingTop(_paddingTop).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(_cardWidth);
                    columns.ConstantColumn(_cardWidth);
                    columns.ConstantColumn(_cardWidth);
                });
                foreach (var card in cards)
                {
                    table.Cell().Element(cardElement =>
                    {
                        cardElement.Height(_cardHeight).Width(_cardWidth).Layers(layers =>
                        {
                            layers.PrimaryLayer()
                                .Border(borders ? 1 : 0, Colors.Black)
                                .Background(blackCorners && (!card.SequenceEqual(File.ReadAllBytes(Path.Combine(env.WebRootPath, "Images", "Transparent.png")))) ? Colors.Black : Colors.White)
                                .Image(card)
                                .WithCompressionQuality(ImageCompressionQuality.Best)
                                .WithRasterDpi(300);
                            if (!blackCorners) return;
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
                }
            });
        };
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