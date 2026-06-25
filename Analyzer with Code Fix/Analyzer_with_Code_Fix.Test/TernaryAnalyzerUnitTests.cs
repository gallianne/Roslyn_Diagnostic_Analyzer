using Analyzer_with_Code_Fix;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;
using VerifyCS = Code_Analyzer.Test.CSharpCodeFixVerifier<
    Analyzer_with_Code_Fix.TernaryAnalyzer,
    Analyzer_with_Code_Fix.TernaryAnalyzerCodeFixProvider>;

namespace Code_Analyzer.Test
{
    [TestClass]
    public class TernaryAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task BasicCall()
        {
            string test = @"
bool isTrue = true;
bool inverse = isTrue ? false : true;
";
            string fullCode = GetCode(test);
            await VerifyCS.VerifyAnalyzerAsync(fullCode);
        }

        [TestMethod]
        public async Task Nested()
        {
            string test = @"
bool isTrue1 = true;
bool isTrue2 = true;
bool areAllTrue = isTrue1 ? (isTrue2 ? true : false) : false;";

            DiagnosticResult expected = VerifyCS.Diagnostic(TernaryAnalyzer.DIAGNOSTIC_ID_NESTED);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TooLongComponent()
        {
            string txt = "";
            while (txt.Length <= TernaryAnalyzer.MAX_EXPRESSION_LENGTH)
                txt += 'a';

            string test = "bool isTrue = true;\n";
            test += $"bool str = isTrue ? \"{txt}\" : \"no\";";

            DiagnosticResult expected = VerifyCS.Diagnostic(TernaryAnalyzer.DIAGNOSTIC_ID_TOO_LONG);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TooLongExpression()
        {
            string txt = "";
            while (txt.Length < TernaryAnalyzer.MAX_EXPRESSION_LENGTH - 6)
                txt += 'a';

            string test = "bool isTrue1 = true;\n";
            test += "bool isTrue2 = true;\n";
            test += $"bool str = isTrue1 && isTrue2 ? \"{txt}\" : \"{txt}\";";

            DiagnosticResult expected = VerifyCS.Diagnostic(TernaryAnalyzer.DIAGNOSTIC_ID_TOO_LONG);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task MultiLine()
        {
            string test = @"
bool isTrue = true;
string isTrueStr = isTrue 
    ? ""true""
    : ""false"";";

            DiagnosticResult expected = VerifyCS.Diagnostic(TernaryAnalyzer.DIAGNOSTIC_ID_MULTI_LINE);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        public string GetCode(string innerCode)
        {
            StringBuilder builder = new StringBuilder("namespace ConsoleApplication1");
            builder.AppendLine("{");
            builder.AppendLine("\tusing System;");
            builder.AppendLine("\tusing System.Collections.Generic;");
            builder.AppendLine("\tusing System.Linq;");
            builder.AppendLine("\tusing System.Text;");
            builder.AppendLine("\tusing System.Threading.Tasks;");
            builder.AppendLine("\tusing System.Diagnostics;");
            builder.AppendLine("");
            builder.AppendLine("\tpublic class MyClass");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tpublic MyClass()");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\t");
            builder.Append(innerCode);
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t}");
            builder.AppendLine("}");
            builder.AppendLine("");
            return builder.ToString();
        }
    }
}