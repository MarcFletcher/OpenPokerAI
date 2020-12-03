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
#if logging
using log4net;
#endif
namespace Encog.Neural.Networks.Training.Strategy
{
    /// <summary>
    /// The reset strategy will reset the weights if the neural network fails to fall
    /// below a specified error by a specified number of cycles. This can be useful
    /// to throw out initially "bad/hard" random initializations of the weight
    /// matrix.
    /// </summary>
    public class ResetStrategy : IStrategy
    {
#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(ResetStrategy));
#endif
        /// <summary>
        /// The required minimum error.
        /// </summary>
        private double required;

        /// <summary>
        /// The number of cycles to reach the required minimum error.
        /// </summary>
        private int cycles;

        /// <summary>
        /// The training algorithm that is using this strategy.
        /// </summary>
        private ITrain train;

        /// <summary>
        /// How many bad cycles have there been so far.
        /// </summary>
        private int badCycleCount;

        /// <summary>
        /// Construct a reset strategy.  The error rate must fall
        /// below the required rate in the specified number of cycles,
        /// or the neural network will be reset to random weights and
        /// thresholds.
        /// </summary>
        /// <param name="required">The required error rate.</param>
        /// <param name="cycles">The number of cycles to reach that rate.</param>
        public ResetStrategy(double required, int cycles)
        {
            this.required = required;
            this.cycles = cycles;
            this.badCycleCount = 0;
        }

        /// <summary>
        /// Initialize this strategy.
        /// </summary>
        /// <param name="train">The training algorithm.</param>
        public void Init(ITrain train)
        {
            this.train = train;
        }

        /// <summary>
        /// Called just after a training iteration.
        /// </summary>
        public void PostIteration()
        {

        }

        /// <summary>
        /// Called just before a training iteration.
        /// </summary>
        public void PreIteration()
        {
            if (this.train.Error > this.required)
            {
                this.badCycleCount++;
                if (this.badCycleCount > this.cycles)
                {
#if logging
                    if (this.logger.IsDebugEnabled)
                    {
                        this.logger.Debug("Failed to imrove network, resetting.");
                    }
#endif
                    this.train.Network.Reset();
                    this.badCycleCount = 0;
                }
            }
            else
            {
                this.badCycleCount = 0;
            }
        }
    }

}
