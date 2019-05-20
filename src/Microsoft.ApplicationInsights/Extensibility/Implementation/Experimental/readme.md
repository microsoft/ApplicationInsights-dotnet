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

## How to evaluate at runtime

I recommend a wrapper class that wraps the string name of the feature:

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
        internal static bool IsExampleFeatureEnabled(TelemetryConfiguration telemetryConfiguration) => telemetryConfiguration.EvaluateExperimentalFeature("exampleFeature");
    }
}
```

To consume:

```
public class MyClass{
	
	// private cache variable
	private bool? isExampleFeatureEnabled;

	// Getter property
	private bool IsExampleFeatureEnabled {
		get {
			if (!isExampleFeatureEnabled.HasValue)
			{
				isExampleFeatureEnabled = ExperimentalFeatures.IsExampleFeatureEnabled(TelemetryConfiguration.Active);
			}

			return isExampleFeatureEnabled.Value;
		}
	}
	

	private void MyMethod()
	{
		if (IsExampleFeatureEnabled)
		{
			// do stuff here
		}
	}
}
```

