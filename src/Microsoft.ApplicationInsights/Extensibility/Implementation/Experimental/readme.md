# Experimental Features

`TelemetryConfiguration.ExperimentalFeatures` is an `IEnumerable<string>` of feature names.

For simplicity, there is no data structure to parse or evaluate.
The presence of a string in this collection indicates that a feature is enabled.



## How to configure

### Via applicationinsights.config

```
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
   <InstrumentationKey>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxx</InstrumentationKey>
   <TelemetryInitializers>...</TelemetryInitializers>
   <TelemetryModules>...</TelemetryModules>
   <ExperimentalFeatures>
      <Add>exampleFeature</Add>
      <Add>anotherFeature</Add>
   </ExperimentalFeatures>
</ApplicationInsights>
```


### Via code

```
var config = new TelemetryConfiguration{
	ExperimentalFeatures = new string[] {"exampleFeature", "anotherFeature"}
	};
```

## How to define a new feature flag (compile time)

Feature flags are compile time constants in the `ExperimentalFeatures` class.

You must add 
- a cache variable to store the evaluation result
- and an `internal` method to invoke from your class.

```
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Experimental
{
    /// <summary>
    /// This class provides a means to interact with the <see cref="TelemetryConfiguration.ExperimentalFeatures" />.
    /// This performs a simple boolean evaluation; does a feature name exist in the string array?
    /// Evaluation results are cached.
    /// </summary>
    internal static class ExperimentalFeatures
    {
        internal static bool? exampleFeature;

        internal static bool IsExampleFeatureEnabled(TelemetryConfiguration telemetryConfiguration) => telemetryConfiguration.EvaluateExperimentalFeature(nameof(exampleFeature), ref exampleFeature);
    }
}
```



## How to evaluate a feature flag (runtime)

To consume, reference the `ExperimentalFeatures` static class from your class:

```
public class MyClass
{
	private void MyMethod()
	{
		if (ExperimentalFeatures.IsExampleFeatureEnabled)
		{
			// do stuff here
		}
	}
}
```

