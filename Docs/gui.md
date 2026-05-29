# Avalonia GUI

`OrderSystem.Desktop` is an Avalonia desktop shell for the retail order system.

The GUI is intentionally thin at this stage. It is a single-view desktop app with an operation console and an output console. The next step can replace the staged operation output with real REST API execution.

`OrderSystem.Desktop` is listed first in `OrderSystem.sln` so GUI execution is the default startup candidate in IDEs that infer startup from solution order.

## Project

```text
OrderSystem.Desktop
├── App.axaml
├── Program.cs
├── ViewModels
│   ├── MainWindowViewModel.cs
│   └── RelayCommand.cs
└── Views
    └── MainWindow.axaml
```

## View

The main window has only two work areas:

- Operation Console: selects one REST operation and edits the sample request body.
- Output Console: shows staged execution output and later API logs.

## Run

The local environment currently uses the Rider bundled .NET SDK:

```bash
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet run --project OrderSystem.Desktop/OrderSystem.Desktop.csproj
```

The desktop shell defaults to `http://localhost:5000` as the API base URL. The existing ASP.NET Core API can be run separately and later wired into typed API clients from the desktop project.

## Verification

The test suite checks that the Avalonia project is registered in the solution and that the single-view console model exposes operation staging and output clearing.
