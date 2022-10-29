using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Language
{
    public class Parser
    {
        private class ParseError : Exception
        {
        }

        private readonly List<Token> _tokens;
        private int _current;

        internal Parser(List<Token> tokens) =>
            _tokens = tokens;


        internal List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        private Stmt Declaration()
        {
            try
            {
                if (Match(TokenType.FUN)) return Function("function");
                return Match(TokenType.VAR) ? VarDeclaration() : Statement();
            }
            catch (ParseError error)
            {
                Debug.LogError("Error: " + error);
                
                Synchronize();
                return null;
            }
        }

        private Expr Expression() =>
            Assignment();

        private Expr Assignment()
        {
            var expr = Or();
            if (!Match(TokenType.EQUAL) && !IsMathEquals()) return expr;
            
            var equals = Previous();
            
            if (IsMathEquals(equals.type))
                if (expr is Expr.Variable newVar)
                    return new Expr.Assign(newVar.name, 
                        new Expr.Binary(newVar, equals, Term()));
            
            var value = Assignment();
            
            if (expr is Expr.Variable variable)
            {
                var name = variable.name;
                return new Expr.Assign(name, value);
            }

            Error(equals, "Invalid assignment target.");
            return expr;
        }

        private bool IsMathEquals(TokenType token) =>
            token is TokenType.STAR_EQUAL or
                TokenType.SLASH_EQUAL or 
                TokenType.PLUS_EQUAL or 
                TokenType.MINUS_EQUAL;
        private bool IsMathEquals() =>
            Match(TokenType.STAR_EQUAL) ||
            Match(TokenType.SLASH_EQUAL) || 
            Match(TokenType.PLUS_EQUAL) ||
            Match(TokenType.MINUS_EQUAL);

        private Stmt.Function Function(string kind)
        {
            var name = Consume(TokenType.IDENTIFIER, 
                $"Expected {kind} + name.");
            Consume(TokenType.LEFT_PAREN,
                "Expected a '(' after function name");
            var parameters = new List<Token>();

            if(Check(TokenType.IDENTIFIER))
                parameters.Add(Consume(TokenType.IDENTIFIER, 
                    @"Invalid parameter found!"));
            
            while (Match(TokenType.COMMA))
            {
                if (Check(TokenType.RIGHT_PAREN)) break;
                
                if (parameters.Count >= 255)
                {
                    Error(Peek(),
                        "Can't have more than 255 parameters.");
                }
                else
                {
                    parameters.Add(Consume(TokenType.IDENTIFIER,
                        "Expected a parameter name."));
                }
            }

            Consume(TokenType.RIGHT_PAREN, 
                "Expected a ')' after parameters.");
            Consume(TokenType.LEFT_BRACE, 
                "Expected a '{' before " + kind + " body.");
            var body = Block();
            return new Stmt.Function(name, parameters, body);
        }
        
        private Expr Or()
        {
            var expr = And();
            while (Match(TokenType.OR))
            {
                var op = Previous();
                var right = And();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr And()
        {
            var expr = Equality();

            while (Match(TokenType.AND))
            {
                var op = Previous();
                var right = Equality();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }
        private Stmt Statement()
        {
            if (Match(TokenType.FOR)) return ForStatement();
            if (Match(TokenType.IF)) return IfStatement();
            if (Match(TokenType.PRINT)) return PrintStatement();
            if (Match(TokenType.RETURN)) return ReturnStatement();
            if (Match(TokenType.WHILE)) return WhileStatement();
            return Match(TokenType.LEFT_BRACE) ? new Stmt.Block(Block()) : ExpressionStatement();
        }

        private Stmt ReturnStatement()
        {
            var keyword = Previous();
            Expr value = null;
            
            if (!Check(TokenType.SEMICOLON))
                value = Expression();
            
            Consume(TokenType.SEMICOLON, 
                "Expected a ';' after return value.");
            
            return new Stmt.Return(keyword, value);
        }
        
        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expected a '(' after for.");

            Stmt initializer;
            if (Match(TokenType.SEMICOLON)) initializer = null;
            else if (Match(TokenType.VAR)) initializer = VarDeclaration();
            else initializer = ExpressionStatement();

            Expr condition = null;
            if (!Check(TokenType.SEMICOLON)) condition = Expression();
            Consume(TokenType.SEMICOLON, "Expected ';' after loop condition.");

            Expr increment = null;
            if (!Check(TokenType.RIGHT_PAREN)) increment = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after for clauses.");

            var body = Statement();

            if (increment == null) return body;
            var bodyList = new List<Stmt>
            {
                body,
                new Stmt.Expression(increment)
            };
            body = new Stmt.Block(bodyList);

            condition ??= new Expr.Literal(true);
            body = new Stmt.While(condition, body);

            if (initializer == null) return body;
            var nextBody = new List<Stmt>
            {
                initializer,
                body
            };
            body = new Stmt.Block(nextBody);

            return body;
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expected '(' after if.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expected a ')' after condition expression.");
           
            var thenBranch = Statement();
            
            Stmt elseBranch = null;
            if (Match(TokenType.ELSE))
                elseBranch = Statement();
            
            return new Stmt.If(condition, thenBranch, elseBranch);
        }
        
        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value");
            return new Stmt.Print(value);
        }
        
        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, 
                "Expected an exit condition after 'while'.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, 
                "Expected a ')' after condition expression.");
            var body = Statement();
            return new Stmt.While(condition, body);
        }

        private Stmt VarDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expected a variable name");

            Expr initializer = null;

            if (Match(TokenType.EQUAL))
                initializer = Expression();

            Consume(TokenType.SEMICOLON, "Expected ';' after a variable declaration");
            return new Stmt.Var(name, initializer);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(TokenType.SEMICOLON, "Expected ';' after an expression");
            return new Stmt.Expression(expr);
        }

        private List<Stmt> Block()
        {
            var statements = new List<Stmt>();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
                statements.Add(Declaration());

            Consume(TokenType.RIGHT_BRACE, "Expected '}' after block");
            return statements;
        }

        private Expr Equality()
        {
            var expr = Comparison();

            while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                var op = Previous();
                var right = Comparison();
                expr = new Expr.Binary(expr, op, right);
            }

            // will always return comparison
            return expr;
        }

        private Expr Comparison()
        {
            var expr = Term();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                var op = Previous();
                var right = Term();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Term()
        {
            var expr = Factor();
            
            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                var op = Previous();
                var right = Factor();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Factor()
        {
            var expr = Unary();

            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                var op = Previous();
                var right = Unary();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Call()
        {
            var expr = Primary();
            
            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                    expr = FinishCall(expr);
                else break;
            }
            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            var args = new List<Expr>();
            
            if (args.Count > 255) Error(Peek(), 
                "Maximum supported function arguments is 255.");

            if (!Check(TokenType.RIGHT_PAREN))
            {
                args.Add(Expression()); 
                // todo, this is where the match messes up on second fib
                while(Match(TokenType.COMMA)) 
                    args.Add(Expression());
            }
            
            var paren = Consume(TokenType.RIGHT_PAREN, 
                "Expected a ')' after function arguments.");
            
            return new Expr.Call(callee, paren, args);
        }
        
        private Expr Unary()
        {
            if (!Match(TokenType.BANG, TokenType.MINUS))
                return Call();

            var op = Previous();
            var right = Unary();

            return new Expr.Unary(op, right);
        }

        private Expr Primary()
        {
            if (Match(TokenType.FALSE)) return new Expr.Literal(false);
            if (Match(TokenType.TRUE)) return new Expr.Literal(true);
            if (Match(TokenType.NIL)) return new Expr.Literal(null);

            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expr.Literal(Previous().literal);
            }

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous());
            }

            if (!Match(TokenType.LEFT_PAREN)) throw Error(Peek(), "Expected expression");

            var expr = Expression();

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        private bool Match(params TokenType[] types)
        {
            if (!types.Any(Check)) return false;

            Advance();
            return true;
        }

        private Token Consume(TokenType type, String message)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), message);
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd() =>
            Peek().type == TokenType.EOF;


        private Token Peek() =>
            _tokens[_current];


        private Token Previous() =>
            _tokens[_current - 1];


        private static ParseError Error(Token token, string message)
        {
            LangName.Error(token, message);
            return new ParseError();
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().type == TokenType.SEMICOLON) return;

                switch (Peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                Advance();
            }
        }
    }
}