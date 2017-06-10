using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DependentTaskRunner
{
    public class TaskRunner<T> where T : class
    {
        private readonly Func<T, IEnumerable<T>> _getDependenciesFunction;
        private readonly Func<T, Task<bool>> _performTaskFunction;

        public TaskRunner(Func<T, IEnumerable<T>> getDependenciesFunction, Func<T, Task<bool>> performTaskFunction)
        {
            _getDependenciesFunction = getDependenciesFunction;
            _performTaskFunction = performTaskFunction;
        }

        public void PerformTasks(IEnumerable<T> tasks)
        {
            var tasksState = new TasksState<T>
            {
                DoneTasks = new HashSet<T>(),
                TasksToDo = new List<T>(tasks),
                TasksInProgress = new HashSet<T>()
            };

            PerformTasks(tasksState);
        }

        private void PerformTasks(TasksState<T> tasksState)
        {
            T task;
            lock (tasksState)
            {
                if (!tasksState.TasksToDo.Any() && !tasksState.TasksInProgress.Any())
                    return;

                task = FindTaskToDo(tasksState);
            }

            if (task == null)
            {
                if (tasksState.TasksInProgress.Any())
                    return;

                throw new Exception("There are some tasks with dependencies that can't be satisfied.");
            }

            MoveTaskToInProgress(tasksState, task);
            Task.Run(() => PerformTasks(tasksState));

            PerformTaskAndTheTasksDependentOnIt(tasksState, task);
        }

        private void PerformTaskAndTheTasksDependentOnIt(TasksState<T> tasksState, T task)
        {
            _performTaskFunction(task).ContinueWith(taskResult =>
            {
                if (!taskResult.Result)
                    return;

                MoveTaskToDone(tasksState, task);
                PerformTasks(tasksState);
            }).Wait();
        }

        private T FindTaskToDo(TasksState<T> tasksState)
        {
            foreach (var task in tasksState.TasksToDo)
            {
                var dependencies = _getDependenciesFunction(task);
                if (dependencies == null || dependencies.All(d => tasksState.DoneTasks.Contains(d)))
                    return task;
            }

            return null;
        }

        private static void MoveTaskToInProgress(TasksState<T> tasksState, T task)
        {
            lock (tasksState)
            {
                tasksState.TasksToDo.Remove(task);
                tasksState.TasksInProgress.Add(task);
            }
        }

        private static void MoveTaskToDone(TasksState<T> tasksState, T task)
        {
            lock (tasksState)
            {
                tasksState.TasksInProgress.Remove(task);
                tasksState.DoneTasks.Add(task);
            }
        }
    }
}
