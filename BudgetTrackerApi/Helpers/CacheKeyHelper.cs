namespace BudgetTrackerApi.Helpers;

public static class CacheKeyHelper
{
    public static string GetBudgetStatusKey(int userId, int year, int month) =>
        $"budget-status:{userId}:{year}-{month}";
}
