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
using Encog.Neural.Data;
using Encog.Util;
using Encog.Neural.Data.Basic;
using Encog.Neural.NeuralData;
using Encog.Neural.Networks.Synapse;
using Encog.Engine.Network.Activation;
using Encog.Engine.Util;

namespace Encog.Neural.Networks.Training.LMA
{
    /// <summary>
    /// Compute the Jaccobian using the chain rule.
    /// </summary>
    public class JacobianChainRule : IComputeJacobian
    {
        /// <summary>
        /// The network that is to be trained.
        /// </summary>
        private BasicNetwork network;

        /// <summary>
        /// The training set to use. Must be indexable.
        /// </summary>
        private IIndexable indexableTraining;

        /// <summary>
        /// The number of training set elements.
        /// </summary>
        private int inputLength;

        /// <summary>
        /// The number of weights and bias values in the neural network.
        /// </summary>
        private int parameterSize;

        /// <summary>
        /// The Jacobian matrix that was calculated.
        /// </summary>
        private double[][] jacobian;

        /// <summary>
        /// The current row in the Jacobian matrix.
        /// </summary>
        private int jacobianRow;

        /// <summary>
        /// The current column in the Jacobian matrix.
        /// </summary>
        private int jacobianCol;

        /// <summary>
        /// Used to read the training data.
        /// </summary>
        private INeuralDataPair pair;

        /// <summary>
        /// The errors for each row in the Jacobian.
        /// </summary>
        private double[] rowErrors;


        /// <summary>
        /// Construct the chain rule calculation.
        /// </summary>
        /// <param name="network">The network to use.</param>
        /// <param name="indexableTraining">The training set to use.</param>
        public JacobianChainRule(BasicNetwork network,
                 IIndexable indexableTraining)
        {
            this.indexableTraining = indexableTraining;
            this.network = network;
            this.parameterSize = network.Structure.CalculateSize();
            this.inputLength = (int)this.indexableTraining.Count;
            this.jacobian = EngineArray.AllocateDouble2D(this.inputLength, this.parameterSize);
            this.rowErrors = new double[this.inputLength];

            BasicNeuralData input = new BasicNeuralData(
                   this.indexableTraining.InputSize);
            BasicNeuralData ideal = new BasicNeuralData(
                   this.indexableTraining.IdealSize);
            this.pair = new BasicNeuralDataPair(input, ideal);
        }

        /// <summary>
        /// Calculate the derivative.
        /// </summary>
        /// <param name="a">The activation function.</param>
        /// <param name="d">The value to calculate for.</param>
        /// <returns>The derivative.</returns>
        private double CalcDerivative(IActivationFunction a, double d)
        {
            return a.DerivativeFunction(d);
        }

        /// <summary>
        /// Calculate the derivative.
        /// </summary>
        /// <param name="a">The activation function.</param>
        /// <param name="d">The value to calculate for.</param>
        /// <returns>The derivative.</returns>
        private double CalcDerivative2(IActivationFunction a, double d)
        {
            double[] temp = new double[1];
            temp[0] = d;
            a.ActivationFunction(temp, 0, 1);
            a.ActivationFunction(temp, 0, 1);
            return temp[0];
        }

        /// <summary>
        ///  Calculate the Jacobian matrix.
        /// </summary>
        /// <param name="weights">The weights for the neural network.</param>
        /// <returns>The sum squared of the weights.</returns>
        public double Calculate(double[] weights)
        {
            double result = 0.0;

            for (int i = 0; i < this.inputLength; i++)
            {
                this.jacobianRow = i;
                this.jacobianCol = 0;

                this.indexableTraining.GetRecord(i, this.pair);

                double e = CalculateDerivatives(this.pair);
                this.rowErrors[i] = e;
                result += e * e;

            }

            return result / 2.0;
        }

        /// <summary>
        /// Calculate the derivatives for this training set element.
        /// </summary>
        /// <param name="pair">The training set element.</param>
        /// <returns>The sum squared of errors.</returns>
        private double CalculateDerivatives(INeuralDataPair pair)
        {
            // error values
            double e = 0.0;
            double sum = 0.0;

            IActivationFunction function = this.network.GetLayer(
                    BasicNetwork.TAG_INPUT).ActivationFunction;

            NeuralOutputHolder holder = new NeuralOutputHolder();

            this.network.Compute(pair.Input, holder);

            IList<ISynapse> synapses = this.network.Structure.Synapses;

            int synapseNumber = 0;

            ISynapse synapse = synapses[synapseNumber++];

            double output = holder.Output[0];
            e = pair.Ideal[0] - output;

            this.jacobian[this.jacobianRow][this.jacobianCol++] = CalcDerivative(
                    function, output);

            for (int i = 0; i < synapse.FromNeuronCount; i++)
            {
                double lastOutput = holder.Result[synapse][i];

                this.jacobian[this.jacobianRow][this.jacobianCol++] = CalcDerivative(
                        function, output)
                        * lastOutput;
            }

            ISynapse lastSynapse;

            while (synapseNumber < synapses.Count)
            {
                lastSynapse = synapse;
                synapse = synapses[synapseNumber++];
                INeuralData outputData = holder.Result[lastSynapse];

                int biasCol = this.jacobianCol;
                this.jacobianCol += synapse.ToLayer.NeuronCount;

                // for each neuron in the input layer
                for (int neuron = 0; neuron < synapse.ToNeuronCount; neuron++)
                {
                    output = outputData[neuron];

                    // for each weight of the input neuron
                    for (int i = 0; i < synapse.FromNeuronCount; i++)
                    {
                        sum = 0.0;
                        // for each neuron in the next layer
                        for (int j = 0; j < lastSynapse.ToNeuronCount; j++)
                        {
                            // for each weight of the next neuron
                            for (int k = 0; k < lastSynapse.FromNeuronCount; k++)
                            {
                                double x = lastSynapse.WeightMatrix[k, j];
                                double y = output;
                                sum += lastSynapse.WeightMatrix[k, j]
                                        * output;
                            }
                            sum += lastSynapse.ToLayer.BiasWeights[j];
                        }

                        double x1 = CalcDerivative(function, output);
                        double x2 = CalcDerivative2(function, sum);
                        double x3 = holder.Result[synapse][i];

                        double w = lastSynapse.WeightMatrix[neuron, 0];
                        double val = CalcDerivative(function, output)
                                * CalcDerivative2(function, sum) * w;

                        double z1 = val
                        * holder.Result[synapse][i];
                        double z2 = val;

                        this.jacobian[this.jacobianRow][this.jacobianCol++] = val
                                * holder.Result[synapse][i];
                        this.jacobian[this.jacobianRow][biasCol + neuron] = val;
                    }
                }
            }

            // return error
            return e;
        }


        /// <summary>
        /// The Jacobian matrix.
        /// </summary>
        public double[][] Jacobian
        {
            get
            {
                return this.jacobian;
            }
        }

        /// <summary>
        /// The errors for each row of the Jacobian.
        /// </summary>
        public double[] RowErrors
        {
            get
            {
                return this.rowErrors;
            }
        }
    }
}
