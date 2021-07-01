using System;
using System.Collections.Generic;
using System.Linq;

namespace Forcsh
{
    using FStack = Stack<(FType, String)>;
    using FWordDict = Dictionary<string, Func<FEnvironment, FEnvironment>>;
    
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

        public static FEnvironment FDup(FEnvironment e)
        {
            var s = e.DataStack;
            s.Push(DataStack.Peek());
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment FSwap(FEnvironment e)
        {
            var s = e.DataStack;
            var top = s.Pop();
            var second = s.Pop();
            s.Push(top);
            s.Push(second);
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment FDrop(FEnvironment e)
        {
            var s = e.DataStack;
            s.Pop();
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment FDot(FEnvironment e)
        {
            var s = e.DataStack;
            var (t, v) = s.Pop();
            System.Console.WriteLine(v);
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
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

            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment FAdd(FEnvironment e)
        {
            var s = e.DataStack;
            var (xt, xv) = s.Pop();
            var (yt, yv) = s.Pop();

            var result = System.Convert.ToInt32(xv) + System.Convert.ToInt32(yv);
            s.Push((FType.FInt, result.ToString()));
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment GET(FEnvironment e)
        {
            var s = e.DataStack;
            var inp = Console.ReadLine();
            s.Push((FType.FStr, inp));
            return new FEnvironment(s, WordDict, e.Input, e.ImmediateMode);
        }

        public static FEnvironment FBranchOnFalse(FEnvironment e)
        {
            var offset = Convert.ToInt32(e.Input.First());
            var tail = e.Input.Skip(1);
            var (bt, bv) = e.DataStack.Pop();
            if (bt == FType.FBool && bv == "False")
            {
                var newInput = tail.Skip(offset - 1);
                return new FEnvironment(e.DataStack, e.WordDict, newInput, e.ImmediateMode);
            }
            else
            {
                return new FEnvironment(e.DataStack, e.WordDict, tail, e.ImmediateMode);
            }
        }

        public static FEnvironment FComment(FEnvironment e)
        {
            var tail = e.Input
                .SkipWhile(w => w != ")")
                .Skip(1);
            return new FEnvironment(e.DataStack, e.WordDict, tail, e.ImmediateMode);
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
                var tempEnv = new FEnvironment(e.DataStack, e.WordDict, wordData, e.ImmediateMode);
                while (true)
                {
                    if (tempEnv.Input.Count() == 0)
                        break;
                    tempEnv = GetNext(tempEnv);
                }

                return new FEnvironment(tempEnv.DataStack, tempEnv.WordDict, oldInput, tempEnv.ImmediateMode);
            };
        }

        public static FEnvironment FWord(FEnvironment e)
        {
            var wordName = e.Input.First();
            var wordData = e.Input.Skip(1);
            e.WordDict[wordName] = WordWrapper(wordData);

            return new FEnvironment(e.DataStack, e.WordDict, new string[] { }, e.ImmediateMode);
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
        
        public static FEnvironment GetNext(FEnvironment e)
        {
            var (tail, (t, v)) = TokenizeHead(e);
            if (t == FType.FWord)
            {
                return e.WordDict[v](new FEnvironment(e.DataStack, e.WordDict, tail, e.ImmediateMode));
            }
            else
            {
                e.DataStack.Push((t, v));
                return new FEnvironment(e.DataStack, e.WordDict, tail, e.ImmediateMode);
            }
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
                if (!e.Input.Any())
                {
                    var nextLine = Console.ReadLine();
                    if (nextLine == null)
                        break;
                    else if (nextLine.Trim() == "")
                        continue;
                    else
                        e = new FEnvironment(e.DataStack, e.WordDict, nextLine.Split(), e.ImmediateMode);
                }

                e = GetNext(e);
            }
        }
    }
}