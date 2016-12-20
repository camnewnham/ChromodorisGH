/*
 *      ___  _  _  ____   __   _  _   __  ____   __  ____  __  ____ 
 *     / __)/ )( \(  _ \ /  \ ( \/ ) /  \(    \ /  \(  _ \(  )/ ___)
 *    ( (__ ) __ ( )   /(  O )/ \/ \(  O )) D ((  O ))   / )( \___ \
 *     \___)\_)(_/(__\_) \__/ \_)(_/ \__/(____/ \__/(__\_)(__)(____/
 *
 *    Copyright Cameron Newnham 2015-2016
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU General Public License as published by
 *    the Free Software Foundation, either version 3 of the License, or
 *    (at your option) any later version.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *    GNU General Public License for more details.
 *
 *    You should have received a copy of the GNU General Public License
 *    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromodoris.MeshTools
{
    public class CurvatureApproximation
    {
        public Mesh mesh;
        public double[] minRad;
        public double[] maxRad;
        public double[] mean;
        public double[] gaussian;
        public Vector3d[] minDirs;
        public Vector3d[] maxDirs;

        public CurvatureApproximation(Mesh mesh)
        {
            this.mesh = mesh;
            
            int c = mesh.Vertices.Count;
            minRad = new double[c];
            maxRad = new double[c];
            mean = new double[c];
            gaussian = new double[c];
            minDirs = new Vector3d[c];
            maxDirs = new Vector3d[c];
            Compute();
        }


        public void Compute()
        {
            if (mesh.Normals.Count == 0) mesh.Normals.ComputeNormals();
            double epsilon = 0.0001;

            var topoRef = new Dictionary<int, int>();
            for (int i = 0; i < mesh.TopologyVertices.Count; i++)
            {
                topoRef.Add(i, mesh.TopologyVertices.MeshVertexIndices(i)[0]);
            }

            for (int i = 0; i < mesh.TopologyVertices.Count; i++)
            {
                var conn = mesh.TopologyVertices.ConnectedTopologyVertices(i);

                int mInd = topoRef[i];
                var myVert = mesh.Vertices[mInd];
                var myNorm = mesh.Normals[mInd];

                double minC = 100000;
                double maxC = -100000;
                Vector3d minDir = Vector3d.Unset;
                Vector3d maxDir = Vector3d.Unset;


                for (int j = 0; j < conn.Length; j++)
                {
                    int nInd = topoRef[conn[j]];

                    var otherVert = mesh.Vertices[nInd];

                    Vector3f vec = myVert - otherVert;
                    var len = vec.Length;
                    vec.Unitize();

                    double angle = Vector3d.VectorAngle(vec, myNorm) - Math.PI * 0.5;

                    if (len > 0)
                    {
                        if (!Rhino.RhinoMath.IsValidDouble(angle))
                        {
                            angle = 0;
                        }

                        double curveInv = 0;
                        if (angle < epsilon && angle > -epsilon)
                        {
                        }
                        else
                        {

                            curveInv = Math.Max(1 / (len / Math.Sin(angle)), -1);
                        }

                        if (curveInv < minC)
                        {
                            minC = curveInv;
                            minDir = new Vector3d(vec);
                        }
                        if (curveInv > maxC)
                        {
                            maxC = curveInv;
                            maxDir = new Vector3d(vec);
                        }
                    }
                }

                foreach (int mvInd in mesh.TopologyVertices.MeshVertexIndices(i))
                {
                    minRad[mvInd] = maxC;
                    maxRad[mvInd] = minC;
                    mean[mvInd] = (minC + maxC) * 0.5;
                    gaussian[mvInd] = minC * maxC;
                    minDirs[mvInd] = minDir;
                    maxDirs[mvInd] = maxDir;
                }
            }
        }
    }
}
