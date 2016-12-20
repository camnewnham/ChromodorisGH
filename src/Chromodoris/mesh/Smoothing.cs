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

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using System.Text;

namespace Chromodoris.MeshTools
{
    public class VertexSmooth
    {
        int iterations;
        double step;
        Rhino.Geometry.Mesh mesh;

        private List<int[]> neighbourVerts;
        private Point3f[] topoVertLocations;
        private List<int[]> topoVertexIndices;

        public VertexSmooth(Rhino.Geometry.Mesh _mesh, double _step, int _iterations)
        {
            iterations = _iterations;
            step = _step;
            mesh = _mesh;
            neighbourVerts = new List<int[]>();
            topoVertLocations = new Point3f[mesh.TopologyVertices.Count];
            topoVertexIndices = new List<int[]>();
        }

        public Mesh compute()
        {
            {
                for (int i = 0; i < mesh.TopologyVertices.Count; i++)
                {
                    int[] mvInds = mesh.TopologyVertices.MeshVertexIndices(i);
                    topoVertLocations[i] = mesh.Vertices[mvInds[0]];
                    topoVertexIndices.Add(mvInds);
                    neighbourVerts.Add(mesh.TopologyVertices.ConnectedTopologyVertices(i));
                }

                for (int i = 0; i < iterations; i++)
                {
                    smoothMultiThread();
                }

                Point3f[] mVerts = new Point3f[mesh.Vertices.Count];

                for (int i = 0; i < topoVertLocations.Length; i++)
                {
                    Point3f loc = topoVertLocations[i];
                    foreach (int vInd in topoVertexIndices[i])
                    {
                        mVerts[vInd] = loc;
                    }
                }

                Rhino.Geometry.Mesh newMesh = new Rhino.Geometry.Mesh();
                newMesh.Vertices.AddVertices(mVerts);
                newMesh.Faces.AddFaces(mesh.Faces);
                return newMesh;
            }
        }

        public void smoothSingleThread()
        {
            for (int v = 0; v < mesh.TopologyVertices.Count; v++)
            {
                smoothTopoIndex(v);
            }
        }

        public void smoothMultiThread()
        {
            var pLel = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.ForEach(Enumerable.Range(0, mesh.TopologyVertices.Count), pLel, v => smoothTopoIndex(v));
        }

        public void smoothTopoIndex(int v)
        {
            Point3d loc = topoVertLocations[v];
            int[] nvs = neighbourVerts[v];
            if (nvs.Length > 0)
            {
                Point3d avg = new Point3d();
                foreach (int nv in nvs)
                {
                    avg += topoVertLocations[nv];
                }
                avg = avg / (double)nvs.Length;
                Vector3d pos = new Vector3d(loc) + (avg - loc) * step;
                topoVertLocations[v] = new Point3f((float)pos.X, (float)pos.Y, (float)pos.Z);
            }
        }
    }
}
