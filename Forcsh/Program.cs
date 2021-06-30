using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Forcsh
{
    
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Action<Stack<(FType, String)>>>;
    
    public readonly struct FEnvironment
    {
        public FStack DataStack { get; }
        public FWordDict WordDict { get; }
        public IEnumerable<String> Input { get; }
        
        public bool ImmediateMode { get; }
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, IEnumerable<string> input, bool immediateMode)
        {
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            ImmediateMode = immediateMode;
        }
    }

    public enum FType
    {
        FStr,
        FFloat,
        FInt,
        FWord,
        FBool
    }
    
    public static class Program
    {
        public static FStack DataStack = new FStack();

        public static void DUP(FStack s)
        {
            s.Push(DataStack.Peek());
        }

        public static void SWAP(FStack s)
        {
            var top = s.Pop();
            var second = s.Pop();
            s.Push(top);
            s.Push(second);
        }

        public static void DROP(FStack s)
        {
            s.Pop();
        }

        public static void DOT(FStack s)
        {
            var (t, v) = s.Pop();
            System.Console.WriteLine(v);
        }
        
        public static void EQ(FStack s)
        {
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
        }

        public static void ADD(FStack s)
        {
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            
            var result = System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv);
            s.Push((FType.FInt, result.ToString()));
        }

        public static void GET(FStack s)
        {
            var inp = Console.ReadLine();
            s.Push((FType.FStr, inp));
        }

        public static FWordDict WordDict = new FWordDict
        {
            ["+"] = ADD,
            ["."] = DOT,
            ["="] = EQ,
            ["DUP"] = DUP,
            ["DROP"] = DROP,
            ["SWAP"] = SWAP,
        };
        
        public static (FType, String) Tokenize(string s, FWordDict wordDict)
        {
            if (wordDict.ContainsKey(s))
            {
                return (FType.FWord, s);
            }
            else
            {
                int i; float f; bool b;
                
                if (int.TryParse(s, out i))
                    return (FType.FInt, s);
                else if (float.TryParse(s, out f))
                    return (FType.FFloat, s);
                else if (bool.TryParse(s, out b))
                    return (FType.FBool, s);
                else
                    return (FType.FStr, s);
            }
        }
        
        public static FEnvironment GetNext(FEnvironment e)
        {
            var (tail, (t, v)) = TokenizeHead(e);
            if (e.ImmediateMode)
            {
                if (t == FType.FWord)
                {
                    Console.Out.WriteLine("Executing word " + v);
                    e.WordDict[v](e.DataStack);
                    
                }
                else
                {
                    Console.Out.WriteLine("pushing value onto stack: " + v);
                    e.DataStack.Push((t, v));
                }
            } 
            return new FEnvironment(e.DataStack, e.WordDict, tail, e.ImmediateMode);
        }

        public static (IEnumerable<string> tail, (FType, string) token) TokenizeHead(FEnvironment e)
        {
            var head = e.Input.First();
            var tail = e.Input.Skip(1);
            var token = Tokenize(head, e.WordDict);
            return (tail, token);
        }

        public static void Main(string[] args)
        {
            IEnumerable<String> line = new List<string>();
            var e = new FEnvironment(DataStack, WordDict, line, true);
            while (true)
            {
                if (e.Input.Count() == 0)
                {
                    var nextLine = Console.ReadLine();
                    if (nextLine == null)
                        break;
                    else
                        e = new FEnvironment(e.DataStack, e.WordDict, nextLine.Split(), e.ImmediateMode);
                }
                e = GetNext(e);
            }

            /* General strategy: 
                * Global stack.
                * Words are functions with no params that modify global stack
                * Output everything to cs file
                * Win
            
            * Builtins:
                * DUP
                * SWAP
                * DROP
                * . (pop & print)
                * = (pop top two, compare, 1 if eq, 0 if not)
            */
        }
    }
}