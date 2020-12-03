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
using Encog.Engine.Network.Activation;

namespace Encog.Neural.Networks.Pattern
{
    /// <summary>
    /// Patterns are used to create common sorts of neural networks.
    /// Information about the structure of the neural network is 
    /// communicated to the pattern, and then generate is called to
    /// produce a neural network of this type.
    /// </summary>
    public interface INeuralNetworkPattern
    {
        /// <summary>
        /// Add the specified hidden layer.
        /// </summary>
        /// <param name="count">The number of neurons in the hidden layer.</param>
        void AddHiddenLayer(int count);

        /// <summary>
        /// Generate the specified neural network.
        /// </summary>
        /// <returns>The resulting neural network.</returns>
        BasicNetwork Generate();

        /// <summary>
        /// Set the activation function to be used for all created layers
        /// that allow an activation function to be specified.  Not all
        /// patterns allow the activation function to be specified.
        /// </summary>
        IActivationFunction ActivationFunction
        {
            get;
            set;
        }

        /// <summary>
        /// Set the number of input neurons.
        /// </summary>
        int InputNeurons
        {
            get;
            set;
        }

        /// <summary>
        /// Set the number of output neurons.
        /// </summary>
        int OutputNeurons
        {
            get;
            set;
        }

        /// <summary>
        /// Clear the hidden layers so that they can be redefined.
        /// </summary>
        void Clear();
    }
}
