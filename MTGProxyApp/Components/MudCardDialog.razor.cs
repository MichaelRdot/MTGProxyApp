using Microsoft.AspNetCore.Components;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class MudCardDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public required CardDto Card { get; set; }
    [Parameter] public CardFilterOptions FilterOptions { get; set; } = new();

    private List<CardDto> _cardList = new();

    private void Close() => MudDialog.Cancel();

    private void SelectArt(CardDto card) => MudDialog.Close(DialogResult.Ok(card));

    private string GetImageUrl(CardDto card) => card.ImageUris?.Png?.ToString() ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Png?.ToString() ?? "images/card-placeholder.png";

    protected override async Task OnInitializedAsync()
    {
        var oracleId = Card.OracleId ?? Card.CardFaces?[0].OracleId;
        if (oracleId != null)
            _cardList = await ScryfallService.GetPrintsByOracleId(oracleId, FilterOptions.Language, FilterOptions.HighresOnly);
        await base.OnInitializedAsync();
    }
}
