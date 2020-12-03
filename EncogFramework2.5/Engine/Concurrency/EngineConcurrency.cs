// Encog(tm) Artificial Intelligence Framework v2.5
// .Net Version
// http://www.heatonresearch.com/encog/
// http://code.google.com/p/encog-java/
// 
// Copyright 2008-2010 by Heaton Research Inc.
// 
// Released under the LGPL.
//
// This is free software; you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation; either version 2.1 of
// the License, or (at your option) any later version.
//
// This software is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this software; if not, write to the Free
// Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
// 02110-1301 USA, or see the FSF site: http://www.fsf.org.
// 
// Encog and Heaton Research are Trademarks of Heaton Research, Inc.
// For information on Heaton Research trademarks, visit:
// 
// http://www.heatonresearch.com/copyright.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#if logging
using log4net;
#endif

namespace Encog.Engine.Concurrency.Job
{
    /// <summary>
    /// This class abstracts thread pools, and potentially grids and other types of
    /// concurrency. It is used by other classes inside of Encog to allow tasks to be
    /// executed efficiently on multicore machines.
    /// </summary>
    public class EngineConcurrency
    {

        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static EngineConcurrency instance;

        /// <summary>
        /// The number of active tasks.
        /// </summary>
        private int activeTasks;

        private int currentTaskGroup;

        /// <summary>
        /// The instance to the singleton.
        /// </summary>
        public static EngineConcurrency Instance
        {
            get
            {
                if (EngineConcurrency.instance == null)
                {
                    EngineConcurrency.instance = new EngineConcurrency();
                }
                return EngineConcurrency.instance;
            }
        }

#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(EngineConcurrency));
#endif

        /// <summary>
        /// Construct a concurrency object.
        /// </summary>
        public EngineConcurrency()
        {
            SetMaxThreadsToCoreCount();
            this.currentTaskGroup = 0;
        }

        /// <summary>
        /// Process the specified task.  It will be processed either now,
        /// or queued to process on the thread pool.  No group is assigned.
        /// </summary>
        /// <param name="task">The task to process.</param>
        public void ProcessTask(IEngineTask task)
        {
            ProcessTask(task, null);
        }

        /// <summary>
        /// Process the specified task.  It will be processed either now,
        /// or queued to process on the thread pool.
        /// </summary>
        /// <param name="task">The task to process.</param>
        /// <param name="group">The group this task belongs to.</param>
        public void ProcessTask(IEngineTask task, TaskGroup  group)
        {
            lock (this)
            {
                this.activeTasks++;
            }
            if( group!=null )
                group.TaskStarting();
            PoolItem item = new PoolItem(this, task, group);
            ThreadPool.QueueUserWorkItem(item.ThreadPoolCallback);
        }

        /// <summary>
        /// How many threads should be used?
        /// </summary>
        public int MaxThreads
        {
            get
            {
                int t1, t2;
                ThreadPool.GetMaxThreads(out t1, out t2);
                return t1;
            }
            set
            {
                //ThreadPool.SetMaxThreads(value, value);
            }
        }

        /// <summary>
        /// Set the max threads to the number of processors.
        /// </summary>
        public void SetMaxThreadsToCoreCount()
        {
#if !SILVERLIGHT
            this.MaxThreads = System.Environment.ProcessorCount;
#endif
        }
/*
        /// <summary>
        /// Wait for all threads in the pool to complete.
        /// </summary>
        /// <param name="timeOutSeconds">How long to wait for all threads to complete.</param>
        public void Shutdown(long timeOutSeconds)
        {
            long start = DateTime.Now.Ticks;
            long current;
            long elapsed;

            do
            {
                lock (this)
                {
                    if (this.activeTasks < 1)
                        return;
                }
                current = DateTime.Now.Ticks;
                elapsed = current - start;
                elapsed /= 10000; // to miliseconds
                elapsed /= 1000; // to seconds
                Thread.Sleep(100);
            } while (elapsed < timeOutSeconds);
        }
        */
        /// <summary>
        /// Create a new task group.
        /// </summary>
        /// <returns>The new task group.</returns>
        public TaskGroup CreateTaskGroup()
        {
            TaskGroup result = null;
            lock (this)
            {
                this.currentTaskGroup++;
                result = new TaskGroup(this.currentTaskGroup);
                
            }
            return result;
        }

        internal void TaskFinished(PoolItem poolItem)
        {
            lock (this)
            {
                this.activeTasks--;
            }
        }
    }

}
