using System.Threading.Tasks;

namespace Shadow.Agent.DA;

public interface IMessengerClient
{
    Task SendMessageAsync(string channelId, string text);
}