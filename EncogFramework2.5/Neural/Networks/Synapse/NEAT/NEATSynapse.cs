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
using Encog.Persist.Attributes;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Data;
using Encog.Persist;
using Encog.MathUtil.Matrices;
using Encog.Persist.Persistors.Generic;
using Encog.Neural.Data.Basic;
using Encog.Engine.Network.Activation;

namespace Encog.Neural.Networks.Synapse.NEAT
{

    /// <summary>
    /// Implements a NEAT network as a synapse between two layers. In Encog, a NEAT
    /// network is created by using a NEATSynapse between an input and output layer.
    /// 
    /// NEAT networks only have an input and an output layer. There are no actual
    /// hidden layers. Rather this synapse will evolve many hidden neurons that have
    /// connections that are not easily defined by layers. Connections can be
    /// feedforward, recurrent, or self-connected.
    /// 
    /// NEAT networks relieve the programmer of the need to define the hidden layer
    /// structure of the neural network.
    /// 
    /// The output from the neural network can be calculated normally or using a snapshot.
    /// The snapshot mode is slower, but it can be more accurate.  The snapshot handles 
    /// recurrent layers better, as it takes the time to loop through the network multiple
    /// times to "flush out" the recurrent links.
    /// 
    /// NeuroEvolution of Augmenting Topologies (NEAT) is a genetic algorithm for the
    /// generation of evolving artificial neural networks. It was developed by Ken
    /// Stanley while at The University of Texas at Austin.
    /// 
    /// http://www.cs.ucf.edu/~kstanley/
    /// </summary>
    public class NEATSynapse : BasicPersistedSubObject, ISynapse, IContextClearable
    {
        /// <summary>
        /// The activation function to use with the NEAT neurons.
        /// </summary>
        private IActivationFunction activationFunction;

        /// <summary>
        /// The from layer.
        /// </summary>
        [EGIgnore]
        private ILayer fromLayer;

        /// <summary>
        /// The depth of the network.
        /// </summary>
        [EGIgnore]
        private int networkDepth;

        /// <summary>
        /// The neurons that make up this network.
        /// </summary>
        private List<NEATNeuron> neurons = new List<NEATNeuron>();

        /// <summary>
        /// Should snapshot be used to calculate the output of the neural network.
        /// </summary>
        [EGIgnore]
        private bool snapshot = false;

        /// <summary>
        /// The target layer.
        /// </summary>
        [EGIgnore]
        private ILayer toLayer;

        /// <summary>
        /// Construct a NEAT synapse. 
        /// </summary>
        /// <param name="fromLayer">The input layer.</param>
        /// <param name="toLayer">The output layer.</param>
        /// <param name="neurons">The neurons in this synapse.</param>
        /// <param name="activationFunction">The activation function to use.</param>
        /// <param name="networkDepth">The depth of the network.</param>
        public NEATSynapse(ILayer fromLayer, ILayer toLayer,
                 IList<NEATNeuron> neurons,
                 IActivationFunction activationFunction, int networkDepth)
        {
            this.fromLayer = fromLayer;
            this.toLayer = toLayer;
            this.neurons.AddRange(neurons);
            this.networkDepth = networkDepth;
            this.activationFunction = activationFunction;
        }

