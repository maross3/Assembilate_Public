using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Language
{
    public class LangName : IDisposable
    {
        private static Interpreter _interpreter = new();
        private static string _source;
        private static bool _hadError;
        private static bool _hadRuntimeError;
        private static LangName _instance;
        public static void Main(string[] args)
        {
            var _dir = "./src/tests/ScopeReassignment.txt";
            // runFile(dir);
            // runPrompt();				

//			if (args.length < 1) {
//				System.out.println("Usage: jlox [script]");
//				System.exit(64);
//			} else if (args.length == 1) {
//				runFile(args[0]);
//			} else {
//				runPrompt();
//			}
        }


        private static void RunFile(string path)
        {
            var srcLines = File.ReadAllLines(path);
            
            // todo test
            var src = srcLines.Aggregate("", (current, line) => current + line);
            Run(src);

            if (_hadError) Debug.LogError("Termination due to internal error");
            if (_hadRuntimeError) Debug.LogError("Termination due to runtime error");
        }

        public static void Run(string src)
        {
            _instance?.Dispose();
            _instance = new LangName();
            try
            {


                var scanner = new Scanner(src);
                var tokens = scanner.ScanTokens();

                var parser = new Parser(tokens);
                var statements = parser.Parse();

                if (_hadError)
                    Debug.LogError("Termination due to internal error");

                // static type err here
                _interpreter.Interpret(statements);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError("error thrown: " + e);
#endif
                // ignore for now
                // do logging logic, instead of logging everywhere
            }
            
        }

        internal static void Error(int line, string message) =>
            Debug.LogError($"<Line: {line}>  {message}");

        

        // should print more detailed errors in port
        private static void Report(int line, string where, string message)
        {
            Debug.LogError($"<Line: {line}> " + message + where);
            _hadError = true;
        }

        internal static void Error(Token token, string message)
        {
            if (token.type == TokenType.EOF)
                Report(token.line, " at end", message);
            else
                Report(token.line, " at '" + token.lexeme + "'", message);
        }

        internal static void RuntimeError(RuntimeError error)
        {
            Debug.LogError($"<Line: {error.token.line}> " + "Runtime Error: " + error.Message);  
            _hadRuntimeError = true;
        }

        public void Dispose()
        {
            _instance = null;
            _interpreter = null;
            _interpreter = new();
            _source = null;
            _hadError = false;
            _hadRuntimeError = false;
        }
    }
}