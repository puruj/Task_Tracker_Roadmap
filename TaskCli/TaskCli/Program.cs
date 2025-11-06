// add, update, delete tasks
// mark tasks as complete or in prorgress
// list all tasks
// list tasks that are done
// list all tasks not done
// list task that are in progress

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var app = new TaskApp();
app.Run(args);

class TaskApp
{
    private const string fileName = "Tasks.Json";
    private static readonly string dbPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        //for enum serialization
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public int Run(string[] args)
    {
        try
        {
            if(args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            string cmd = args[0].ToLowerInvariant();

            switch(cmd)
            {
                case "add": return Add(args.Skip(1).ToArray());
                case "update": return Update(args.Skip(1).ToArray());
                case "help":
                    PrintHelp();
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown command: {cmd}\n");
                    PrintHelp();
                    return 1;
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }

    }



    private static int Add(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: task-cli add \"description\"");
            return 1;
        }

        string description = string.Join(" ", args).Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            Console.Error.WriteLine("Description cannot be empty.");
            return 1;
        }

        var tasks = LoadTasks();
        int newId = tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
        var now = DateTimeOffset.UtcNow;

        tasks.Add(new TaskItem
        {
            Id = newId,
            Description = description,
            TaskStatus = TaskStatus.ToDo,
            CreatedAt = now,
            UpdatedAt = now
        });

        SaveTasks(tasks);
        Console.WriteLine($"Task added successfully (ID: {newId})");
        return 0;
    }

    private int Update(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[0], out int id))
        {
            Console.Error.WriteLine("Usage: task-cli update <id> \"new description\"");
            return 1;
        }

        string newDescription = string.Join(" ", args.Skip(1)).Trim();
        if (string.IsNullOrWhiteSpace(newDescription))
        {
            Console.Error.WriteLine("New description cannot be empty.");
            return 1;
        }

        var tasks = LoadTasks();
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            Console.Error.WriteLine($"Task with ID {id} not found.");
            return 1;
        }

        task.Description = newDescription;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        SaveTasks(tasks);
        Console.WriteLine($"Task {id} updated successfully.");

        return 0;
    }
    private static List<TaskItem> LoadTasks()
    {
        try
        {
            if(!File.Exists(dbPath))
            {
                return new List<TaskItem>();
            }
            var json = File.ReadAllText(dbPath);
            if(string.IsNullOrWhiteSpace(json))
            {
                return new List<TaskItem>();
            }
            return JsonSerializer.Deserialize<List<TaskItem>>(json, jsonOptions) ?? new List<TaskItem>();
        }
        catch
        {
            // if file is corrupt, fall back to empty list
            return new List<TaskItem>();
        }
    }

    private static void SaveTasks(List<TaskItem> tasks)
    {
        var json = JsonSerializer.Serialize(tasks, jsonOptions);
        File.WriteAllText(dbPath, json);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
              Usage:
              task-cli add "Buy groceries"
              task-cli update <id> "New description"
              task-cli delete <id>
              task-cli mark-in-progress <id>
              task-cli mark-done <id>
              task-cli list [done|todo|in-progress]

            Notes:
              • Tasks are stored in ./Tasks.json
              • Status values: todo | in-progress | done

            """);
    }
}

enum TaskStatus
{
    ToDo,
    InProgress,
    Done
}
class TaskItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public TaskStatus TaskStatus { get; set; } = TaskStatus.ToDo;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;


}