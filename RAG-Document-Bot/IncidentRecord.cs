using Microsoft.Extensions.VectorData;

public class IncidentRecord
{
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    // IMPORTANT: "Dimensions" must match the length of vectors your
    // embedding model actually produces. nomic-embed-text produces
    // 768-dimensional vectors. If you switch embedding models later,
    // verify the real length (Milestone 1 in the guide shows how) and
    // update this number to match - a mismatch will cause errors when
    // upserting or searching.
    [VectorStoreVector(dimensions: 768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}