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
using Encog.Solve.Genetic.Innovation;
using Encog.Solve.Genetic.Population;
using Encog.Neural.Networks.Synapse.NEAT;
using Encog.Solve.Genetic.Genome;
using Encog.Solve.Genetic.Genes;
using Encog.Persist.Attributes;

namespace Encog.Neural.Networks.Training.NEAT
{
    /// <summary>
    /// Implements a NEAT innovation list.
    /// 
    /// NeuroEvolution of Augmenting Topologies (NEAT) is a genetic algorithm for the
    /// generation of evolving artificial neural networks. It was developed by Ken
    /// Stanley while at The University of Texas at Austin.
    /// 
    /// http://www.cs.ucf.edu/~kstanley/
    /// </summary>
    public class NEATInnovationList : BasicInnovationList
    {
        /// <summary>
        /// The next neuron id.
        /// </summary>
        [EGAttribute]
        private long nextNeuronID = 0;

        /// <summary>
        /// The population.
        /// </summary>
        [EGReference]
        private IPopulation population;

        /// <summary>
        /// Construct an innovation list. 
        /// </summary>
        /// <param name="population">The population.</param>
        /// <param name="links">The links.</param>
        /// <param name="neurons">The neurons.</param>
        public NEATInnovationList(IPopulation population, Chromosome links, Chromosome neurons)
        {

            this.population = population;
            foreach (IGene gene in neurons.Genes)
            {
                NEATNeuronGene neuronGene = (NEATNeuronGene)gene;

                NEATInnovation innovation = new NEATInnovation(neuronGene,
                       population.AssignInnovationID(), AssignNeuronID());

                Innovations.Add(innovation);

            }

            foreach (IGene gene in links.Genes)
            {
                NEATLinkGene linkGene = (NEATLinkGene)gene;
                NEATInnovation innovation = new NEATInnovation(linkGene
                        .FromNeuronID, linkGene.ToNeuronID,
                        NEATInnovationType.NewLink, this.population.AssignInnovationID());
                Innovations.Add(innovation);

            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NEATInnovationList()
        {
        }

        
        /// <summary>
        /// Assign a neuron ID. 
        /// </summary>
        /// <returns>The neuron id.</returns>
        private long AssignNeuronID()
        {
            return nextNeuronID++;
        }

        /// <summary>
        /// Check to see if we already have an innovation. 
        /// </summary>
        /// <param name="input">The input neuron.</param>
        /// <param name="output">The output neuron.</param>
        /// <param name="type">The type.</param>
        /// <returns>The innovation, either new or existing if found.</returns>
        public NEATInnovation CheckInnovation(long input, long output,
                NEATInnovationType type)
        {
            foreach (IInnovation i in Innovations)
            {
                NEATInnovation innovation = (NEATInnovation)i;
                if ((innovation.FromNeuronID == input)
                        && (innovation.ToNeuronID == output)
                        && (innovation.InnovationType == type))
                {
                    return innovation;
                }
            }

            return null;
        }

        /// <summary>
        /// Create a new neuron gene from an id. 
        /// </summary>
        /// <param name="neuronID">The neuron id.</param>
        /// <returns>The neuron gene.</returns>
        public NEATNeuronGene CreateNeuronFromID(long neuronID)
        {
            NEATNeuronGene result = new NEATNeuronGene(NEATNeuronType.Hidden,
                    0, 0, 0);

            foreach (IInnovation i in Innovations)
            {
                NEATInnovation innovation = (NEATInnovation)i;
                if (innovation.NeuronID == neuronID)
                {
                    result.NeuronType = innovation.NeuronType;
                    result.Id = innovation.NeuronID;
                    result.SplitY = innovation.SplitY;
                    result.SplitX = innovation.SplitX;

                    return result;
                }
            }

            return result;
        }

        
        /// <summary>
        /// Create a new innovation. 
        /// </summary>
        /// <param name="input">The input neuron.</param>
        /// <param name="output">The output neuron.</param>
        /// <param name="type">The type.</param>
        public void CreateNewInnovation(long input, long output,
                 NEATInnovationType type)
        {
            NEATInnovation newInnovation = new NEATInnovation(input, output, type,
                    this.population.AssignInnovationID());

            if (type == NEATInnovationType.NewNeuron)
            {
                newInnovation.NeuronID = AssignNeuronID();
            }

            Innovations.Add(newInnovation);
        }

       
        /// <summary>
        /// Create a new innovation. 
        /// </summary>
        /// <param name="from">The from neuron.</param>
        /// <param name="to">The to neuron.</param>
        /// <param name="innovationType">The innovation type.</param>
        /// <param name="neuronType">The neuron type.</param>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns>The new innovation.</returns>
        public long CreateNewInnovation(long from, long to,
                NEATInnovationType innovationType,
                NEATNeuronType neuronType, double x, double y)
        {
            NEATInnovation newInnovation = new NEATInnovation(from, to,
                    innovationType, population.AssignInnovationID(), neuronType, x, y);

            if (innovationType == NEATInnovationType.NewNeuron)
            {
                newInnovation.NeuronID = AssignNeuronID();
            }

            Innovations.Add(newInnovation);

            return (this.nextNeuronID - 1); // ??????? should it be innov?
        }
    }
}
