namespace Encog.Engine.Network.Flat
{

    using Encog.Engine;
    using Encog.Engine.Data;
    using Encog.Engine.Network.Activation;
    using Encog.Engine.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Implements a flat (vector based) neural network in the Encog Engine. This is
    /// meant to be a very highly efficient feedforward, or simple recurrent, neural
    /// network. It uses a minimum of objects and is designed with one principal in
    /// mind-- SPEED. Readability, c reuse, object oriented programming are all
    /// secondary in consideration.
    /// Vector based neural networks are also very good for GPU processing. The flat
    /// network classes will make use of the GPU if you have enabled GPU processing.
    /// See the Encog class for more info.
    /// </summary>
    ///
    public class FlatNetwork : IEngineNeuralNetwork
    {

        /// <summary>
        /// The default bias activation.
        /// </summary>
        ///
        public const double DEFAULT_BIAS_ACTIVATION = 1.0d;

        /// <summary>
        /// The value that indicates that there is no bias activation.
        /// </summary>
        ///
        public const double NO_BIAS_ACTIVATION = 0.0d;

        /// <summary>
        /// The number of input neurons in this network.
        /// </summary>
        ///
        private int inputCount;

        /// <summary>
        /// The number of neurons in each of the layers.
        /// </summary>
        ///
        private int[] layerCounts;

        /// <summary>
        /// The number of context neurons in each layer. These context neurons will
        /// feed the next layer.
        /// </summary>
        ///
        private int[] layerContextCount;

        /// <summary>
        /// The number of neurons in each layer that are actually fed by neurons in
        /// the previous layer. Bias neurons, as well as context neurons, are not fed
        /// from the previous layer.
        /// </summary>
        ///
        private int[] layerFeedCounts;

        /// <summary>
        /// An index to where each layer begins (based on the number of neurons in
        /// each layer).
        /// </summary>
        ///
        private int[] layerIndex;

        /// <summary>
        /// The outputs from each of the neurons.
        /// </summary>
        ///
        private double[] layerOutput;

        /// <summary>
        /// The number of output neurons in this network.
        /// </summary>
        ///
        private int outputCount;

        /// <summary>
        /// The index to where the weights that are stored at for a given layer.
        /// </summary>
        ///
        private int[] weightIndex;

        /// <summary>
        /// The weights for a neural network.
        /// </summary>
        ///
        private double[] weights;

        /// <summary>
        /// The activation types.
        /// </summary>
        ///
        private IActivationFunction[] activationFunctions;

        /// <summary>
        /// The context target for each layer. This is how the backwards connections
        /// are formed for the recurrent neural network. Each layer either has a
        /// zero, which means no context target, or a layer number that indicates the
        /// target layer.
        /// </summary>
        ///
        private int[] contextTargetOffset;

        /// <summary>
        /// The size of each of the context targets. If a layer's contextTargetOffset
        /// is zero, its contextTargetSize should also be zero. The contextTargetSize
        /// should always match the feed count of the targeted context layer.
        /// </summary>
        ///
        private int[] contextTargetSize;

        /// <summary>
        /// The bias activation for each layer. This is usually either 1, for a bias,
        /// or zero for no bias.
        /// </summary>
        ///
        private double[] biasActivation;

        /// <summary>
        /// The layer that training should begin on.
        /// </summary>
        ///
        private int beginTraining;

        /// <summary>
        /// The layer that training should end on.
        /// </summary>
        ///
        private int endTraining;

        /// <summary>
        /// Does this network have some connections disabled.
        /// </summary>
        ///
        private bool isLimited;

        /// <summary>
        /// The limit, under which, all a cconnection is not considered to exist.
        /// </summary>
        ///
        private double connectionLimit;

        /// <summary>
        /// Default constructor.
        /// </summary>
        ///
        public FlatNetwork()
        {

        }

