﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Forcsh
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Func<FEnvironment, FEnvironment>>;

    public enum FMode
    {
        Halt,
        Eval
    };
    
    public readonly struct FEnvironment
    {
        public FStack DataStack { get; }
        public FWordDict WordDict { get; }
        public IEnumerable<String> Input { get; }
        public FMode Mode { get; }
        
        public FEnvironment(FStack dataStack, FWordDict wordDict, IEnumerable<string> input, FMode mode)
        {
            DataStack = dataStack;
            WordDict = wordDict;
            Input = input;
            Mode = mode;
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

        public static FEnvironment FDup(FEnvironment e)
        {
            var s = e.DataStack;
            s.Push(DataStack.Peek());
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FSwap(FEnvironment e)
        {
            var s = e.DataStack;
            var top = s.Pop();
            var second = s.Pop();
            s.Push(top);
            s.Push(second);
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FDrop(FEnvironment e)
        {
            var s = e.DataStack;
            s.Pop();
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FDot(FEnvironment e)
        {
            var s = e.DataStack;
            var (t, v) = s.Pop();
            System.Console.WriteLine(v);
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FEq(FEnvironment e)
        {
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();
            if (xt == FType.FInt && yt == FType.FInt)
            {
                var res = System.Convert.ToInt32(xv) == System.Convert.ToInt32(yv);
                s.Push((FType.FBool, res.ToString()));
            }

            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FAdd(FEnvironment e)
        {
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();

            var result = System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv);
            s.Push((FType.FInt, result.ToString()));
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment GET(FEnvironment e)
        {
            var s = e.DataStack;
            var inp = Console.ReadLine();
            s.Push((FType.FStr, inp));
            return new FEnvironment(s, WordDict, e.Input, e.Mode);
        }

        public static FEnvironment FBranchOnFalse(FEnvironment e)
        {
            var offset = Convert.ToInt32(e.Input.First());
            var tail = e.Input.Skip(1);
            var (bt, bv) = e.DataStack.Pop();
            if (bt == FType.FBool && bv == "False")
            {
                var newInput = tail.Skip(offset - 1);
                return new FEnvironment(e.DataStack, e.WordDict, newInput, e.Mode);
            }
            else
            {
                return new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode);
            }
        }

        public static FEnvironment FComment(FEnvironment e)
        {
            var tail = e.Input
                .SkipWhile(w => w != ")")
                .Skip(1);
            return new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode);
        }

        // "Surveys" the contents of the stack.
        // That is, it outputs the whole stack:
        // left is the bottommost, right is the topmost
        public static FEnvironment FSurvey(FEnvironment e)
        {
            var s = e
                .DataStack
                .Reverse()
                .Aggregate("", (a, x) => a + " " + x.Item2)
                .Trim();
                
            Console.Out.WriteLine(s);
            return e;
        }

        public static FEnvironment FAssert(FEnvironment e)
        {
            var (bt, bv) = e.DataStack.Pop();
            if (bt == FType.FBool && bv == "True")
                return e;
            else
                throw new Exception("FAssert Failed");
        }

        public static FEnvironment FSike(FEnvironment e)
        {
            return e;
        }

        public static Func<FEnvironment, FEnvironment> WordWrapper(IEnumerable<String> wordData)
        {
            return (FEnvironment e) =>
            {
                var oldInput = e.Input;
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.Mode);
                while (true)
                {
                    if (tempEnv.Input.Count() == 0)
                        break;
                    tempEnv = Eval(tempEnv);
                }

                return new FEnvironment(tempEnv.DataStack, tempEnv.WordDict, oldInput, tempEnv.Mode);
            };
        }

        public static FEnvironment FWord(FEnvironment e)
        {
            var wordName = e.Input.First();
            var wordData = e.Input.Skip(1);
            e.WordDict[wordName] = WordWrapper(wordData);

            return new FEnvironment(e.DataStack, e.WordDict, new string[] { }, e.Mode);
        }

        public static FWordDict WordDict = new FWordDict
        {
            ["+"] = FAdd,
            ["."] = FDot,
            ["="] = FEq,
            [":"] = FWord,
            [";"] = FSike,
            ["DUP"] = FDup,
            ["DROP"] = FDrop,
            ["ASSERT"] = FAssert,
            ["SWAP"] = FSwap,
            ["BRANCHF"] = FBranchOnFalse,
            ["SURVEY"] = FSurvey,
            ["("] = FComment,
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
        
        public static FEnvironment Eval(FEnvironment e)
        {
            if (e.Mode == FMode.Halt)
                return e;
            
            var (tail, (t, v)) = TokenizeHead(e);
            if (t == FType.FWord)
            {
                return e.WordDict[v](new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode));
            }
            else
            {
                e.DataStack.Push((t, v));
                return new FEnvironment(e.DataStack, e.WordDict, tail, e.Mode);
            }
        }

        public static (IEnumerable<string> tail, (FType, string) token) TokenizeHead(FEnvironment e)
        {
            var head = e.Input.First();
            var tail = e.Input.Skip(1);
            var token = Tokenize(head, e.WordDict);
            return (tail, token);
        }

        public static FEnvironment Read(FEnvironment e,  Func<string> readLine)
        {
            if (!e.Input.Any())
            {
                var nextLine = Console.ReadLine();
                if (nextLine == null)
                    return new FEnvironment(e.DataStack, e.WordDict, e.Input, FMode.Halt);
                else if (nextLine.Trim() == "")
                    return Read(e, readLine);
                else
                    return new FEnvironment(e.DataStack, e.WordDict, nextLine.Split(), e.Mode);
            }
            else
            {
                return e;
            }
        }

        public static void Main(string[] args)
        {
            var e = new FEnvironment(DataStack, WordDict, new List<string>(), FMode.Eval);
            while (e.Mode != FMode.Halt)
                e = Eval(Read(e, Console.ReadLine));
        }
    }
}