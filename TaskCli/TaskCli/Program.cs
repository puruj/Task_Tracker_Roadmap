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
    private static readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

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
    public int id { get; set; }
    public string description { get; set; } = "";
    public TaskStatus taskStatus { get; set; } = TaskStatus.ToDo;
    public DateTime createdAt { get; set; } = DateTime.Now;
    public DateTime updatedAt { get; set; } = DateTime.Now;


}