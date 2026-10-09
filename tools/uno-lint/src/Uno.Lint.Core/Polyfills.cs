// netstandard2.0 lacks IsExternalInit, which C# init accessors need.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
