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

namespace Encog.MathUtil
{
    /// <summary>
    /// This class is used to convert strings into numeric values.  If the
    /// string holds a non-numeric value, a zero is returned.
    /// </summary>
    public sealed class Convert
    {

        /// <summary>
        /// Private constructor.
        /// </summary>
        private Convert()
        {
        }

        /// <summary>
        /// Convert a string to a double.  Just make the number a zero
        /// if the string is invalid.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string converted to numeric.</returns>
        public static double String2double(String str)
        {
            double result = 0;
            try
            {
                if (str != null)
                {
                    result = double.Parse(str);
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        /// <summary>
        /// Convert a string to an int.  Just make the number a zero
        /// if the string is invalid.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string converted to numeric.</returns>
        public static int String2int(String str)
        {
            int result = 0;
            try
            {
                if (str != null)
                {
                    result = int.Parse(str);
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }
    }

}
