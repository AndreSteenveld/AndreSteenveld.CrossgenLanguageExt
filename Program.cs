using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace AndreSteenveld.CrossgenLanguageExt
{
    class Program
    {
        static int Main(string[] args) =>
            Some("Hello world")
                .Do( Console.WriteLine )
                .Match(
                    Some: _ => 0,
                    None: () => 1
                );
    }
}
