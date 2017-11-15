# How to Contribute

If you're interested in contributing, take a look at the general [contributer's guide](https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md) first.

## Build and Unit Test

To successfully build the sources on your machine, make sure you've installed the following prerequisites:
* Visual Studio 2017 Community or Enterprise
* .NET 4.6

Once you've installed the prerequisites execute either ```buildDebug.cmd``` or ```buildRelease.cmd``` script in the repository root to build the project locally..
```buildRelease.cmd``` also runs StlyeCop checks, and is required before merging any pull requests.

You can also open the solutions in Visual Studio and build directly from there.
The following solution contains the product code and unit tests 
	"\Microsoft.ApplicationInsights.sln" 


Once built, all unit tests can be run from Visual Studio itself.
