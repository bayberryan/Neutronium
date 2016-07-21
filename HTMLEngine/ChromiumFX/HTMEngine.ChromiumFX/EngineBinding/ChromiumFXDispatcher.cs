﻿using System;
using System.Threading.Tasks;
using Chromium.Remote;
using MVVM.HTML.Core.JavascriptEngine.Window;
using System.Collections.Generic;

namespace HTMEngine.ChromiumFX.EngineBinding 
{
    public class ChromiumFXDispatcher : IDispatcher 
    {
        private readonly CfrV8Context _Context;
        private readonly CfrBrowser _Browser;
        private readonly object _Locker = new object();

        private CfrTaskRunner TaskRunner { get; set; }

        public ChromiumFXDispatcher(CfrBrowser browser, CfrV8Context context) 
        {
            _Browser = browser;
            _Context = context;
            TaskRunner = _Context.TaskRunner;
        }

        public Task RunAsync(Action act) 
        {
            var taskCompletionSource = new TaskCompletionSource<int>();
            var action = ToTaskAction(act, taskCompletionSource);
            RunInContext(action);
            return taskCompletionSource.Task;
        }

        public void Run(Action act) 
        {
            RunAsync(act).Wait();
        }

        public Task<T> EvaluateAsync<T>(Func<T> compute) 
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            var action = ToTaskAction(compute, taskCompletionSource);
            RunInContext(action);
            return taskCompletionSource.Task;
        }

        public T Evaluate<T>(Func<T> compute) 
        {
            return EvaluateAsync(compute).Result;
        }

        public bool IsInContext() 
        {
            return TaskRunner.BelongsToCurrentThread();
        }

        private Action ToTaskAction(Action perform, TaskCompletionSource<int> taskCompletionSource) 
        {
            return ToTaskAction(() => { perform(); return 0; }, taskCompletionSource);
        }

        private Action ToTaskAction<T>(Func<T> perform, TaskCompletionSource<T> taskCompletionSource) 
        {
            Action result = () => 
            {
                using (GetContext()) 
                {
                    try 
                    {
                        taskCompletionSource.TrySetResult(perform());
                    }
                    catch (Exception exception) 
                    {
                        taskCompletionSource.TrySetException(exception);
                    }
                }
            };
            return result;
        }

        private IDisposable GetContext() 
        {
            return new ChromiumFXContext(_Context);
        }

        private class ChromiumFXContext : IDisposable 
        {
            private readonly CfrV8Context _Context;
            public ChromiumFXContext(CfrV8Context context) 
            {
                _Context = context;
                _Context.Enter();
            }
            public void Dispose() 
            {
                _Context.Exit();
            }
        }

        private IDisposable GetRemoteContext() 
        {
            return new ChromiumFXCRemoteContext(_Browser);
        }

        private HashSet<ChromiumFXTask> _Tasks = new HashSet<ChromiumFXTask>();

        private void RunInContext(Action action) 
        {
            using (var ctx = GetRemoteContext()) 
            {
                if (TaskRunner.BelongsToCurrentThread()) {
                    action();
                    return;
                }

                var task = AddTask(action);
                task.Clean = () => RemoveTask(task);

                TaskRunner.PostTask(task.Task);
            }
        }

        private ChromiumFXTask AddTask(Action action)
        {
            lock(_Locker)
            {
                var task = new ChromiumFXTask(action);
                _Tasks.Add(task);
                return task;
            }         
        }

        private void RemoveTask(ChromiumFXTask task)
        {
            lock (_Locker)
                _Tasks.Remove(task);
        }
    }
}
