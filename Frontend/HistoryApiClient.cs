public class HistoryApiClient(HttpClient httpClient)
{
    private readonly HttpClient httpClient = httpClient;

    public async Task<HistoricalItem[]> GetHistory()
    {
        return await httpClient.GetFromJsonAsync<HistoricalItem[]>("/history") ?? [];
    }
}
