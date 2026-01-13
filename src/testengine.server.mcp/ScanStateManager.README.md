# Test Engine Scan State Manager

## Overview

This implementation focuses on collecting app facts and generating recommendations for test generation.

## Key Components

1. **`SaveFactFunction`** - Function for collecting app facts
   - Records individual facts about app components
   - Stores data in memory for efficient processing

2. **`ExportFactsFunction`** - Exports collected facts to a file
   - Creates a single consolidated JSON file
   - Adds metrics and test recommendations

## Benefits of This Approach

- **Efficient**: Minimal processing and file I/O
- **Maintainable**: Clean architecture with clear responsibilities
- **Direct**: Provides raw app facts with minimal transformation
- **Actionable**: Includes specific test recommendations

## How to Use

Use the functions in your PowerFx code:

```
// Collect facts during scanning
SaveFact(
  {
    Category: "Screens", 
    Key: screenName, 
    AppPath: appPath, 
    Value: screenDetails
  }
);

// Export facts when scanning is complete
ExportFacts({AppPath: appPath});
```

## Output Format

The approach outputs a single `{appName}.app-facts.json` file with:

1. Raw app facts categorized by type (Screens, Controls, DataSources, etc.)
2. App metadata
3. App metrics (screen count, control count, etc.)
4. Test recommendations based on app structure

## Integration with GitHub Copilot

This format provides GitHub Copilot with the necessary context to generate effective tests:

1. **App Structure**: Copilot understands the screens, controls, and data sources
2. **Navigation Flows**: Copilot can trace navigation paths
3. **Test Priorities**: Recommendations guide Copilot to focus on critical areas
4. **Test Coverage**: Metrics help ensure comprehensive testing
