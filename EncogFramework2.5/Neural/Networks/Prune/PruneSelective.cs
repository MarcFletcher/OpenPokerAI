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
using Encog.MathUtil.Matrices;
#if logging
using log4net;
#endif
using Encog.MathUtil.RBF;
using Encog.MathUtil;
using Encog.MathUtil.Randomize;
using Encog.Engine.Network.RBF;

namespace Encog.Neural.Networks.Prune
{
    /// <summary>
    /// Prune a neural network selectivly. This class allows you to either add or
    /// remove neurons from layers of a neural network.
    /// </summary>
    public class PruneSelective
    {
        /// <summary>
        /// The network to prune.
        /// </summary>
        private BasicNetwork network;

        
        /// <summary>
        /// Construct an object prune the neural network. 
        /// </summary>
        /// <param name="network">The network to prune.</param>
        public PruneSelective(BasicNetwork network)
        {
            this.network = network;
        }

        /// <summary>
        /// Change the neuron count for the network. If the count is increased then a
        /// zero-weighted neuron is added, which will not affect the output of the
        /// neural network. If the neuron count is decreased, then the weakest neuron
        /// will be removed.
        /// </summary>
        /// <param name="layer">The layer to adjust.</param>
        /// <param name="neuronCount">The new neuron count for this layer.</param>
        public void ChangeNeuronCount(ILayer layer, int neuronCount)
        {

            if (neuronCount == 0)
                throw new NeuralNetworkError("Can't decrease to zero neurons.");

            // is there anything to do?
            if (neuronCount == layer.NeuronCount)
            {
                return;
            }

            if (neuronCount > layer.NeuronCount)
            {
                IncreaseNeuronCount(layer, neuronCount);
            }
            else
            {
                DecreaseNeuronCount(layer, neuronCount);
            }
        }

        /// <summary>
        /// Internal function to decrease the neuron count of a layer. 
        /// </summary>
        /// <param name="layer">The layer to affect.</param>
        /// <param name="neuronCount">The new neuron count.</param>
        private void DecreaseNeuronCount(ILayer layer, int neuronCount)
        {
            // create an array to hold the least significant neurons, which will be
            // removed

            int lostNeuronCount = layer.NeuronCount - neuronCount;
            int[] lostNeuron = FindWeakestNeurons(layer, lostNeuronCount);

            // finally, actually prune the neurons that the previous steps
            // determined to remove
            for (int i = 0; i < lostNeuronCount; i++)
            {
                Prune(layer, lostNeuron[i] - i);
            }
        }

        /// <summary>
        /// Determine the significance of the neuron. The higher the return value,
        /// the more significant the neuron is. 
        /// </summary>
        /// <param name="layer">The layer to query.</param>
        /// <param name="neuron">The neuron to query.</param>
        /// <returns>How significant is this neuron.</returns>
        public double DetermineNeuronSignificance(ILayer layer,
                int neuron)
        {
            // calculate the bias significance
            double result = 0;

            if (layer.HasBias)
            {
                result += layer.BiasWeights[neuron];
            }

            // calculate the outbound significance
            foreach (ISynapse synapse in layer.Next)
            {
                if (synapse.WeightMatrix != null)
                {
                    for (int i = 0; i < synapse.ToNeuronCount; i++)
                    {
                        result += synapse.WeightMatrix[neuron, i];
                    }
                }
            }

            // calculate the bias significance
            ICollection<ISynapse> inboundSynapses = this.network.Structure
                    .GetPreviousSynapses(layer);

            foreach (ISynapse synapse in inboundSynapses)
            {
                if (synapse.WeightMatrix != null)
                {
                    for (int i = 0; i < synapse.FromNeuronCount; i++)
                    {
                        result += synapse.WeightMatrix[i, neuron];
                    }
                }
            }

            return Math.Abs(result);
        }

