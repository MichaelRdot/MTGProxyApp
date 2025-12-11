using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Pages;

public partial class Home : ComponentBase
{
        string _deckTextField = "";
    string _deckName = "";
    string _deckTooltip = "Total 0 prints, or 0 Pages with 0 cards on the last page.";
    readonly string _toolTipHelperText = "This shows you the total number of cards that need to be printed with some other good information.";
    readonly string _deckTextFieldLabel = "Deck Text";
    readonly string _deckTextFieldHelperText = "This is where you put your deck :)";
    readonly string _deckTextFieldPlaceholderText = "1 Sol ring (C21) 263\n5 sol ring\nsol ring (C21)\nsol ring\n\n" +
                                                    "You can also input an entire Moxfield Decklist. Any Export from " +
                                                    "Moxfield or Archideckt SHOULD work, but if it doesn't please let me know :)\n\n" +
                                                    "Now with tokens!!! This is news worth talking about, it took me much longer " +
                                                    "to add that than you would think that it would.";
    readonly string _deckNameLabel = "Deck Name";
    readonly string _deckNameHelperText = "This is where you put the name of your deck :)";
    readonly string _deckNamePlaceholderText = "A Really Cool, Really Awesome Deck Name";

    string _blackCornersToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    string _bordersToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    string _printFlipCardsSeparateToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    
    bool _loadingCards;
    float _loadingValue;
    List<string> _currentCardList = new();
    List<CardDto?> _cards = new();

    readonly Exception _noCardException = new ("No card found");

    bool _printFlipCardsSeparateToggle;
    bool _blackCornersToggle;
    bool _bordersToggle;
    List<List<byte[]>> _cardPrintList = new();
    List<string> _cardsFailedList = new();

    bool _creatingDocument;
    float _cardsPrintedValue;

