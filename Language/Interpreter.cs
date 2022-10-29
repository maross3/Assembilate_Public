using System;
using System.Collections.Generic;
using UnityEngine;
// if true;
// if false;
// both pass
namespace Language
{
    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        internal readonly Environment sGlobals;
        internal Environment _environment;

        internal Interpreter()
        {
            sGlobals = new Environment(); 
                
            sGlobals.Define("Clock", 
                new LangCallable());
            _environment = sGlobals;
        }
        internal void Interpret(List<Stmt> statements)
        {
            try 
            {
                foreach(var statement in statements) 
                {
                    Execute(statement);
                }
            } 
            catch (RuntimeError error) 
            {
                LangName.RuntimeError(error);
                throw new Exception(error.Message);
            }
        }
        public object VisitAssignExpr(Expr.Assign expr)
        {
            var value = Evaluate(expr.value);
            _environment.Assign(expr.name, value);
            return value;
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            // this should evaluate functions,
            // but it passes one to Visit Call
            var left = Evaluate(expr.left);
            var right = Evaluate(expr.right);
            

            switch (expr.op.type)
            {
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                case TokenType.GREATER:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left > (double) right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left >= (double) right;
                case TokenType.LESS:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left < (double) right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left <= (double) right;
                case TokenType.MINUS:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left - (double) right;
                case TokenType.PLUS_EQUAL:
                case TokenType.PLUS:
                    if (left is double && right is double) return (double) left + (double) right;
                    if (left is string && right is string) return (string) left + (string) right;

                    throw new RuntimeError(expr.op,
                        "Operands must be two numbers or two strings");
                case TokenType.SLASH_EQUAL:
                case TokenType.SLASH:
                    if((double)right == 0) throw new RuntimeError(expr.op, 
                      "Cannot divide by 0");
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left / (double) right;
                case TokenType.STAR_EQUAL:
                case TokenType.STAR:
                    CheckNumberOperands(expr.op, left, right);
                    return (double) left * (double) right;
                default:
                    return null;
            }
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            var callee = Evaluate(expr.callee);
            var args = new List<object>();
            
            foreach(var arg in expr.args)
                args.Add(Evaluate(arg));

            if (callee is ILangCallable fn)
            {
                if (args.Count != fn.Arity())
                    throw new RuntimeError(expr.paren, $"Expected {fn.Arity()} parameters, but " +
                                                       $"recieved {args.Count}.");
                return fn.Call(this, args);
            }


            throw new RuntimeError(expr.paren,
                "Can only call functions and classes.");
        }
        
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            var left = Evaluate(expr.left);
            

            if (expr.op.type == TokenType.OR && IsTruthy(left))
                 return left;
            
            if (expr.op.type != TokenType.AND) return Evaluate(expr.right);
            
            return !IsTruthy(left) ? left : Evaluate(expr.right);
        } 
        
        public object VisitGroupingExpr(Expr.Grouping expr) =>
            Evaluate(expr.expression);

        public object VisitLiteralExpr(Expr.Literal expr) =>
            expr.value;

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            var right = Evaluate(expr.right);

            switch (expr.op.type)
            {
                case TokenType.MINUS:
                    CheckNumberOperand(expr.op, right);
                    return -(double) right;
                case TokenType.BANG:
                    return !IsTruthy(right);
            }

            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr) =>
            _environment.Get(expr.name);

        public object VisitBlockStmt(Stmt.Block stmt)
        {
            if (stmt.statements[0] is Stmt.Var vrble)
                VisitVarStmt(vrble);
            
            ExecuteBlock(stmt.statements, _environment);
            return null;
        }

        public object VisitExpressionStmt(Stmt.Expression stmt) =>
            Evaluate(stmt.expression);

        public object VisitIfStatement(Stmt.If stmt)
        {
            if(IsTruthy(Evaluate(stmt.condition)))
                Execute(stmt.thenBranch);
            else if (stmt.elseBranch != null)
                Execute(stmt.elseBranch);
            return null;
        }

        public object VisitPrintStmt(Stmt.Print stmt)
        {
            // todo type mismatch throws errors
            var value = Evaluate(stmt.expression);
            
                try
            {
                var expr = _environment.Get(stmt.expression.name);
                value = expr;
                
                if (value is Environment env)
                    Debug.Log(Stringify(env.Get(stmt.expression.name)));
                else 
                    Debug.Log(Stringify(value));
            }
            catch
            {
                if (value is Environment env)
                    Debug.Log(Stringify(env.Get(stmt.expression.name)));
                else 
                    Debug.Log(Stringify(value));
            }
            return null;
        }

        public object VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.initializer != null)
                value = Evaluate(stmt.initializer);
            
            _environment.Assign(stmt.name, value);
            return null;
        }

        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.condition)))
                Execute(stmt.body);
            
            return null;
        }

        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            // var environ = _environment.Get(stmt.name);
            var function = new LangFunction(stmt, _environment);
            _environment.Assign(stmt.name, function);
            // _environment.Define(stmt.name.lexeme, function);
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            // eval expr.binary breaks this without enclosing. 
            object value = null;
            if (stmt.value != null) 
                value = Evaluate(stmt.value);
            
            throw new Return(value);
        }

        private object Evaluate(Expr expr) =>
            expr.Accept(this);


        private void Execute(Stmt stmt) =>
            stmt.Accept(this);

        internal void ExecuteBlock(List<Stmt> statements,
            Environment environment)
        {
            var previous = _environment;
            try
            {
                _environment = environment;

                foreach (var statement in statements)
                    Execute(statement);
            }
            finally
            {
                _environment = previous;
            }
        }

        private static bool IsEqual(object left, object right) =>
            left switch
            {
                null when right == null => true,
                null => false,
                _ => left.Equals(right)
            };


        private static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            return true;
        }

        private void CheckNumberOperand(Token op, object operand)
        {
            if (operand is double) return;
            // todo test error
            throw new RuntimeError(op, "Operand must be a number");
        }

        private void CheckNumberOperands(Token op, object first, object second)
        {
            // first needs a var
            if (first is double && second is double) return;
            if (first is Environment env && second is double sec)
            {
            }
            // todo test error
            //throw new RuntimeError(op, "Operands must be numbers");
        }

        private static string Stringify(object obj)
        {
            switch (obj)
            {
                case null:
                    return "nil";
                case double:
                {
                    var text = obj.ToString();

                    // Check if double has explicit .0's in C#:
                    // Was meant to negate java type Double's explicit .0
                    if (text.EndsWith(".0")) text = text.Substring(0, text.Length - 2);

                    return text;
                }
                default:
                    return obj.ToString();
            }
        }
    }
}