namespace Utilities
{
    public static class StaticElements
    {
        public static IReadOnlyDictionary<string, object> numericSelectAllAttributes = new Dictionary<string, object>
        {
            ["onfocus"] = "this.select()",
            ["onclick"] = "this.select()"
        };

    }
}