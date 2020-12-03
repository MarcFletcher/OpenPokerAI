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
using Encog.Parse.Tags.Read;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Parse.Tags.Write;
using Encog.MathUtil;
using Encog.Util.CSV;
using Encog.Engine.Network.Activation;

#if logging
using log4net;
#endif

namespace Encog.Persist.Persistors
{
    /// <summary>
    /// The Encog persistor used to persist the ContextLayer class.
    /// </summary>
    public class ContextLayerPersistor : IPersistor
    {
        public const String PROPERTY_CONTEXT = "context";

#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(BasicNetwork));
#endif

        /// <summary>
        /// Load the specified Encog object from an XML reader.
        /// </summary>
        /// <param name="xmlIn">The XML reader to use.</param>
        /// <returns>The loaded object.</returns>
        public IEncogPersistedObject Load(ReadXML xmlIn)
        {
            int neuronCount = 0;
            int x = 0;
            int y = 0;
            double biasActivation = 1;
            String threshold = null;
            IActivationFunction activation = null;
            String end = xmlIn.LastTag.Name;
            String context = null;

            while (xmlIn.ReadToTag())
            {
                if (xmlIn.IsIt(BasicLayerPersistor.TAG_ACTIVATION, true))
                {
                    xmlIn.ReadToTag();
                    String type = xmlIn.LastTag.Name;
                    activation = BasicLayerPersistor.LoadActivation(type, xmlIn);
                }
                else if (xmlIn.IsIt(BasicLayerPersistor.PROPERTY_NEURONS, true))
                {
                    neuronCount = xmlIn.ReadIntToTag();
                }
                else if (xmlIn.IsIt(BasicLayerPersistor.PROPERTY_X, true))
                {
                    x = xmlIn.ReadIntToTag();
                }
                else if (xmlIn.IsIt(BasicLayerPersistor.PROPERTY_Y, true))
                {
                    y = xmlIn.ReadIntToTag();
                }
                else if (xmlIn.IsIt(BasicLayerPersistor.PROPERTY_THRESHOLD, true))
                {
                    threshold = xmlIn.ReadTextToTag();
                }
                else if (xmlIn.IsIt(PROPERTY_CONTEXT, true))
                {
                    context = xmlIn.ReadTextToTag();
                }
                else if (xmlIn.IsIt(BasicLayerPersistor.PROPERTY_BIAS_ACTIVATION, true))
                {
                    biasActivation = double.Parse(xmlIn.ReadTextToTag());
                }
                else if (xmlIn.IsIt(end, false))
                {
                    break;
                }
            }

            if (neuronCount > 0)
            {
                ContextLayer layer;

                if (threshold == null)
                {
                    layer = new ContextLayer(activation, false, neuronCount);
                }
                else
                {
                    double[] t = NumberList.FromList(CSVFormat.EG_FORMAT, threshold);
                    layer = new ContextLayer(activation, true, neuronCount);
                    for (int i = 0; i < t.Length; i++)
                    {
                        layer.BiasWeights[i] = t[i];
                    }
                }

                if (context != null)
                {
                    double[] c = NumberList.FromList(CSVFormat.EG_FORMAT, context);

                    for (int i = 0; i < c.Length; i++)
                    {
                        layer.Context[i] = c[i];
                    }
                }

                layer.X = x;
                layer.Y = y;
                layer.BiasActivation = biasActivation;

                return layer;
            }
            return null;
        }

        /// <summary>
        /// Save the specified Encog object to an XML writer.
        /// </summary>
        /// <param name="obj">The object to save.</param>
        /// <param name="xmlOut">The XML writer to save to.</param>
        public void Save(IEncogPersistedObject obj, WriteXML xmlOut)
        {
            PersistorUtil.BeginEncogObject(
                    EncogPersistedCollection.TYPE_CONTEXT_LAYER, xmlOut, obj, false);
            ContextLayer layer = (ContextLayer)obj;

            xmlOut.AddProperty(BasicLayerPersistor.PROPERTY_NEURONS, layer
                    .NeuronCount);
            xmlOut.AddProperty(BasicLayerPersistor.PROPERTY_X, layer.X);
            xmlOut.AddProperty(BasicLayerPersistor.PROPERTY_Y, layer.Y);

            if (layer.HasBias)
            {
                StringBuilder result = new StringBuilder();
                NumberList.ToList(CSVFormat.EG_FORMAT, result, layer.BiasWeights);
                xmlOut.AddProperty(BasicLayerPersistor.PROPERTY_THRESHOLD, result
                        .ToString());
            }

            StringBuilder ctx = new StringBuilder();
            NumberList.ToList(CSVFormat.EG_FORMAT, ctx, layer.Context.Data);
            xmlOut.AddProperty(PROPERTY_CONTEXT, ctx.ToString());


            xmlOut.AddProperty(BasicLayerPersistor.PROPERTY_BIAS_ACTIVATION, layer.BiasActivation );
            BasicLayerPersistor.SaveActivationFunction(layer.ActivationFunction, xmlOut);

            xmlOut.EndTag();
        }
    }
}
