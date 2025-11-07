namespace TaskCli.Tests
{
    using TaskCli;
    using System;
    using System.IO;
    using Xunit;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class AddCommandTests
    {
        [Fact]
        public void Add_NoDescription_ReturnsErrorAndShowsUsage()
        {
            // Arrange: use a temp file as our "db"
            var tempFile = Path.GetTempFileName();
            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act
                int code = app.Run(new[] { "add" });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli add", error.ToString());
                Assert.True(new FileInfo(tempFile).Length == 0);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Add_ValidDescription_CreatesTodoTaskWithId1()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                // Act
                int code = app.Run(new[] { "add", "Buy", "milk" });

                // Assert exit code
                Assert.Equal(0, code);

                // Read and deserialize with same options as app
                var json = File.ReadAllText(tempFile);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);

                Assert.NotNull(tasks);
                Assert.Single(tasks);

                var task = tasks![0];
                Assert.Equal(1, task.Id);
                Assert.Equal("Buy milk", task.Description);
                Assert.Equal(TaskStatus.Todo, task.TaskStatus);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }

    public class ListCommandTests
    {
        [Fact]
        public void List_Done_FiltersOnlyDoneTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                // Arrange: seed two tasks (one todo, one done)
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "todo-task", TaskStatus = TaskStatus.Todo },
                new TaskItem { Id = 2, Description = "done-task", TaskStatus = TaskStatus.Done },
            };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                File.WriteAllText(tempFile, JsonSerializer.Serialize(seedTasks, jsonOptions));

                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                // Act
                int code = app.Run(new[] { "list", "done" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the done task
                Assert.Contains("done-task", text);

                // should not show the todo task
                Assert.DoesNotContain("todo-task", text);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}