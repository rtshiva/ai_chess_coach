using System.Collections.Generic;
using ChessCoach.Api.Data.Entities;

namespace ChessCoach.Api.Domain;

public class HierarchicalTaxonomyProfile
{
    public Dictionary<string, CategoryNode> Domains { get; set; } = new();

    public void ProjectFromEvents(IEnumerable<MoveAnalysisEvent> events)
    {
        foreach (var ev in events)
        {
            if (!Domains.ContainsKey(ev.PrimaryCategory))
            {
                Domains[ev.PrimaryCategory] = new CategoryNode();
            }
            var primary = Domains[ev.PrimaryCategory];
            primary.TotalCount++;

            if (!primary.SubCategories.ContainsKey(ev.SubCategory))
            {
                primary.SubCategories[ev.SubCategory] = new SubMetric();
            }
            var sub = primary.SubCategories[ev.SubCategory];
            sub.TotalOpportunities++;
            if (ev.CentipawnLoss > 120)
            {
                sub.BlunderCount++;
            }
        }
    }
}

public class CategoryNode
{
    public int TotalCount { get; set; }
    public Dictionary<string, SubMetric> SubCategories { get; set; } = new();
}

public class SubMetric
{
    public int TotalOpportunities { get; set; }
    public int BlunderCount { get; set; }
    public double ErrorRate => TotalOpportunities > 0 ? (double)BlunderCount / TotalOpportunities : 0;
}
