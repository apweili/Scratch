namespace MyWaferMap.Extensions;

public static class AreaDivideRuleExtensions
{
    public static (IReadOnlyList<IReadOnlyList<IArea>>? ContinuousGranularityDivision,
        IReadOnlyList<IReadOnlyList<IArea>> FinestGrainDivision) DivideRecursively(
            this IAreaDivideRule areaDivideRule, IArea area)
    {
        var currentRule = areaDivideRule;
        IReadOnlyList<IReadOnlyList<IArea>>? continuousGranularityDivision = null;
        IReadOnlyList<IReadOnlyList<IArea>>? finestGrainDivision = null;
        while (currentRule != null)
        {
            finestGrainDivision = finestGrainDivision == null
                ? currentRule.Divide(area)
                : MapToNextRule(finestGrainDivision, currentRule);

            if (continuousGranularityDivision == null)
            {
                if (currentRule.IsDivideRecursiveContinually())
                {
                    continuousGranularityDivision = finestGrainDivision;
                }
            }

            currentRule = currentRule.ChildRule;
        }

        return (continuousGranularityDivision, finestGrainDivision!);
    }

    private static bool IsDivideRecursiveContinually(this IAreaDivideRule areaDivideRule)
    {
        var currentRule = areaDivideRule;
        while (currentRule != null)
        {
            if (!currentRule.IsDivideContinually)
            {
                return false;
            }

            currentRule = currentRule.ChildRule;
        }

        return true;
    }

    private static List<List<IArea>> MapToNextRule(IReadOnlyList<IReadOnlyList<IArea>> areas,
        IAreaDivideRule areaDivideRule)
    {
        var result = new List<List<IArea>>();
        foreach (var column in areas)
        {
            var dictionary = new Dictionary<int, IEnumerable<IArea>>();
            foreach (var row in column)
            {
                var rowDivision = areaDivideRule.Divide(row);
                foreach (var area in rowDivision.Where(r => r.Count > 0))
                {
                    var columnNumber = area[0].MatrixCoordinate.Column;
                    if (dictionary.TryGetValue(columnNumber, out var subColumns))
                    {
                        subColumns = subColumns.Concat(area);
                    }
                    else
                    {
                        subColumns = area;
                    }

                    dictionary[columnNumber] = subColumns;
                }
            }

            result.AddRange(dictionary.OrderBy(d => d.Key).Select(d => d.Value.ToList()));
        }

        return result;
    }
}