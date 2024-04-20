using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Core;
using Couchbase.N1QL;

namespace taskManager
{

    internal class Program
    {
        static Cluster cluster;
        static IBucket bucket;

        static void Main(string[] args)
        {
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = new List<Uri> { new Uri("http://localhost:8091") },
            });
            var authenticator = new PasswordAuthenticator("Nada", "2342002Nada");
            cluster.Authenticate(authenticator);
            bucket = cluster.OpenBucket("taskManager");

            try
            {
                Console.WriteLine("Welcome to Task Manager!\n");
                bool exit = false;

                while (!exit)
                {
                    Console.WriteLine("\nPlease select one of the available commands : \n");
                    Console.WriteLine("1. Add task");
                    Console.WriteLine("2. List a specific task tasks");
                    Console.WriteLine("3. List all tasks");
                    Console.WriteLine("4. Update task");
                    Console.WriteLine("5. Mark task as completed");
                    Console.WriteLine("6. Delete task");
                    Console.WriteLine("7. Delete all tasks");
                    Console.WriteLine("8. List of completed tasks");
                    Console.WriteLine("9. List of incompleted tasks");
                    Console.WriteLine("10. Exit");
                    Console.Write("\nEnter your choice : ");
                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            AddTask();
                            break;

                        case "2":
                            ListSpecificTask();
                            break;

                        case "3":
                            ListTasks();
                            break;

                        case "4":
                            UpdateTask();
                            break;

                        case "5":
                            MarkTaskAsCompleted();
                            break;

                        case "6":
                            DeleteTask();
                            break;

                        case "7":
                            DeleteAllTasks();
                            break;

                        case "8":
                            ListCompletedTasks();
                            break;

                        case "9":
                            ListInCompletedTasks();
                            break;

                        case "10":
                            exit = true;
                            break;

                        default:
                            Console.WriteLine("Invalid command. Please try again.");
                            break;
                    }
                }
            }
            finally
            {
                bucket.Dispose();
                cluster.Dispose();
            }
        }

        static void AddTask()
        {
            Console.Write("Enter task Title: ");
            var title = Console.ReadLine();
            Console.Write("Enter task Description: ");
            var description = Console.ReadLine();
            var newTask = new Task
            {
                ID = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                IsCompleted = false
            };
            var result = bucket.Upsert(newTask.ID, newTask);
            if (result.Success)
            {
                Console.WriteLine("Task added successfully.");
            }
            else
            {
                Console.WriteLine("Failed to add the task. Please try again.");
            }
        }

        static void ListSpecificTask()
        {
            Console.Write("Enter the task ID to list a specific task : ");
            var taskIdToList = Console.ReadLine();
            var taskToList = bucket.Get<dynamic>(taskIdToList);
            if (taskToList.Success)
            {
                Console.WriteLine("\nTask Details:");
                Console.WriteLine($"Task" +
                    $"" +
                    $" ID: {taskIdToList}");
                Console.WriteLine($"Title: {taskToList.Value.title}");
                Console.WriteLine($"Description: {taskToList.Value.description}");
                Console.WriteLine($"IsCompleted: {taskToList.Value.isCompleted}");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }

        static void ListTasks()
        {
            //SELECT * FROM `TaskBucket`;
            var query = new QueryRequest("SELECT META().id, title, description, isCompleted FROM `taskManager`");
            var queryResult = bucket.Query<dynamic>(query);

            if (queryResult.Success)
            {
                Console.WriteLine("List of All Tasks : \n");
                foreach (var row in queryResult.Rows)
                {
                    Console.WriteLine($"Task ID : {row.id}");
                    Console.WriteLine($"Title : {row.title}");
                    Console.WriteLine($"Description : {row.description}");
                    Console.WriteLine($"IsCompleted : {row.isCompleted}");
                    Console.WriteLine();
                }

            }
            else
            {
                Console.WriteLine("No Tasks.");

            }
        }

        static void UpdateTask()
        {
            Console.Write("Enter the task ID to update : ");
            var taskIdToUpdate = Console.ReadLine();
            var taskToUpdate = bucket.Get<dynamic>(taskIdToUpdate);

            if (taskToUpdate.Success)
            {
                var taskData = taskToUpdate.Value;
                Console.Write("Enter updated title : ");
                taskData.title = Console.ReadLine();
                Console.Write("Enter updated description : ");
                taskData.description = Console.ReadLine();
                bucket.Upsert(taskIdToUpdate, taskData);
                Console.WriteLine("Task updated successfully.");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }

        static void DeleteTask()
        {
            Console.Write("Enter the task ID to delete : ");
            var taskIdToDelete = Console.ReadLine();

            if (bucket.Remove(taskIdToDelete).Success)
            {
                Console.WriteLine("Task deleted successfully.");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }

        static void DeleteAllTasks()
        {
            var query = new QueryRequest("SELECT META().id, title, description, isCompleted FROM `taskManager`");

            var queryResult = bucket.Query<dynamic>(query);

            if (queryResult.Success)
            {
                foreach (var row in queryResult.Rows)
                {
                    var taskId = row.id;
                    var document = new Document<dynamic>
                    {
                        Id = taskId
                    };

                    var result = bucket.Remove(document);

                    if (result.Success)
                    {
                        Console.WriteLine("Task with ID " + taskId + " has been deleted.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete task with ID " + taskId);
                    }
                }
                Console.WriteLine("All tasks have been deleted.");
            }
        }

        static void MarkTaskAsCompleted()
        {
            Console.Write("Enter task ID to mark as completed: ");
            var taskId = Console.ReadLine();
            var task = bucket.Get<Task>(taskId);
            if (task.Success)
            {
                task.Value.IsCompleted = true;
                bucket.Replace(taskId, task.Value);
                Console.WriteLine("Task marked as completed.");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }

        static void ListCompletedTasks()
        {
            var query = new QueryRequest("SELECT META().id, title, description, isCompleted FROM `taskManager` WHERE isCompleted = true");
            var queryResult = bucket.Query<dynamic>(query);

            if (queryResult.Success)
            {
                Console.WriteLine("List of Completed Tasks : \n");

                foreach (var row in queryResult.Rows)
                {
                    Console.WriteLine($"Task ID : {row.id}");
                    Console.WriteLine($"Title : {row.title}");
                    Console.WriteLine($"Description : {row.description}");
                    Console.WriteLine($"IsCompleted : {row.isCompleted}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve completed tasks.");
            }
        }

        static void ListInCompletedTasks()
        {
            var query = new QueryRequest("SELECT META().id, title, description, isCompleted FROM `taskManager` WHERE isCompleted = false");


            var queryResult = bucket.Query<dynamic>(query);

            if (queryResult.Success)
            {
                Console.WriteLine("List of Incompleted Tasks : \n");

                foreach (var row in queryResult.Rows)
                {
                    Console.WriteLine($"Task ID : {row.id}");
                    Console.WriteLine($"Title : {row.title}");
                    Console.WriteLine($"Description : {row.description}");
                    Console.WriteLine($"IsCompleted : {row.isCompleted}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve Incompleted tasks.");
            }
        }
    }
}

