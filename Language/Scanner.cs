using System;
using System.Collections.Generic;

namespace Language
{
    public class Scanner
    {
        private static Dictionary<string, TokenType> _keywords;
        private readonly string _source;
        private readonly List<Token> _tokens = new();
        private int _current;
        private int _line = 1;
        private int _start;

        public Scanner(string source)
        {
            _source = source;


            _keywords = new Dictionary<string, TokenType>
            {
                {"and", TokenType.AND},
                {"class", TokenType.CLASS},
                {"else", TokenType.ELSE},
                {"false", TokenType.FALSE},
                {"for", TokenType.FOR},
                {"fun", TokenType.FUN},
                {"if", TokenType.IF},
                {"nil", TokenType.NIL},
                {"or", TokenType.OR},
                {"print", TokenType.PRINT},
                {"return", TokenType.RETURN},
                {"super", TokenType.SUPER},
                {"this", TokenType.THIS},
                {"true", TokenType.TRUE},
                {"var", TokenType.VAR},
                {"while", TokenType.WHILE}
            };
        }

        internal List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EOF, "", null, _line));
            return _tokens;
        }

        private void ScanToken()
        {
            var c = Advance();

            switch (c)
            {
                case '(':
                    AddToken(TokenType.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenType.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenType.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenType.RIGHT_BRACE);
                    break;
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case '-':
                    AddToken(Match('=') ? TokenType.MINUS_EQUAL : TokenType.MINUS);
                    break;
                case '+':
                    AddToken(Match('=') ? TokenType.PLUS_EQUAL : TokenType.PLUS);
                    break;
                case ';':
                    AddToken(TokenType.SEMICOLON);
                    break;
                case '*':
                    AddToken(Match('=') ? TokenType.STAR_EQUAL : TokenType.STAR);
                    break;
                case '!':
                    AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;
                // i should be handled
                // if it's an identifier, it should reach through expr
                case '/':
                    if (Match('/') || Match('*'))
                        while (Peek() != '\n' && !IsAtEnd())
                            Advance();
                    else if (Match('='))
                        AddToken(TokenType.SLASH_EQUAL);
                    else
                        AddToken(TokenType.SLASH);
                    break;
                case '&':
                    if(Match('&'))
                        AddToken(TokenType.AND);
                    break;
                case '|':
                    if(Match('|'))
                        AddToken(TokenType.OR);
                    break;
                case ' ':
                case '\r':
                case '\t':
                    // ignore whitespace
                    break;
                case '\n':
                    _line++;
                    break;
                case '"':
                    IsString();
                    break;

                default:
                    if (IsDigit(c)) Number();
                    else if (IsAlpha(c)) Identifier();
                    // todo test error
                     else LangName.Error(_line, "Unexpected character: " + c);
                    break;
            }
        }

        private void Identifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            var text = _source.Substring(_start, _current - _start);

            TokenType type;
            try
            {
                type = _keywords[text];
            }
            catch
            {
                type = TokenType.IDENTIFIER;
            }

            AddToken(type);
        }

        private static bool IsAlpha(char c) =>
            c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-';

        private static bool IsAlphaNumeric(char c) =>
            IsAlpha(c) || IsDigit(c);

        private bool IsAtEnd() =>
            _current >= _source.Length;
        
        private void Number()
        {
            while (IsDigit(Peek())) Advance();

            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                Advance();

                while (IsDigit(Peek())) Advance();
            }

            var doubleStr = _source.Substring(_start, _current - _start);
            AddToken(TokenType.NUMBER,
                Convert.ToDouble(doubleStr));
        }

        private void IsString()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') _line++;
                Advance();
            }

            // todo test unterminated string error
            if (IsAtEnd())
            {
                LangName.Error(_line, "Unterminated string found!");
                return;
            }
            Advance();

            var value = _source.Substring(_start + 1, _current - 1 - _start - 1);

            AddToken(TokenType.STRING, value);
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;

            _current++;
            return true;
        }

        private char Peek() =>
            IsAtEnd() ? '\0' : _source[_current];

        private char PeekNext() =>
            _current + 1 >= _source.Length ? '\0' : _source[_current + 1];
        
        private static bool IsDigit(char c) =>
            c is >= '0' and <= '9';
        
        private char Advance() =>
            _source[_current++];

        private void AddToken(TokenType type) =>
            AddToken(type, null);

        private void AddToken(TokenType type, object literal)
        {
            var text = _source.Substring(_start, _current - _start);
            _tokens.Add(new Token(type, text, literal, _line));
        }
    }
}