# Minimum .NET Framework Version Changed

**Category:** Breaking Change  
**Applies to:** .NET Framework Applications  
**Migration Effort:** Simple  

## Change Summary

The minimum supported .NET Framework version has been raised from 4.5.2 to 4.6.2 in 3.x. This is required for OpenTelemetry dependencies.

## Migration

**2.x (supported):**
```xml
<TargetFramework>net452</TargetFramework>
```

**3.x (minimum):**
```xml
<TargetFramework>net462</TargetFramework>
```

## Migration Checklist

- [ ] Verify all projects target .NET Framework 4.6.2 or higher
- [ ] Update project files (.csproj) to target net462 or higher
- [ ] Test application after framework upgrade
- [ ] Check for any framework-specific API changes

## See Also

- [Microsoft .NET Framework Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-framework)
