public sealed class HistoryApiClient1(HttpClient httpClient)
{
    public async Task SaveHistoryItem(HistoricalItem historyItem) => 
        await httpClient.PostAsJsonAsync("/history", historyItem);
}
