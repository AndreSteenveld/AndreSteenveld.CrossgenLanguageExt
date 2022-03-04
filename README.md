# Introduction

This repository contains a small application in an attempt to reproduce an issue involving `crossgen2` and `LanguageExt` (v4.0.2). It seems that applications using `LanguageExt` can't be published using the "ReadyToRun" option, when building it looks like crossgen is very busy cross-joining all generics from `LanguageExt` with each other. This didn't happen with `crossgen1` when using .NET 5.

Check the changelog at the end of this README for all the changes, workarounds and experiments.

# Setup and reproduction

There are two project files, one for .NET 5 and one for .NET 6 with their own build profiles. Running both applications just from the commandline works fine;

```bash
$ dotnet run --project ./AndreSteenveld.CrossgenLanguageExt5.csproj 
Hello world

$ dotnet run --project ./AndreSteenveld.CrossgenLanguageExt6.csproj 
Hello world
```

As this is a quick reproduction case and nothing serious I haven't bothered creating any `pubxml` files, I'm assuming this doesn't make any significant difference for this specific issue. Running the following commands works fine and builds me a usable binary;

```bash
#
# Building .NET 5
#
$ dotnet publish ./AndreSteenveld.CrossgenLanguageExt5.csproj -p:PublishSingleFile=true --configuration Release --runtime win-x64 --self-contained true

# The output is what I expected
$ ls ./bin/Release/net5.0/win-x64/publish/
AndreSteenveld.CrossgenLanguageExt5.exe*  AndreSteenveld.CrossgenLanguageExt5.pdb  clrcompression.dll*  clrjit.dll*  coreclr.dll*  mscordaccore.dll*

# And running the application works fine
$ ./bin/Release/net5.0/win-x64/publish/AndreSteenveld.CrossgenLanguageExt5.exe 
Hello world
```

```bash
#
# Building .NET 6
#
$ dotnet publish ./AndreSteenveld.CrossgenLanguageExt6.csproj -p:PublishSingleFile=true --configuration Release --runtime win-x64 --self-contained true

# Same here, output is expected
$ ls ./bin/Release/net6.0/win-x64/publish/
AndreSteenveld.CrossgenLanguageExt6.exe*  AndreSteenveld.CrossgenLanguageExt6.pdb

# ...And running the application works fine
$ ./bin/Release/net6.0/win-x64/publish/AndreSteenveld.CrossgenLanguageExt6.exe 
Hello world
```

Before running these builds I removed the `./bin/` and `./obj/` directories by running `rm -rf ./bin/ ./obj/`.

```bash
#
# Building .NET 5 with ReadyToRun, it does take a while ~15 minutes.
#
$ dotnet publish ./AndreSteenveld.CrossgenLanguageExt5.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --configuration Release --runtime win-x64 --self-contained true

# The output is what I expected and saw earlier;
$ ls ./bin/Release/net5.0/win-x64/publish/
AndreSteenveld.CrossgenLanguageExt5.exe*  AndreSteenveld.CrossgenLanguageExt5.pdb  clrcompression.dll*  clrjit.dll*  coreclr.dll*  mscordaccore.dll*

# The binary is a hefty 217M... so I guess it does contain everything except the kitchen sink
$ ls -hl ./bin/Release/net5.0/win-x64/publish/AndreSteenveld.CrossgenLanguageExt5.exe 
-rwxr-xr-x 1 asteenveld 1049089 217M Mar  2 10:16 ./bin/Release/net5.0/win-x64/publish/AndreSteenveld.CrossgenLanguageExt5.exe*

# Running the thing works fine
$ ./bin/Release/net5.0/win-x64/publish/AndreSteenveld.CrossgenLanguageExt5.exe 
Hello world
```

