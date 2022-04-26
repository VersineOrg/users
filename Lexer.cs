using System;
using System.Collections.Generic;

namespace users;

    public static class Lexer
    {
        public static List<Token> Lex(string path)
        {
            List<Token> Lexed = new List<Token>();
            String temp = "";
            foreach (var c in path)
            {
                if (c == '/')
                {
                    if (temp != "")
                    {
                        Lexed.Add(new Token(temp));
                        temp = "";
                    }
                }
                else
                {
                    temp += c;
                }
            }
            Lexed.Add(new Token(temp));
            return Lexed;
        }
    }
    