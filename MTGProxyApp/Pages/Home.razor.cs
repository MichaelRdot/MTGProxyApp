using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MTGProxyApp.Components;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Pages;

public partial class Home : ComponentBase
{
    private string _deckTextField = "";
    private string _deckName = "";
    private string _deckTooltip = "Total 0 prints, or 0 Pages with 0 cards on the last page.";
    private const string ToolTipHelperText = "This shows you the total number of cards that need to be printed with some other good information.";
    private const string DeckTextFieldLabel = "Deck Text";
    private const string DeckTextFieldHelperText = "This is where you put your deck :)";

    private const string DeckTextFieldPlaceholderText = "1 Sol ring (C21) 263\n" +
                                                        "5 sol ring\n" +
                                                        "sol ring (C21)\n" +
                                                        "sol ring\n\n";
    private const string DeckNameLabel = "Deck Name";
    private const string DeckNameHelperText = "This is where you put the name of your deck :)";
    private const string DeckNamePlaceholderText = "A Really Cool, Really Awesome Deck Name";
    private const string AllPageTitles = "Michael Proxies\n" + 
                                          "Michael does what?\n" + 
                                          ":)\n";

    private string _pageTitle = "";
    private string _blackCornersToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    private string _bordersToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    private string _printFlipCardsSeparateToggleIcon = Icons.Material.Filled.CheckBoxOutlineBlank;
    
    private bool _loadingCards;
    private float _loadingValue;
    private List<string> _currentCardList = [];
    private List<CardDto?> _cards = [];

    private readonly Exception _noCardException = new ("No card found");

    private bool _printFlipCardsSeparateToggle;
    private bool _blackCornersToggle;
    private bool _bordersToggle;
    private List<List<string>> _cardUrlList = new() { new(), new(), new() };
    private List<string> _cardsFailedList = new();

    private bool _creatingDocument;
    private void OnCardUpdated(CardDto newCard)
    {
        if (newCard.LineIndex == -1)
        {
            var idx = _cards.FindIndex(c => ReferenceEquals(c, newCard));
            if (idx >= 0) _cards[idx] = newCard;
            if (newCard.Count == 0) _cards.Remove(newCard);
            UpdatePrintList();
            return;
        }
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
    private async Task Load()
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
                    await Task.Delay(100);
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
    private string UpdateDeckList(CardDto card) => $"{card.Count} {card.Name} ({card.Set.ToUpperInvariant()}) {card.CollectorNumber}";
    private string GetCardFrontUrl(CardDto card) =>
        card.Flip
            ? card.CardFaces![0].ImageUris?.Png?.ToString() ?? ""
            : card.ImageUris?.Png?.ToString() ?? "";

    private string GetCardBackUrl(CardDto card) =>
        card.CardFaces?[1].ImageUris?.Png?.ToString() ?? "";

    private void UpdatePrintList()
    {
        _cardUrlList = new() { new(), new(), new() };
        foreach (var card in _cards)
        {
            for (var i = 0; i < card.Count; i++)
            {
                var frontUrl = GetCardFrontUrl(card);
                if (card.Flip && _printFlipCardsSeparateToggle)
                {
                    _cardUrlList[1].Add(frontUrl);
                    _cardUrlList[2].Add(GetCardBackUrl(card));
                }
                else
                {
                    _cardUrlList[0].Add(frontUrl);
                    if (card.Flip) _cardUrlList[0].Add(GetCardBackUrl(card));
                }
            }
        }
        if (_cardUrlList[0].Count != 0) _deckTooltip = $"Total {_cardUrlList[0].Count} prints, or {Math.Ceiling((double)_cardUrlList[0].Count / 9)} pages with {(_cardUrlList[0].Count - 1) % 9 + 1} cards on the last page. ";
        if (_printFlipCardsSeparateToggle) _deckTooltip += $"{_cardUrlList[1].Count} flip cards, or {2 * Math.Ceiling((double)_cardUrlList[1].Count / 9)} pages with {(_cardUrlList[1].Count - 1) % 9 + 1} cards on the last two pages.";
    }
    private async Task<CardDto> CheckScryfall(string query)
    {
        var cardList = await ScryfallService.GetCardsBySearchQuery(query);
        return cardList?.Data[0] == null ? throw _noCardException : cardList.Data[0];
    }
    private void BlackCornersToggle()
    {
        _blackCornersToggle = !_blackCornersToggle;
        _blackCornersToggleIcon = _blackCornersToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
    }
    private void BordersToggle()
    {
        _bordersToggle = !_bordersToggle;
        _bordersToggleIcon = _bordersToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
    }
    private void PrintFlipCardsSeparateToggle()
    {
        _printFlipCardsSeparateToggle = !_printFlipCardsSeparateToggle;
        _printFlipCardsSeparateToggleIcon = _printFlipCardsSeparateToggle ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank;
        UpdatePrintList();
    }
    private void OnDownloadFinished()
    {
        _creatingDocument = false;
    }
    private void DownloadStart()
    {
        UpdatePrintList();
        _creatingDocument = true;
    }
    private async Task Upload()
    { 
        DialogOptions cardDialogOptions = new() { BackdropClick = true };
        var dialog = await DialogService.ShowAsync<UploadDialog>("Upload Image", cardDialogOptions);
        var result = await dialog.Result;
        if (result.Canceled || result.Data is not List<(string FileName, byte[] Data, string PreviewUrl)> uploadedFiles) return;
        foreach (var (fileName, data, previewUrl) in uploadedFiles)
        {
            _cards.Add(new CardDto
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                Count = 1,
                LineIndex = -1,
                Set = "",
                CollectorNumber = "",
                PreLoadedCardImageFront = data,
                ImageUris = new CardDto.CardPngDto { Png = new Uri(previewUrl, UriKind.Relative) }
            });
        }
        UpdatePrintList();
        StateHasChanged();
    }
    protected override void OnInitialized()
    {
        if (!RendererInfo.IsInteractive) return;
        var random = new Random();
        var pageTitleList = AllPageTitles
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        _pageTitle = pageTitleList[random.Next(0, pageTitleList.Count)];
    }
}