        /// <summary>
        /// Find the weakest neurons.
        /// </summary>
        /// <param name="layer">The layer to check.</param>
        /// <param name="count">The number of neurons to find.</param>
        /// <returns>The neurons found.</returns>
        private int[] FindWeakestNeurons(ILayer layer, int count)
        {
            // create an array to hold the least significant neurons, which will be
            // returned
            double[] lostNeuronSignificance = new double[count];
            int[] lostNeuron = new int[count];

            // init the potential lost neurons to the first ones, we will find
            // better choices if we can
            for (int i = 0; i < count; i++)
            {
                lostNeuron[i] = i;
                lostNeuronSignificance[i] = DetermineNeuronSignificance(layer, i);
            }

            // now loop over the remaining neurons and see if any are better ones to
            // remove
            for (int i = count; i < layer.NeuronCount; i++)
            {
                double significance = DetermineNeuronSignificance(layer, i);

                // is this neuron less significant than one already chosen?
                for (int j = 0; j < count; j++)
                {
                    if (lostNeuronSignificance[j] > significance)
                    {
                        lostNeuron[j] = i;
                        lostNeuronSignificance[j] = significance;
                        break;
                    }
                }
            }

            return lostNeuron;
        }

        /// <summary>
        /// The network that is being processed.
        /// </summary>
        public BasicNetwork Network
        {
            get
            {
                return this.network;
            }
        }


        /// <summary>
        /// Internal function to increase the neuron count. This will add a
        /// zero-weight neuron to this layer. 
        /// </summary>
        /// <param name="layer">The layer to increase.</param>
        /// <param name="neuronCount">The new neuron count.</param>
        private void IncreaseNeuronCount(ILayer layer, int neuronCount)
        {
            // adjust the bias
            double[] newBias = new double[neuronCount];
            if (layer.HasBias)
            {
                for (int i = 0; i < layer.NeuronCount; i++)
                {
                    newBias[i] = layer.BiasWeights[i];
                }

                layer.BiasWeights = newBias;
            }

            // adjust the outbound weight matrixes
            foreach (ISynapse synapse in layer.Next)
            {
                if (synapse.WeightMatrix != null)
                {
                    Matrix newMatrix = new Matrix(neuronCount, synapse
                            .ToNeuronCount);
                    // copy existing matrix to new matrix
                    for (int row = 0; row < layer.NeuronCount; row++)
                    {
                        for (int col = 0; col < synapse.ToNeuronCount; col++)
                        {
                            newMatrix[row, col] = synapse.WeightMatrix[row, col];
                        }
                    }
                    synapse.WeightMatrix = newMatrix;
                }
            }

            // adjust the inbound weight matrixes
            ICollection<ISynapse> inboundSynapses = this.network.Structure
                    .GetPreviousSynapses(layer);

            foreach (ISynapse synapse in inboundSynapses)
            {
                if (synapse.WeightMatrix != null)
                {
                    Matrix newMatrix = new Matrix(synapse.FromNeuronCount,
                            neuronCount);
                    // copy existing matrix to new matrix
                    for (int row = 0; row < synapse.FromNeuronCount; row++)
                    {
                        for (int col = 0; col < synapse.ToNeuronCount; col++)
                        {
                            newMatrix[row, col] = synapse.WeightMatrix[row, col];
                        }
                    }
                    synapse.WeightMatrix = newMatrix;
                }
            }

            // adjust the bias
            if (layer.HasBias)
            {
                double[] newBias2 = new double[neuronCount];

                for (int i = 0; i < layer.NeuronCount; i++)
                {
                    newBias2[i] = layer.BiasWeights[i];
                }
                layer.BiasWeights = newBias2;
            }

            // adjust RBF
            if (layer is RadialBasisFunctionLayer)
            {
                RadialBasisFunctionLayer rbf = (RadialBasisFunctionLayer)layer;
                IRadialBasisFunction[] newRBF = new IRadialBasisFunction[neuronCount];
                for (int i = 0; i < rbf.RadialBasisFunction.Length; i++)
                {
                    newRBF[i] = rbf.RadialBasisFunction[i];
                }

                for (int i = rbf.RadialBasisFunction.Length; i < neuronCount; i++)
                {
                    newRBF[i] = new GaussianFunction(ThreadSafeRandom.NextDouble() - 0.5,
                         ThreadSafeRandom.NextDouble(), ThreadSafeRandom.NextDouble() - 0.5);
                }

                rbf.RadialBasisFunction = newRBF;

            }

            // finally, up the neuron count
            layer.NeuronCount = neuronCount;
        }

       
        /// <summary>
        /// Prune one of the neurons from this layer. Remove all entries in this
        /// weight matrix and other layers. 
        /// </summary>
        /// <param name="targetLayer">The neuron to prune. Zero specifies the first neuron.</param>
        /// <param name="neuron">The neuron to prune.</param>
        public void Prune(ILayer targetLayer, int neuron)
        {
            // delete a row on this matrix
            foreach (ISynapse synapse in targetLayer.Next)
            {
                if (synapse.WeightMatrix != null)
                {
                    synapse.WeightMatrix =
                        MatrixMath.DeleteRow(synapse.WeightMatrix, neuron);
                }
            }

            // delete a column on the previous
            ICollection<ILayer> previous = this.network.Structure
                    .GetPreviousLayers(targetLayer);

            foreach (ILayer prevLayer in previous)
            {
                if (previous != null)
                {
                    foreach (ISynapse synapse in prevLayer.Next)
                    {
                        if (synapse.WeightMatrix != null)
                        {
                            synapse.WeightMatrix =
                                MatrixMath.DeleteCol(synapse.WeightMatrix,
                                    neuron);
                        }
                    }
                }
            }

            // remove the bias
            if (targetLayer.HasBias)
            {
                double[] newBias = new double[targetLayer
                        .NeuronCount - 1];

                int targetIndex = 0;
                for (int i = 0; i < targetLayer.NeuronCount; i++)
                {
                    if (i != neuron)
                    {
                        newBias[targetIndex++] = targetLayer.BiasWeights[i];
                    }
                }

                targetLayer.BiasWeights = newBias;
            }

            // adjust RBF
            if (targetLayer is RadialBasisFunctionLayer)
            {
                RadialBasisFunctionLayer rbf = (RadialBasisFunctionLayer)targetLayer;
                IRadialBasisFunction[] newRBF = new GaussianFunction[targetLayer
                        .NeuronCount - 1];

                int targetIndex = 0;
                for (int i = 0; i < targetLayer.NeuronCount; i++)
                {
                    if (i != neuron)
                    {
                        newRBF[targetIndex++] = rbf.RadialBasisFunction[i];
                    }
                }
                rbf.RadialBasisFunction = newRBF;

            }

            // update the neuron count
            targetLayer.NeuronCount -= 1;

        }

