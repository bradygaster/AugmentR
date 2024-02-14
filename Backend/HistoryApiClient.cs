public sealed class HistoryApiClient(HttpClient httpClient)
{
    public async Task SaveHistoryItem(HistoricalItem historyItem) => 
        await httpClient.PostAsJsonAsync("/history", historyItem);
}
