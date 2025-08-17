# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AssistEC is a .NET 8 Blazor Server application using interactive server-side rendering. The project follows standard ASP.NET Core Blazor patterns with component-based architecture.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the application (development mode)
dotnet run --project AssistEC

# Run with specific launch profile
dotnet run --project AssistEC --launch-profile https
```

### Testing
```bash
# Run tests (if any test projects are added)
dotnet test
```

## Architecture

### Project Structure
- `AssistEC.sln` - Visual Studio solution file
- `AssistEC/` - Main web application project
  - `Components/` - Blazor components organized by function
    - `Layout/` - Layout components (MainLayout, NavMenu)
    - `Pages/` - Routable page components
    - `App.razor` - Root application component
    - `Routes.razor` - Router configuration
    - `_Imports.razor` - Global using statements
  - `Program.cs` - Application entry point and service configuration
  - `Properties/launchSettings.json` - Development server configuration
  - `wwwroot/` - Static web assets

### Key Technologies
- .NET 8
- Blazor Server with Interactive Server Components
- Bootstrap for styling
- ASP.NET Core hosting

### Component Architecture
- Uses `@rendermode InteractiveServer` for interactive components
- Components follow Blazor naming conventions (.razor files)
- Layout system based on `MainLayout` component
- Navigation handled through `NavMenu` component
- Router configured in `Routes.razor` with `DefaultLayout`

### Development Ports
- HTTP: localhost:5094
- HTTPS: localhost:7134 (primary)
- IIS Express: localhost:4040 (HTTP), localhost:44372 (HTTPS)

## Configuration
- Development environment configured in `launchSettings.json`
- App settings in `appsettings.json` and `appsettings.Development.json`
- Target framework: .NET 8.0
- Nullable reference types enabled
- Implicit usings enabled