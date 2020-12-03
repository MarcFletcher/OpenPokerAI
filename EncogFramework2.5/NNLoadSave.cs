// Encog(tm) Artificial Intelligence Framework v2.3
// .Net Version
// http://www.heatonresearch.com/encog/
// http://code.google.com/p/encog-java/
// 
// Contributed to Encog By M.Fletcher
// University of Cambridge, Dept. of Physics, UK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.Persist;
using Encog.Neural.Networks;
using System.IO;
using Encog.Persist.Location;

namespace Encog
{
    public static class NNLoadSave
    {
        #region NetworkLoadSave

        public static BasicNetwork loadNetwork(byte[] networkBytes)
        {
            FilePersistence location = new FilePersistence();
            location.AddStreamData(networkBytes);
            EncogPersistedCollection collection = new EncogPersistedCollection(location, FileMode.Open);
            BasicNetwork returnNetwork = (BasicNetwork)collection.Find("neuralNet");

            //Make sure we have the flattended network cached.
            returnNetwork.Structure.FinalizeStructure();

            return returnNetwork;
        }

        public static BasicNetwork loadNetwork(string filename, string filePath)
        {
            if (filename.Substring(filename.Length - 4, 4) != ".eNN")
                throw new Exception("Incorrect extension in filename");

            EncogPersistedCollection collection = new EncogPersistedCollection(Path.Combine(filePath, filename), FileMode.Open);
            BasicNetwork returnNetwork = (BasicNetwork)collection.Find("neuralNet");

            //Make sure we have the flattended network cached.
            returnNetwork.Structure.FinalizeStructure();

            return returnNetwork;
        }

        public static void saveNetwork(BasicNetwork network, string filename, string filePath)
        {
            if (filename.Substring(filename.Length - 4, 4) != ".eNN")
                throw new Exception("Incorrect extension in filename");

            EncogPersistedCollection collection = new EncogPersistedCollection(Path.Combine(filePath, filename), FileMode.Create);

            if (filename.Contains("\\"))
            {
                int indexOfSlash = filename.IndexOf('\\');
                collection.Add("neuralNet", network);
            }
            else
                collection.Add("neuralNet", network);
        }

        #endregion NetworkLoadSave
    }
}
