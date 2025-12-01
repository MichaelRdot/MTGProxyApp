using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuestPDFPlayground;

public static class CutLineContainer
{
    public static void CutLine(this IContainer container, float thickness, float paperWidth, float paperHeight, float cardWidth, float cardHeight)
    { 
            container
            .Width(paperWidth)
            .Height(paperHeight)
            .Layers(layers =>
            {
                    layers.PrimaryLayer().Element(e =>
                            e.AlignMiddle()
                                    .TranslateY(-cardHeight / 2f)
                                    .LineHorizontal(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignCenter()
                                    .TranslateX(-cardWidth / 2f)
                                    .LineVertical(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignMiddle()
                                    .TranslateY(cardHeight / 2f)
                                    .LineHorizontal(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignCenter()
                                    .TranslateX(cardWidth / 2f)
                                    .LineVertical(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignMiddle()
                                    .TranslateY(-cardHeight / 2f - cardHeight)
                                    .LineHorizontal(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignCenter()
                                    .TranslateX(-cardWidth / 2f - cardWidth)
                                    .LineVertical(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignMiddle()
                                    .TranslateY(cardHeight / 2f + cardHeight)
                                    .LineHorizontal(thickness)
                                    .LineColor(Colors.Black));

                    layers.Layer().Element(e =>
                            e.AlignCenter()
                                    .TranslateX(cardWidth / 2f + cardWidth)
                                    .LineVertical(thickness)
                                    .LineColor(Colors.Black));
            });
    }
}