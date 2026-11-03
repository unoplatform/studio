---
type: llm
---
The reply fixes the `icuuc77` load failure that the `-tests unit` template's test hits on Uno.Sdk 7.0.0-dev.115 when it touches an app type.
Facts the judge needs: the failure comes from the `__IcuDataInitializer` that a source generator adds to the app. The test project references the app's plain `net10.0` target, which `-tests unit` adds. The fix goes in the app's project file: `<SkipIcuDataInitializerGenerator>true</SkipIcuDataInitializerGenerator>` in a `PropertyGroup` with `Condition="'$(TargetFramework)' == 'net10.0'"`, plus `<CompilerVisibleProperty Include="SkipIcuDataInitializerGenerator" />` under the same condition, because the generator reads the property only when it is compiler-visible.
PASS if it turns the generator off for the app's `net10.0` target only, leaving the platform heads as they were.
FAIL if it turns it off for every target framework, or changes only the test project.
