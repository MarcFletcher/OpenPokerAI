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
using Encog.Persist.Attributes;

namespace Encog.Solve.Genetic.Genome
{
    /// <summary>
    /// A basic abstract genome.  Provides base functionality.
    /// </summary>
    public abstract class BasicGenome : IGenome
    {
        /// <summary>
        /// The adjusted score.
        /// </summary>
        [EGAttribute]
        private double adjustedScore;

        /// <summary>
        /// The amount to spawn.
        /// </summary>
        [EGAttribute]
        private double amountToSpawn;

        /// <summary>
        /// The owner.
        /// </summary>
        [EGIgnore]
        private GeneticAlgorithm geneticAlgorithm;
        
        /// <summary>
        /// The genome id.
        /// </summary>
        [EGAttribute]
        private long genomeID;

        /// <summary>
        /// The organism generated by this gene.
        /// </summary>
        [EGIgnore]
        private Object organism;

        /// <summary>
        /// The score of this genome.
        /// </summary>
        [EGAttribute]
        private double score;

        /// <summary>
        /// The adjusted score.
        /// </summary>
        public double AdjustedScore 
        {
            get
            {
                return this.adjustedScore;
            }
            set
            {
                this.adjustedScore = value;
            }
        }

        /// <summary>
        /// The amount to spawn.
        /// </summary>
        public double AmountToSpawn 
        {
            get
            {
                return this.amountToSpawn;
            }
            set
            {
                this.amountToSpawn = value;
            }
        }

        /// <summary>
        /// The chromosomes for this gene.
        /// </summary>
        private IList<Chromosome> chromosomes = new List<Chromosome>();

        /// <summary>
        /// The genetic algorithm for this gene.
        /// </summary>
        public GeneticAlgorithm GA 
        {
            get
            {
                return this.geneticAlgorithm;
            }
            set
            {
                this.geneticAlgorithm = value;
            }
        }

        /// <summary>
        /// The genome id.
        /// </summary>
        public long GenomeID 
        {
            get
            {
                return this.genomeID;
            }
            set
            {
                this.genomeID = value;
            }
        }

        /// <summary>
        /// The organism generated by this gene.
        /// </summary>
        public Object Organism 
        {
            get
            {
                return this.organism;
            }
            set
            {
                this.organism = value;
            }
        }

        /// <summary>
        /// The score of this genome.
        /// </summary>
        public double Score 
        {
            get
            {
                return this.score;
            }
            set
            {
                this.score = value;
            }
        }

        /// <summary>
        /// Construct a basic genome.
        /// </summary>
        /// <param name="geneticAlgorithm">The GA this genome belongs to.</param>
        public BasicGenome(GeneticAlgorithm geneticAlgorithm)
        {
            this.GA = geneticAlgorithm;
        }

        /// <summary>
        /// The number of genes in this genome.
        /// </summary>
        /// <returns>The number of genes in this genome.</returns>
        public int CalculateGeneCount()
        {
            int result = 0;

            // sum the genes in the chromosomes.
            foreach (Chromosome chromosome in chromosomes)
            {
                result += chromosome.Genes.Count;
            }
            return result;
        }

        
        /// <summary>
        /// Used to compare two chromosomes. Used to sort by score.
        /// </summary>
        /// <param name="other">The other chromosome to compare.</param>
        /// <returns>The value 0 if the argument is a chromosome that has an equal
        /// score to this chromosome; a value less than 0 if the argument is
        /// a chromosome with a score greater than this chromosome; and a
        /// value greater than 0 if the argument is a chromosome what a score
        /// less than this chromosome.</returns>
        public int CompareTo(IGenome other)
        {

            if (GA.CalculateScore.ShouldMinimize)
            {
                if (Math.Abs(Score - other.Score) < EncogFramework.DEFAULT_DOUBLE_EQUAL)
                {
                    return 0;
                }
                else if (Score > other.Score)
                {
                    return 1;
                }
                return -1;
            }
            else
            {
                if (Math.Abs(Score - other.Score) < EncogFramework.DEFAULT_DOUBLE_EQUAL)
                {
                    return 0;
                }
                else if (Score > other.Score)
                {
                    return -1;
                }
                return 1;

            }
        }


        /// <summary>
        /// The number of chromosomes.
        /// </summary>
        public IList<Chromosome> Chromosomes
        {
            get
            {
                return chromosomes;
            }
        }

        /// <summary>
        /// Mate two genomes. Will loop over all chromosomes.
        /// </summary>
        /// <param name="father">The father.</param>
        /// <param name="child1">The first child.</param>
        /// <param name="child2">The second child.</param>
        public void Mate(IGenome father, IGenome child1, IGenome child2, GATracker.MatedElement matedElement)
        {
            int motherChromosomes = Chromosomes.Count;
            int fatherChromosomes = father.Chromosomes.Count;

            if (motherChromosomes != fatherChromosomes)
            {
                throw new GeneticError(
                        "Mother and father must have same chromosome count, Mother:"
                                + motherChromosomes + ",Father:"
                                + fatherChromosomes);
            }

            for (int i = 0; i < fatherChromosomes; i++)
            {
                Chromosome motherChromosome = chromosomes[i];
                Chromosome fatherChromosome = father.Chromosomes[i];
                Chromosome offspring1Chromosome = child1.Chromosomes[i];
                Chromosome offspring2Chromosome = child2.Chromosomes[i];

                //This splices the chromosomes together, using a random number to decide splice points
                GA.Crossover.Mate(motherChromosome, fatherChromosome, offspring1Chromosome, offspring2Chromosome);

                //Set the gene number
                if (matedElement != null)
                {
                    matedElement.SetNumChildGenes(offspring1Chromosome.Genes.Count, 1);
                    matedElement.SetNumChildGenes(offspring2Chromosome.Genes.Count, 2);
                }

                //Now we potentially mutate the children
                if (ThreadSafeRandom.NextDouble() < GA.MutationPercent)
                {
                    if (matedElement != null) matedElement.Child1Mutated = true;
                    GA.Mutate.PerformMutation(offspring1Chromosome, matedElement, 1);
                }

                //Now we potentially mutate the children
                if (ThreadSafeRandom.NextDouble() < GA.MutationPercent)
                {
                    if (matedElement != null) matedElement.Child2Mutated = true;
                    GA.Mutate.PerformMutation(offspring2Chromosome, matedElement, 2);
                }
            }

            child1.Decode();
            child2.Decode();
            GA.PerformScoreCalculation(child1);
            GA.PerformScoreCalculation(child2);
        }

        /// <summary>
        /// Mate two genomes. Will loop over all chromosomes.
        /// </summary>
        /// <param name="father">The father.</param>
        /// <param name="child1">The first child.</param>
        /// <param name="child2">The second child.</param>
        public void Mate(IGenome father, IGenome child1, IGenome child2)
        {
            Mate(father, child1, child2, null);
        }

        /// <summary>
        /// Convert the chromosome to a string.
        /// </summary>
        /// <returns>The chromosome as a string.</returns>
        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[BasicGenome: score=");
            builder.Append(Score);
            return builder.ToString();
        }

        /// <summary>
        /// Use the genes to update the organism.
        /// </summary>
        public abstract void Decode();

        /// <summary>
        /// Use the organism to update the genes.
        /// </summary>
        public abstract void Encode();
    }
}
