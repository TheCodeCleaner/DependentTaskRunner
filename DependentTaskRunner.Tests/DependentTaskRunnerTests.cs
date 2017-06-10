using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependentTaskRunner.Tests
{
    [TestClass]
    public class DependentTaskRunnerTests
    {
        [TestMethod]
        public void ThreeDependentTasksShouldExecuteInOrder()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string>()},
                {"b", new List<string> {"c"}},
                {"c", new List<string> {"a"}}
            };

            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);

            Assert.AreEqual(orderedTasks[0], "a");
            Assert.AreEqual(orderedTasks[1], "c");
            Assert.AreEqual(orderedTasks[2], "b");
        }

        [TestMethod]
        public void NoTasksNoExecution()
        {
            var tasks = new List<string>();
            var taskToDependency = new Dictionary<string, List<string>>();

            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);

            Assert.IsFalse(orderedTasks.Any());
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void CircularDependencyShouldThrowException()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string>()},
                {"b", new List<string> {"c"}},
                {"c", new List<string> {"b"}}
            };

            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void NoTaskWithNoDependencyShouldThrowException()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> {"b"}},
                {"b", new List<string> {"c"}},
                {"c", new List<string> {"b"}}
            };


            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void TaskDependentOnItselfShouldThrowException()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> ()},
                {"b", new List<string> {"c", "b"}},
                {"c", new List<string> {"b"}}
            };


            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void NotTrivialCircularDependencyShouldThrowException()
        {
            var tasks = new List<string> { "a", "b", "c", "d", "e" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> {"e"}},
                {"b", new List<string> ()},
                {"c", new List<string> {"a"}},
                {"d", new List<string> {"a", "b"}},
                {"e", new List<string> {"b", "c"}}
            };

            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void TaskDependentOnNotExistingTaskShouldThrowException()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> ()},
                {"b", new List<string> {"d"}},
                {"c", new List<string> {"a"}},
                {"d", new List<string> {"a"}}
            };


            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);
        }

        [TestMethod]
        public void NotTrivialExecutionShouldExecuteInRightOrder()
        {
            var tasks = new List<string> { "a", "b", "c", "d", "e" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> ()},
                {"b", new List<string> ()},
                {"c", new List<string> {"a", "b"}},
                {"d", new List<string> {"a"}},
                {"e", new List<string> {"b", "d"}}
            };

            var orderedTasks = new List<string>();
            var taskRunner = CreateBasicTaskRunner(taskToDependency, orderedTasks);

            taskRunner.PerformTasks(tasks);

            var indexA = 0;
            var indexB = 0;
            var indexC = 0;
            var indexD = 0;
            var indexE = 0;
            for (var i = 0; i < orderedTasks.Count; i++)
            {
                if (orderedTasks[i] == "a") indexA = i;
                if (orderedTasks[i] == "b") indexB = i;
                if (orderedTasks[i] == "c") indexC = i;
                if (orderedTasks[i] == "d") indexD = i;
                if (orderedTasks[i] == "e") indexE = i;
            }

            Assert.IsTrue(indexC > indexA && indexC > indexB);
            Assert.IsTrue(indexD > indexA);
            Assert.IsTrue(indexE > indexB && indexE > indexD);
        }

        [TestMethod]
        public void TwoIndependentTasksShouldWorkInParallel()
        {
            var tasks = new List<string> { "a", "b" };
            var barrier = new Barrier(3);

            var tasker = new TaskRunner<string>(task => new List<string>(), task =>
            {
                return Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    return true;
                });
            });

            Task.Run(() => tasker.PerformTasks(tasks));

            Assert.IsTrue(barrier.SignalAndWait(1000));
        }

        [TestMethod]
        public void TaskShouldWorkDiretlyAfterItsDependenciesAreSatisfied()
        {
            var tasks = new List<string> { "a", "b", "c" };
            var taskToDependency = new Dictionary<string, List<string>>
            {
                {"a", new List<string> ()},
                {"b", new List<string> ()},
                {"c", new List<string> {"a"}}
            };

            var barrier = new Barrier(3);

            var tasker = new TaskRunner<string>(task => taskToDependency[task], task =>
            {
                return Task.Run(() =>
                {
                    if (task == "a")
                        return true;

                    barrier.SignalAndWait();
                    return true;
                });
            });

            Task.Run(() => tasker.PerformTasks(tasks));

            Assert.IsTrue(barrier.SignalAndWait(1000));
        }

        [TestMethod]
        public void TasksShouldNotContinueExecutionIfThereIsFailedTask()
        {
            var tasks = new List<string> { "a", "b" };
            var taskToDependency =
                new Dictionary<string, List<string>> { { "a", new List<string>() }, { "b", new List<string> { "a" } } };

            var orderedTasks = new List<string>();
            var tasker = new TaskRunner<string>(task => taskToDependency[task], task =>
            {
                return Task.Run(() =>
                {
                    orderedTasks.Add(task);
                    return task != "a";
                });
            });

            tasker.PerformTasks(tasks);

            Assert.AreEqual(orderedTasks[0], "a");
            Assert.AreEqual(orderedTasks.Count, 1);
        }

        private static TaskRunner<string> CreateBasicTaskRunner(IReadOnlyDictionary<string, List<string>> taskToDependency, ICollection<string> orderedTasks)
        {
            return new TaskRunner<string>(task => taskToDependency[task], task =>
            {
                return Task.Run(() =>
                {
                    orderedTasks.Add(task);
                    return true;
                });

            });
        }
    }
}
