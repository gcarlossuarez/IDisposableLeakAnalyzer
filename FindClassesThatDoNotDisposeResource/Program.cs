/*
To see the plantuml diagram, copy the code below and paste it in the plantuml online editor: https://plantuml.com/es/

CLASS DIAGRAM
@startuml
   
   package FindClassesThatDoNotResource {
   
     class AnalizeLeaks {
       +static Task Process()
       -static bool TypedSymbolIsIDisposable(INamedTypeSymbol i)
       -static Task AnalizeReturnStatementCase(Solution, HashSet<string>, Dictionary<IMethodSymbol, MethodSymbolMethodDeclaration>, StreamWriter, HashSet<KeyResult>)
       -static void AnalizeObjectCreationCase(SyntaxNode, HashSet<string>, SemanticModel, SyntaxTree, Document, StreamWriter, Project, HashSet<KeyResult>)
       -static string GetVariableName(SemanticModel, SyntaxNode, SyntaxNode, ref bool)
       -static IdentifierNameSyntax GetLastIdentifierNameSyntax(ExpressionSyntax)
       -static bool HasDisposeCall(...)
       -static string GertPrefixNamespaceClass(SyntaxNode)
       -static bool BelongsUsing(...)
       -static bool IsSomeTypeOfMethodDeclaration(SyntaxNode)
       -static void GetMethodSignature(...)
     }
   
     class MessageWriter {
       +static void WriteMessage(StreamWriter, string, ConsoleColor?, string, string, string, int, string, string, string extraInfo="")
     }
   
     class Messages {
       {static} CsvSeparator : string
       {static} CurrentLanguage : Language
       +static string Get(string key, params object[] args)
     }
   
     class KeyResult {
       +Proyecto : string
       +Archivo : string
       +Línea : int
       +Clase : string
       +Método : string
     }
   
     class MethodSymbolMethodDeclaration {
       +MethodSymbol : IMethodSymbol
       +MethodDeclaration : MethodDeclarationSyntax
       +PropertyDeclaration : PropertyDeclarationSyntax
       +FilePath : string
     }
   
     enum Language {
       English,
       Spanish
     }
   
     AnalizeLeaks --> MessageWriter
     AnalizeLeaks --> Messages
     AnalizeLeaks --> KeyResult
     AnalizeLeaks --> MethodSymbolMethodDeclaration
     Messages --> Language
   
   }
   @enduml
    


FLOWCHART 

@startuml
   
   start
   : Read appSettings from configuration;
   : Get solution path (FullPathSolution);
   : Set language (EN/ES);
   : Set CSV separator;
   
   if (OUTPUT directory exists?) then (yes)
   else (no)
   : Create OUTPUT directory;
   endif
   
   : Register MSBuild version;
   : Create MSBuildWorkspace;
   
   if (Show MSBuild errors?) then (yes)
   : Subscribe WorkspaceFailed to show errors;
   endif
   
   : Create results.csv file;
   : Write CSV header;
   
   : workspace.OpenSolutionAsync(solutionPath);
   : Get projects from the solution;
   
   : For each project;
   repeat
   : Analyze project;
   : For each document;
   repeat
   : Get SyntaxTree and SemanticModel;
   : Analyze IDisposable classes;
   : Save IDisposable classes;
   : Analyze IDisposable object creation;
   : Log warnings on screen and CSV;
   repeat while (more documents)
   repeat while (more projects)
   
   : Identify methods that return IDisposable;
   : Analyze methods that return IDisposable (AnalizeReturnStatementCase);
   : Log possible leaks on screen and CSV;
   
   : Analyze IDisposable object creation (AnalizeObjectCreationCase);
   : Log additional leaks or warnings;
   
   : End analysis;
   : Display "Analysis completed" message;
   stop
   
   @enduml
 */

using CommandLine;

namespace FindClassesThatDoNotDisposeResource
{
    using System.Configuration;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.MSBuild;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Build.Locator;