    void OnCardUpdated(CardDto newCard)
    {
        var index = newCard.LineIndex;
        var newLine = UpdateDeckList(newCard);
        _currentCardList = _deckTextField
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (newCard.Count == 0)
        {
            _currentCardList.RemoveAt(index);
            _cards.Remove(newCard);
            for (var i = index; i < _cards.Count; i++) if (_cards[i] is not null) _cards[i]!.LineIndex = i;
        }
        else
        {
            newCard.LineIndex = index;
            _currentCardList[newCard.LineIndex] = newLine;
        }

        var tempDeckText = new StringBuilder();
        foreach (var cardLine in _currentCardList) tempDeckText.Append(cardLine + "\n");
        _deckTextField = tempDeckText.ToString();
        for (var i = 0; i < _cards.Count; i++) if (_cards[i].LineIndex == newCard.LineIndex) _cards[i] = newCard;
        UpdatePrintList();
    }
    async Task Load()
    {
        var tempDeckText = new StringBuilder();
        _cardsFailedList.Clear();
        _cards = new();
        _currentCardList = _deckTextField
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        _loadingCards = true;
        StateHasChanged();
        foreach (var cardLine in _currentCardList)
        {
            if (!DeckLineModel.TryParse(cardLine, out var cardModel))
            {
                Snackbar.Add($"Had an issue parsing line: {cardLine}", Severity.Error);
            }
            else
            {
                var queryStringBuilder = new StringBuilder();
                try
                {
                    queryStringBuilder.Append($"!\"{cardModel.Name}\"");
                    if (cardModel.SetCode != null) queryStringBuilder.Append($" set:\"{cardModel.SetCode}\"");
                    if (cardModel.CollectorNumber != null) queryStringBuilder.Append($" cn:\"{cardModel.CollectorNumber}\"");
                    var card = await CheckScryfall(queryStringBuilder.ToString());
                    
                    card.Count = cardModel.Count;
                    card.LineIndex = _currentCardList.IndexOf(cardLine);
                    card.Flip = card.CardFaces?[0].ImageUris != null;
                    if (card.Flip)
                    {
                        card.PreLoadedCardImageBack = await HttpService.LoadCardImage(card.CardFaces[1].ImageUris.Png.ToString());
                        card.PreLoadedCardImageFront = await HttpService.LoadCardImage(card.CardFaces[0].ImageUris.Png.ToString());
                    }
                    else card.PreLoadedCardImageFront = await HttpService.LoadCardImage(card.ImageUris.Png.ToString());
                    if(card.TypeLine != null && (card.TypeLine.Contains("Token") || card.TypeLine.Contains("Card"))) card.IsToken = true;
                    _cards.Add(card);
                    tempDeckText.Append($"{UpdateDeckList(card)}\n");
                    _loadingValue = 100f * _cards.Count / _currentCardList.Count;
                    StateHasChanged();
                    Snackbar.Add($"Successfully added card: {cardModel.Name}", Severity.Success);
                }
                catch (Exception e)
                {
                    _cardsFailedList.Add(cardLine);
                    Snackbar.Add($"{e.Message} for line: {cardLine}", Severity.Error);
                }
            }
        }
        if(_cardsFailedList.Count > 0)
        {
            tempDeckText.Append("\nCard Lines Failed:\n");
            foreach (var cardLine in _cardsFailedList) tempDeckText.Append($"{cardLine}\n");
        }

        UpdatePrintList();
        _deckTextField = tempDeckText.ToString();
        _loadingCards = false;
        _loadingValue = 0;
    }
    string UpdateDeckList(CardDto card) => $"{card.Count} {card.Name} ({card.Set.ToUpperInvariant()}) {card.CollectorNumber}";
    void UpdatePrintList()
    {
        _cardPrintList = new();
        _cardPrintList.Add(new List<byte[]>());
        _cardPrintList.Add(new List<byte[]>());
        _cardPrintList.Add(new List<byte[]>());
        foreach (var card in _cards)
        {
            for (var i = 0; i < card.Count; i++)
            {
                if (card.PreLoadedCardImageBack != null && _printFlipCardsSeparateToggle)
                {
                    _cardPrintList[1].Add(card.PreLoadedCardImageFront);
                    _cardPrintList[2].Add(card.PreLoadedCardImageBack);
                }
                else
                {
                    _cardPrintList[0].Add(card.PreLoadedCardImageFront);
                    if (card.PreLoadedCardImageBack != null) _cardPrintList[0].Add(card.PreLoadedCardImageBack);
                }
            }
        }
        if (_cardPrintList[0].Count != 0) _deckTooltip = $"Total {_cardPrintList[0].Count} prints, or {Math.Ceiling((double)_cardPrintList[0].Count / 9)} pages with {(_cardPrintList[0].Count - 1) % 9 + 1} cards on the last page. ";
        if (_printFlipCardsSeparateToggle) _deckTooltip += $"{_cardPrintList[1].Count} flip cards, or {2 * Math.Ceiling((double)_cardPrintList[1].Count / 9)} pages with {(_cardPrintList[1].Count - 1) % 9 + 1} cards on the last two pages.";
    }
    async Task<CardDto> CheckScryfall(string query)
    {
        var cardList = await ScryfallService.GetCardsBySearchQuery(query);
        return cardList?.Data[0] == null ? throw _noCardException : cardList.Data[0];
    }
    void BlackCornersToggle()
    {
        _blackCornersToggle = !_blackCornersToggle;
        _blackCornersToggleIcon = _blackCornersToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
    }
    void BordersToggle()
    {
        _bordersToggle = !_bordersToggle;
        _bordersToggleIcon = _bordersToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
    }
    void PrintFlipCardsSeparateToggle()
    {
        _printFlipCardsSeparateToggle = !_printFlipCardsSeparateToggle;
        _printFlipCardsSeparateToggleIcon = _printFlipCardsSeparateToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
        UpdatePrintList();
    }
    void OnDownloadFinished()
    {
        _creatingDocument = false;
    }
    void DownloadStart()
    {
        UpdatePrintList();
        _creatingDocument = true;
    }
}