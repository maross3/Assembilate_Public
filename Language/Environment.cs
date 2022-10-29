using System;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;

namespace Language
{
    public class Environment
    {
        internal readonly Environment enclosing;
        private readonly Dictionary<string, object> _values = new();

        internal Environment()
        {
            enclosing = null;
        }

        internal Environment(Dictionary<string, object> newValues, Environment enclosing)
        {
            _values = newValues;
            this.enclosing = enclosing;
        }

        internal Environment(Environment enclosing) =>
            this.enclosing = enclosing;

        internal object Get(Token name)
        {
            
            
            if (_values.ContainsKey(name.lexeme))
                return _values[name.lexeme];

            return enclosing == null ? enclosing :
                enclosing.Get(name);
                
            //throw new RuntimeError(name,
                // "Undefined variable: " + name.lexeme); 
                // recursive outter-scope check
        }
        
        internal void Assign(Token name, object value)
        {
            if (_values.ContainsKey(name.lexeme) && enclosing != null && 
                name.type == TokenType.FUN)
            {
                var clonedEnvironment = Clone(name.lexeme, value);
                enclosing.Assign(name, new Environment(clonedEnvironment, this));
                return;
            }

            // if we don't have value in our env, define it
            if (!_values.ContainsKey(name.lexeme))
                Define(name.lexeme, value);
            else if (enclosing != null) // if we have the key, check parent
                enclosing.Assign(name, value); 
            else _values[name.lexeme] = value; // if parent didn't have it, and we do, assign it

            // enclosing?.Assign(name, this);
        }

        internal void Define(string name, object value) =>
            _values.Add(name, value);

        public Dictionary<string, object> Clone()
        {
            var dict = new Dictionary<string, object>();
            foreach (var ob in _values)
                dict.Add(ob.Key, ob.Value);
            return dict;
        }

        public Dictionary<string, object> Clone(string name, object val)
        {
            var dict = new Dictionary<string, object>();
            
            foreach (var ob in _values)
                dict.Add(ob.Key, ob.Value);

            if (dict.ContainsKey(name))
                dict[name] = val;
            else 
                Define(name, val);
            return dict;
        }
    }
}