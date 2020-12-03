﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Encog.Neural.Networks.Layers
{
    /// <summary>
    /// The implemented function types of the RBFs
    /// </summary>
    public enum RBFEnum
    {
        /// <summary>
        /// Regular Gaussian function.
        /// </summary>
        Gaussian,
        
        /// <summary>
        /// Multi quadric function.
        /// </summary>
        Multiquadric,
        
        /// <summary>
        /// Inverse multi quadric function.
        /// </summary>
        InverseMultiquadric,

        /// <summary>
        /// The Mexican hat function.
        /// </summary>
        MexicanHat
        
    }
}
