# Fundo

`Fundo` is a Windows desktop file search application built with WinUI 3 and .NET 10.

The goal of the project is simple: search files quickly and comfortably, with support for both classic file system queries and indexed search using an SQLite database (which can be usefull if you index an samba share or other slow storage-devices).

## Features

Current functionality includes:

- search by file name
- optional regular expressions for file name matching
- search by date ranges
  - last access time
  - creation time
  - last write time
- search by file size ranges
- filter by file attributes
  - hidden
  - system
  - archive
- full-text search
  - case-sensitive search
  - regular expressions
  - whole word search
  - negated content matching
- global shortcut support

## Technology

- `.NET 10`
- `WinUI 3`
- `Entity Framework Core` with `SQLite`
- Windows target: `net10.0-windows10.0.17763.0`

## Project Structure

- `fundo/` - main application
- `FundoTests/` - automated tests

## Requirements

To build and run `Fundo`, you need:

- Windows 10 version 1809 or newer
- .NET 10 SDK
- Visual Studio 2026 or a compatible `dotnet` CLI environment

## Build

Build the application with:

`dotnet build fundo/fundo.csproj`

## Test

Run the tests with:

`dotnet test FundoTests/FundoTests.csproj`

## Publish

Example release publish command:

`dotnet publish fundo/fundo.csproj -c Release -r win-x64`

## License

`Fundo` is published under the GNU General Public License v3.0.

If you enjoy the software, you are warmly invited to buy the author a beer someday.

## Feedback

Questions, suggestions, and bug reports are welcome:

- `fundo.search@gmail.com`

## Status

`Fundo` is under active development. Planned improvements and future ideas are tracked in `fundo/todo.txt`.

Most of the source code is created by copilot. Vibe coding ftw :-)

`Fundo` is not published in the Microsoft Store yet, but it is planned for the next weeks.