        /// <summary>
        /// Create a flat network from an array of layers.
        /// </summary>
        ///
        /// <param name="layers">The layers.</param>
        public FlatNetwork(FlatLayer[] layers)
        {
            Init(layers);
        }

        /// <summary>
        /// Construct a flat neural network.
        /// </summary>
        ///
        /// <param name="input">Neurons in the input layer.</param>
        /// <param name="hidden1">Neurons in the first hidden layer. Zero for no first hiddenlayer.</param>
        /// <param name="hidden2">Neurons in the second hidden layer. Zero for no second hiddenlayer.</param>
        /// <param name="output">Neurons in the output layer.</param>
        /// <param name="tanh">True if this is a tanh activation, false for sigmoid.</param>
        public FlatNetwork(int input, int hidden1, int hidden2,
                int output, bool tanh)
        {
            double[] paras = new double[1];
            FlatLayer[] layers;
            IActivationFunction act = (tanh) ? (IActivationFunction)(new ActivationTANH())
                    : (IActivationFunction)(new ActivationSigmoid());
            paras[0] = 1; // slope

            if ((hidden1 == 0) && (hidden2 == 0))
            {
                layers = new FlatLayer[2];
                layers[0] = new FlatLayer(act, input,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[1] = new FlatLayer(act, output,
                        FlatNetwork.NO_BIAS_ACTIVATION, paras);
            }
            else if ((hidden1 == 0) || (hidden2 == 0))
            {
                int count = Math.Max(hidden1, hidden2);
                layers = new FlatLayer[3];
                layers[0] = new FlatLayer(act, input,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[1] = new FlatLayer(act, count,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[2] = new FlatLayer(act, output,
                        FlatNetwork.NO_BIAS_ACTIVATION, paras);
            }
            else
            {
                layers = new FlatLayer[4];
                layers[0] = new FlatLayer(act, input,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[1] = new FlatLayer(act, hidden1,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[2] = new FlatLayer(act, hidden2,
                        FlatNetwork.DEFAULT_BIAS_ACTIVATION, paras);
                layers[3] = new FlatLayer(act, output,
                        FlatNetwork.NO_BIAS_ACTIVATION, paras);
            }

            this.isLimited = false;
            this.connectionLimit = 0.0d;

            Init(layers);
        }

        /// <summary>
        /// Calculate the error for this neural network. The error is calculated
        /// using root-mean-square(RMS).
        /// </summary>
        ///
        /// <param name="data">The training set.</param>
        /// <returns>The error percentage.</returns>
        public double CalculateError(IEngineIndexableSet data)
        {
            ErrorCalculation errorCalculation = new ErrorCalculation();

            double[] actual = new double[this.outputCount];
            IEngineData pair = BasicEngineData.CreatePair(data.InputSize,
                    data.IdealSize);

            for (int i = 0; i < data.Count; i++)
            {
                data.GetRecord(i, pair);
                Compute(pair.InputArray, actual);
                errorCalculation.UpdateError(actual, pair.IdealArray);
            }
            return errorCalculation.Calculate();
        }

        /// <summary>
        /// Clear any context neurons.
        /// </summary>
        ///
        public void ClearContext()
        {
            int index = 0;

            for (int i = 0; i < this.layerIndex.Length; i++)
            {

                bool hasBias = (this.layerContextCount[i] + this.layerFeedCounts[i]) != this.layerCounts[i];

                // fill in regular neurons
                for (int j = 0; j < this.layerFeedCounts[i]; j++)
                {
                    this.layerOutput[index++] = 0;
                }

                // fill in the bias
                if (hasBias)
                {
                    this.layerOutput[index++] = this.biasActivation[i];
                }

                // fill in context
                for (int j = 0; j < this.layerContextCount[i]; j++)
                {
                    this.layerOutput[index++] = 0;
                }
            }
        }

        /// <summary>
        /// Clone the network.
        /// </summary>
        ///
        /// <returns>A clone of the network.</returns>
        public virtual Object Clone()
        {
            FlatNetwork result = new FlatNetwork();
            CloneFlatNetwork(result);
            return result;
        }

        /// <summary>
        /// Clone a flat network.
        /// </summary>
        /// <param name="result">The cloned flat network.</param>
        public void CloneFlatNetwork(FlatNetwork result)
        {
            result.inputCount = this.inputCount;
            result.layerCounts = EngineArray.ArrayCopy(this.layerCounts);
            result.layerIndex = EngineArray.ArrayCopy(this.layerIndex);
            result.layerOutput = EngineArray.ArrayCopy(this.layerOutput);
            result.layerFeedCounts = EngineArray.ArrayCopy(this.layerFeedCounts);
            result.contextTargetOffset = EngineArray
                    .ArrayCopy(this.contextTargetOffset);
            result.contextTargetSize = EngineArray
                    .ArrayCopy(this.contextTargetSize);
            result.layerContextCount = EngineArray
                    .ArrayCopy(this.layerContextCount);
            result.biasActivation = EngineArray.ArrayCopy(this.biasActivation);
            result.outputCount = this.outputCount;
            result.weightIndex = this.weightIndex;
            result.weights = this.weights;

            result.activationFunctions = new IActivationFunction[this.activationFunctions.Length];
            for (int i = 0; i < result.activationFunctions.Length; i++)
            {
                result.activationFunctions[i] = (IActivationFunction)this.activationFunctions[i].Clone();
            }

            result.beginTraining = this.beginTraining;
            result.endTraining = this.endTraining;
        }

        /// <summary>
        /// Calculate the output for the given input.
        /// </summary>
        ///
        /// <param name="input">The input.</param>
        /// <param name="output">Output will be placed here.</param>
        public virtual void Compute(double[] input, double[] output)
        {
            int sourceIndex = this.layerOutput.Length
                    - this.layerCounts[this.layerCounts.Length - 1];

            EngineArray.ArrayCopy(input, 0, this.layerOutput, sourceIndex,
                    this.inputCount);

            for (int i = this.layerIndex.Length - 1; i > 0; i--)
            {
                ComputeLayer(i);
            }

            EngineArray.ArrayCopy(this.layerOutput, 0, output, 0, this.outputCount);
        }

        /// <summary>
        /// Calculate a layer.
        /// </summary>
        ///
        /// <param name="currentLayer">The layer to calculate.</param>
        protected internal void ComputeLayer(int currentLayer)
        {

            int inputIndex = this.layerIndex[currentLayer];
            int outputIndex = this.layerIndex[currentLayer - 1];
            int inputSize = this.layerCounts[currentLayer];
            int outputSize = this.layerFeedCounts[currentLayer - 1];

            int index = this.weightIndex[currentLayer - 1];

            int limitX = outputIndex + outputSize;
            int limitY = inputIndex + inputSize;

            // weight values
            for (int x = outputIndex; x < limitX; x++)
            {
                double sum = 0;
                for (int y = inputIndex; y < limitY; y++)
                {
                    sum += this.weights[index++] * this.layerOutput[y];
                }
                this.layerOutput[x] = sum;
            }

            this.activationFunctions[currentLayer - 1].ActivationFunction(
                    this.layerOutput, outputIndex, outputSize);

            // update context values
            int offset = this.contextTargetOffset[currentLayer];

            for (int x_0 = 0; x_0 < this.contextTargetSize[currentLayer]; x_0++)
            {
                this.layerOutput[offset + x_0] = this.layerOutput[outputIndex + x_0];
            }
        }

        /// <summary>
        /// Dec the specified data into the weights of the neural network. This
        /// method performs the opposite of encNetwork.
        /// </summary>
        ///
        /// <param name="data">The data to be decd.</param>
        public virtual void DecodeNetwork(double[] data)
        {
            if (data.Length != this.weights.Length)
            {
                throw new EncogEngineError(
                        "Incompatable weight sizes, can't assign length="
                                + data.Length + " to length=" + data.Length);
            }
            this.weights = data;

        }

        /// <summary>
        /// Enc the neural network to an array of doubles. This includes the
        /// network weights. To read this into a neural network, use the
        /// decNetwork method.
        /// </summary>
        ///
        /// <returns>The encd network.</returns>
        public virtual double[] EncodeNetwork()
        {
            return this.weights;
        }


        /// <summary>
        /// The offset of the context target for each layer.
        /// </summary>
        public int[] ContextTargetOffset
        {
            get
            {
                return this.contextTargetOffset;
            }
        }



        /// <summary>
        /// The context target size for each layer. Zero if the layer doesnot feed a context layer.
        /// </summary>
        public int[] ContextTargetSize
        {
            get
            {
                return this.contextTargetSize;
            }
        }



        /// <summary>
        /// The length of the array the network would enc to.
        /// </summary>
        public virtual int EncodeLength
        {
            get
            {
                return this.weights.Length;
            }
        }

        /// <summary>
        /// The number of input neurons.
        /// </summary>
        public virtual int InputCount
        {
            get
            {
                return this.inputCount;
            }
        }



        /// <summary>
        /// The number of neurons in each layer.
        /// </summary>
        public int[] LayerCounts
        {
            get
            {
                return this.layerCounts;
            }
        }



        /// <summary>
        /// The number of neurons in each layer that are fed by the previouslayer.
        /// </summary>
        public int[] LayerFeedCounts
        {
            get
            {
                return this.layerFeedCounts;
            }
        }



        /// <summary>
        /// Indexes into the weights for the start of each layer.
        /// </summary>
        public int[] LayerIndex
        {
            get
            {
                return this.layerIndex;
            }
        }



        /// <summary>
        /// The output for each layer.
        /// </summary>
        public double[] LayerOutput
        {
            get
            {
                return this.layerOutput;
            }
        }

        /// <summary>
        /// The neuron count.
        /// </summary>
        public int NeuronCount
        {
            get
            {
                int result = 0;
                /* foreach */
                foreach (int element in this.layerCounts)
                {
                    result += element;
                }
                return result;
            }
        }



        /// <summary>
        /// The number of output neurons.
        /// </summary>
        public virtual int OutputCount
        {
            get
            {
                return this.outputCount;
            }
        }

        /// <summary>
        /// The index of each layer in the weight and threshold array.
        /// </summary>
        public int[] WeightIndex
        {
            get
            {
                return this.weightIndex;
            }
        }



        /// <summary>
        /// The index of each layer in the weight and threshold array.
        /// </summary>
        public double[] Weights
        {
            get
            {
                return this.weights;
            }
        }


        /// <summary>
        /// Neural networks with only one type of activation function offer certain
        /// optimization options. This method determines if only a single activation
        /// function is used.
        /// </summary>
        ///
        /// <returns>The number of the single activation function, or -1 if there areno activation functions or more than one type of activationfunction.</returns>
        public Type HasSameActivationFunction()
        {
            IList<Type> map = new List<Type>();

            /* foreach */
            foreach (IActivationFunction activation in this.activationFunctions)
            {
                if (!map.Contains(activation.GetType()))
                {
                    map.Add(activation.GetType());
                }
            }

            if (map.Count != 1)
            {
                return null;
            }
            else
            {
                return map[0];
            }
        }

        /// <summary>
        /// Construct a flat network.
        /// </summary>
        ///
        /// <param name="layers">The layers of the network to create.</param>
        public void Init(FlatLayer[] layers)
        {
            int layerCount = layers.Length;

            this.inputCount = layers[0].Count;
            this.outputCount = layers[layerCount - 1].Count;

            this.layerCounts = new int[layerCount];
            this.layerContextCount = new int[layerCount];
            this.weightIndex = new int[layerCount];
            this.layerIndex = new int[layerCount];
            this.activationFunctions = new IActivationFunction[layerCount];
            this.layerFeedCounts = new int[layerCount];
            this.contextTargetOffset = new int[layerCount];
            this.contextTargetSize = new int[layerCount];
            this.biasActivation = new double[layerCount];

            int index = 0;
            int neuronCount = 0;
            int weightCount = 0;

            for (int i = layers.Length - 1; i >= 0; i--)
            {

                FlatLayer layer = layers[i];
                FlatLayer nextLayer = null;

                if (i > 0)
                {
                    nextLayer = layers[i - 1];
                }

                this.biasActivation[index] = layer.BiasActivation;
                this.layerCounts[index] = layer.TotalCount;
                this.layerFeedCounts[index] = layer.Count;
                this.layerContextCount[index] = layer.ContectCount;
                this.activationFunctions[index] = layer.Activation;

                neuronCount += layer.TotalCount;

                if (nextLayer != null)
                {
                    weightCount += layer.Count * nextLayer.TotalCount;
                }

                if (index == 0)
                {
                    this.weightIndex[index] = 0;
                    this.layerIndex[index] = 0;
                }
                else
                {
                    this.weightIndex[index] = this.weightIndex[index - 1]
                            + (this.layerCounts[index] * this.layerFeedCounts[index - 1]);
                    this.layerIndex[index] = this.layerIndex[index - 1]
                            + this.layerCounts[index - 1];
                }

                int neuronIndex = 0;
                for (int j = layers.Length - 1; j >= 0; j--)
                {
                    if (layers[j].ContextFedBy == layer)
                    {
                        this.contextTargetSize[i] = layers[j].ContectCount;
                        this.contextTargetOffset[i] = neuronIndex
                                + layers[j].TotalCount
                                - layers[j].ContectCount;
                    }
                    neuronIndex += layers[j].TotalCount;
                }

                index++;
            }

            this.beginTraining = 0;
            this.endTraining = this.layerCounts.Length - 1;

            this.weights = new double[weightCount];
            this.layerOutput = new double[neuronCount];

            ClearContext();
        }

        /// <summary>
        /// Perform a simple randomization of the weights of the neural network
        /// between -1 and 1.
        /// </summary>
        ///
        public void Randomize()
        {
            Randomize(1, -1);
        }

        /// <summary>
        /// Perform a simple randomization of the weights of the neural network
        /// between the specified hi and lo.
        /// </summary>
        ///
        /// <param name="hi">The network high.</param>
        /// <param name="lo">The network low.</param>
        public void Randomize(double hi, double lo)
        {
            for (int i = 0; i < this.weights.Length; i++)
            {
                this.weights[i] = ((new Random()).Next() * (hi - lo)) + lo;
            }
        }


        /// <summary>
        /// the beginTraining to set
        /// </summary>
        public int BeginTraining
        {
            get
            {
                return beginTraining;
            }
            set
            {
                this.beginTraining = value;
            }
        }



        /// <summary>
        /// Where to end training.
        /// </summary>
        public int EndTraining
        {
            get
            {
                return endTraining;
            }
            set
            {
                this.endTraining = value;
            }
        }



        /// <summary>
        /// The connection limit.  Connections below this do not exist.
        /// </summary>
        public double ConnectionLimit
        {
            get
            {
                return connectionLimit;
            }
            set
            {
                this.connectionLimit = value;
                if (this.connectionLimit > EncogEngine.DEFAULT_ZERO_TOLERANCE)
                    this.isLimited = true;
            }
        }

        /// <summary>
        /// True, if this is a connection limited network.
        /// </summary>
        public bool Limited
        {
            get
            {
                return isLimited;
            }
        }


        /// <summary>
        /// Clear the connection limit.
        /// </summary>
        public void ClearConnectionLimit()
        {
            this.connectionLimit = 0.0d;
            this.isLimited = false;
        }


        /// <summary>
        /// The activation functions.
        /// </summary>
        public IActivationFunction[] ActivationFunctions
        {
            get
            {
                return activationFunctions;
            }
        }


    }
}
