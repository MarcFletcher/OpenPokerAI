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
using Encog.Util;
using Encog.Engine.Util;

namespace Encog.MathUtil.Matrices.Decomposition
{
    /// <summary>
    /// LU Decomposition.
    ///
    /// For an m-by-n matrix A with m >= n, the LU decomposition is an m-by-n unit
    /// lower triangular matrix L, an n-by-n upper triangular matrix U, and a
    /// permutation vector piv of length m so that A(piv,:) = L*U. If m less than n, then L
    /// is m-by-m and U is m-by-n.
    ///
    /// The LU decompostion with pivoting always exists, even if the matrix is
    /// singular, so the constructor will never fail. The primary use of the LU
    /// decomposition is in the solution of square systems of simultaneous linear
    /// equations. This will fail if isNonsingular() returns false.
    /// 
    /// This file based on a class from the public domain JAMA package.
    /// http://math.nist.gov/javanumerics/jama/
    /// </summary>
    public class LUDecomposition
    {
        /// <summary>
        /// Array for internal storage of decomposition.
        /// </summary>
        private double[][] LU;

        /// <summary>
        /// column dimension.
        /// </summary>
        private int m;

        /// <summary>
        /// row dimension.
        /// </summary>
        private int n; 

        /// <summary>
        /// pivot sign.
        /// </summary>
        private int pivsign;

        /// <summary>
        /// Internal storage of pivot vector.
        /// </summary>
        private int[] piv;

        /// <summary>
        /// LU Decomposition
        /// </summary>
        /// <param name="A">Rectangular matrix</param>
        public LUDecomposition(Matrix A)
        {
            // Use a "left-looking", dot-product, Crout/Doolittle algorithm.

            LU = A.GetArrayCopy();
            m = A.Rows;
            n = A.Cols;
            piv = new int[m];
            for (int i = 0; i < m; i++)
            {
                piv[i] = i;
            }
            pivsign = 1;
            double[] LUrowi;
            double[] LUcolj = new double[m];

            // Outer loop.

            for (int j = 0; j < n; j++)
            {

                // Make a copy of the j-th column to localize references.

                for (int i = 0; i < m; i++)
                {
                    LUcolj[i] = LU[i][j];
                }

                // Apply previous transformations.

                for (int i = 0; i < m; i++)
                {
                    LUrowi = LU[i];

                    // Most of the time is spent in the following dot product.

                    int kmax = Math.Min(i, j);
                    double s = 0.0;
                    for (int k = 0; k < kmax; k++)
                    {
                        s += LUrowi[k] * LUcolj[k];
                    }

                    LUrowi[j] = LUcolj[i] -= s;
                }

                // Find pivot and exchange if necessary.

                int p = j;
                for (int i = j + 1; i < m; i++)
                {
                    if (Math.Abs(LUcolj[i]) > Math.Abs(LUcolj[p]))
                    {
                        p = i;
                    }
                }
                if (p != j)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double t = LU[p][k];
                        LU[p][k] = LU[j][k];
                        LU[j][k] = t;
                    }
                    int temp = piv[p];
                    piv[p] = piv[j];
                    piv[j] = temp;
                    pivsign = -pivsign;
                }

                // Compute multipliers.