        /// <summary>
        /// Create a NEAT synapse.
        /// </summary>
        /// <param name="fromLayer">The from layer.</param>
        /// <param name="toLayer">The to layer.</param>
        public NEATSynapse(ILayer fromLayer, ILayer toLayer)
        {
            this.fromLayer = fromLayer;
            this.toLayer = toLayer;
            this.networkDepth = 0;
            this.activationFunction = new ActivationSigmoid();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NEATSynapse()
        {
        }

        /// <summary>
        /// A clone of this object.
        /// </summary>
        /// <returns>A clone of this object.</returns>
        public override object Clone()
        {
            return null;
        }


        /// <summary>
        /// Compute the output from this synapse. 
        /// </summary>
        /// <param name="input">The input to this synapse.</param>
        /// <returns>The output from this synapse.</returns>
        public INeuralData Compute(INeuralData input)
        {
            INeuralData result = new BasicNeuralData(ToNeuronCount);

            if (this.neurons.Count == 0)
            {
                throw new NeuralNetworkError("This network has not been evolved yet, it has no neurons in the NEAT synapse.");
            }

            int flushCount = 1;

            if (snapshot)
            {
                flushCount = networkDepth;
            }

            // iterate through the network FlushCount times
            for (int i = 0; i < flushCount; ++i)
            {
                int outputIndex = 0;
                int index = 0;

                result.Clear();

                // populate the input neurons
                while (neurons[index].NeuronType == NEATNeuronType.Input)
                {
                    neurons[index].Output = input[index];

                    index++;
                }

                // set the bias neuron
                neurons[index++].Output = 1;

                while (index < neurons.Count)
                {

                    NEATNeuron currentNeuron = neurons[index];

                    double sum = 0;

                    foreach (NEATLink link in currentNeuron.InboundLinks)
                    {
                        double weight = link.Weight;
                        double neuronOutput = link.FromNeuron.Output;
                        sum += weight * neuronOutput;
                    }

                    double[] d = new double[1];
                    d[0] = sum / currentNeuron.ActivationResponse;
                    activationFunction.ActivationFunction(d,0,1);

                    neurons[index].Output = d[0];

                    if (currentNeuron.NeuronType == NEATNeuronType.Output)
                    {
                        result.Data[outputIndex++] = currentNeuron.Output;
                    }
                    index++;
                }
            }

            return result;
        }

        /// <summary>
        /// A persistor to load/save this type of synapse.
        /// </summary>
        /// <returns></returns>
        public override IPersistor CreatePersistor()
        {
            return new GenericPersistor(typeof(NEATSynapse));
        }


        /// <summary>
        /// The from layer.
        /// </summary>
        public ILayer FromLayer
        {
            get
            {
                return fromLayer;
            }
            set
            {
                this.fromLayer = value;
            }
        }

        /// <summary>
        /// The neuron count from the "from layer".
        /// </summary>
        public int FromNeuronCount
        {
            get
            {
                return fromLayer.NeuronCount;
            }
        }

        /// <summary>
        /// The weight and threshold matrix.
        /// </summary>
        public Matrix WeightMatrix
        {
            get
            {
                return null;
            }
            set
            {
                throw new NeuralNetworkError(
                    "Neat synapse cannot have a simple matrix.");
 
            }
        }

        
        /// <summary>
        /// Get the size of the matrix, or zero if one is not defined.
        /// </summary>
        public int MatrixSize
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// The network depth.
        /// </summary>
        /// <returns>The network depth.</returns>
        public int NetworkDepth
        {
            get
            {
                return networkDepth;
            }
        }

        /// <summary>
        /// The NEAT neurons.
        /// </summary>
        /// <returns>The NEAT neurons.</returns>
        public IList<NEATNeuron> getNeurons()
        {
            return neurons;
        }

        /// <summary>
        /// The "to layer".
        /// </summary>
        public ILayer ToLayer
        {
            get
            {
                return toLayer;
            }
            set
            {
                this.toLayer = value;
            }
        }

        /// <summary>
        /// The neuron count from the "to layer".
        /// </summary>
        public int ToNeuronCount
        {
            get
            {
                return toLayer.NeuronCount;
            }
        }

        /// <summary>
        /// The type of synapse that this is.
        /// </summary>
        public SynapseType SynapseType
        {
            get
            {
                return SynapseType.NEAT;
            }
        }

        /// <summary>
        /// True if this is a self-connected synapse. That is, the from and
        /// to layers are the same.
        /// </summary>
        public bool IsSelfConnected
        {
            get
            {
                return false;
            }
        }


        /// <summary>
        /// True if the weights for this synapse can be modified.
        /// </summary>
        public bool IsTeachable
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// The activation function to be used by the NEAT neurons.
        /// </summary>
        public IActivationFunction ActivationFunction
        {
            get
            {
                return this.activationFunction;
            }
            set
            {
                this.activationFunction = value;
            }
        }


        /// <summary>
        /// Should snapshot processing be used?  Default is no.
        /// </summary>
        public bool Snapshot
        {
            get
            {
                return this.snapshot;
            }
            set
            {
                this.snapshot = value;
            }
        }
        

        /// <summary>
        /// Clear any context.
        /// </summary>
        public void ClearContext()
        {
            foreach( NEATNeuron neurons in this.neurons )
            {
                neurons.Output = 0;
            }
        }
    }
}
