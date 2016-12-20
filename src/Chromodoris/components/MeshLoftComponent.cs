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

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace Chromodoris.Components
{
    public class MeshLoftComponent : GH_Component
    {
        public MeshLoftComponent()
          : base("Mesh Loft", "MLoft",
              "Lofts polylines together to create welded meshes, minimizing overhead.",
              "Chromodoris", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddCurveParameter("Polylines", "P", "The polylines to loft. They must have the same number of points.", GH_ParamAccess.list);
            pManager[pManager.AddBooleanParameter("Cap", "C", "Cap the loft.", GH_ParamAccess.item, false)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The mesh loft.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Curve> crvs = new List<Curve>();
            List<Polyline> pls = new List<Polyline>();
            bool cap = false;

            if (!DA.GetDataList("Polylines", crvs)) return;
            DA.GetData("Cap", ref cap);

            int count = -1;
            foreach (var c in crvs)
            {
                Polyline p;
                if (c.TryGetPolyline(out p)) {
                    if (count == -1)
                    {
                        count = p.Count;
                    }
                    else
                    {
                        if (p.Count != count)
                        {
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Control point count must be the same between polylines.");
                            return;
                        }
                    }

                    pls.Add(p);
                } else
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must be polylines.");
                    return;
                }
            }
            
            DA.SetData("Mesh", new GH_Mesh(GetLofted(pls, cap)));
        }

        public Mesh GetLofted(List<Polyline> polylines, bool cap)
        {

            Mesh mesh = new Mesh();
            bool closed = polylines[0].IsClosed;
            int count = polylines[0].Count;

            int step = count;
            if (closed) step--;


            if (polylines.Count > 0)
            {
                int[] lastVerts = new int[step];
                for (int p = 0; p < step; p++)
                {
                    lastVerts[p] = mesh.Vertices.Add(polylines[0][p]);
                }

                for (int i = 1; i < polylines.Count; i++)
                {
                    int[] newVerts = new int[step];
                    for (int p = 0; p < step; p++)
                    {
                        newVerts[p] = mesh.Vertices.Add(polylines[i][p]);
                    }

                    for (int v = 1; v <= step; v++)
                    {
                        if (v == step)
                        {
                            if (closed) mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[0], newVerts[0], newVerts[v - 1]);
                        }
                        else
                        {
                            mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[v], newVerts[v], newVerts[v - 1]);

                        }
                    }
                    lastVerts = newVerts;
                }

                if (cap && closed)
                {
                    mesh.Append(Mesh.CreateFromClosedPolyline(polylines[0]));
                    mesh.Append(Mesh.CreateFromClosedPolyline(polylines[polylines.Count - 1]));
                }
            }

            return mesh;
        }
            
            
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Chromodoris.Properties.Resources.Icon_MeshLoft;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{21BA7483-E9E2-4FDD-8CC9-FF4B19D55423}"); }
        }
    }
}