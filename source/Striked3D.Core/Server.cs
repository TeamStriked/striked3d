using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Striked3D.Core
{
    public enum ServerType
    {
        SyncService,
        SyncRenderService,
        AsyncService,
        None
    }
    public enum ServerState
    {
        INIT,
        RUNNING,
        IDLE,
        CLOSING,
        CLOSED
    }
    public abstract class Server
    {
        private int threadId = 0;
        private System.Threading.Thread serviceThread;
        private bool serviceIsRunning = false;

        public delegate void StatusUpdateHandler(ServerState status);
        public event StatusUpdateHandler OnUpdateStatus;

        public Queue<EngineTask> tasks = new Queue<EngineTask>();

        protected abstract void Loop(double delta);
        protected abstract void Register();
        protected abstract void Deregister();
        public abstract int Priority { get; }
        public virtual int ThreadTime { 
            get
            {
                return 10;
            } 
        }
        public abstract ServerType RunType { get; }

        protected AutoResetEvent syncEvent = new AutoResetEvent(false);

        public AutoResetEvent FinishEvent = new AutoResetEvent(false);

        protected ServerThreadRunner commandClass;
        protected  Striked3D.Core.Window serverWindow { get; set; }
        public double DeltaTime { get; set; }

        public void Sync(double delta)
        {
            DeltaTime = delta;
            syncEvent.Set();
        }

        public Server(ServerThreadRunner _commandClass)
        {
            commandClass = _commandClass;
        }

        public void AddTask(EngineTask task)
        {
            tasks.Enqueue(task);
        }
        public bool IsServerRunning
        {
            get
            {
                return serviceIsRunning;
            }
        }

        public void Stop()
        {
            serviceIsRunning = false;
        }

        public void Initialize(Striked3D.Core.Window _window)
        {
            this.serverWindow = _window;
            if (RunType != ServerType.None)
            {
                Logger.Debug(this, "Starting Thread..");

                this.serviceIsRunning = true;
                serviceThread = new System.Threading.Thread(RunThread);
                serviceThread.Start();

                threadId = serviceThread.ManagedThreadId;
            }
            else
            {
                this.Register();
            }
        }

        private void RunThread()
        {
            Logger.Debug(this, "Thread is on initalizing..");
            UpdateServiceState(ServerState.INIT);
            this.Register();

            while (IsServerRunning)
            {
                UpdateServiceState(ServerState.RUNNING);

                if (RunType != ServerType.AsyncService)
                {
                    syncEvent.WaitOne();
                }

                this.ExecuteTasks();

                try
                {
                    this.Loop(DeltaTime);
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex.Message.ToString(), ex.StackTrace);
                }

                UpdateServiceState(ServerState.IDLE);
                if (RunType == ServerType.AsyncService)
                {
                    Thread.Sleep(ThreadTime);
                }
                else
                {
                  this.FinishEvent.Set();
                }
            }

            Logger.Debug(this, "Thread is on closing..");

            UpdateServiceState(ServerState.CLOSING);
            this.Deregister();
            UpdateServiceState(ServerState.CLOSED);
        }

        private void ExecuteTasks()
        {
            while(this.tasks.Count > 0)
            {
                var task = this.tasks.Dequeue();
                Type ccType = commandClass.GetType();

                try
                {
                    MethodInfo theMethod = ccType.GetMethod(task.command.method);
                    var returnObj = theMethod.Invoke(commandClass, task.command.arguments);

                    var res = new EngineCommandResult();
                    res.success = true;
                    res.result = returnObj;

              
                    serverWindow.AddCompleteTask(new EngineCompleteTask
                    {
                        completedTask = task,
                        result = res
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(this, "Cant execute: " + task.command.method + " on " + ccType.FullName.ToString(), ex.StackTrace);
                }
            }
        }

        private void UpdateServiceState(ServerState state)
        {
            if (OnUpdateStatus != null)
            {
                OnUpdateStatus(state);
            }
        }
    }
}
