public sealed class HistoryApiClient(HttpClient httpClient)
{
    public async Task<HistoricalItem[]> GetHistory()
    {
        return await httpClient.GetFromJsonAsync<HistoricalItem[]>("/history") ?? [];
    }
}
