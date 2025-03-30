# IDisposable Leak Analyzer

A C# static analyzer designed to detect potential memory leaks related to incorrect or missing implementations of the `IDisposable` interface in .NET projects.

## 🌟 Overview

This tool scans your entire .NET solution, identifies classes implementing `IDisposable`, and detects possible memory leaks or resource disposal omissions. Results are clearly displayed in the console and recorded in a CSV file for easier analysis.

## ⚙️ How It Works

- Loads settings from an `app.config` file, where you can specify:
  - Solution path
  - Output directory
  - Language (English or Spanish)
  - CSV separator
  - Option to display MSBuild warnings/errors

- Utilizes the Roslyn API to parse the syntax and semantic model of your code.
- Reports instances where disposable objects:
  - Are created but never disposed.
  - Are returned by methods without proper disposal.
  - Are used in contexts lacking clear disposal patterns (`using`, `.Dispose()`, etc.).

## 🎯 Key Features

- **Multi-language support:** English and Spanish.
- **Detailed logging:** Comprehensive results logged in an easily readable CSV file.
- **Console verbosity:** Optionally displays detailed MSBuild diagnostics.
- **Easy Configuration:** Adjustable via a straightforward `.config` file.

## 🚧 Limitations

- **Static Analysis Constraints:**
  - Cannot detect runtime-dependent disposal logic (e.g., conditional disposals).
  - May generate false positives if disposal occurs in methods outside of the analyzed scope (reflection, dynamic invocations).

- **Performance Considerations:**
  - Large solutions may take considerable time to analyze.
  - High memory usage for very large codebases.

- **Configuration Dependency:**
  - Currently relies exclusively on a traditional `.config` file, not JSON-based configurations.

- **MSBuild Version Dependency:**
  - Specifically designed and tested to work with MSBuild from Visual Studio 2022. Using other MSBuild versions may produce unexpected results.

## 🚀 Getting Started

### Prerequisites

- .NET Framework or .NET Core installed.
- MSBuild available on your machine (preferably from Visual Studio 2022).

### Configuration

Edit the `app.config` file to set up your project-specific parameters:

```xml
<configuration>
  <appSettings>
    <add key="FullPathSolution" value="C:\Path\to\your\solution.sln" />
    <add key="CsvSeparator" value=";" />
    <add key="Language" value="EN" />
    <add key="ShowErrorsOrWarningsFromMsBuild" value="false" />
  </appSettings>
</configuration>
```

### Running the Analyzer

Simply execute the compiled program:

```shell
dotnet IDisposableLeakAnalyzer.dll
```

### Reviewing Results

Check the generated `resultados.csv` file located in the configured OUTPUT folder.

📌 Validation Exception File (ValidationException.json)
The analyzer allows excluding known valid scenarios through the ValidationException.json file. This JSON file defines specific cases (such as methods, classes, or lines of code) that should intentionally be ignored to prevent false positives.

🚩 Configuration
First, copy the provided template file to your working file:

shell
Copiar
Editar
cp ValidationException.example.json ValidationException.json
Next, customize your local ValidationException.json with project-specific exceptions you wish the analyzer to bypass.

📋 File Format
Each entry in the JSON array represents one scenario to exclude from validation:

json
Copiar
Editar
[
  {
    "project": "YourProjectName",
    "file": "Path\\To\\YourFile.cs",
    "lineNumber": 42,
    "class": "Namespace.YourClass",
    "method": "FullMethodSignature"
  }
]
Explanation of fields:

project: The name of the project or module.

file: The relative or absolute local file path.

lineNumber: Exact line number in the specified file.

class: Fully qualified class name including namespace.

method: Complete method signature as it appears in your code.

🎯 Purpose and Benefits
Clearly documents known exceptions for better maintainability.

Helps collaborators understand explicitly exempted scenarios.

Simplifies handling of false positives and intentional usage patterns.


## 💡 Future Enhancements

- Improved heuristic detection of complex disposal patterns.

## 📄 License

Distributed under the MIT License.


Developed and maintained by [Germán Carlos Suárez Alanís]. Feedback and contributions are welcome!


### Demo
https://youtu.be/fQDkEtLCM6c


---

Feel free to open issues or contribute improvements to enhance the tool further.