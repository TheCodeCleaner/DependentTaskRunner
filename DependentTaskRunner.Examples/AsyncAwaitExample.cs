using System;
using System.Threading.Tasks;

namespace DependentTaskRunner.Examples
{
    public class AsyncAwaitExample
    {
        public static void Run()
        {
            var taskRunner = new TaskRunner<MyTask>(t => t.Dependencies, async t => await t.Perform());

            var taskA = new MyTask { Perform = RunA };
            var taskB = new MyTask { Perform = RunB };
            var taskC = new MyTask { Dependencies = new[] { taskA, taskB }, Perform = RunC };

            taskRunner.PerformTasks(new[] { taskA, taskB, taskC });
        }

        private static async Task<bool> RunA()
        {
            Console.WriteLine(DateTime.Now + " A Started");
            await Task.Delay(2000);
            Console.WriteLine(DateTime.Now + " A Finished");
            return true;
        }

        private static async Task<bool> RunB()
        {
            Console.WriteLine(DateTime.Now + " B Started");
            await Task.Delay(3000);
            Console.WriteLine(DateTime.Now + " B Finished");
            return true;
        }

        private static async Task<bool> RunC()
        {
            Console.WriteLine(DateTime.Now + " C Started");
            await Task.Delay(4000);
            Console.WriteLine(DateTime.Now + " C Finished");
            return true;
        }

        internal class MyTask
        {
            public MyTask[] Dependencies { get; set; }

            public Func<Task<bool>> Perform { get; set; }
        }
    }
}
