using StackUp.Core.Entities;
using StackUp.Core.Enums;
using StackUp.Core.Progression;

namespace StackUp.Tests;

public class EffectiveExerciseSettingsTests
{
    [Fact]
    public void Resolve_NoOverrides_UsesGlobalDefaults()
    {
        var eff = EffectiveExerciseSettings.Resolve(new Exercise(), new AppSettings());
        Assert.Equal(10, eff.RepMin);
        Assert.Equal(15, eff.RepMax);
        Assert.Equal(2.5, eff.WeightIncrementKg);
        Assert.Equal(1, eff.RepIncrement);
        Assert.Equal(CountingSetRule.FirstSet, eff.Rule);
    }

    [Fact]
    public void Resolve_Overrides_WinOverGlobals()
    {
        var exercise = new Exercise
        {
            RepRangeMinOverride = 6,
            RepRangeMaxOverride = 8,
            WeightIncrementKgOverride = 5,
            RepIncrementOverride = 2,
            CountingSetRuleOverride = CountingSetRule.BestSet,
        };
        var eff = EffectiveExerciseSettings.Resolve(exercise, new AppSettings());
        Assert.Equal(new EffectiveExerciseSettings(6, 8, 5, 2, CountingSetRule.BestSet), eff);
    }

    [Fact]
    public void Resolve_PartialOverride_MixesBoth()
    {
        var exercise = new Exercise { WeightIncrementKgOverride = 1.25 };
        var settings = new AppSettings { DefaultRepRangeMin = 8 };
        var eff = EffectiveExerciseSettings.Resolve(exercise, settings);
        Assert.Equal(8, eff.RepMin);
        Assert.Equal(1.25, eff.WeightIncrementKg);
        Assert.Equal(15, eff.RepMax);
    }
}
