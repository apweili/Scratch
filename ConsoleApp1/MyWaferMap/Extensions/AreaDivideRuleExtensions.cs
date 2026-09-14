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
            var columnResult = new List<IEnumerable<IArea>>();
            int? subColumnCount = null;
            foreach (var row in column)
            {
                var rowDivision = areaDivideRule.Divide(row);
                if (!subColumnCount.HasValue)
                {
                    subColumnCount = rowDivision.Count;
                    columnResult.AddRange(rowDivision);
                }
                else
                {
                    for (var i = 0; i < subColumnCount; i++)
                    {
                        columnResult[i] = columnResult[i].Concat(rowDivision[i]);
                    }
                }
            }

            result.AddRange(columnResult.Select(x => x.ToList()));
        }

        return result;
    }
}