```bash
#
# Building .NET 6 with ReadyToRun, this just never completes. I also found an option to provide specific options to crossgen, I've added the `--verbose` flag so I can see
# if something is going on. I also timed the run in case it would finish to get a some sort information on how long it would take for this simple case.
#
$ time dotnet publish ./AndreSteenveld.CrossgenLanguageExt6.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishReadyToRunCrossgen2ExtraArgs='--verbose' --configuration Release --runtime win-x64 --self-contained true
Microsoft (R) Build Engine version 17.0.0+c9eb9dd64 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  Restored C:\Users\asteenveld\source\repos\AndreSteenveld.CrossgenLanguageExt\AndreSteenveld.CrossgenLanguageExt6.csproj (in 1.54 sec).
  AndreSteenveld.CrossgenLanguageExt6 -> C:\Users\asteenveld\source\repos\AndreSteenveld.CrossgenLanguageExt\bin\Release\net6.0\win-x64\AndreSteenveld.CrossgenLanguageExt6.dll
Attempting to cancel the build...
C:\Program Files\dotnet\sdk\6.0.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.CrossGen.targets(463,5): warning MSB5021: Terminating the task executable "crossgen2" and its child processes because the build was canceled. [C:\Users\asteenveld\source\rep
os\AndreSteenveld.CrossgenLanguageExt\AndreSteenveld.CrossgenLanguageExt6.csproj]
C:\Program Files\dotnet\sdk\6.0.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.CrossGen.targets(463,5): error MSB4181: The "RunReadyToRunCompiler" task returned false but did not log an error. [C:\Users\asteenveld\source\repos\AndreSteenveld.CrossgenLa
nguageExt\AndreSteenveld.CrossgenLanguageExt6.csproj]

real   	84m26.764s
user   	0m0.000s
sys    	0m0.125s

#
# As it turns out adding `-p:PublishReadyToRunCrossgen2ExtraArgs='--verbose'` didn't do anything if not invoked with a publish profile... ¯\_(ツ)_/¯. The crossgen executable
# is being used is "~/.nuget/packages/microsoft.netcore.app.crossgen2.win-x64/6.0.2/tools/crossgen2.exe` when I lookup the process in the resource manager.
# 
```

# Misc

The projects were created by running the following sequence;

```bash
$ dotnet new console --framework net6.0

# --force, to override `Program.cs`
$ dotnet new console --framework net5.0 --force

# LanguageExt was added to both projects using;
$ dotnet add ./AndreSteenveld.CrossgenLanguageExt5.csproj package LanguageExt.Core
$ dotnet add ./AndreSteenveld.CrossgenLanguageExt6.csproj package LanguageExt.Core
```

```bash
#
# Commands are run in a git bash terminal on a windows machine
#
$ dotnet --info
.NET SDK (reflecting any global.json):
 Version:   6.0.102
 Commit:    02d5242ed7

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.19043
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   C:\Program Files\dotnet\sdk\6.0.102\

Host (useful for support):
  Version: 6.0.2
  Commit:  839cdfb0ec

.NET SDKs installed:
  3.1.416 [C:\Program Files\dotnet\sdk]
  5.0.211 [C:\Program Files\dotnet\sdk]
  5.0.404 [C:\Program Files\dotnet\sdk]
  5.0.405 [C:\Program Files\dotnet\sdk]
  6.0.102 [C:\Program Files\dotnet\sdk]

.NET runtimes installed:
  Microsoft.AspNetCore.All 2.1.30 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.1.30 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.1.22 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 5.0.13 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 5.0.14 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 6.0.2 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 2.1.30 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.1.22 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 5.0.13 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 5.0.14 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 6.0.2 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.WindowsDesktop.App 3.1.22 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 5.0.13 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 5.0.14 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 6.0.2 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]

To install additional .NET runtimes or SDKs:
  https://aka.ms/dotnet-download
```

# Changelog

## 2022-03-04 Updating the SDK

I've updated my SDK by downloading the most recent version of .NET 6, `dotnet --info` now reports that I'm running version `6.0.200`. RRunning the build with the newer version of .NET 6 didn't change anything of significance;

```bash
$ time dotnet publish ./AndreSteenveld.CrossgenLanguageExt6.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishReadyToRunCrossgen2ExtraArgs='--verbose' --configuration Release --runtime win-x64 --self-contained trueMicrosoft (R) Build Engine version 17.1.0+ae57d105c for .NET
Copyright (C) Microsoft Corporation. All rights reserved.   
  Determining projects to restore...
  Restored C:\Users\asteenveld\source\repos\AndreSteenveld.CrossgenLanguageExt\AndreSteenveld.CrossgenLanguageExt6.csproj (in 1.38 sec).
  AndreSteenveld.CrossgenLanguageExt6 -> C:\Users\asteenveld\source\repos\AndreSteenveld.CrossgenLanguageExt\bin\Release\net6.0\win-x64\AndreSteenveld.CrossgenLanguageExt6.dll
Attempting to cancel the build...
C:\Program Files\dotnet\sdk\6.0.200\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.CrossGen.targets(463,5): warning MSB5021: Terminating the task executable "crossgen2" and its child processes because the build was canceled. [C:\Users\asteenveld\source\rep
os\AndreSteenveld.CrossgenLanguageExt\AndreSteenveld.CrossgenLanguageExt6.csproj]

real   	46m55.785s
user   	0m0.000s
sys    	0m0.078s
```

