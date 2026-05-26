namespace MTGProxyApp.Dtos;

public record CardSplitResult(Guid OriginalCardId, CardDto SplitCard);
