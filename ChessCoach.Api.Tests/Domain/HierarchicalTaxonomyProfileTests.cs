using System.Collections.Generic;
using ChessCoach.Api.Data.Entities;
using ChessCoach.Api.Domain;
using Xunit;

namespace ChessCoach.Api.Tests.Domain;

public class HierarchicalTaxonomyProfileTests
{
    [Fact]
    public void ProjectFromEvents_CorrectlyAggregatesData()
    {
        // Arrange
        var events = new List<MoveAnalysisEvent>
        {
            new MoveAnalysisEvent { PrimaryCategory = "Tactics", SubCategory = "Forks", CentipawnLoss = 50 },
            new MoveAnalysisEvent { PrimaryCategory = "Tactics", SubCategory = "Forks", CentipawnLoss = 150 }, // Blunder
            new MoveAnalysisEvent { PrimaryCategory = "Tactics", SubCategory = "Pins", CentipawnLoss = 130 },  // Blunder
            new MoveAnalysisEvent { PrimaryCategory = "Strategy", SubCategory = "PawnStructure", CentipawnLoss = 10 }
        };

        var profile = new HierarchicalTaxonomyProfile();

        // Act
        profile.ProjectFromEvents(events);

        // Assert
        Assert.Equal(2, profile.Domains.Count);
        
        Assert.True(profile.Domains.ContainsKey("Tactics"));
        var tactics = profile.Domains["Tactics"];
        Assert.Equal(3, tactics.TotalCount);
        Assert.Equal(2, tactics.SubCategories.Count);

        var forks = tactics.SubCategories["Forks"];
        Assert.Equal(2, forks.TotalOpportunities);
        Assert.Equal(1, forks.BlunderCount);
        Assert.Equal(0.5, forks.ErrorRate);

        var pins = tactics.SubCategories["Pins"];
        Assert.Equal(1, pins.TotalOpportunities);
        Assert.Equal(1, pins.BlunderCount);
        Assert.Equal(1.0, pins.ErrorRate);

        var strategy = profile.Domains["Strategy"];
        Assert.Equal(1, strategy.TotalCount);
    }
}
