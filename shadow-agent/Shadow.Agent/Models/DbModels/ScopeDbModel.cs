using System;

namespace Shadow.Agent.Models.DbModels;

public class ScopeDbModel
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string MessengerChannelId { get; init; }
    public int NotifyReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}