using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using AsyncSuggester;

namespace AsyncSuggester.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void When_async_avaliable()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Data.SqlClient;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Test()
            {
                 var conn = new SqlConnection(\""\"");
                 conn.Open();
            }   
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsyncSuggester",
                Message = "Consider using async version of method 'Open'",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }        
        
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void When_Sync_suffix()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Data.SqlClient;

    namespace ConsoleApplication1
    {
        class Some 
        {
            public void ExecuteSync(){}
            public Task Execute(){return Task.FromResult(1);}
        }
        class TypeName
        {
            public void Test()
            {
                 var some = new Some();
                 some.ExecuteSync();
            }   
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsyncSuggester",
                Message = "Consider using async version of method 'ExecuteSync'",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 22, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void When_Sync_suffix_and_TaskOfT()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Data.SqlClient;

    namespace ConsoleApplication1
    {
        class Some 
        {
            public bool ExecuteSync(){return false;}
            public Task<bool> Execute(){return Task.FromResult(true);}
        }
        class TypeName
        {
            public void Test()
            {
                 var some = new Some();
                 Console.WriteLine(some.ExecuteSync());
            }   
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsyncSuggester",
                Message = "Consider using async version of method 'ExecuteSync'",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 22, 36)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsyncSuggesterAnalyzer();
        }
    }
}