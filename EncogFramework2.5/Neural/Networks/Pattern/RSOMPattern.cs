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
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Synapse;
using Encog.Engine.Network.Activation;
#if logging
using log4net;
#endif
namespace Encog.Neural.Networks.Pattern
{

    /// <summary>
    /// A recurrent self organizing map is a self organizing map that has
    /// a recurrent context connection on the hidden layer.  This type
    /// of neural network is adept at classifying temporal data.
    /// </summary>
    public class RSOMPattern : INeuralNetworkPattern
    {

        /// <summary>
        /// The number of input neurons.
        /// </summary>
        private int inputNeurons;

        /// <summary>
        /// The number of output neurons.
        /// </summary>
        private int outputNeurons;

#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(RSOMPattern));
#endif

        /// <summary>
        /// Add a hidden layer.  SOM networks do not have hidden layers, so
        /// this will throw an error.
        /// </summary>
        /// <param name="count">The number of hidden neurons.</param>
        public void AddHiddenLayer(int count)
        {
            String str = "A SOM network does not have hidden layers.";
#if logging  
            if (this.logger.IsErrorEnabled)
            {
                this.logger.Error(str);
            }
#endif
            throw new PatternError(str);

        }

        /// <summary>
        /// Generate the RSOM network.
        /// </summary>
        /// <returns>The neural network.</returns>
        public BasicNetwork Generate()
        {
            ILayer output = new BasicLayer(new ActivationLinear(), false,
                    this.outputNeurons);
            ILayer input = new BasicLayer(new ActivationLinear(), false,
                    this.inputNeurons);

            BasicNetwork network = new BasicNetwork();
            ILayer context = new ContextLayer(this.outputNeurons);
            network.AddLayer(input);
            network.AddLayer(output);

            output.AddNext(context, SynapseType.OneToOne);
            context.AddNext(input);

            int y = PatternConst.START_Y;
            input.X = PatternConst.START_X;
            input.Y = y;

            context.X = PatternConst.INDENT_X;
            context.Y = y;

            y += PatternConst.INC_Y;

            output.X = PatternConst.START_X;
            output.Y = y;

            network.Structure.FinalizeStructure();
            network.Reset();
            return network;
        }

        /// <summary>
        /// Set the activation function.  A SOM uses a linear activation
        /// function, so this method throws an error.
        /// </summary>
        public IActivationFunction ActivationFunction
        {
            set
            {
                String str = "A SOM network can't define an activation function.";
#if logging
                if (this.logger.IsErrorEnabled)
                {
                    this.logger.Error(str);
                }
#endif
                throw new PatternError(str);
            }
            get
            {
                return null;
            }

        }

        /// <summary>
        /// Clear out any hidden neurons.
        /// </summary>
        public void Clear()
        {
        }

        /// <summary>
        /// Set the number of output neurons.
        /// </summary>
        public int OutputNeurons
        {
            get
            {
                return this.outputNeurons;
            }
            set
            {
                this.outputNeurons = value;
            }

        }

        /// <summary>
        /// The number of input neurons.
        /// </summary>
        public int InputNeurons
        {
            get
            {
                return this.inputNeurons;
            }
            set
            {
                this.inputNeurons = value;
            }

        }
    }

}
