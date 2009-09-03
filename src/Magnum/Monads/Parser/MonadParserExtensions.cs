// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Monads.Parser
{
    using System;
    using System.Linq;

    public static class MonadParserExtensions
    {
        public static Parser<TInput, TValue> Where<TInput, TValue>(this Parser<TInput, TValue> parser, Func<TValue, bool> pred)
        {
            return input =>
                {
                    Result<TInput, TValue> result = parser(input);
                    if (result == null || !pred(result.Value))
                        return null;

                    return result;
                };
        }

        public static Parser<TInput, TSelect> Select<TInput, TValue, TSelect>(this Parser<TInput, TValue> parser, Func<TValue, TSelect> selector)
        {
            return input =>
                {
                    Result<TInput, TValue> result = parser(input);
                    if (result == null)
                        return null;

                    return new Result<TInput, TSelect>(selector(result.Value), result.Rest);
                };
        }

        public static Parser<TInput, TSelect> SelectMany<TInput, TValue, TIntermediate, TSelect>(this Parser<TInput, TValue> parser, Func<TValue, Parser<TInput, TIntermediate>> selector, Func<TValue, TIntermediate, TSelect> projector)
        {
            return input =>
                {
                    Result<TInput, TValue> result = parser(input);
                    if (result == null)
                        return null;

                    TValue val = result.Value;
                    Result<TInput, TIntermediate> nextResult = selector(val)(result.Rest);
                    if (nextResult == null)
                        return null;

                    return new Result<TInput, TSelect>(projector(val, nextResult.Value), nextResult.Rest);
                };
        }

        public static Parser<TInput, TValue> Or<TInput, TValue>(this Parser<TInput, TValue> first,
                                                        Parser<TInput, TValue> second)
        {
            return input => first(input) ?? second(input);
        }

        public static Parser<TInput, TSecondValue> And<TInput, TFirstValue, TSecondValue>(this Parser<TInput, TFirstValue> first,
                                                                                          Parser<TInput, TSecondValue> second)
        {
            return input => second(first(input).Rest);
        }
    }

    public abstract class Parser<TInput>
    {
        public Parser<TInput, TValue> Succeed<TValue>(TValue value)
        {
            return input => new Result<TInput, TValue>(value, input);
        }

        public Parser<TInput, TValue[]> Rep<TValue>(Parser<TInput, TValue> parser)
        {
            return Rep1(parser).Or(Succeed(new TValue[0]));
        }

        public Parser<TInput, TValue[]> Rep1<TValue>(Parser<TInput, TValue> parser)
        {
            return from x in parser
                   from xs in Rep(parser)
                   select (new[] {x}).Concat(xs).ToArray();
        }
    }
}