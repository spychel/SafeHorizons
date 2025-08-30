namespace SafeHorizons.Api.Dto;

public class AlgorithmData
{
    public string Caption { get; set; } = string.Empty;
    public IEnumerable<string> Steps { get; set; } = [];
}
