
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Xunit;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;
using System.CodeDom;
using System;
using System.IO;

namespace TestCases.Workflows
{
    public class SpecialCharactersVBTests
    {
        //We check that Roslyn compilation from an expression tree changes special characters in strings. For example  “ is replaced with ",
        //since “ cannot be used in VB code in a string
        //This happens when compiling workflow expressions in VB.NET for example
        [Fact]
        public void VbCompilation_ShouldReplaceSpecialCharacters()
        {
            // Step 1: Create CodeDOM Structure
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace("DynamicNamespace");
            // Optionally, add imports
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            compileUnit.Namespaces.Add(codeNamespace);

            // Define the TestClass
            CodeTypeDeclaration testClass = new CodeTypeDeclaration("TestClass")
            {
                IsClass = true,
                TypeAttributes = System.Reflection.TypeAttributes.Public
            };
            codeNamespace.Types.Add(testClass);

            // Define the GetMessage method
            CodeMemberMethod getMessageMethod = new CodeMemberMethod
            {
                Name = "GetMessage",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            // Add the return statement: return "“Hello, Roslyn!“";
            getMessageMethod.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodePrimitiveExpression("“This works“")
                )
            );
            testClass.Members.Add(getMessageMethod);

            string vbCode;
            using (VBCodeProvider vbProvider = new VBCodeProvider())
            {
                // Generate VB.NET Code
                vbCode = GenerateCode(vbProvider, compileUnit);
                
            }

            //Compile vbCode with Roslyn
            var syntaxTree = VisualBasicSyntaxTree.ParseText(vbCode);

            var compilation = VisualBasicCompilation.Create(
                assemblyName: "DynamicAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            // Ensure compilation was successful
            Assert.True(result.Success, "Compilation failed: " + string.Join("\n", result.Diagnostics));

            // Load the compiled assembly
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());

            // Get the compiled type and method
            var type = assembly.GetType("DynamicNamespace.TestClass");
            Assert.NotNull(type);
            var method = type.GetMethod("GetMessage", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method);

            // Invoke the method and check the result
            string resultMessage = (string)method.Invoke(null, null);

            //We check that the special character “ is replaced with "
            Assert.Equal("\"This works\"", resultMessage); 

        }


        static string GenerateCode(CodeDomProvider provider, CodeCompileUnit compileUnit)
        {
            using (StringWriter sw = new StringWriter())
            {
                CodeGeneratorOptions options = new CodeGeneratorOptions
                {
                    BracingStyle = "C",
                    IndentString = "    "
                };
                provider.GenerateCodeFromCompileUnit(compileUnit, sw, options);
                return sw.ToString();
            }
        }
    }
}
