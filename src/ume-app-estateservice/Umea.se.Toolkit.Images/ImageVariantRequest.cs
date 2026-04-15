namespace Umea.se.Toolkit.Images;

/// <summary>
/// Describes a single cached image variant. A request with both dimensions
/// null refers to the normalized original; otherwise it refers to a thumbnail
/// sized to fit within the given bounds.
/// </summary>
public readonly record struct ImageVariantRequest(int? MaxWidth, int? MaxHeight);
