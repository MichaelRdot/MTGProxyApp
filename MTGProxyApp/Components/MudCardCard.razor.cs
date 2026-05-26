using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class MudCardCard : ComponentBase
{
    [Parameter] public required CardDto Card { get; set; }
    [Parameter] public EventCallback<CardDto> UpdatedCard { get; set; }
    [Parameter] public EventCallback<int> UpdateCardPosition { get; set; }
    [Parameter] public EventCallback<CardSplitResult> OnCardSplit { get; set; }
    [Parameter] public CardFilterOptions FilterOptions { get; set; } = new();

    private string? _cardImage = "";
    private bool _mdfc;
    private bool _isEditingCount;
    private int _editCount;
    private bool _focusCountField;
    private MudNumericField<int>? _countField;

    protected override async Task OnInitializedAsync()
    {
        if (Card.CardFaces?[0].ImageUris != null) _mdfc = true;
        _cardImage = _mdfc ? Card.CardFaces?[0].ImageUris?.Png?.ToString() : Card.ImageUris?.Png?.ToString();
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusCountField && _countField != null)
        {
            _focusCountField = false;
            await _countField.FocusAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void StartCountEdit()
    {
        _editCount = Card.Count;
        _isEditingCount = true;
        _focusCountField = true;
    }

    private async Task CommitCountEdit()
    {
        if (!_isEditingCount) return;
        _isEditingCount = false;
        var newCount = Math.Max(0, _editCount);
        if (newCount != Card.Count)
        {
            Card.Count = newCount;
            if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
        }
    }

    private async Task OnCountFieldBlur(FocusEventArgs _) => await CommitCountEdit();

    private async Task HandleCountKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or "Escape") await CommitCountEdit();
    }

    private async Task OpenArtPickerAsync()
    {
        if (Card.EffectiveOracleId == null) return;
        var cardDialogParameters = new DialogParameters<MudCardDialog> { { d => d.Card, Card }, { d => d.FilterOptions, FilterOptions } };
        DialogOptions cardDialogOptions = new() { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true, BackdropClick = true };
        var dialog = await DialogService.ShowAsync<MudCardDialog>($"{Card.Name}", cardDialogParameters, cardDialogOptions);
        var result = await dialog.Result;
        if (result.Canceled) return;
        if (result.Data is CardDto card)
        {
            var newCard = card.Clone();
            newCard.Count = Card.Count;
            newCard.LineIndex = Card.LineIndex;
            newCard.IsToken = Card.IsToken;
            Card = newCard;

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

    private async Task OpenSplitDialogAsync()
    {
        if (Card.Count <= 1) return;
        var parameters = new DialogParameters<SplitDialog>
        {
            { d => d.MaxCount, Card.Count },
            { d => d.CardName, Card.Name }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SplitDialog>($"Split \"{Card.Name}\"", parameters, options);
        var result = await dialog.Result;
        if (result.Canceled || result.Data is not int splitCount) return;

        var originalId = Card.Id;
        Card.Count -= splitCount;

        var splitCard = Card.Clone();
        splitCard.Count = splitCount;
        splitCard.LineIndex = -1;

        if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
        if (OnCardSplit.HasDelegate) await OnCardSplit.InvokeAsync(new CardSplitResult(originalId, splitCard));
    }

    private async Task Add()
    {
        Card.Count++;
        if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
    }

    private async Task Subtract()
    {
        Card.Count--;
        if (UpdatedCard.HasDelegate) await UpdatedCard.InvokeAsync(Card);
    }
}
