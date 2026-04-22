using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace OtoServisApp.Models;

public class District
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("neighborhoods")]
    public List<Neighborhood> Neighborhoods { get; set; }
}

public class Neighborhood
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ProvinceData
{
    [JsonPropertyName("districts")]
    public List<District> Districts { get; set; }
}

public class TurkiyeApiProvinceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ProvinceData Data { get; set; }
}