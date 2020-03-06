using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLanguage.ExpressionParser
{
    public enum Token
    {
        EOF,
        Add,
        Subtract,
        Multiply,
        Divide,
        OpenParens,
        CloseParens,
        Identifier,
        Number
    }

    public class Tokenizer
    {
        public Tokenizer(TextReader reader)
        {
            _reader = reader;
            NextChar();
            NextToken();
        }

        TextReader _reader;
        char _currentChar;
        Token _currentToken;
        double _number;
        string _identifier;

        public Token Token
        {
            get { return _currentToken; }
        }

        public double Number
        {
            get { return _number; }
        }

        public string Identifier
        {
            get { return _identifier; }
        }

        // Read the next character from the input strem
        // and store it in _currentChar, or load '\0' if EOF
        void NextChar()
        {
            int ch = _reader.Read();
            _currentChar = ch < 0 ? '\0' : (char)ch;
        }

        // Read the next token from the input stream
        public void NextToken()
        {
            // Skip whitespace
            while (char.IsWhiteSpace(_currentChar))
            {
                NextChar();
            }

            // Special characters
            switch (_currentChar)
            {
                case '\0':
                    _currentToken = Token.EOF;
                    return;

                case '+':
                    NextChar();
                    _currentToken = Token.Add;
                    return;

                case '-':
                    NextChar();
                    _currentToken = Token.Subtract;
                    return;

                case '*':
                    NextChar();
                    _currentToken = Token.Multiply;
                    return;

                case '/':
                    NextChar();
                    _currentToken = Token.Divide;
                    return;

                case '(':
                    NextChar();
                    _currentToken = Token.OpenParens;
                    return;

                case ')':
                    NextChar();
                    _currentToken = Token.CloseParens;
                    return;
            }

            // Number?
            if (char.IsDigit(_currentChar) || _currentChar == '.')
            {
                // Capture digits/decimal point
                var sb = new StringBuilder();
                bool haveDecimalPoint = false;
                while (char.IsDigit(_currentChar) || (!haveDecimalPoint && _currentChar == '.'))
                {
                    sb.Append(_currentChar);
                    haveDecimalPoint = _currentChar == '.';
                    NextChar();
                }

                // Parse it
                _number = double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
                _currentToken = Token.Number;
                return;
            }

            // Identifier - starts with letter or underscore
            if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                var sb = new StringBuilder();

                // Accept letter, digit or underscore
                while (char.IsLetterOrDigit(_currentChar) || _currentChar == '_')
                {
                    sb.Append(_currentChar);
                    NextChar();
                }

                // Setup token
                _identifier = sb.ToString();
                _currentToken = Token.Identifier;
                return;
            }

            throw new InvalidDataException($"Unexpected character: {_currentChar}");
        }
    }

    public abstract class Node
    {
        public abstract double Eval();
    }

    class NodeNumber : Node
    {
        public NodeNumber(double number)
        {
            _number = number;
        }

        double _number;

        public override double Eval()
        {
            // Just return it.  Too easy.
            return _number;
        }
    }

    // NodeBinary for binary operations such as Add, Subtract etc...
    class NodeBinary : Node
    {
        // Constructor accepts the two nodes to be operated on and function
        // that performs the actual operation
        public NodeBinary(Node lhs, Node rhs, Func<double, double, double> op)
        {
            _lhs = lhs;
            _rhs = rhs;
            _op = op;
        }

        Node _lhs;                              // Left hand side of the operation
        Node _rhs;                              // Right hand side of the operation
        Func<double, double, double> _op;       // The callback operator

        public override double Eval()
        {
            // Evaluate both sides
            var lhsVal = _lhs.Eval();
            var rhsVal = _rhs.Eval();

            // Evaluate and return
            var result = _op(lhsVal, rhsVal);
            return result;
        }
    }

    // NodeUnary for unary operations such as Negate
    class NodeUnary : Node
    {
        // Constructor accepts the two nodes to be operated on and function
        // that performs the actual operation
        public NodeUnary(Node rhs, Func<double, double> op)
        {
            _rhs = rhs;
            _op = op;
        }

        Node _rhs;                              // Right hand side of the operation
        Func<double, double> _op;               // The callback operator

        public override double Eval()
        {
            // Evaluate RHS
            var rhsVal = _rhs.Eval();

            // Evaluate and return
            var result = _op(rhsVal);
            return result;
        }
    }

    public class Parser
    {
        // Constructor - just store the tokenizer
        public Parser(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        Tokenizer _tokenizer;

        // Parse an entire expression and check EOF was reached
        public Node ParseExpression()
        {
            // For the moment, all we understand is add and subtract
            var expr = ParseAddSubtract();

            // Check everything was consumed
            if (_tokenizer.Token != Token.EOF)
                throw new Exception("Unexpected characters at end of expression");

            return expr;
        }

        // Parse an sequence of add/subtract operators
        Node ParseAddSubtract()
        {
            // Parse the left hand side
            var lhs = ParseMultiplyDivide();

            while (true)
            {
                // Work out the operator
                Func<double, double, double> op = null;
                if (_tokenizer.Token == Token.Add)
                {
                    op = (a, b) => a + b;
                }
                else if (_tokenizer.Token == Token.Subtract)
                {
                    op = (a, b) => a - b;
                }

                // Binary operator found?
                if (op == null)
                    return lhs;             // no

                // Skip the operator
                _tokenizer.NextToken();

                // Parse the right hand side of the expression
                var rhs = ParseMultiplyDivide();

                // Create a binary node and use it as the left-hand side from now on
                lhs = new NodeBinary(lhs, rhs, op);
            }
        }

        // Parse an sequence of add/subtract operators
        Node ParseMultiplyDivide()
        {
            // Parse the left hand side
            var lhs = ParseUnary();

            while (true)
            {
                // Work out the operator
                Func<double, double, double> op = null;
                if (_tokenizer.Token == Token.Multiply)
                {
                    op = (a, b) => a * b;
                }
                else if (_tokenizer.Token == Token.Divide)
                {
                    op = (a, b) => a / b;
                }

                // Binary operator found?
                if (op == null)
                    return lhs;             // no

                // Skip the operator
                _tokenizer.NextToken();

                // Parse the right hand side of the expression
                var rhs = ParseUnary();

                // Create a binary node and use it as the left-hand side from now on
                lhs = new NodeBinary(lhs, rhs, op);
            }
        }


        // Parse a unary operator (eg: negative/positive)
        Node ParseUnary()
        {
            // Positive operator is a no-op so just skip it
            if (_tokenizer.Token == Token.Add)
            {
                // Skip
                _tokenizer.NextToken();
                return ParseUnary();
            }

            // Negative operator
            if (_tokenizer.Token == Token.Subtract)
            {
                // Skip
                _tokenizer.NextToken();

                // Parse RHS 
                // Note this recurses to self to support negative of a negative
                var rhs = ParseUnary();

                // Create unary node
                return new NodeUnary(rhs, (a) => -a);
            }

            // No positive/negative operator so parse a leaf node
            return ParseLeaf();
        }

        // Parse a leaf node
        // (For the moment this is just a number)
        Node ParseLeaf()
        {
            // Is it a number?
            if (_tokenizer.Token == Token.Number)
            {
                var node = new NodeNumber(_tokenizer.Number);
                _tokenizer.NextToken();
                return node;
            }

            // Parenthesis?
            if (_tokenizer.Token == Token.OpenParens)
            {
                // Skip '('
                _tokenizer.NextToken();

                // Parse a top-level expression
                var node = ParseAddSubtract();

                // Check and skip ')'
                if (_tokenizer.Token != Token.CloseParens)
                    throw new Exception("Missing close parenthesis");
                _tokenizer.NextToken();

                // Return
                return node;
            }

            // Don't Understand
            throw new Exception($"Unexpect token: {_tokenizer.Token}");
        }


        #region Convenience Helpers

        // Static helper to parse a string
        public static Node Parse(string str)
        {
            return Parse(new Tokenizer(new StringReader(str)));
        }

        // Static helper to parse from a tokenizer
        public static Node Parse(Tokenizer tokenizer)
        {
            var parser = new Parser(tokenizer);
            return parser.ParseExpression();
        }

        #endregion
    }
}
