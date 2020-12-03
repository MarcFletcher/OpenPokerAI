/*
 * Encog(tm) Core v2.5 
 * http://www.heatonresearch.com/encog/
 * http://code.google.com/p/encog-java/
 * 
 * Copyright 2008-2010 by Heaton Research Inc.
 * 
 * Released under the LGPL.
 *
 * This is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
 * 02110-1301 USA, or see the FSF site: http://www.fsf.org.
 * 
 * Encog and Heaton Research are Trademarks of Heaton Research, Inc.
 * For information on Heaton Research trademarks, visit:
 * 
 * http://www.heatonresearch.com/copyright.html
 */

namespace Encog.Engine.Data
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Training data is stored in two ways, depending on if the data is for
    /// supervised, or unsupervised training.
    /// For supervised training just an input value is provided, and the ideal output
    /// values are null.
    /// For unsupervised training both input and the expected ideal outputs are
    /// provided.
    /// This interface abstracts classes that provide a holder for both of these two
    /// data items.
    /// </summary>
    ///
    public interface IEngineData
    {

        /// <summary>
        /// Set the ideal data, the desired output.
        /// </summary>
        double[] IdealArray
        {
            get;
            set;
        }


        /// <summary>
        /// Set the input.
        /// </summary>
        double[] InputArray
        {
            get;
            set;
        }

        /// <summary>
        /// True if this training pair is supervised. That is, it has bothinput and ideal data.
        /// </summary>
        bool Supervised
        {
            get;
        }
    }
}
