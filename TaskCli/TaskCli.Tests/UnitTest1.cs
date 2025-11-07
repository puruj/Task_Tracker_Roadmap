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
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                int code = app.Run(new[] { "add" });

                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli add", error.ToString());

                // file should not be created / written
                Assert.False(File.Exists(tempFile));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                // guard to avoid FileNotFoundException
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);   
                }
            }
        }


        [Fact]
        public void Add_ValidDescription_CreatesTodoTaskWithId1()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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

    public class UpdateCommandTests
    {
        [Fact]
        public void Update_NoId_ReturnsErrorAndShowsUsage()
        {
            // Arrange: use a temp file as our "db"
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act
                int code = app.Run(new[] { "update" });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli update", error.ToString());
                // file should not be created / written
                Assert.False(File.Exists(tempFile));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                // guard to avoid FileNotFoundException
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void Update_Id_WhitespaceDescription_ShowsEmptyDescriptionError_AndDoesNotChangeTask()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Seed a real task with ID 1
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                app.Run(new[] { "add", "Buy", "milk" });

                var before = File.ReadAllText(tempFile);

                // Capture output for the update attempt
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act: valid id, whitespace-only description => should hit the branch
                int code = app.Run(new[] { "update", "1", "   " });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("New description cannot be empty.", error.ToString());

                // Ensure task file not modified
                var after = File.ReadAllText(tempFile);
                Assert.Equal(before, after);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Update_IdNotFound_ReturnsErrorAndDoesNotChangeTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                app.Run(new[] { "add", "Buy", "milk" });

                var before = File.ReadAllText(tempFile);

                // Capture output for the update attempt
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                int code = app.Run(new[] { "update", "2", "new description" });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Task with ID", error.ToString());

                // Ensure task file not modified
                var after = File.ReadAllText(tempFile);
                Assert.Equal(before, after);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Update_IdFound_UpdatesTaskAndReturnsSuccess()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Seed task with ID 1
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Buy", "milk" });
                Assert.Equal(0, addCode);

                // Capture output for update
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act: valid id + new description
                int code = app.Run(new[] { "update", "1", "Buy", "bread" });

                // Assert: success + correct console output
                Assert.Equal(0, code);
                Assert.Contains("Task 1 updated successfully.", output.ToString());
                Assert.True(string.IsNullOrEmpty(error.ToString()));

                // Assert: file actually updated
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
                Assert.Equal("Buy bread", task.Description);   // updated ?
                Assert.Equal(TaskStatus.Todo, task.TaskStatus); // status unchanged ?
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile))
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