using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderCategoryClassifier(
    IDataStore dataStore,
    IChatClient chatClient,
    ILogger<WorkOrderCategoryClassifier> logger) : IWorkOrderCategoryClassifier
{
    private const string SystemPrompt =
        """
        Du är en kategoriserare för felanmälningar och arbetsordrar inom svensk fastighetsförvaltning.
        Du får en lista med tillgängliga kategorier och en beskrivning av ett ärende.
        Välj de mest relevanta kategorierna och rangordna dem efter hur väl de matchar beskrivningen.
        Returnera ENBART bladkategorier (kategorier utan underkategorier).
        Svara med en JSON-array av objekt med CategoryId, CategoryName och Confidence (0.0–1.0).
        """;

    public IReadOnlyList<WorkOrderCategoryNode> GetCategoriesForType(int workOrderTypeId)
    {
        return [.. dataStore.WorkOrderCategories.Where(c => c.WorkOrderTypeIds.Contains(workOrderTypeId))];
    }

    public async Task<IReadOnlyList<WorkOrderCategorySuggestion>> ClassifyAsync(
        string description, int workOrderTypeId, CancellationToken ct = default)
    {
        IReadOnlyList<WorkOrderCategoryNode> categories = GetCategoriesForType(workOrderTypeId);
        if (categories.Count == 0)
        {
            logger.LogWarning("No categories found for work order type {WorkOrderTypeId}", workOrderTypeId);
            return [];
        }

        string categoryTree = BuildCategoryTree(categories);
        string userMessage = $"""
            Tillgängliga kategorier:
            {categoryTree}

            Beskrivning av ärendet:
            {description}
            """;

        logger.LogDebug("Classifying work order with {CategoryCount} available categories", categories.Count);

        ChatResponse<List<WorkOrderCategorySuggestion>> result = await chatClient.GetResponseAsync<List<WorkOrderCategorySuggestion>>(
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, userMessage)
            ],
            cancellationToken: ct);

        List<WorkOrderCategorySuggestion> suggestions = result.Result ?? [];

        return [.. suggestions.OrderByDescending(s => s.Confidence)];
    }

    private string BuildCategoryTree(IReadOnlyList<WorkOrderCategoryNode> categories)
    {
        Dictionary<int, WorkOrderCategoryNode> byId = categories.ToDictionary(c => c.Id);
        HashSet<int> childIds = [.. categories.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value)];
        List<WorkOrderCategoryNode> leaves = [.. categories.Where(c => !childIds.Contains(c.Id))];

        List<string> lines = [];
        foreach (WorkOrderCategoryNode? leaf in leaves)
        {
            string path = BuildPath(leaf, byId);
            lines.Add($"- {path} (id: {leaf.Id})");
        }

        return string.Join('\n', lines);
    }

    private static string BuildPath(WorkOrderCategoryNode node, Dictionary<int, WorkOrderCategoryNode> byId)
    {
        List<string> parts = [];
        WorkOrderCategoryNode? current = node;
        while (current is not null)
        {
            parts.Add(current.Name);
            current = current.ParentId.HasValue && byId.TryGetValue(current.ParentId.Value, out WorkOrderCategoryNode? parent)
                ? parent
                : null;
        }

        parts.Reverse();
        return string.Join(" / ", parts);
    }
}
