# Legacy oracle

A 40-line .NET Framework 4.8 program that runs the legacy VB.NET code (`SGC.Negocio`, `SGC.Batch`) and
answers test cases through stdin/stdout. Tests on .NET 10 use it to compare new code with the old code
*running*, instead of with a description of it.

Why out of process: .NET 10 cannot load `SGC.Batch.exe` (`BadImageFormatException`: it is a .NET
Framework executable built with "prefer 32-bit"). The same approach works for any legacy assembly that
modern .NET can't load (`System.Web` dependencies, 32-bit COM).

Build (after `legacy\src\build.cmd`):

```bat
dotnet build tools\oracle
```

Protocol, one line per case:

```
B|2026-11-15|1560|00000022500   -> 99990.00111 70000.002256 00012.345609 4 16310000156000
E|975|2026-07-15|2026-07-16     -> 19.5;0.33
```
