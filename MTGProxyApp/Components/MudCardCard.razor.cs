using Microsoft.AspNetCore.Components;
using MTGProxyApp.Dtos;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class MudCardCard : ComponentBase
{
    [Parameter] public required CardDto Card { get; set; }
    [Parameter] public EventCallback<CardDto> UpdatedCard { get; set; }
    [Parameter] public EventCallback<int> UpdateCardPosition { get; set; }

    private string? _cardImage = "";
    private bool _mdfc;

    protected override async Task OnInitializedAsync()
    {
        if (Card.CardFaces?[0].ImageUris != null) _mdfc = true;
        _cardImage = _mdfc ? Card.CardFaces?[0].ImageUris?.Png?.ToString() : Card.ImageUris?.Png?.ToString();
        await base.OnInitializedAsync();
    }

    private async Task OpenArtPickerAsync()
    {
        var cardDialogParameters = new DialogParameters { ["Card"] = Card };
        DialogOptions cardDialogOptions = new() { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true, BackdropClick = true };
        var dialog = await DialogService.ShowAsync<MudCardDialog>($"{Card.Name}", cardDialogParameters, cardDialogOptions);
        var result = await dialog.Result;
        if (result.Canceled) return;
        if (result.Data is CardDto card)
        {
            card.Count = Card.Count;
            card.LineIndex = Card.LineIndex;
            card.IsToken = Card.IsToken;
            Card = card;

            Card.Flip = Card.CardFaces?[0].ImageUris != null;
            if (Card.Flip)
            {
                Card.PreLoadedCardImageBack = await HttpService.LoadCardImage(card.CardFaces[1].ImageUris.Png.ToString());
                Card.PreLoadedCardImageFront = await HttpService.LoadCardImage(card.CardFaces[0].ImageUris.Png.ToString());
            }
            else Card.PreLoadedCardImageFront = await HttpService.LoadCardImage(card.ImageUris.Png.ToString());

            _cardImage = Card.Flip ? Card.CardFaces?[0].ImageUris?.Png?.ToString() : Card.ImageUris?.Png?.ToString();
            if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
        }
    }

    async Task Add()
    {
        Card.Count++;
        if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
    }

    async Task Subtract()
    {
        Card.Count--;
        if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
    }

}