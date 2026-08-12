namespace SteelTube.Application.Inventory.GetCurrentStock
{
    /// <summary>
    /// No parameters today — the query returns every material's current
    /// stock (SRS 9.3). A future revision could add filtering/paging here
    /// without touching the handler's shape.
    /// </summary>
    public sealed class GetCurrentStockQuery
    {
    }
}