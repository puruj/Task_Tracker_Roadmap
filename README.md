# TaskCli — Tiny Task Tracker (C#/.NET 8)

![.NET](https://github.com/puruj/Task_Tracker_Roadmap/actions/workflows/dotnet.yml/badge.svg)

A small, tested command-line task tracker. It stores tasks in `Tasks.json` in the **current working directory**, so you can keep separate lists per folder. Distributed as a `dotnet` global tool.

---

## Features
- Add / update / delete tasks
- Mark status: `todo`, `in-progress`, `done`
- Filtered listing: `list`, `list todo`, `list in-progress`, `list done`
- JSON storage (camelCase; enums serialized as strings)
- Unit tests with xUnit
- CI via GitHub Actions

---

## Install (from source)

```bash
# from repo root
cd TaskCli
dotnet build -c Release
dotnet pack  -c Release

# install as a global tool from the local package
dotnet tool install --global Puruj.TaskCli --add-source ./TaskCli/bin/Release
```

After install:
```bash
task-cli --help
```

Update later (after bumping `<Version>` in the `.csproj`):
```bash
dotnet tool update --global Puruj.TaskCli --add-source ./TaskCli/bin/Release
```

Uninstall:
```bash
dotnet tool uninstall --global Puruj.TaskCli
```

---

## Usage

```bash
# Add a task
task-cli add "Buy groceries"

# Update description
task-cli update 1 "Buy groceries and milk"

# Delete
task-cli delete 1

# Mark status
task-cli mark-todo 2
task-cli mark-in-progress 2
task-cli mark-done 2

# List (optionally filter)
task-cli list
task-cli list todo
task-cli list in-progress
task-cli list done
```

**Storage:** the tool reads/writes `Tasks.json` in the **current directory**.  
Run it in different folders to keep separate lists.

Example output:
```
 ID  STATUS        UPDATED           DESCRIPTION
 1   todo          2025-11-11 17:23  Buy Nintendo switch 2
```

---

## Development

```bash
# build & test
dotnet build
dotnet test

# pack as a tool
cd TaskCli
dotnet pack -c Release
```

CI: Every push/PR on `main` runs `dotnet test` via GitHub Actions (`.github/workflows/dotnet.yml`).

---

## Roadmap
- Global store option (e.g., `%USERPROFILE%\.task-cli\tasks.json`)
- Due dates, priorities, tags, edit command
- JSON schema & validation
- Publish to NuGet for one-line install without `--add-source`

## License
MIT
