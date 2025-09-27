using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Motiv.CodeFix.Tests;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            TestState.AdditionalReferences.Add(typeof(MotivCodeFixProvider).Assembly);

            // Add the source for required types
            TestState.Sources.Add(
                """

                namespace System.Runtime.CompilerServices
                {
                    internal static class IsExternalInit {}
                }

                namespace Motiv
                {
                    public abstract class SpecBase<TModel, TMetadata>
                    {
                        public abstract bool IsSatisfiedBy(TModel model);
                    }

                    public abstract class Spec<TModel>
                    {
                        protected Spec() { }
                        protected Spec(System.Func<SpecBase<TModel, string>> factory) { }

                        public virtual SpecResult IsSatisfiedBy(TModel model)
                        {
                            return new SpecResult(true);
                        }

                        public class SpecResult
                        {
                            public SpecResult(bool satisfied) => Satisfied = satisfied;
                            public bool Satisfied { get; }
                        }
                    }

                    public static class Spec
                    {
                        public static Builder<TModel> Build<TModel>(System.Func<TModel, bool> predicate)
                        {
                            return new Builder<TModel>(predicate);
                        }

                        public class Builder<TModel>
                        {
                            private readonly System.Func<TModel, bool> _predicate;
                            
                            public Builder(System.Func<TModel, bool> predicate) => _predicate = predicate;
                            
                            public Builder<TModel> WhenTrue(string metadata) => this;
                            public Builder<TModel> WhenFalse(string metadata) => this;
                            
                            public SpecBase<TModel, string> Create()
                            {
                                return new StubSpec<TModel>(_predicate);
                            }
                        }
                    }

                    internal class StubSpec<TModel> : SpecBase<TModel, string>
                    {
                        private readonly System.Func<TModel, bool> _predicate;
                        public StubSpec(System.Func<TModel, bool> predicate) => _predicate = predicate;
                        public override bool IsSatisfiedBy(TModel model) => _predicate(model);
                    }
                }

                """);

            FixedState.Sources.Add(
                """

                namespace System.Runtime.CompilerServices
                {
                    internal static class IsExternalInit {}
                }

                namespace Motiv
                {
                    public abstract class SpecBase<TModel, TMetadata>
                    {
                        public abstract bool IsSatisfiedBy(TModel model);
                    }

                    public abstract class Spec<TModel>
                    {
                        protected Spec() { }
                        protected Spec(System.Func<SpecBase<TModel, string>> factory) { }

                        public virtual SpecResult IsSatisfiedBy(TModel model)
                        {
                            return new SpecResult(true);
                        }

                        public class SpecResult
                        {
                            public SpecResult(bool satisfied) => Satisfied = satisfied;
                            public bool Satisfied { get; }
                        }
                    }

                    public static class Spec
                    {
                        public static Builder<TModel> Build<TModel>(System.Func<TModel, bool> predicate)
                        {
                            return new Builder<TModel>(predicate);
                        }

                        public class Builder<TModel>
                        {
                            private readonly System.Func<TModel, bool> _predicate;
                            
                            public Builder(System.Func<TModel, bool> predicate) => _predicate = predicate;
                            
                            public Builder<TModel> WhenTrue(string metadata) => this;
                            public Builder<TModel> WhenFalse(string metadata) => this;
                            
                            public SpecBase<TModel, string> Create()
                            {
                                return new StubSpec<TModel>(_predicate);
                            }
                        }
                    }

                    internal class StubSpec<TModel> : SpecBase<TModel, string>
                    {
                        private readonly System.Func<TModel, bool> _predicate;
                        public StubSpec(System.Func<TModel, bool> predicate) => _predicate = predicate;
                        public override bool IsSatisfiedBy(TModel model) => _predicate(model);
                    }
                }

                """);
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = ["/warnaserror:nullable"];
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args,
                baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            return nullableWarnings;
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
        }
    }
}
