; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2026.2.429

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BAUTOVERSIONING00 | Configuration | Warning | Assembly info generator installed but not enabled
BAUTOVERSIONING01 | SourceGenerator | Warning | Assembly info generation failed
BAUTOVERSIONING02 | Configuration | Error | AssemblyCompany not configured
BAUTOVERSIONING03 | Configuration | Error | AssemblyProduct not configured

## Release 2026.2.521

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BAUTOVERSIONING04 | Configuration | Warning | Assembly info generator setup instructions (companion to BAUTOVERSIONING00)

## Release 2026.3.928

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BNAV00 | Configuration | Warning | Assembly info generator installed but not enabled (renamed from BAUTOVERSIONING00)
BNAV01 | SourceGenerator | Warning | Assembly info generation failed (renamed from BAUTOVERSIONING01)
BNAV02 | Configuration | Error | AssemblyCompany not configured (renamed from BAUTOVERSIONING02)
BNAV03 | Configuration | Error | AssemblyProduct not configured (renamed from BAUTOVERSIONING03)
BNAV04 | Configuration | Warning | Assembly info generator setup instructions, companion to BNAV00 (renamed from BAUTOVERSIONING04)

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BAUTOVERSIONING00 | Configuration | Warning | Renamed to BNAV00
BAUTOVERSIONING01 | SourceGenerator | Warning | Renamed to BNAV01
BAUTOVERSIONING02 | Configuration | Error | Renamed to BNAV02
BAUTOVERSIONING03 | Configuration | Error | Renamed to BNAV03
BAUTOVERSIONING04 | Configuration | Warning | Renamed to BNAV04
