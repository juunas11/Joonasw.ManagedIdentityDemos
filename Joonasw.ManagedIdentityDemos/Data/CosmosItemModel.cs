using System.Text.Json.Serialization;

namespace Joonasw.ManagedIdentityDemos.Data;

public class CosmosItemModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
