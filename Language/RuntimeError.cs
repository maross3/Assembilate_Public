using System;
using UnityEngine;

namespace Language
{
    public class RuntimeError : Exception
    {
        internal readonly Token token;

        internal RuntimeError(object token, string message = "")
        {
            if (token is not Token tkn)
                return;
            Debug.LogError(message);
            this.token = tkn;
        }

        internal RuntimeError(object value) : base()
        {
            
        }
    }
}