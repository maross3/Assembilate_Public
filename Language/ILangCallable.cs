using System;
using System.Collections.Generic;

namespace Language
{
    public interface ILangCallable
    {
        int Arity();
        object Call(Interpreter interpreter, List<object> args);
    }

    // notes: These are instanced
    // static would call sGlobal. Need to rename
    public class LangFunction : ILangCallable
    {
        private readonly Stmt.Function _declaration;
        private Environment _closure;
        internal LangFunction(Stmt.Function declaration, Environment enclosing)
        {
            _declaration = declaration;
            
            var env = new Environment(enclosing.Clone(), null);
            _closure = env;
            _closure.Assign(_declaration.name, _declaration);
        }
        
        public int Arity() =>
            _declaration.parameters.Count;
        

        public object Call(Interpreter interpreter, List<object> args)
        {
            // assign all _closure vars
            // if something is missing from _closure.Clone(new vars),
            // we still have the enclosing original
            var env = new Environment(_closure.Clone(), _closure);
            
            for (var i = 0; i < _declaration.parameters.Count; i++)
                env.Define(_declaration.parameters[i].lexeme, args[i]);

            try
            {
                interpreter.ExecuteBlock(_declaration.body, env);
            }
            catch (Return val)
            {
                return val.value;
            }
            
            
            return null;
        }

        public override string ToString() =>
            "<Function: " + _declaration.name.lexeme + " >";
        
    }
    public class LangCallable : ILangCallable
    {
        public int Arity()
        {
            return 0;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            return System.DateTime.Now.Millisecond / 1000.0;
        }

        public override string ToString()
        {
            return "Native Function";
        }
    }
}