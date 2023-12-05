public class HistoryApiClient(HttpClient httpClient)
{
    private readonly HttpClient httpClient = httpClient;

    public async Task SaveHistoryItem(HistoricalItem historyItem) => 
        await httpClient.PostAsJsonAsync("/history", historyItem);
}
