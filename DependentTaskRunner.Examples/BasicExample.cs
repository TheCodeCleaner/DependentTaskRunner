using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependentTaskRunner.Examples
{
    public static class BasicExample
    {
        public static void Run()
        {
            var taskRunner = new TaskRunner<MyTask>(t => t.Dependencies, t => Task.Run(()=> t.Perform()));

            var taskA = new MyTask { Perform = RunA };
            var taskB = new MyTask { Perform = RunB };
            var taskC = new MyTask { Dependencies = new[] { taskA, taskB }, Perform = RunC };

            taskRunner.PerformTasks(new[] { taskA, taskB, taskC });
        }

        private static bool RunA()
        {
            Console.WriteLine(DateTime.Now + " A Started");
            Thread.Sleep(2000);
            Console.WriteLine(DateTime.Now + " A Finished");
            return true;
        }

        private static bool RunB()
        {
            Console.WriteLine(DateTime.Now + " B Started");
            Thread.Sleep(3000);
            Console.WriteLine(DateTime.Now + " B Finished");
            return true;
        }

        private static bool RunC()
        {
            Console.WriteLine(DateTime.Now + " C Started");
            Thread.Sleep(4000);
            Console.WriteLine(DateTime.Now + " C Finished");
            return true;
        }

        internal class MyTask
        {
            public MyTask[] Dependencies { get; set; }

            public Func<bool> Perform { get; set; }
        }
    }
}
