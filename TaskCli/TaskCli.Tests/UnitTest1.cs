namespace TaskCli.Tests
{
    using TaskCli;
    using System;
    using System.IO;
    using Xunit;
    using System.Text.Json;
    using System.Text.Json.Serialization;


    public class RunCommandTests
    {
        [Fact]
        public void HelpCommand_PrintsUsageAndReturnsZero()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                int code = app.Run(new[] { "help" });

                Assert.Equal(0, code);
                Assert.Contains("Usage:", output.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void NoArgs_PrintsUsageAndReturnsZero()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                int code = app.Run(Array.Empty<string>());

                Assert.Equal(0, code);
                Assert.Contains("Usage:", output.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void UnknownCommand_ShowsErrorAndReturnsOne()
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

                int code = app.Run(new[] { "wat" });

                Assert.Equal(1, code);
                Assert.Contains("Unknown command: wat", error.ToString());
                Assert.Contains("Usage:", output.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }

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
        public void Add_WhitespaceDescription_ShowsEmptyDescriptionError_AndDoesNotCreateFile()
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

                // Act
                int code = app.Run(new[] { "add", "   " });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Description cannot be empty.", error.ToString());
                Assert.False(File.Exists(tempFile)); // nothing written
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
        public void Add_ValidDescription_CreatesTodoTaskWithId1()
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

                // Act
                int code = app.Run(new[] { "add", "Buy", "milk" });

                // Assert exit code
                Assert.Equal(0, code);

                // Optional: assert success message
                Assert.Contains("Task added successfully (ID: 1)", output.ToString());
                Assert.True(string.IsNullOrEmpty(error.ToString()));

                // Assert JSON content
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
                Console.SetOut(originalOut);
                Console.SetError(originalErr);

                if (File.Exists(tempFile))
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
                Assert.Contains("Task with ID 2 not found.", error.ToString());

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
        public void Update_InvalidId_ReturnsUsageAndDoesNotChangeTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Seed a valid task so we can verify nothing changes
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Buy", "milk" });
                Assert.Equal(0, addCode);

                var before = File.ReadAllText(tempFile);

                // Capture output for invalid update
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act: invalid id "abc" + some description text
                int code = app.Run(new[] { "update", "abc", "new", "description" });

                // Assert: hits the (!int.TryParse) path
                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli update <id> \"new description\"", error.ToString());
                Assert.True(string.IsNullOrEmpty(output.ToString())); // no success message

                // Assert: file unchanged
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
                Assert.Equal("Buy bread", task.Description);   // updated ✅
                Assert.Equal(TaskStatus.Todo, task.TaskStatus); // status unchanged ✅
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

    public class DeleteCommandTests()
    {
        [Fact]
        public void Delete_NoId_ReturnsErrorAndShowsUsage()
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
                int code = app.Run(new[] { "Delete" });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli delete", error.ToString());
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
        public void Delete_IdNotFound_ReturnsErrorAndDoesNotChangeTasks()
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

                // Capture output for the delete attempt
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                int code = app.Run(new[] { "delete", "2"});

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Task with ID 2 not found.", error.ToString());

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
        public void Delete_InvalidId_ReturnsUsageAndDoesNotChangeTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Seed a valid task so we can verify nothing changes
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Buy", "milk" });
                Assert.Equal(0, addCode);

                var before = File.ReadAllText(tempFile);

                // Capture output for invalid update
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act: invalid id "abc"
                int code = app.Run(new[] { "delete", "abc"});

                // Assert: hits the (!int.TryParse) path
                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli delete <id>", error.ToString());
                Assert.True(string.IsNullOrEmpty(output.ToString())); // no success message

                // Assert: file unchanged
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
        public void Delete_IdFound_DeletesTaskAndReturnsSuccess()
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

                // Capture output for delete
                var output = new StringWriter();
                var error = new StringWriter();
                Console.SetOut(output);
                Console.SetError(error);

                // Act: delete existing task 1
                int code = app.Run(new[] { "delete", "1" });

                // Assert: success + correct console output
                Assert.Equal(0, code);
                Assert.Contains("Task 1 deleted successfully.", output.ToString());
                Assert.True(string.IsNullOrEmpty(error.ToString()));

                // Assert: file actually updated (task removed)
                var json = File.ReadAllText(tempFile);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);

                Assert.NotNull(tasks);
                Assert.Empty(tasks!); // ✅ no tasks left
                                      // Or:
                                      // Assert.DoesNotContain(tasks, t => t.Id == 1);
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

    public class MarkStatusCommandTests
    {
        [Fact]
        public void MarkInProgress_NoId_ShowsUsageAndReturnsError()
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

                // No ID passed
                int code = app.Run(new[] { "mark-in-progress" });

                Assert.Equal(1, code);
                Assert.Contains("Usage: task-cli mark-in-progress <id>", error.ToString());
                // nothing should have been written
                Assert.False(File.Exists(tempFile));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void MarkInProgress_TaskNotFound_ReturnsErrorAndDoesNotChangeFile()
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

                var addCode = app.Run(new[] { "add", "Sample", "task" });
                Assert.Equal(0, addCode);

                Console.SetOut(new StringWriter());
                Console.SetError(error);

                // use an ID that doesn't exist
                int code = app.Run(new[] { "mark-in-progress", "999" });

                Assert.Equal(1, code);
                Assert.Contains("Task with ID 999 not found.", error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void Todo_ValidId_SetsStatusTodo()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Add a task first
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Sample", "task" });
                Assert.Equal(0, addCode);

                // Act: mark it todo
                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                int code = app.Run(new[] { "mark-todo", "1" });

                Assert.Equal(0, code);
                Assert.Contains("Task 1 marked as todo.", output.ToString());

                // Verify JSON
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
                Assert.Equal(TaskStatus.Todo, task.TaskStatus);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void MarkInProgress_ValidId_SetsStatusInProgress()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Add a task first
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Sample", "task" });
                Assert.Equal(0, addCode);

                // Act: mark it in-progress
                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                int code = app.Run(new[] { "mark-in-progress", "1" });

                Assert.Equal(0, code);
                Assert.Contains("Task 1 marked as in-progress.", output.ToString());

                // Verify JSON
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
                Assert.Equal(TaskStatus.InProgress, task.TaskStatus);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void MarkDone_ValidId_SetsStatusDone()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;

            try
            {
                var app = new TaskApp(tempFile);

                // Add a task first
                Console.SetOut(new StringWriter());
                Console.SetError(new StringWriter());
                var addCode = app.Run(new[] { "add", "Sample", "task" });
                Assert.Equal(0, addCode);

                // Act: mark it done
                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                int code = app.Run(new[] { "mark-done", "1" });

                Assert.Equal(0, code);
                Assert.Contains("Task 1 marked as done.", output.ToString());

                // Verify JSON
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
                Assert.Equal(TaskStatus.Done, task.TaskStatus);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

    }

    public class ListCommandTests
    {
        [Fact]
        public void List_InvalidFilter_ShowsErrorAndUsage()
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

                // Act
                int code = app.Run(new[] { "list", "invalid-filter" });

                // Assert
                Assert.Equal(1, code);
                Assert.Contains("Unknown status filter. Use: done | todo | in-progress", error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_Todo_FiltersOnlyTodoTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
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
                int code = app.Run(new[] { "list", "todo" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the todo task
                Assert.Contains("todo-task", text);

                // should not show the done task
                Assert.DoesNotContain("done-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_Done_FiltersOnlyDoneTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
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
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_InProgress_FiltersOnlyInProgressTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "inprogress-task", TaskStatus = TaskStatus.InProgress},
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
                int code = app.Run(new[] { "list", "inprogress" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the inprogress task
                Assert.Contains("inprogress-task", text);

                // should not show the done task
                Assert.DoesNotContain("done-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_In_Progress_FiltersOnlyInProgressTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "inprogress-task", TaskStatus = TaskStatus.InProgress},
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
                int code = app.Run(new[] { "list", "in_progress" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the inprogress task
                Assert.Contains("inprogress-task", text);

                // should not show the done task
                Assert.DoesNotContain("done-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_FilterInDashProgress_FiltersOnlyInProgressTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "inprogress-task", TaskStatus = TaskStatus.InProgress},
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
                int code = app.Run(new[] { "list", "in-progress" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the inprogress task
                Assert.Contains("inprogress-task", text);

                // should not show the done task
                Assert.DoesNotContain("done-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_No_Filter_FilterAll()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {                
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "todo-task", TaskStatus = TaskStatus.Todo},
                new TaskItem { Id = 2, Description = "inprogress-task", TaskStatus = TaskStatus.InProgress},
                new TaskItem { Id = 3, Description = "done-task", TaskStatus = TaskStatus.Done },
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
                int code = app.Run(new[] { "list"});

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show the all tasks
                Assert.Contains("todo-task", text);
                Assert.Contains("inprogress-task", text);
                Assert.Contains("done-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_Valid_Filter_FilterNoMatches()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                // Arrange: seed two tasks (one todo, one done)
                var seedTasks = new[]
                {
                new TaskItem { Id = 1, Description = "todo-task", TaskStatus = TaskStatus.Todo},
                new TaskItem { Id = 2, Description = "inprogress-task", TaskStatus = TaskStatus.InProgress},
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

                // should show no tasks returned
                Assert.DoesNotContain("todo-task", text);
                Assert.DoesNotContain("inprogress-task", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_NoFile_PrintsNoTasksAndReturnsZero()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                // Act
                int code = app.Run(new[] { "list"});

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show no tasks returned
                Assert.Contains("No tasks found.", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_EmptyFile_PrintsNoTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                File.WriteAllText(tempFile, string.Empty);

                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                // Act
                int code = app.Run(new[] { "list" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show no tasks returned
                Assert.Contains("No tasks found.", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void List_CorruptFile_DoesNotThrowAndPrintsNoTasks()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            try
            {
                File.WriteAllText(tempFile, "this is not json at all");

                var app = new TaskApp(tempFile);

                var output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(new StringWriter());

                // Act
                int code = app.Run(new[] { "list" });

                // Assert
                Assert.Equal(0, code);

                var text = output.ToString();

                // should show no tasks returned
                Assert.Contains("No tasks found.", text);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                File.Delete(tempFile);
            }
        }
    }
}