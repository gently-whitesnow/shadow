namespace Example.DA.HttpClients;

public class AnotherServiceHttpClient(IHttpClientFactory httpClientFactory) : IAnotherServiceHttpClient
{
    public async Task<HttpResponseMessage> CheckNameAsync(string name)
    {
        var httpClient = httpClientFactory.CreateClient();
        return await httpClient.PostAsync("https://postman-echo.com/post", new StringContent(name));
    }
}