                if (j < m & LU[j][j] != 0.0)
                {
                    for (int i = j + 1; i < m; i++)
                    {
                        LU[i][j] /= LU[j][j];
                    }
                }
            }
        }

        
        /// <summary>
        /// Is the matrix nonsingular?
        /// </summary>
        public bool IsNonsingular
        {
            get
            {
                for (int j = 0; j < n; j++)
                {
                    if (LU[j][j] == 0)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Return lower triangular factor
        /// </summary>
        public Matrix L
        {
            get
            {
                Matrix x = new Matrix(m, n);
                double[][] l = x.Data;
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i > j)
                        {
                            l[i][j] = LU[i][j];
                        }
                        else if (i == j)
                        {
                            l[i][j] = 1.0;
                        }
                        else
                        {
                            l[i][j] = 0.0;
                        }
                    }
                }
                return x;
            }
        }

        /**
         * Return upper triangular factor
         * 
         * @return U
         */

        public Matrix U
        {
            get
            {
                Matrix x = new Matrix(n, n);
                double[][] u = x.Data;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i <= j)
                        {
                            u[i][j] = LU[i][j];
                        }
                        else
                        {
                            u[i][j] = 0.0;
                        }
                    }
                }
                return x;
            }
        }

        /// <summary>
        /// Return pivot permutation vector
        /// </summary>
        public int[] Pivot
        {
            get
            {
                int[] p = new int[m];
                for (int i = 0; i < m; i++)
                {
                    p[i] = piv[i];
                }
                return p;
            }
        }

        /// <summary>
        /// Return pivot permutation vector as a one-dimensional double array
        /// </summary>
        public double[] DoublePivot
        {
            get
            {
                double[] vals = new double[m];
                for (int i = 0; i < m; i++)
                {
                    vals[i] = (double)piv[i];
                }
                return vals;
            }
        }

        /// <summary>
        /// Determinant
        /// </summary>
        /// <returns>det(A)</returns>
        public double Det()
        {
            if (m != n)
            {
                throw new MatrixError("Matrix must be square.");
            }
            double d = (double)pivsign;
            for (int j = 0; j < n; j++)
            {
                d *= LU[j][j];
            }
            return d;
        }

        
        /// <summary>
        /// Solve A*X = B
        /// </summary>
        /// <param name="B">A Matrix with as many rows as A and any number of columns.</param>
        /// <returns>so that L*U*X = B(piv,:)</returns>
        public Matrix Solve(Matrix B)
        {
            if (B.Rows != m)
            {
                throw new MatrixError(
                        "Matrix row dimensions must agree.");
            }
            if (!this.IsNonsingular)
            {
                throw new MatrixError("Matrix is singular.");
            }

            // Copy right hand side with pivoting
            int nx = B.Cols;
            Matrix Xmat = B.GetMatrix(piv, 0, nx - 1);
            double[][] X = Xmat.Data;

            // Solve L*Y = B(piv,:)
            for (int k = 0; k < n; k++)
            {
                for (int i = k + 1; i < n; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        X[i][j] -= X[k][j] * LU[i][k];
                    }
                }
            }
            // Solve U*X = Y;
            for (int k = n - 1; k >= 0; k--)
            {
                for (int j = 0; j < nx; j++)
                {
                    X[k][j] /= LU[k][k];
                }
                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        X[i][j] -= X[k][j] * LU[i][k];
                    }
                }
            }
            return Xmat;
        }

        /// <summary>
        /// Solve the matrix for a 1d array.
        /// </summary>
        /// <param name="value">The value to solve for.</param>
        /// <returns>The solved matrix.</returns>
        public double[] Solve(double[] value)
        {
            if (value == null)
            {
                throw new MatrixError("value");
            }

            if (value.Length != this.LU.Length)
            {
                throw new MatrixError("Invalid matrix dimensions.");
            }

            if (!this.IsNonsingular)
            {
                throw new MatrixError("Matrix is singular");
            }

            // Copy right hand side with pivoting
            int count = value.Length;
            double[] b = new double[count];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = value[piv[i]];
            }

            int rows = LU[0].Length;
            int columns = LU[0].Length;
            double[][] lu = LU;


            // Solve L*Y = B
            double[] X = new double[count];
            for (int i = 0; i < rows; i++)
            {
                X[i] = b[i];
                for (int j = 0; j < i; j++)
                {
                    X[i] -= lu[i][j] * X[j];
                }
            }

            // Solve U*X = Y;
            for (int i = rows - 1; i >= 0; i--)
            {
                // double sum = 0.0;
                for (int j = columns - 1; j > i; j--)
                {
                    X[i] -= lu[i][j] * X[j];
                }
                X[i] /= lu[i][i];
            }
            return X;
        }

 
        /// <summary>
        /// Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public double[][] Inverse()
        {
            if (!this.IsNonsingular)
            {
                throw new MatrixError("Matrix is singular");
            }

            int rows = this.LU.Length;
            int columns = LU[0].Length;
            int count = rows;
            double[][] lu = LU;

            double[][] X = EngineArray.AllocateDouble2D(rows, columns);
            for (int i = 0; i < rows; i++)
            {
                int k = this.piv[i];
                X[i][k] = 1.0;
            }

            // Solve L*Y = B(piv,:)
            for (int k = 0; k < columns; k++)
            {
                for (int i = k + 1; i < columns; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            // Solve U*X = Y;
            for (int k = columns - 1; k >= 0; k--)
            {
                for (int j = 0; j < count; j++)
                {
                    X[k][j] /= lu[k][k];
                }

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            return X;
        }

    }
}
