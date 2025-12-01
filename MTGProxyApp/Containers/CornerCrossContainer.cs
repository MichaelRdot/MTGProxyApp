using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuestPDFPlayground;

public static class CornerCrossContainer
{
    public static IContainer CornerCross(this IContainer container, float size, float thickness)
    {
        container
            .Unconstrained()
            .TranslateX(-size / 2f)
            .TranslateY(-size / 2f)
            .Width(size)
            .Height(size)
            .Layers(layers =>
            {
                layers.PrimaryLayer().Element(x =>
                    x.AlignMiddle()
                        .LineHorizontal(thickness)
                        .LineColor(Colors.White));
                layers.Layer().Element(x =>
                    x.AlignCenter()
                        .LineVertical(thickness)
                        .LineColor(Colors.White));
            });

        return container;
    }
}
