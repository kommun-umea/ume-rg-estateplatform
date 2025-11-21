using System.Globalization;
using System.Xml.Linq;

namespace Umea.se.EstateService.Logic.Helpers;

public static class SvgCleaner
{
    /// <summary>
    /// Removes a list of specified nodes from the SVG document based on their IDs.
    /// </summary>
    public static XDocument RemoveNodes(XDocument document, IEnumerable<string> idsToRemove)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(idsToRemove);

        HashSet<string> idSet = new(
            idsToRemove.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal);

        if (idSet.Count == 0)
        {
            return document;
        }

        document.Descendants()
                .Where(element => idSet.Contains(element.Attribute("id")?.Value ?? string.Empty))
                .ToList()
                .ForEach(element => element.Remove());

        return document;
    }

    /// <summary>
    /// Flattens a nested SVG structure by using the inner SVG's viewBox and content.
    /// </summary>
    public static XDocument CropSvgToContent(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        XNamespace svgNs = "http://www.w3.org/2000/svg";

        XElement? rootSvg = document.Root;

        if (rootSvg == null || rootSvg.Name.LocalName != "svg")
        {
            return document;
        }

        XElement? innerSvg = rootSvg.Elements(svgNs + "svg").FirstOrDefault();

        if (innerSvg == null)
        {
            return document;
        }

        XAttribute? innerViewBox = innerSvg.Attribute("viewBox");
        if (innerViewBox == null)
        {
            return document;
        }

        List<XNode> contentToMove = [.. innerSvg.Nodes()];
        rootSvg.SetAttributeValue("viewBox", innerViewBox.Value);

        XAttribute? innerAspectRatio = innerSvg.Attribute("preserveAspectRatio");
        if (innerAspectRatio != null)
        {
            rootSvg.SetAttributeValue("preserveAspectRatio", innerAspectRatio.Value);
        }

        rootSvg.Attribute("x")?.Remove();
        rootSvg.Attribute("y")?.Remove();

        rootSvg.RemoveNodes();

        rootSvg.Add(contentToMove);

        return document;
    }

    /// <summary>
    /// Ensures font-size declarations include pixel units so browsers treat them as valid values.
    /// </summary>
    public static XDocument NormalizeFontSizes(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        XElement? root = document.Root;
        if (root is null)
        {
            return document;
        }

        foreach (XElement element in root.DescendantsAndSelf())
        {
            NormalizeFontSizeAttribute(element.Attribute("font-size"));
            NormalizeFontSizeInStyle(element.Attribute("style"));
        }

        return document;
    }

    private static void NormalizeFontSizeAttribute(XAttribute? fontSizeAttribute)
    {
        if (fontSizeAttribute is null)
        {
            return;
        }

        string? value = fontSizeAttribute.Value;
        if (!NeedsPixelSuffix(value))
        {
            return;
        }

        fontSizeAttribute.Value = $"{value!.Trim()}px";
    }

    private static void NormalizeFontSizeInStyle(XAttribute? styleAttribute)
    {
        if (styleAttribute is null)
        {
            return;
        }

        string original = styleAttribute.Value;
        if (string.IsNullOrWhiteSpace(original))
        {
            return;
        }

        string[] declarations = original.Split(';', StringSplitOptions.RemoveEmptyEntries);
        bool modified = false;

        for (int i = 0; i < declarations.Length; i++)
        {
            string declaration = declarations[i];
            int colonIndex = declaration.IndexOf(':');

            if (colonIndex <= 0)
            {
                continue;
            }

            string propertyName = declaration[..colonIndex].Trim();
            if (!propertyName.Equals("font-size", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string propertyValue = declaration[(colonIndex + 1)..].Trim();
            if (!NeedsPixelSuffix(propertyValue))
            {
                continue;
            }

            declarations[i] = $"{propertyName}:{propertyValue.Trim()}px";
            modified = true;
        }

        if (modified)
        {
            styleAttribute.Value = string.Join(";", declarations);
        }
    }

    private static bool NeedsPixelSuffix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();

        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        return true;
    }
}
