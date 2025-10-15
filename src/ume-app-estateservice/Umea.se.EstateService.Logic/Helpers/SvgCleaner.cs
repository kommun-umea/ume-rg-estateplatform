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

        HashSet<string> idSet = new HashSet<string>(
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

        List<XNode> contentToMove = innerSvg.Nodes().ToList();
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
}
