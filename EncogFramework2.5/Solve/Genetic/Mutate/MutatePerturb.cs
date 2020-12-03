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
using Encog.Solve.Genetic.Genes;
using Encog.MathUtil;
using Encog.Solve.Genetic.Genome;

namespace Encog.Solve.Genetic.Mutate
{
    /// <summary>
    /// A simple mutation based on random numbers.
    /// </summary>
    public class MutatePerturb : IMutate
    {
        /// <summary>
        /// The amount to perturb by.
        /// </summary>
        private double perturbAmount1;
        private double percentageGenesToPerturb1;
        private double percentageUsePerturbAmount1;

        private double perturbAmount2;
        private double percentageGenesToPerturb2;

        /// <summary>
        /// Construct a perturb mutation.
        /// </summary>
        /// <param name="perturbAmount">The amount to mutate by(percent).</param>
        /// <param name="percentageGenesToPerturb">The number of genes within this network to perturb.</param>
        public MutatePerturb(double perturbAmount, double percentageGenesToPerturb =1.0)
        {
            if (percentageGenesToPerturb < 0 || percentageGenesToPerturb > 1)
                throw new Exception("percentageGenesToPerturb must be between 0 and 1.");

            this.perturbAmount1 = perturbAmount;
            this.percentageGenesToPerturb1 = percentageGenesToPerturb;

            this.percentageUsePerturbAmount1 = 1;
        }

        /// <summary>
        /// Construct a perturb mutation with extra options
        /// </summary>
        /// <param name="perturbAmount">The amount to mutate by(percent).</param>
        /// <param name="percentageGenesToPerturb">The number of genes within this network to perturb.</param>
        public MutatePerturb(double perturbAmount1, double percentageGenesToPerturb1, double percentageUsePertubAmount1, double perturbAmount2, double percentageGenesToPerturb2)
        {
            if (percentageGenesToPerturb1 < 0 || percentageGenesToPerturb1 > 1)
                throw new Exception("percentageGenesToPerturb must be between 0 and 1.");

            this.perturbAmount1 = perturbAmount1;
            this.percentageGenesToPerturb1 = percentageGenesToPerturb1;

            this.percentageUsePerturbAmount1 = percentageUsePertubAmount1;
            this.perturbAmount2 = perturbAmount2;
            this.percentageGenesToPerturb2 = percentageGenesToPerturb2;
        }

        /// <summary>
        /// Perform a perturb mutation on the specified chromosome.
        /// </summary>
        /// <param name="chromosome">The chromosome to mutate.</param>
        public void PerformMutation(Chromosome chromosome, GATracker.MatedElement mutatedElement, int childIndex)
        {
            bool usePertubAmount1 = ThreadSafeRandom.NextDouble() <= percentageUsePerturbAmount1;

            foreach (IGene gene in chromosome.Genes)
            {
                if (gene is DoubleGene)
                {
                    if (usePertubAmount1)
                    {
                        if (ThreadSafeRandom.NextDouble() < percentageGenesToPerturb1)
                        {
                            DoubleGene doubleGene = (DoubleGene)gene;
                            double value = doubleGene.Value;
                            double peturbAmount = (perturbAmount1 - (ThreadSafeRandom.NextDouble()) * perturbAmount1 * 2);

                            mutatedElement.AddChildMutation(new GATracker.MutationRecord(perturbAmount1, peturbAmount), childIndex);

                            value += peturbAmount;
                            doubleGene.Value = value;
                        }
                    }
                    else
                    {
                        //if (ThreadSafeRandom.NextDouble() < percentageGenesToPerturb1)
                        //{
                        //    DoubleGene doubleGene = (DoubleGene)gene;
                        //    double value = doubleGene.Value;
                        //    double peturbAmount = (perturbAmount1 - (ThreadSafeRandom.NextDouble()) * perturbAmount1 * 2);

                        //    mutatedElement.AddChildMutation(new GATracker.MutationRecord(perturbAmount1, peturbAmount), childIndex);

                        //    value += peturbAmount;
                        //    doubleGene.Value = value;
                        //}
                        if (ThreadSafeRandom.NextDouble() < percentageGenesToPerturb2)
                        {
                            DoubleGene doubleGene = (DoubleGene)gene;
                            double value = doubleGene.Value;
                            double peturbAmount = (perturbAmount2 - (ThreadSafeRandom.NextDouble()) * perturbAmount2 * 2);

                            mutatedElement.AddChildMutation(new GATracker.MutationRecord(perturbAmount2, peturbAmount), childIndex);

                            value += peturbAmount;
                            doubleGene.Value = value;
                        }
                    }
                }
            }
        }

        #region IMutate Members

        public void PerformMutation(Chromosome chromosome)
        {
            PerformMutation(chromosome, null, 0);
        }

        #endregion
    }
}
