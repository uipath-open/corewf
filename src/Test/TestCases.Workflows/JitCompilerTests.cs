using Shouldly;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace TestCases.Workflows
{
    public class JitCompilerTests
    {
        private readonly CSharpJitCompiler _csJitCompiler;
        private readonly VbJitCompiler _vbJitCompiler;
        private readonly string[] _namespaces;

        public JitCompilerTests()
        {
            _namespaces = new[] { "TestCases.Workflows", "System", "System.Linq", "System.Linq.Expressions", "System.Collections.Generic" };
            _vbJitCompiler = new(new HashSet<Assembly> { typeof(string).Assembly, typeof(ClassWithIndexer).Assembly, typeof(Expression).Assembly, typeof(Enumerable).Assembly });
            _csJitCompiler = new(new HashSet<Assembly> { typeof(string).Assembly, typeof(ClassWithIndexer).Assembly, typeof(Expression).Assembly, typeof(Enumerable).Assembly });
        }

        [Fact]
        public void VisualBasicJitCompiler_PropertyAccess()
        {
            var expressionToCompile = "testIndexerClass.Indexer(indexer)";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "index").ShouldBe("index");
        }

        [Fact]
        public void VisualBasicJitCompiler_MethodCall()
        {
            var expressionToCompile = "testIndexerClass.Method(method)";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "method").ShouldBe("method");
        }

        [Fact]
        public void VisualBasicJitCompiler_FieldAccess()
        {
            var expressionToCompile = "testIndexerClass.Field + field";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "field").ShouldBe("field");
        }

        [Fact]
        public void VisualBasicJitCompiler_PropertyAccess_SameNameAsVariable()
        {
            static Type VariableTypeGetter(string name, StringComparison stringComparison)
            {
                return name switch
                {
                    "Indexer" => typeof(TestIndexerClass),
                    _ => typeof(string),
                };
            }

            var expressionToCompile = "Indexer.Indexer(\"indexer\")";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string>)result.Compile())(new TestIndexerClass()).ShouldBe("indexer");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(17)]
        public void VisualBasicJitCompiler_ExpressionWithMultipleVariablesVariables(int noOfVar)
        {
            static Type VariableTypeGetter(string name, StringComparison stringComparison)
            {
                return name switch
                {
                    _ => typeof(bool),
                };
            }
            var variables = Enumerable.Range(0, noOfVar).Select(x => ($"a{x}"));
            var expressionToCompile = string.Join(" AND ", variables);
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(bool)));
            result.ReturnType.ShouldBe(typeof(bool));
        }

        [Fact]
        public void CSharpJitCompiler_PropertyAccess()
        {
            static Type VariableTypeGetter(string name, StringComparison stringComparison)
            {
                return name switch
                {
                    "testIndexerClass" => typeof(TestIndexerClass),
                    "Indexer" => typeof(int), // consider we have "Indexer" variable declared in the current context.
                    "indexer" => typeof(string),
                    _ => null
                };
            }

            var expressionToCompile = "testIndexerClass.Indexer[indexer]";
            var result = _csJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));

            // "Indexer" variable is added as a parameter, but it is fine, that does not trigger a validation error.
            ((Func<TestIndexerClass, int, string, string>)result.Compile())(new TestIndexerClass(), 0, "index").ShouldBe("index");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(17)]
        public void CSharpJitCompiler_ExpressionWithMultipleVariablesVariables(int noOfVar)
        {
            static Type VariableTypeGetter(string name, StringComparison stringComparison)
            {
                return name switch
                {
                    _ => typeof(bool),
                };
            }
            var variables = Enumerable.Range(0, noOfVar).Select(x => ($"a{x}"));
            var expressionToCompile = string.Join(" && ", variables);
            var result = _csJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(bool)));
            result.ReturnType.ShouldBe(typeof(bool));
        }

        [Fact]
        public void VbExpression_UndeclaredObject()
        {
            var expressionToCompile = "new UndeclaredClass()";
            var sut = () => _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, (s, c)=>null, typeof(object)));

            Assert.ThrowsAny<SourceExpressionException>(sut);
        }

        [Fact]
        public void VbExpression_WithObjectInitializer()
        {
            var expressionToCompile = "new TestIndexerClass() With {.Field=\"1\"}";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, (s, c)=>null, typeof(TestIndexerClass)));
            result.ReturnType.ShouldBe(typeof(TestIndexerClass));
        }

        [Fact]
        public void VbExpression_UndeclaredObjectWithObjectInitializer()
        {
            var expressionToCompile = "new UndeclaredClass() With {.Field=\"1\"}";
            var sut = () => _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, (s, c)=>null, typeof(object)));

            Assert.ThrowsAny<SourceExpressionException>(sut);
        }

        [Fact]
        public void CompileExpression_Can_Access_Assembly_Loaded_In_Other_Alc()
        {
            var testAssemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\CompilerMissingAssembly.dll");
            var loadContext = new AssemblyLoadContext("MyCollectibleALC");
            var assembly = loadContext.LoadFromAssemblyPath(testAssemblyPath);

            // Use the loaded assembly in the JitCompiler.
            VbJitCompiler vbJitCompiler = new([typeof(string).Assembly, typeof(ClassWithIndexer).Assembly, typeof(Expression).Assembly, typeof(Enumerable).Assembly, assembly]);

            List<string> namespaces = new List<string>(_namespaces);
            namespaces.AddRange(["ClassLibrary1"]);

            new Action(() => vbJitCompiler.CompileExpression(
                new ExpressionToCompile("ClassLibrary1.Class1.Value", namespaces, (s, c) => null, typeof(string)))).ShouldNotThrow();
        }

        private static Type VariableTypeGetter(string name, StringComparison stringComparison)
            => name switch
            {
                "testIndexerClass" => typeof(TestIndexerClass),
                _ => typeof(string),
            };
    }

    public class ClassWithIndexer
    {
        public ClassWithIndexer() { }
        public string this[string indexer] => indexer;
    }

    public class TestIndexerClass
    {
        public string Field = string.Empty;
        public TestIndexerClass() { }
#pragma warning disable CA1822 // Mark members as static
        public ClassWithIndexer Indexer { get => new(); }
        public string Method(string method) => method;
#pragma warning restore CA1822 // Mark members as static
    }
}