    public class Program
    {
        private static string[] _args;
        static void Main(string[] args)
        {
            try
            {
                _args = args;
                Task.Run(() => Process()).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine(Messages.Get("PressAnyKeyToContinue"));
            Console.ReadKey();
        }

        public static async Task Process()
        {
            CommandLineOptions cmdOptions = null;
            Parser.Default.ParseArguments<CommandLineOptions>(_args)
                .WithParsed(opts => cmdOptions = opts)
                .WithNotParsed(errors => Environment.Exit(1));

            // Read <appSettings> section
            var appSettings = new System.Configuration.AppSettingsReader();

            // Read the variable from the config file
            string solutionPath = cmdOptions.SolutionPath ?? appSettings.GetValue("FullPathSolution", typeof(string))?.ToString();
            Messages.CsvSeparator = cmdOptions.CsvSeparator ??  appSettings.GetValue("CsvSeparator", typeof(string))?.ToString();

            string outputPath = @".\OUTPUT";
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            string csvPath = cmdOptions.OutputPath ?? Path.Combine(outputPath, "resultados.csv");

            var langSetting = cmdOptions.Language ?? appSettings.GetValue("Language", typeof(string))?.ToString();
            Messages.CurrentLanguage = langSetting == "EN" ? Language.English : Language.Spanish;

            bool showErrorsOrWarningsFromMsBuild = cmdOptions.Verbose ||
                appSettings.GetValue("ShowErrorsOrWarningsFromMsBuild", typeof(string))?.ToString() == "true";

            // Ensure the correct version of MSBuild is registered:
            var instance = MSBuildLocator.QueryVisualStudioInstances().LastOrDefault();
            if (instance != null && !MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterInstance(instance);
            }

            using (var workspace = MSBuildWorkspace.Create())
            {
                if (showErrorsOrWarningsFromMsBuild)
                {
                    workspace.WorkspaceFailed += (o, e) =>
                    {
                        Console.WriteLine($"Error loading workspace: {e.Diagnostic.Message}");
                    };
                }

                using (var csvWriter = new StreamWriter(csvPath, false))
                {
                    csvWriter.WriteLine(Messages.Get("Csv_Header"));

                    var solution = await workspace.OpenSolutionAsync(solutionPath);

                    //Console.WriteLine($"Proyectos cargados: {solution.Projects.Count()}");
                    HashSet<KeyResult> keyResults = new HashSet<KeyResult>();
                    // 🔍 **1. Identify all IDisposable classes in the solution **
                    var disposableClasses = new HashSet<string>();
                    foreach (var project in solution.Projects)
                    {
                        Console.WriteLine(Messages.Get("ProjectAnalysis", project.Name));

                        foreach (var document in project.Documents)
                        {
                            var syntaxTree = await document.GetSyntaxTreeAsync();
                            var semanticModel = await document.GetSemanticModelAsync();
                            var root = await syntaxTree.GetRootAsync();

                            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                            foreach (var classDeclaration in classDeclarations)
                            {
                                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                                if (classSymbol != null &&
                                    classSymbol.AllInterfaces.Any(TypedSymbolIsIDisposable))
                                {
                                    disposableClasses.Add(classSymbol.ToString());
                                    //Console.WriteLine(
                                    //    $"🔹 Clase IDisposable encontrada: {classSymbol} en {document.FilePath}");
                                    //csvWriter.WriteLine($"{project.Name};{document.FilePath};-;{classSymbol};✅ OK;");

                                    MessageWriter.WriteMessage(
                                        csvWriter,
                                        messageKey: "DisposableClassFound",
                                        color: null,
                                        csvStatusKey: "Csv_Correct",
                                        project: project.Name,
                                        filePath: document.FilePath,
                                        lineNumber: -1,
                                        className: classSymbol.ToString(),
                                        methodSignature: ""
                                    );


                                }
                            }

                            var syntaxNodes = root.DescendantNodes()
                                .Where(x => x is ObjectCreationExpressionSyntax
                                            || x is VariableDeclaratorSyntax
                                            || x is AssignmentExpressionSyntax
                                            || x is InvocationExpressionSyntax);

                            foreach (var syntaxNode in syntaxNodes)
                            {
                                var typeSymbol = semanticModel.GetTypeInfo(syntaxNode).Type as INamedTypeSymbol;
                                if (typeSymbol != null)
                                {
                                    // 🔍 Check if it belongs to a .NET native assembly
                                    bool isFromDotNet = typeSymbol.ContainingAssembly?.Name.StartsWith("System") == true
                                                        || typeSymbol.ContainingAssembly?.Name.StartsWith("Microsoft") == true;

                                    // 🔍 Check if it implements IDisposable
                                    bool isDisposable = typeSymbol.AllInterfaces.Any(TypedSymbolIsIDisposable);

                                    if (isFromDotNet && isDisposable && !disposableClasses.Contains(typeSymbol.ToString()))
                                    {
                                        int lineNumber = syntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line + 1;

                                        MessageWriter.WriteMessage(
                                            csvWriter: null, // It is not written to CSV in this case
                                            messageKey: "DotNetDisposableClassWarning",
                                            color: ConsoleColor.Yellow,
                                            csvStatusKey: null,
                                            project: "", // Not used
                                            filePath: document.FilePath,
                                            lineNumber: lineNumber,
                                            className: typeSymbol.ToString(),
                                            methodSignature: ""
                                        );

                                        disposableClasses.Add(typeSymbol.ToString());
                                    }
                                }
                            }
                        }
                    }

                    var returnMethods = new Dictionary<IMethodSymbol, MethodSymbolMethodDeclaration>();
                    // ** 🔍 2. Identify all classes that return instances of the "IDisposable" class.
                    foreach (var project in solution.Projects)
                    {
                        foreach (var document in project.Documents)
                        {
                            var syntaxTree = await document.GetSyntaxTreeAsync();
                            var semanticModel = await document.GetSemanticModelAsync();
                            var root = await syntaxTree.GetRootAsync();

                            // Find all Methods and Propertys in current document
                            foreach (var declarationSyntaxNode in root.DescendantNodes()
                                         .Where(md =>
                                             md is MethodDeclarationSyntax || md is PropertyDeclarationSyntax ||
                                             md is LocalFunctionStatementSyntax))
                            {
                                var methodSymbol = semanticModel.GetDeclaredSymbol(declarationSyntaxNode) as IMethodSymbol;
                                // If method has return clase and DataType of return clause belongs to IDisposable classes
                                if (methodSymbol != null && disposableClasses.Contains(methodSymbol.ReturnType.ToString()))
                                {
                                    // Identify is method declaration or property delcaration
                                    if (declarationSyntaxNode is MethodDeclarationSyntax)
                                    {
                                        returnMethods[methodSymbol] = new MethodSymbolMethodDeclaration(methodSymbol,
                                            (MethodDeclarationSyntax)declarationSyntaxNode, propertyDeclarationSyntax: null,
                                             localFunctionStatementSyntax: null, filePath: document.FilePath);
                                    }
                                    else if(declarationSyntaxNode is PropertyDeclarationSyntax)
                                    {
                                        returnMethods[methodSymbol] = new MethodSymbolMethodDeclaration(methodSymbol,
                                            methodDeclarationSyntax: null, 
                                            propertyDeclarationSyntax:  (PropertyDeclarationSyntax)declarationSyntaxNode, localFunctionStatementSyntax: null, 
                                            filePath: document.FilePath);
                                    }
                                    else
                                    {
                                        returnMethods[methodSymbol] = new MethodSymbolMethodDeclaration(methodSymbol,
                                            methodDeclarationSyntax: null, propertyDeclarationSyntax: null, 
                                            localFunctionStatementSyntax: (LocalFunctionStatementSyntax)declarationSyntaxNode,
                                            filePath: document.FilePath);
                                    }
                                }
                            }
                        }
                    }


                    await AnalizeReturnStatementCase(solution, disposableClasses, returnMethods, csvWriter, keyResults);


                    // 🔍 **4. Find instances of these classes in all projects **
                    foreach (var project in solution.Projects)
                    {
                        Console.WriteLine(Messages.Get("ProjectAnalysis", project.Name));
                        foreach (var document in project.Documents)
                        {
                            var syntaxTree = await document.GetSyntaxTreeAsync();
                            var semanticModel = await document.GetSemanticModelAsync();
                            var root = await syntaxTree.GetRootAsync();

                            AnalizeObjectCreationCase(root, disposableClasses, semanticModel, syntaxTree, document, csvWriter, project,
                                keyResults);
                        }
                    }
                }
            }

            Console.WriteLine(Messages.Get("AnalysisComplete", Path.GetFullPath(csvPath)));
        }

        /// <summary>
        /// Identify if INamedTypeSymbol is IDisposable
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool TypedSymbolIsIDisposable(INamedTypeSymbol i)
        {
            return i.ToString() == "System.IDisposable" || i.ToString() == "IDisposable";
        }

        /// <summary>
        /// Analize Return Statement Case.
        /// See mermaid diagram for more details:https://mermaid.live/edit#pako:eNqNVm1v2jAQ_isnf9okingpUJC2qS9oQ1tf1DJVW-gHNzkga2Ij27RQ1P8-23lzQuj2BXzP3T33-HxxsiM-D5CMyDziL_6SCgXTixmbsTul1x88-wenjEbbV7xFtRbGozBGps6pxIePM3Z09BkmCoWGbwT_g76SXmoDf0YBSP0lpK4HQ16JBofhgvtrQ15HkfkgZHV0earl-4rqbssU3UwFoqctSEwwNswFj3M-TVKKzvMxpkyF_qVuUZRQpAhYqJbFzcmIbjlXNt8s4Eq7klRHkd1IGur2Y8KeuU9VyFldRwrveLMSKKVeJaRuYxyOTNElqiUP7rbxI092lgCQIIm6Ig0K9kynS2BJz5foPyX2eBNKJXcXHGWFF43HHJ-wo5Q45Zc3Q7pHoGl1rw4042DGL0w2mc5soXy6XeHOpoDSS-BzLSTjaxZxVkxttuW9xPgRxanva9xzjZoTOMwyCfTEhPMQxRWN0Sub_5F_rZYoCtyzdvWYXHU2a7xRgvrppJsx9FIECig5ezdVc5X1uVw3VGhXiSuBnDEvZ1tpe0rcGcqwq3UU7SbSFWcgMy41ke7h183LgZR0ws5pFJ1hxNlC_pQhW3gGABeB2E6rK7_YfC3HjFWO6UCU1lbBim58o_IilCsu0cQkz1SoYEmfEVKHZXSeoXKK25Y7VJPMF0zFGj2NQJhD8AmURh8OEjnNKjuTdlUSnIbtZ2SCstpn-m0SXLNblOtIVXU9GifoBlY4hI22FQ5RFa2czLOAXUFt-rbXlrqcvL0FVLpuguA7blP12kilgeLwlOHSyF_qag-1VGlz70Wo8J4KZqbQGnDDlXmCaAQ_kD7BS-K023aj94TYm8yx37lLD7wqxixwoJ2WGHOBzsVpL-9y1D_u7f3grIfak7_D81JBhqSF3Ld8pUzu2g90SmRfHXmFVQqkBZyvkgp_5tkLc9g_ePrn3Q8m0iAxipiGgf7s2s0YwIzoayLGGRnpZYBzao6OzNibDqVrxfWl5ZOReTIbRPD1YklGcxpJba1Xgdl3SBeCxlnIirLfnLsmGe3IhozarX6z12n1ep1-__ikPWwNG2RLRt1uc9gatLrtwfC4N-h3er23Bnm1DN3mcWvYOe4M2t1W_6Tdbvff_gKcvbDr
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="disposableClasses"></param>
        /// <param name="returnMethods"></param>
        /// <param name="csvWriter"></param>
        /// <param name="keyResults"></param>
        /// <returns></returns>
        private static async Task AnalizeReturnStatementCase(Solution solution, HashSet<string> disposableClasses,
            Dictionary<IMethodSymbol, MethodSymbolMethodDeclaration> returnMethods,
            StreamWriter csvWriter, HashSet<KeyResult> keyResults)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    var semanticModel = await document.GetSemanticModelAsync();
                    var root = await syntaxTree.GetRootAsync();

                    foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        const string METHOD_SIGNATURE_NOT_FOUNDED = "N/A";
                        bool isDisposed = false;
                        string methodSignature = METHOD_SIGNATURE_NOT_FOUNDED;
                        var symbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
                        MethodSymbolMethodDeclaration methodSymbolMethodDeclaration;
                        if (symbol != null && returnMethods.TryGetValue(symbol, out methodSymbolMethodDeclaration))
                        {
                            if (invocation.Expression != null && invocation.Expression is MemberAccessExpressionSyntax)
                            {
                                MemberAccessExpressionSyntax memberAccessExpressionSyntax =
                                    (MemberAccessExpressionSyntax)invocation.Expression;
                                SyntaxNode syntaxNode = null;
                                if (memberAccessExpressionSyntax.Expression != null &&
                                    memberAccessExpressionSyntax.Expression is ObjectCreationExpressionSyntax)
                                {
                                    syntaxNode =
                                        (ObjectCreationExpressionSyntax)memberAccessExpressionSyntax.Expression;
                                }
                                else if (memberAccessExpressionSyntax.Expression != null &&
                                         memberAccessExpressionSyntax.Expression is IdentifierNameSyntax)
                                {
                                    syntaxNode =
                                        (IdentifierNameSyntax)memberAccessExpressionSyntax.Expression;
                                }
                                else
                                {
                                    syntaxNode = memberAccessExpressionSyntax.Expression;
                                }

                                if (syntaxNode == null)
                                {
                                    continue;
                                }

                                SyntaxNode parent1;
                                bool insideUsing;
                                bool usedInUsingLater;
                                MethodDeclarationSyntax methodDeclaration;
                                ConstructorDeclarationSyntax constructorDeclaration;
                                PropertyDeclarationSyntax propertyDeclaration;
                                var hasDisposeCall = BelongsUsing(syntaxNode, out methodSignature, out parent1, out insideUsing,
                                    out usedInUsingLater, out methodDeclaration, out constructorDeclaration, out propertyDeclaration);

                                if (!hasDisposeCall)
                                {
                                    bool foundeOutOfOwnMethodDeclaration = false;
                                    string memberContainsDispose;
                                    hasDisposeCall = HasDisposeCall(parent1.Parent as VariableDeclaratorSyntax,
                                        methodDeclaration, false, constructorDeclaration,
                                        propertyDeclaration, false, ref foundeOutOfOwnMethodDeclaration,
                                        out memberContainsDispose);
                                }

                                isDisposed = hasDisposeCall;
                            }
                            else if (invocation.Expression != null && invocation.Expression is IdentifierNameSyntax)
                            {
                                IdentifierNameSyntax identifierNameSyntax =
                                    invocation.Expression as IdentifierNameSyntax;
                                SyntaxNode syntaxNode = identifierNameSyntax?.Parent;
                                SyntaxNode parent1;
                                bool insideUsing;
                                bool usedInUsingLater;
                                MethodDeclarationSyntax methodDeclaration;
                                ConstructorDeclarationSyntax constructorDeclaration;
                                PropertyDeclarationSyntax propertyDeclaration;
                                var hasDisposeCall = BelongsUsing(syntaxNode, out methodSignature, out parent1, out insideUsing,
                                    out usedInUsingLater, out methodDeclaration, out constructorDeclaration, out propertyDeclaration);

                                if (!hasDisposeCall)
                                {
                                    bool foundeOutOfOwnMethodDeclaration = false;
                                    string memberContainsDispose;
                                    hasDisposeCall = HasDisposeCall(parent1.Parent as VariableDeclaratorSyntax,
                                        methodDeclaration, false, constructorDeclaration,
                                        propertyDeclaration, false, ref foundeOutOfOwnMethodDeclaration,
                                        out memberContainsDispose);
                                }

                                isDisposed = hasDisposeCall;
                            }

                            int lineNumber = syntaxTree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;

                            if (!isDisposed)
                            {
                                if (string.IsNullOrEmpty(methodSignature) ||
                                    methodSignature == METHOD_SIGNATURE_NOT_FOUNDED)
                                {
                                    MethodDeclarationSyntax methodDeclaration;
                                    ConstructorDeclarationSyntax constructorDeclaration;
                                    PropertyDeclarationSyntax propertyDeclaration;
                                    GetMethodSignature(invocation.Expression, ref methodSignature, out methodDeclaration,
                                        out constructorDeclaration, out propertyDeclaration);
                                }

                                MessageWriter.WriteMessage(
                                    csvWriter,
                                    messageKey: "PotentialLeak",
                                    color: ConsoleColor.Yellow,
                                    csvStatusKey: "Csv_PotentialLeak",
                                    project: project.Name,
                                    filePath: document.FilePath,
                                    lineNumber: lineNumber,
                                    className: symbol.ReturnType.ToString(),
                                    methodSignature: methodSignature
                                );
                            }
                            keyResults.Add(new KeyResult(proyecto: project.Name, archivo: document.FilePath,
                                línea: lineNumber, clase: symbol.ReturnType.ToString(), método: methodSignature));
                        }
                    }
                }
            }
        }

        private static void AnalizeObjectCreationCase(SyntaxNode root, HashSet<string> disposableClasses, SemanticModel semanticModel,
            SyntaxTree syntaxTree, Document document, StreamWriter csvWriter, Project project, HashSet<KeyResult> keyResults)
        {
            //var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            //    .Where(o => disposableClasses.Contains(semanticModel.GetTypeInfo(o).Type?.ToString()));
            var objectCreations = root.DescendantNodes()
                .Where(x => x is ObjectCreationExpressionSyntax
                            || x is VariableDeclaratorSyntax
                            || x is AssignmentExpressionSyntax
                            || x is InvocationExpressionSyntax)
                .Where(o => disposableClasses.Contains(semanticModel.GetTypeInfo(o).Type?.ToString()));

            foreach (var objCreation in objectCreations)
            {
                // Check if the ObjectCreation is directly inside a return
                var returnStatement = objCreation.Ancestors().OfType<ReturnStatementSyntax>().FirstOrDefault();
                if (returnStatement != null && returnStatement.Expression == objCreation)
                {
                    continue;
                }

                // Checks if objCreation is a varaible tha is returned later.
                if (IsVariableReturned(objCreation))
                {
                    continue;
                }


                string methodSignature;
                SyntaxNode parent;
                bool insideUsing;
                bool usedInUsingLater;
                MethodDeclarationSyntax methodDeclaration;
                ConstructorDeclarationSyntax constructorDeclaration;
                PropertyDeclarationSyntax propertyDeclaration;
                var hasDisposeCall = BelongsUsing(objCreation, out methodSignature, out parent, out insideUsing,
                    out usedInUsingLater, out methodDeclaration, out constructorDeclaration, out propertyDeclaration);

                string className = semanticModel.GetTypeInfo(objCreation).Type.ToString();
                if (!insideUsing && !usedInUsingLater)
                {
                    bool isPropertyOrAttribute = false;
                    string variableName = GetVariableName(semanticModel, parent, objCreation, ref isPropertyOrAttribute);

                    bool foundeOutOfOwnMethodDeclaration = false;
                    string memberContainsDispose;
                    hasDisposeCall = HasDisposeCall(methodDeclaration, constructorDeclaration, propertyDeclaration,
                                                    variableName, isPropertyOrAttribute,
                                                    ref foundeOutOfOwnMethodDeclaration,
                                                    out memberContainsDispose);

                    int lineNumber = syntaxTree.GetLineSpan(objCreation.Span).StartLinePosition.Line + 1;
                    if (!hasDisposeCall)
                    {
                        // project.Name.Trim() == "ComfiarWCF" && document.FilePath.Trim() == "D:\\Source\\APG\\COMFIAR7\\Comfiar7\\ComfiarWCF\\ServiciosWCF\\Aplicacion\\WCFCuit.cs" && lineNumber == 559 && className.Trim() == "System.Data.SqlClient.SqlCommand" && methodSignature.Trim() == "private int ComfiarWCF.ServiciosWCF.Aplicacion.WCFCuit.CantidadComprobantesAutorizados(Contratos.Cuit c, string puntoDeVentaId, string tipoComprobanteId, DateTime? fechaDesde, DateTime? fechaHasta)"
                        if (!keyResults.Contains(new KeyResult(proyecto: project.Name, archivo: document.FilePath,
                                línea: lineNumber, clase: className, método: methodSignature)))
                        {
                            MessageWriter.WriteMessage(
                                    csvWriter,
                                    messageKey: "MemoryLeak",
                                    color: ConsoleColor.Red,
                                    csvStatusKey: "Csv_MemoryLeak",
                                    project: project.Name,
                                    filePath: document.FilePath,
                                    lineNumber: lineNumber,
                                    className: className,
                                    methodSignature: methodSignature
                                );

                            keyResults.Add(new KeyResult(proyecto: project.Name, archivo: document.FilePath,
                                línea: lineNumber, clase: className, método: methodSignature));
                        }
                    }
                    else if (isPropertyOrAttribute && foundeOutOfOwnMethodDeclaration)
                    {
                        if (!keyResults.Contains(new KeyResult(proyecto: project.Name, archivo: document.FilePath,
                                línea: lineNumber, clase: className, método: methodSignature)))
                        {
                            MessageWriter.WriteMessage(
                                csvWriter,
                                messageKey: "DisposeFoundInMember",
                                color: ConsoleColor.Green,
                                csvStatusKey: "Csv_DisposeInOtherMember",
                                project: project.Name,
                                filePath: document.FilePath,
                                lineNumber: lineNumber,
                                className: className,
                                methodSignature: methodSignature,
                                extraInfo: $"{Messages.Get("ExtraInfoDisposeFoundInMember")} {memberContainsDispose}"
                            );


                            keyResults.Add(new KeyResult(proyecto: project.Name, archivo: document.FilePath,
                                línea: lineNumber, clase: className, método: methodSignature));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if objCreation is a varaible tha is returned later.
        /// </summary>
        /// <param name="objCreation"></param>
        /// <returns></returns>
        private static bool IsVariableReturned(SyntaxNode objCreation)
        {
            if (objCreation == null || objCreation.Parent == null)
            {
                return false;
            }

            // Early exit for constructors.
            // NOTE. - Constructors don´t have return sentence.
            if (objCreation.Ancestors().OfType<ConstructorDeclarationSyntax>().Any())
            {
                return false;
            }

            string variableName = GetVariableNameIfIsVariableDeclaratorSyntax(objCreation.Parent);
            if (string.IsNullOrEmpty(variableName))
            {
                return false;
            }

            var declaration = objCreation.Ancestors()
                .FirstOrDefault(x =>
                    x is MethodDeclarationSyntax || x is PropertyDeclarationSyntax ||
                    x is LocalFunctionStatementSyntax);

            if (declaration == null)
            {
                return false;
            }

            return declaration.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Any(returnStatement => IsVariableUsedInReturnExpression(returnStatement.Expression, variableName));
        }

        private static bool IsVariableUsedInReturnExpression(ExpressionSyntax expression, string variableName)
        {
            if (expression == null)
            {
                return false;
            }

            // Case 1: Direct use (return variable;) 
            if (expression is IdentifierNameSyntax)
            {
                IdentifierNameSyntax identifier = (IdentifierNameSyntax)expression;
                if (identifier.Identifier.Text == variableName)
                {
                    return true;
                }
            }

            // Case 2: Tuples (return (variable, 123);) 
            if (expression is TupleExpressionSyntax)
            {
                TupleExpressionSyntax tuple = (TupleExpressionSyntax)expression;
                return tuple.Arguments
                    .Any(arg => IsVariableUsedInReturnExpression(arg.Expression, variableName));
            }

            // Case 3: Fields/properties (return this.field; or return obj.Prop;) 
            if (expression is MemberAccessExpressionSyntax)
            {
                MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)expression;
                if (memberAccess.Expression is IdentifierNameSyntax)
                {
                    IdentifierNameSyntax memberIdentifier = (IdentifierNameSyntax)memberAccess.Expression;
                    if (memberIdentifier.Identifier.Text == variableName)
                    {
                        return true;
                    }
                }
            }

            // Case 4: Anonymous expressions (return new { X = variable };) 
            if (expression is AnonymousObjectCreationExpressionSyntax)
            {
                AnonymousObjectCreationExpressionSyntax anonymousObject =
                    (AnonymousObjectCreationExpressionSyntax)expression;
                return anonymousObject.Initializers
                .Any(init => IsVariableUsedInReturnExpression(init.Expression, variableName));
            }

            // Case 5: Search in nested expressions (return something.Where(x => x == variable);)
            return expression.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == variableName);
        }


        /// <summary>
        /// Determines the name of a variable given an object creation and semantic context.
        /// See Mermaid diagram for details:https://mermaid.live/edit#pako:eNqlVm1v2jAQ_iuWv7SVKGrGO9I2VbSVKrEWDTRpI3wwiSnZEjtynBVG-e-znThOgoFq4wOy73x3z734cXbQoz6GQ7gK6au3RoyD2Z1LgPglXOwu5y6cyoULF1fg-voT8NbY-zVBDBO-e0zARayWFwCBb4gFaBniO-yFiCFO2XRLONp83rskc1mylb7evuPkDaAkCV7IA6NRpplPMQe_c19PKMJgJXQgi9N89MV_sAowa87whi_sjp_oW1l2ALc5-TfU57C_J4PJOxOppZNgxLz1KBQhNdSAEhFKikFdDhDxcCKSWegs6lBVMxnmKSOiBPOvalVBvbAb2sx1EDtIlcMDTYmfd-V5-XPEsNLdKu9R3h5qFMBo7jcxw2JHiWnNmWhPlIM8YnbqNq9HoisWmBoli0qrrehMz9WZ6TZa0lBCTtRKDNJDgEMfUAYmjMaY8a2GecapaW5uGODkkaisNFbtsZRlVgmZhSdPAt-oqtlkSOsja6bvyLCaAwu7sxOwqyNnPKmZSTDX6cxYiudBorfP7JZzFixTjsFHwIVyUZqykYx_H8V8Wx22g-jKJqQ0NhpBYmMhAHzNaPpirWciCC5zWzUtcV5upCdppxcgQlycqExvnT1qtqYdohyagdQVyw8e6YpWN-3tsYaRjcLEH9crMqLCA0mxSlhlr8tqRWRr3of_6N4BpHf0urjERY_Nta612Bylq-KRKmVZMTZN_oL5mvqzbYzl3dYEATJxmWSE8IB4Sne_JK6NgolQGgJOY4l-PhULIFeL4xbF21aiRY441hxagLborU-b5VyN7WxULEOZnYh6grDBa8DXJUqRA12l8pNAVMb5tI5rTS-meFyd4qO4bY_3WTY0ji6SI9R4NNZR9Icvc40qj10Gq7fDK1HcnXy6Tvqsy2V7K3XAUqrbVT9tippplFB_U7iw4FarnSzQwZdEIVCo74lfFhe2mSJTieXlpfi7unIJbMAIswgFvvi03Um9C_laTJQLh2Lp4xVKQ_FN65K9OIpSTsU4enAoaasBFYPA4QqFidilsS-G8S5ALwxF-kiMyA9Ky1s43MENHF47LafZvune9AetXqfddzrtBtzCodNudtptp9frO73uwOl2nX0D_lEuWs3WYDBwbtrtwaDvdPuDzv4vi2IwVw
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="parent"></param>
        /// <param name="objCreation"></param>
        /// <param name="isPropertyOrAttribute"></param>
        /// <returns></returns>
        /// <seealso cref="IsSomeTypeOfMethodOrClassDeclaration"/>
        /// <seealso cref="GetLastIdentifierNameSyntax"/>
        private static string GetVariableName(SemanticModel semanticModel, SyntaxNode parent, SyntaxNode objCreation,
            ref bool isPropertyOrAttribute)
        {
            string variableName = string.Empty;
            VariableDeclaratorSyntax variableDeclaratorSyntax = parent as VariableDeclaratorSyntax;

            if (variableDeclaratorSyntax == null)
            {
                variableDeclaratorSyntax = parent.Parent as VariableDeclaratorSyntax;
            }

            if (variableDeclaratorSyntax != null)
            {
                variableName = variableDeclaratorSyntax?.Identifier.Text;
            }
            else
            {
                //var desc = objCreation.DescendantNodes();
                ClassDeclarationSyntax classDeclarationSyntax = objCreation.Ancestors()
                    .OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclarationSyntax != null)
                {
                    if (objCreation is AssignmentExpressionSyntax)
                    {
                        AssignmentExpressionSyntax assignmentExpressionSyntax = objCreation as AssignmentExpressionSyntax;
                        var symbol = semanticModel.GetSymbolInfo(assignmentExpressionSyntax.Left).Symbol;

                        if (symbol is IFieldSymbol || symbol is IPropertySymbol)
                        {
                            IdentifierNameSyntax identifierNameSyntax = GetLastIdentifierNameSyntax(assignmentExpressionSyntax.Left);
                            variableName = identifierNameSyntax?.Identifier.Text;
                            isPropertyOrAttribute = true;
                        }
                    }

                    if (string.IsNullOrEmpty(variableName))
                    {
                        var propertyDeclarationSyntaxs = classDeclarationSyntax.DescendantNodes()
                            .OfType<PropertyDeclarationSyntax>();

                        foreach (var p in propertyDeclarationSyntaxs)
                        {
                            if (p == objCreation.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault())
                            {
                                variableName = p.Identifier.Text;
                                isPropertyOrAttribute = true;
                                break;
                            }
                        }
                        //if (string.IsNullOrEmpty(variableName))
                        //{
                        //var fieldDeclarationSyntax = classDeclarationSyntax.DescendantNodes()
                        //    .OfType<FieldDeclarationSyntax>();
                        //foreach (var a in fieldDeclarationSyntax)
                        //{
                        //    foreach (var v in a.Declaration.Variables)
                        //    {
                        //        if ((bool)objCreation.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault()?.Declaration?.Variables.Any(vv => vv == v))
                        //        {
                        //            variableName = v.Identifier.Text;
                        //            isPropertyOrAttribute = true;
                        //            break;
                        //        }
                        //    }
                        //}
                        //}
                    }
                }

                if (string.IsNullOrEmpty(variableName))
                {
                    var ancestors = parent.Ancestors();
                    foreach (var a in ancestors)
                    {
                        if (IsSomeTypeOfMethodOrClassDeclaration(a))
                        {
                            break;
                        }

                        if (a is ExpressionStatementSyntax)
                        {
                            ExpressionStatementSyntax e = a as ExpressionStatementSyntax;
                            if (e.Expression is AssignmentExpressionSyntax)
                            {
                                AssignmentExpressionSyntax assignment = e.Expression as AssignmentExpressionSyntax;
                                if (assignment != null && assignment.Left != null && assignment.Left is IdentifierNameSyntax)
                                {
                                    IdentifierNameSyntax identifierNameSyntax =
                                        GetLastIdentifierNameSyntax(assignment.Left);
                                    variableName = identifierNameSyntax.Identifier.Text;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return variableName;
        }


        /// <summary>
        /// Analize Case A.B.C.D.PropertyOrField
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        static IdentifierNameSyntax GetLastIdentifierNameSyntax(ExpressionSyntax expression)
        {
            if (expression is MemberAccessExpressionSyntax)
            {
                MemberAccessExpressionSyntax memberAccess = expression as MemberAccessExpressionSyntax;
                return GetLastIdentifierNameSyntax(memberAccess.Name);
            }
            if (expression is IdentifierNameSyntax)
            {
                IdentifierNameSyntax identifier = expression as IdentifierNameSyntax;
                return identifier;
            }

            return null;
        }

        /// <summary>
        /// Call method that look for a Dispose call inside methodDeclaration, constructorDeclaration or
        /// propertyDeclaration that creates instance of Dispoable class.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="methodDeclaration"></param>
        /// <param name="hasDisposeCall"></param>
        /// <param name="constructorDeclaration"></param>
        /// <param name="propertyDeclaration"></param>
        /// <param name="isPropertyOrAttribute"></param>
        /// <param name="foundeOutOfOwnMethodDeclaration"></param>
        /// <param name="memberContainsDispose"></param>
        /// <returns></returns>
        private static bool HasDisposeCall(VariableDeclaratorSyntax parent, MethodDeclarationSyntax methodDeclaration, bool hasDisposeCall,
            ConstructorDeclarationSyntax constructorDeclaration, PropertyDeclarationSyntax propertyDeclaration,
            bool isPropertyOrAttribute, ref bool foundeOutOfOwnMethodDeclaration, out string memberContainsDispose)
        {
            memberContainsDispose = string.Empty;
            var variableDeclaratorSyntax = parent;
            if (variableDeclaratorSyntax != null)
            {
                hasDisposeCall =
                    HasDisposeCall(methodDeclaration, constructorDeclaration, propertyDeclaration, variableDeclaratorSyntax.Identifier.Text, isPropertyOrAttribute,
                        ref foundeOutOfOwnMethodDeclaration, out memberContainsDispose);
            }

            return hasDisposeCall;
        }

        /// <summary>
        /// Look for a Dispose call inside methodDeclaration, constructorDeclaration or propertyDeclaration that
        /// creates instance of Disposable class.
        /// If not found, then look for the Dispose call in the class that contains invocationExpressionSyntax.
        /// See Mermaid Diagram in:https://mermaid.live/edit#pako:eNqVVl1vmzAU_SuWn1oprZYvWpC2aUrSrg9tprUvW-DBBaeggR3ZZk0a5b_PNh8hYBztBZnre8_xub73wh6GNMLQg-uUvocxYgK8zH3ik2ch1xcrH-qFD4NLcHX1BcxiHP55xCKm0RyHKWJIJJTsHzjI2kZAqAAkT9OvBwVojpSg4BfmGvsZIxbG84RvKMcPpPBdFVZQmkGI0hQkpEsX2Cie6PH0M0q4YHkoKGtJCI07Jh1mDIuYRkCfIjN7cJaxqe0HoxvMxK4lbNM1m1QZoi2SKu8-PQbSwE5kUrJk34RgyWsusC5LU42UpxOPOHvFTGZKoITw0qkuIyFrxrAPBC2LCRCU4aDD0ci8lej0ii1sjYvuoazEW_ka-beQVbdQMSkuW6KON_BA_tJQ380dzUnUH9hO0P9Fn2g1h5ZV07Lvj--L7YZhzuXqeSfBt2CtHFRlmwJPa1p8R9VRXliOdTbj2jRTxfwZSHk46Ec7U7iqBRNu2NC4hgZsuhzhf2KRM1Ie7A6lHK8KE1irl8AOUim-S0g0SxHnjeZbKSNoWwEiIebyVnXZmOIK1S3jYptwwfcd3-OdGAOsQm1BvZ8PVWy8d9oqvLJteGAcLXpLQx-vvLYXJVihJrVDIbP4QmlHpbgv_oxmW1il-r5qqyfZ36v7ehToflejRvVaQt6qBGipJ0FVH2joZS6W62IS1N2gFWG9s3wn3e9r3R49IOZGK0fRqbGbCn2KssxLmm6qdNiCRBcX8nF52XKpgRfFMIEDmGGWoSSS_z17nwDgQxHjDPvQk8sIr1Geyl8enxykK8oFlVMlhJ6iH0BG87cYerrlBjDfREjgeYLeGMoqlw0ivyltvkJvD7fQmzrXU3c8dm4c1xlPnLEzgDvoja6nk8lwNLm5nQ5HQ0cuDgP4oQHG12PXdYefJhPXvR067uEf_-mPQw
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <param name="constructorDeclaration"></param>
        /// <param name="propertyDeclaration"></param>
        /// <param name="variableName"></param>
        /// <param name="isPropertyOrAttribute">True if methodDeclaration is a property or an attribute</param>
        /// <param name="foundeOutOfOwnMethodDeclaration">Setted to true value if Dispose was found inside other method,
        /// property or constructor in class tha contains invocationExpressionSyntax</param>
        /// <param name="memberContainsDispose"></param>
        /// <returns></returns>
        private static bool HasDisposeCall(MethodDeclarationSyntax methodDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration, PropertyDeclarationSyntax propertyDeclaration,
            string variableName, bool isPropertyOrAttribute, ref bool foundeOutOfOwnMethodDeclaration,
            out string memberContainsDispose)
        {
            bool hasDisposeCall = false;
            memberContainsDispose = string.Empty;
            InvocationExpressionSyntax invocationExpressionSyntax = null;
            if (methodDeclaration != null)
            {
                invocationExpressionSyntax = methodDeclaration.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(invocation =>
                    {
                        var identifier = invocation.Expression as MemberAccessExpressionSyntax;
                        return identifier?.Name.Identifier.Text == "Dispose"
                               && identifier.Expression.ToString() == variableName;
                    });
                memberContainsDispose = methodDeclaration.Identifier.Text;
            }
            else if (constructorDeclaration != null)
            {
                invocationExpressionSyntax = constructorDeclaration.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(invocation =>
                    {
                        var identifier = invocation.Expression as MemberAccessExpressionSyntax;
                        return identifier?.Name.Identifier.Text == "Dispose"
                               && identifier.Expression.ToString() == variableName;
                    });
                memberContainsDispose = constructorDeclaration.Identifier.Text;
            }
            else if (propertyDeclaration != null)
            {
                invocationExpressionSyntax = propertyDeclaration.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(invocation =>
                    {
                        var identifier = invocation.Expression as MemberAccessExpressionSyntax;
                        return identifier?.Name.Identifier.Text == "Dispose"
                               && identifier.Expression.ToString() == variableName;
                    });
                memberContainsDispose = propertyDeclaration.Identifier.Text;
            }

            if (invocationExpressionSyntax == null && isPropertyOrAttribute) // If Dispose call was not found in method,
                                                                             // constructor or property, that create
                                                                             // instance for IDisposable class
            {
                var classDeclaration =
                    methodDeclaration?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclaration == null)
                {
                    classDeclaration =
                        constructorDeclaration?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                }

                if (classDeclaration == null)
                {
                    classDeclaration =
                        propertyDeclaration?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                }

                if (classDeclaration != null)
                {
                    var methodsDeclarationsClass =
                        classDeclaration.DescendantNodes().Where(x => IsSomeTypeOfMethodOrClassDeclaration(x));
                    foreach (var methodDeclarationSyntax in methodsDeclarationsClass)
                    {
                        invocationExpressionSyntax = methodDeclarationSyntax.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .FirstOrDefault(invocation =>
                            {
                                var identifier = invocation.Expression as MemberAccessExpressionSyntax;
                                return identifier?.Name.Identifier.Text == "Dispose"
                                       && identifier.Expression.ToString() == variableName;
                            });
                        if (invocationExpressionSyntax != null)
                        {
                            var member = invocationExpressionSyntax.Ancestors().FirstOrDefault(x =>
                                x is MethodDeclarationSyntax || x is ConstructorDeclarationSyntax ||
                                x is PropertyDeclarationSyntax || x is LocalFunctionStatementSyntax);
                            if (member != null)
                            {
                                memberContainsDispose = member is MethodDeclarationSyntax
                                    ? ((MethodDeclarationSyntax)member).Identifier.Text
                                    : member is ConstructorDeclarationSyntax
                                        ? ((ConstructorDeclarationSyntax)member).Identifier.Text
                                        : member is LocalFunctionStatementSyntax 
                                            ? ((LocalFunctionStatementSyntax)member).Identifier.Text
                                                : ((PropertyDeclarationSyntax)member).Identifier.Text;
                                string prefixNameSpaceClassName = GertPrefixNamespaceClass(member);
                                memberContainsDispose = $"{prefixNameSpaceClassName}{memberContainsDispose}";
                            }
                            foundeOutOfOwnMethodDeclaration = true;
                            break;
                        }
                    }
                }
            }

            hasDisposeCall = invocationExpressionSyntax != null;

            return hasDisposeCall;
        }

        /// <summary>
        /// Get the prefix of the namespace and class.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static string GertPrefixNamespaceClass(SyntaxNode member)
        {
            string prefixNameSpaceClassName = string.Empty;
            ClassDeclarationSyntax classDeclaration = member.Ancestors()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration != null)
            {
                prefixNameSpaceClassName = classDeclaration.Identifier.Text;
                var nameSpaceDeclaratrion = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if (nameSpaceDeclaratrion != null)
                {
                    prefixNameSpaceClassName = nameSpaceDeclaratrion.Name.ToString() + "." + prefixNameSpaceClassName;
                }

                prefixNameSpaceClassName += ".";
            }

            return prefixNameSpaceClassName;
        }

        /// <summary>
        /// Checks if the object creation is inside a `using` statement or is outside but is used in a `using` statement
        /// later.
        /// See Mermaid Diagram in:https://mermaid.live/edit#pako:eNqNVttu2zgQ_RWCTwngZKvI3ixc7BZtkhYG2qbIDehaemCkcaxWIr0k1SY18u9LDkWJsuSgeXCkw7nxnOFQW5qJHOicrkrxM1szqcnNecITfq3N88EyofiQ0PSQHB39Qxa80AUri19wx2TB7ktQyw4jPzxIGM-JqPWm1mqe8IKrIodbVfAH8jdZsVLB64TXCvIFR_Qj0yCDJaWltfXxPrMKzCqvy_J1assbqQPrO1tD9v0Lk8D1J6juQb7NMlBqu1BkgygJ4YvHjTT_CsGvn7hmj2-eE74ngolOvjZJPoC--K82ld6xsoazkpmNLA1IBqihwThrIdMXA38WGPca9NvGXi3NS-utyEqKioj7b2cSmDb1IgljdWCg201u-HS5lu7Fb79YjVS5EjXPTcjQb1ARdkXw3vHtJTiHrGQS6wsIH6TruG5IGfHfoXvXQkjk27cH4bY_kKOhpUuXvpyrUeBGsh8gFbSbxOZcejgQRAGT2dr26EpIgmbmpGiozJbbjCjRsCJP7V3Q3ah32O4psh2avFChTTW-gk4fhdjsbMlCRK-lqB_W3baw5qE1BvHQQn0CvRa5ldg7EkYcGND6h1FbqRD4IsUGpH4KMNsFu4FD7d-Zfv_eK2iJECkNlo77Nlp2S311enWPCec6c597kOFMcF3wGvr1eXRY4jDQ13BsXd5_g0w3A3GJkD2uAmFSKJIjb5CTgrvC0_YI9XwxZA95bw_41kHutLcx7G6HtmFxpg0X3fy-kbVr1v5M1wZO90V6ia9mrOxk6FgJFi6lPw4JHzbGcB7ZQ3NRbfSTlbx3lYBFRyZQ6zEQZ08Z-739nkODW3Ph4U3XqduOMKOvvQ-tLiVeho5Z5dslHRTbRsNEA3TrERfXBQ1VH4sTin67cz23yo_c2438oyFDJvbQaHY21ht7hlfTMmP1_U6mvav-tnGj5Lp44EzX0l3tVR_D7xsJltS8OZc4z5Sf-jsxMPQVmEd-Baou9TY8PpdXA06tQKF9qI3DUQ736OnfdWiYd_B7-2nlHfA7C2vtgqHtBc8PDszP4aFfQj-_lnA6oRXIihW5-WrcJpyQhOq1adGEzs1jDitmsic04c_GlNVamKma0bktcULxvqFzTD-hNX5vnBfsQbKqRTeM_ytE5V3MK51v6SOdH82Op_HJ9K_oVRSdxNNoOpvQJzqPpsfTmfmLT2evTman8Sx6ntBfGGF2HMV_RvE0juOT6DSKps__A3W1-as
        /// </summary>
        /// <param name="objCreation"></param>
        /// <param name="methodSignature"></param>
        /// <param name="parent"></param>
        /// <param name="insideUsing"></param>
        /// <param name="usedInUsingLater"></param>
        /// <param name="methodDeclaration"></param>
        /// <param name="constructorDeclarationSyntax"></param>
        /// <param name="propertyDeclaration"></param>
        /// <returns></returns>
        /// <seealso cref="IsSomeTypeOfMethodOrClassDeclaration"/>
        /// <seealso cref="GetMethodSignature"/>
        private static bool BelongsUsing(SyntaxNode objCreation, out string methodSignature, out SyntaxNode parent,
            out bool insideUsing, out bool usedInUsingLater, out MethodDeclarationSyntax methodDeclaration,
            out ConstructorDeclarationSyntax constructorDeclarationSyntax,
            out PropertyDeclarationSyntax propertyDeclaration)
        {
            methodSignature = "N/A";
            methodDeclaration = null;

            var eq = EqualsValueClauseSyntax(objCreation, out parent);

            var ancestors = objCreation.Ancestors();
            insideUsing = false;
            usedInUsingLater = false;

            // Check if the object is in a variable declaration
            string variableName = GetVariableNameIfIsVariableDeclaratorSyntax(parent);

            // Traverse all ancestors until a `Using Statement Syntax` is found
            foreach (var ancestor in ancestors ?? new List<SyntaxNode>())
            {
                if (IsSomeTypeOfMethodOrClassDeclaration(ancestor))
                {
                    // In order not to iterate in vain, if there were no using that contains it
                    break;
                }

                if (ancestor is UsingStatementSyntax)
                {
                    UsingStatementSyntax usingStatement = (UsingStatementSyntax)ancestor;
                    // NOTE. - We verify that the object is part of the using declaration
                    // Case
                    // using(var o = new ObjectUsesIDisposable())
                    // {
                    //      // Some operations
                    //      var other = new OtherObjectUsesIDisposable();
                    // }
                    if (usingStatement.Declaration != null &&
                        usingStatement.Declaration.Variables.Any(v => v.Initializer?.Value == objCreation || (eq != null && eq == v.Initializer)))
                    {
                        insideUsing = true;
                        break;
                    }
                }
            }

            // NOTE. - If not inside a `using`, check if the variable is subsequently used in one
            // Case
            // var o = new ObjectUsesIDisposable();
            // using(o)
            // {
            //      // Some operations
            // }
            if (!insideUsing && !string.IsNullOrEmpty(variableName))
            {
                var methodRoot = ancestors != null
                    ? ancestors.FirstOrDefault(IsSomeTypeOfMethodOrClassDeclaration)
                    : null;
                if (methodRoot != null)
                {
                    // It is searched within the method definition, if the variable defined outside of a using is enclosed within a
                    // using that will free it.
                    var laterUsing = methodRoot.DescendantNodes()
                        .OfType<UsingStatementSyntax>()
                        .Where(us => us.Expression != null && us.Expression is IdentifierNameSyntax)
                        .ToList()
                        .Where(us => ((IdentifierNameSyntax)us.Expression)?.Identifier.ValueText == variableName);

                    if (laterUsing.Any())
                    {
                        usedInUsingLater = true;
                    }
                }
            }

            GetMethodSignature(objCreation, ref methodSignature, out methodDeclaration, out constructorDeclarationSyntax, out propertyDeclaration);
            return insideUsing || usedInUsingLater;
        }

        private static EqualsValueClauseSyntax EqualsValueClauseSyntax(SyntaxNode objCreation, out SyntaxNode parent)
        {
            parent = objCreation.Parent;
            EqualsValueClauseSyntax eq = null;
            if (parent != null && parent is MemberAccessExpressionSyntax)
            {
                MemberAccessExpressionSyntax memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)parent;
                if (memberAccessExpressionSyntax.Expression != null)
                {
                    eq = memberAccessExpressionSyntax.Ancestors().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                    if (eq != null)
                    {
                        parent = eq;
                    }
                }
            }

            return eq;
        }

        private static string GetVariableNameIfIsVariableDeclaratorSyntax(SyntaxNode parent)
        {
            string variableName = null;
            if (parent is EqualsValueClauseSyntax)
            {
                EqualsValueClauseSyntax equalsValueClause = (EqualsValueClauseSyntax)parent;
                if (equalsValueClause.Parent != null && equalsValueClause.Parent is VariableDeclaratorSyntax)
                {
                    VariableDeclaratorSyntax variableDeclarator = (VariableDeclaratorSyntax)equalsValueClause.Parent;
                    variableName = variableDeclarator.Identifier.Text;
                }
            }

            return variableName;
        }

        /// <summary>
        /// Checks if the syntaxNode is a method declaration or class declaration.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <returns></returns>
        private static bool IsSomeTypeOfMethodOrClassDeclaration(SyntaxNode syntaxNode)
        {
            return syntaxNode is MethodDeclarationSyntax || syntaxNode is ClassDeclarationSyntax ||
                   syntaxNode is ConstructorDeclarationSyntax || syntaxNode is PropertyDeclarationSyntax || 
                   syntaxNode is LocalFunctionStatementSyntax;
        }

        /// <summary>
        /// Get the method signature (with namespace and class name) of the object SyntaxNode.
        /// </summary>
        /// <param name="objCreation"></param>
        /// <param name="methodSignature"></param>
        /// <param name="methodDeclaration"></param>
        /// <param name="constructor"></param>
        /// <param name="propertyDeclaration"></param>
        private static void GetMethodSignature(SyntaxNode objCreation, ref string methodSignature,
            out MethodDeclarationSyntax methodDeclaration, out ConstructorDeclarationSyntax constructor,
            out PropertyDeclarationSyntax propertyDeclaration)
        {
            constructor = null;
            propertyDeclaration = null;
            methodDeclaration = objCreation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration == null)
            {
                constructor = objCreation.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                if (constructor == null)
                {
                    propertyDeclaration = objCreation.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                    if (propertyDeclaration != null)
                    {
                        GetMethodSignature(out methodSignature, propertyDeclaration);
                    }
                    else
                    {
                        LocalFunctionStatementSyntax localFunctionStatementSyntax = objCreation as LocalFunctionStatementSyntax;
                        if (localFunctionStatementSyntax != null)
                        {
                            methodSignature = GetMethodSignature(localFunctionStatementSyntax, methodSignature);
                        }
                    }
                }
                else
                {
                    string prefixNameSpaceClassName = string.Empty;
                    var classDeclaration = constructor.Ancestors()
                        .OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    if (classDeclaration != null)
                    {
                        prefixNameSpaceClassName = classDeclaration.Identifier.Text;
                        var namespaceDeclarationSyntax = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                        if (namespaceDeclarationSyntax != null)
                        {
                            prefixNameSpaceClassName = namespaceDeclarationSyntax.Name.ToString() + "." + prefixNameSpaceClassName;
                        }

                        prefixNameSpaceClassName += ".";
                    }

                    methodSignature =
                        $"{constructor.Modifiers} {prefixNameSpaceClassName}{constructor.Identifier}({string.Join(", ", constructor.ParameterList.Parameters)})"
                            .Trim();
                }
            }
            else
            {
                methodSignature = GetMethodSignature(methodDeclaration, methodSignature);
            }
        }

        /// <summary>
        /// Get the method signature (with namespace and class name) of the PropertyDeclarationSyntax.
        /// </summary>
        /// <param name="methodSignature"></param>
        /// <param name="propertyDeclaration"></param>
        private static void GetMethodSignature(out string methodSignature, PropertyDeclarationSyntax propertyDeclaration)
        {
            string prefixNameSpaceClassName = string.Empty;
            var classDeclaration = propertyDeclaration.Ancestors()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration != null)
            {
                prefixNameSpaceClassName = classDeclaration.Identifier.Text;
                var nameSpaceDeclaratrion = propertyDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if (nameSpaceDeclaratrion != null)
                {
                    prefixNameSpaceClassName = nameSpaceDeclaratrion.Name.ToString() + "." + prefixNameSpaceClassName;
                }

                prefixNameSpaceClassName += ".";
            }

            methodSignature =
                $"{propertyDeclaration.Modifiers} {propertyDeclaration.Type} {prefixNameSpaceClassName}{propertyDeclaration.Identifier}"
                    .Trim();
        }

        /// <summary>
        /// Get the method signature (with namespace and class name) of the MethodDeclarationSyntax.
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <param name="methodSignature"></param>
        /// <returns></returns>
        private static string GetMethodSignature(MethodDeclarationSyntax methodDeclaration, string methodSignature)
        {
            if (methodDeclaration != null)
            {
                string prefixNameSpaceClassName = string.Empty;
                ClassDeclarationSyntax classDeclaration = methodDeclaration.Ancestors()
                    .OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclaration != null)
                {
                    prefixNameSpaceClassName = classDeclaration.Identifier.Text;
                    var nameSpaceDeclaratrion = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    if (nameSpaceDeclaratrion != null)
                    {
                        prefixNameSpaceClassName = nameSpaceDeclaratrion.Name.ToString() + "." + prefixNameSpaceClassName;
                    }

                    prefixNameSpaceClassName += ".";
                }
                methodSignature = $"{methodDeclaration.Modifiers} {methodDeclaration.ReturnType} {prefixNameSpaceClassName}{methodDeclaration.Identifier}({string.Join(", ", methodDeclaration.ParameterList.Parameters)})".Trim();
            }

            return methodSignature;
        }

        /// <summary>
        /// Get the method signature (with namespace and class name) of the LocalFunctionStatementSyntax.
        /// </summary>
        /// <param name="localFunctionStatementSyntax"></param>
        /// <param name="methodSignature"></param>
        /// <returns></returns>
        private static string GetMethodSignature(LocalFunctionStatementSyntax localFunctionStatementSyntax, string methodSignature)
        {
            if (localFunctionStatementSyntax != null)
            {
                string prefixNameSpaceClassName = string.Empty;
                ClassDeclarationSyntax classDeclaration = localFunctionStatementSyntax.Ancestors()
                    .OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclaration != null)
                {
                    prefixNameSpaceClassName = classDeclaration.Identifier.Text;
                    var nameSpaceDeclaration = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    if (nameSpaceDeclaration != null)
                    {
                        prefixNameSpaceClassName = nameSpaceDeclaration.Name.ToString() + "." + prefixNameSpaceClassName;
                    }

                    prefixNameSpaceClassName += ".";
                }
                methodSignature = $"(local function) {localFunctionStatementSyntax.Modifiers} {localFunctionStatementSyntax.ReturnType} {prefixNameSpaceClassName}{localFunctionStatementSyntax.Identifier}({string.Join(", ", localFunctionStatementSyntax.ParameterList.Parameters)})".Trim();
            }

            return methodSignature;
        }
    }
}


    