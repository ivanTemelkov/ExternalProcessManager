# IvTem.ExternalProcessManager Samples

## Generic Host sample

Run the sample host from the repository root:

```powershell
dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8
```

The host starts `IvTem.ExternalProcessManager.SampleWorker`, logs diagnostics
snapshots, then stops the worker when `SampleHost:RunSeconds` elapses.

## Native AOT smoke test

Publish the host for Windows x64:

```powershell
dotnet publish samples/IvTem.ExternalProcessManager.SampleHost/IvTem.ExternalProcessManager.SampleHost.csproj -c Release -r win-x64
```

The host publish copies the Native AOT worker executable beside the host
executable. Run the published sample with:

```powershell
samples\IvTem.ExternalProcessManager.SampleHost\bin\Release\net10.0\win-x64\publish\IvTem.ExternalProcessManager.SampleHost.exe --SampleHost:RunSeconds=8
```