        /// <summary>
        /// Stimulate the specified neuron by the specified percent. This is used to
        /// randomize the weights and bias values for weak neurons. 
        /// </summary>
        /// <param name="percent">The percent to randomize by.</param>
        /// <param name="layer">The layer that the neuron is on.</param>
        /// <param name="neuron">The neuron to randomize.</param>
        public void StimulateNeuron(double percent, ILayer layer,
                int neuron)
        {
            Distort d = new Distort(percent);

            if (layer.HasBias)
            {
                layer.BiasWeights[neuron] = d.Randomize(layer.BiasWeights[neuron]);
            }

            // calculate the outbound significance
            foreach (ISynapse synapse in layer.Next)
            {
                for (int i = 0; i < synapse.ToNeuronCount; i++)
                {
                    double v = synapse.WeightMatrix[neuron, i];
                    synapse.WeightMatrix[neuron, i] = d.Randomize(v);
                }
            }

            ICollection<ISynapse> inboundSynapses = this.network.Structure
                    .GetPreviousSynapses(layer);

            foreach (ISynapse synapse in inboundSynapses)
            {
                for (int i = 0; i < synapse.FromNeuronCount; i++)
                {
                    double v = synapse.WeightMatrix[i, neuron];
                    synapse.WeightMatrix[i, neuron] = d.Randomize(v);
                }
            }
        }

        
        /// <summary>
        /// Stimulate weaker neurons on a layer. Find the weakest neurons and then
        /// randomize them by the specified percent. 
        /// </summary>
        /// <param name="layer">The layer to stimulate.</param>
        /// <param name="count">The number of weak neurons to stimulate.</param>
        /// <param name="percent">The percent to stimulate by.</param>
        public void StimulateWeakNeurons(ILayer layer, int count,
                double percent)
        {
            int[] weak = FindWeakestNeurons(layer, count);
            foreach (int element in weak)
            {
                StimulateNeuron(percent, layer, element);
            }
        }
    }
}
