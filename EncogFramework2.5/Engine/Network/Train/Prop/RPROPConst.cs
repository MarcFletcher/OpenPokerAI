/*
 * Encog(tm) Core v2.5 - Java Version
 * http://www.heatonresearch.com/encog/
 * http://code.google.com/p/encog-java/
 
 * Copyright 2008-2010 Heaton Research, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *   
 * For more information on Heaton Research copyrights, licenses 
 * and trademarks visit:
 * http://www.heatonresearch.com/copyright
 */

namespace Encog.Engine.Network.Train.Prop
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Constants used for Resilient Propagation (RPROP) training.
    /// </summary>
    ///
    public sealed class RPROPConst
    {

        /// <summary>
        /// Private constructor.
        /// </summary>
        ///
        private RPROPConst()
        {

        }

        /// <summary>
        /// The default zero tolerance.
        /// </summary>
        ///
        public const double DEFAULT_ZERO_TOLERANCE = 0.00000000000000001d;

        /// <summary>
        /// The POSITIVE ETA value. This is specified by the resilient propagation
        /// algorithm. This is the percentage by which the deltas are increased by if
        /// the partial derivative is greater than zero.
        /// </summary>
        ///
        public const double POSITIVE_ETA = 1.2d;

        /// <summary>
        /// The NEGATIVE ETA value. This is specified by the resilient propagation
        /// algorithm. This is the percentage by which the deltas are increased by if
        /// the partial derivative is less than zero.
        /// </summary>
        ///
        public const double NEGATIVE_ETA = 0.5d;

        /// <summary>
        /// The minimum delta value for a weight matrix value.
        /// </summary>
        ///
        public const double DELTA_MIN = 1e-6d;

        /// <summary>
        /// The starting update for a delta.
        /// </summary>
        ///
        public const double DEFAULT_INITIAL_UPDATE = 0.1d;

        /// <summary>
        /// The maximum amount a delta can reach.
        /// </summary>
        ///
        public const double DEFAULT_MAX_STEP = 50;

    }
}
