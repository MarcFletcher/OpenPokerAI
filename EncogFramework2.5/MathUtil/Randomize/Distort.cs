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
using Encog.MathUtil;

#if logging
using log4net;
#endif

namespace Encog.MathUtil.Randomize
{
    /// <summary>
    /// A randomizer that distorts what is already present in the neural network.
    /// </summary>
    public class Distort : BasicRandomizer
    {

        /// <summary>
        /// The factor to use to distort the numbers.
        /// </summary>
        private double factor;
        private double percentageToDistort;

        #if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(Distort));
        #endif
        
        /// <summary>
        /// Construct a distort randomizer for the specified factor.
        /// </summary>
        /// <param name="perturbAmount">The randomizer factor.</param>
        /// <param name="percentageToDistort"></param>
        public Distort(double perturbAmount, double percentageToDistort = 1.0)
        {
            if (percentageToDistort < 0 || percentageToDistort > 1)
                throw new Exception("percentageToDistort must be between 0 and 1.");

            this.factor = perturbAmount;
            this.percentageToDistort = percentageToDistort;
        }

        /// <summary>
        /// Distort the random number by the factor that was specified 
        /// in the constructor.
        /// </summary>
        /// <param name="d">The number to distort.</param>
        /// <returns>The result.</returns>
        public override double Randomize(double d)
        {
            if (ThreadSafeRandom.NextDouble() < percentageToDistort)
                //If we choose to randomize this neuron
                return d + (this.factor - (ThreadSafeRandom.NextDouble() * this.factor * 2));
            else
                return d;
        }

    }

}
