using Microsoft.AspNetCore.Components;
using MTGProxyApp.Dtos;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class MudCardDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public required CardDto Card { get; set; }

    private List<CardDto> _cardList = new();
    private PaginatedListDto<CardDto?>? _paginatedCardList;
    private List<PaginatedListDto<CardDto>> _paginatedCardListList;

    private void Close() => MudDialog.Cancel();
    
    private void SelectArt(CardDto card) => MudDialog.Close(DialogResult.Ok(card));
    
    private string GetImageUrl(CardDto card) => card.ImageUris?.Png?.ToString() ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Png?.ToString() ?? "images/card-placeholder.png";

    protected override async Task OnInitializedAsync()
    {
        _paginatedCardList = (Card.CardFaces?[0].OracleId == null) ? await ScryfallService.GetCardsBySearchQuery($"oracleid:\"{Card.OracleId}\" unique:prints") : await ScryfallService.GetCardsBySearchQuery($"oracleid:\"{Card.CardFaces[0].OracleId}\" unique:prints");
        _cardList = _paginatedCardList.Data;
        await base.OnInitializedAsync();
    }

    async Task NextPage()
    {
        _paginatedCardList = await HttpService.GetResponse<PaginatedListDto<CardDto>>(_paginatedCardList.NextPage);
        _cardList.AddRange(_paginatedCardList.Data);
    }
}