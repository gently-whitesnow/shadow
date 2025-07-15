namespace Example.DA.HttpClients;

public interface IAnotherServiceHttpClient
{
    Task<HttpResponseMessage> CheckNameAsync(string name);
}